# SPRINT 3.3.1: Markasal İskonto (Brand-Based Discounts)

**Tarih:** 5 Şubat 2025  
**Durum:** ✅ TAMAMLANDI  
**Test Sonuçları:** 15/15 geçti (2 deterministik çalıştırma)

---

## 📋 Özet

SPRINT 3.3'teki fiyatlandırma motoruna **marka bazlı iskonto kuralları** eklendi. Şimdi sistem şu hiyerarşik çözümlemeyi destekliyor:

### Öncelik Sırası (9 Seviye)
1. **CUSTOMER + Variant** (en yüksek öncelik)
2. **CUSTOMER + Brand** ⭐ YENİ
3. **CUSTOMER + All** (varyant/marka yok)
4. **CUSTOMER_GROUP + Variant**
5. **CUSTOMER_GROUP + Brand** ⭐ YENİ
6. **CUSTOMER_GROUP + All**
7. **PRODUCT_GROUP + Variant**
8. **PRODUCT_GROUP + Brand** ⭐ YENİ
9. **PRODUCT_GROUP + All** (en düşük öncelik)

---

## 🎯 İş Gereksinimleri

### Senaryo
> "Müşteri X'e tüm Bosch ürünleri için %20 iskonto yapıyoruz. Ama bazı Bosch parçalar için özel fiyat listesi var. Sistem önce özel fiyatı uygulamalı, yoksa marka iskontosunu kullanmalı."

### Çözüm
- `Product` entity'sine **Brand** alanı eklendi (örn: "Bosch", "NGK", "Mobil")
- `PriceRule` entity'sine **BrandId** alanı eklendi (VariantId ile birbirini dışlar)
- **9 seviyeli hiyerarşik çözümleme** implementasyonu
- Tezgah ekranında "**Marka iskontosu (Bosch): %20.00**" gösterimi

---

## 🔧 Teknik Değişiklikler

### 1️⃣ Entity Güncellemeleri

#### Product.cs
```csharp
public class Product
{
    // ...existing fields...
    
    /// <summary>
    /// Product brand (e.g., "Bosch", "NGK", "Mobil")
    /// Used for brand-based discount rules
    /// </summary>
    public string? Brand { get; set; }
}
```

#### PriceRule.cs
```csharp
public class PriceRule
{
    // ...existing fields...
    
    /// <summary>
    /// Optional brand name filter
    /// Mutually exclusive with VariantId
    /// If set, applies to all products with this brand
    /// </summary>
    public string? BrandId { get; set; }
}
```

### 2️⃣ Database Şeması

**Migration:** `20260205223232_AddBrandBasedDiscounts`

**Eklenen Kolonlar:**
- `products.Brand` (varchar(100), nullable)
- `price_rules.BrandId` (varchar(100), nullable)

**Performans İndeksleri:**
```sql
-- Product brand sorguları için
CREATE INDEX ix_products_tenant_brand 
ON products (TenantId, Brand);

-- Price rule çözümleme için (8 alan)
CREATE INDEX ix_price_rules_tenant_brand_lookup 
ON price_rules (
    TenantId, Scope, TargetId, BrandId, 
    Currency, ValidFrom, ValidTo, Priority
);
```

### 3️⃣ Servis Mantığı

#### PricingService.FindApplicablePriceRuleAsync

**Önceki Versiyon:** ~25 satır, basit scope filtreleme  
**Yeni Versiyon:** ~134 satır, hiyerarşik çözümleme

**Algoritma:**
1. **Tek sorguda tüm geçerli kuralları getir**
   - Tenant + Scope + TargetId filtresi
   - (VariantId == null VEYA VariantId == requestedVariant)
   - (BrandId == null VEYA BrandId == productBrand)
   - Currency eşleşmesi
   - ValidFrom/ValidTo kontrolü

2. **Bellekte 9 seviyeli öncelik kontrolü**
   ```csharp
   // Level 1: CUSTOMER + Variant
   var rule = allRules
       .Where(r => r.Scope == "CUSTOMER" && 
                   r.VariantId == variantId)
       .OrderByDescending(r => r.Priority)
       .ThenByDescending(r => r.CreatedAt)
       .FirstOrDefault();
   if (rule != null) return rule;
   
   // Level 2: CUSTOMER + Brand
   if (!string.IsNullOrEmpty(brandName))
   {
       rule = allRules
           .Where(r => r.Scope == "CUSTOMER" && 
                       r.BrandId == brandName)
           .OrderByDescending(r => r.Priority)
           .ThenByDescending(r => r.CreatedAt)
           .FirstOrDefault();
       if (rule != null) return rule;
   }
   
   // ... levels 3-9 similar pattern
   ```

#### Frontend Entegrasyonu

`CalculateAsync` metodunda:
```csharp
if (!string.IsNullOrEmpty(appliedRule.BrandId))
{
    ruleDescription = $"Marka iskontosu ({appliedRule.BrandId}): %{discountPercent:F2}";
}
else
{
    ruleDescription = $"İndirim: %{discountPercent:F2}";
}
```

---

## ✅ Test Kapsamı

### Yeni Testler (7 Adet)

1. **CustomerBrandDiscount_Applies**
   - Müşteri için Bosch %20 iskonto
   - Sonuç: 100 TL → 80 TL
   - Açıklama: "Marka iskontosu (Bosch): %20.00"

2. **CustomerVariant_Overrides_CustomerBrand**
   - Aynı müşteri için: Variant %15, Brand %20
   - Sonuç: Variant kazanır → 85 TL
   - Açıklama: Marka iskontosu içermiyor

3. **GroupBrand_Applies_WhenNoCustomerRule**
   - Müşteri kuralı yok, grup için NGK %10
   - Sonuç: 100 TL → 90 TL
   - Açıklama: "Marka iskontosu (NGK): %10.00"

4. **ProductGroupBrand_Fallback**
   - En düşük öncelik (ürün grubu), Mobil %5
   - Sonuç: 100 TL → 95 TL
   - Açıklama: "Marka iskontosu (Mobil): %5.00"

5. **BrandDiscount_DateValidity_Respected**
   - Süresi geçmiş Castrol kuralı (ValidTo = yesterday)
   - Sonuç: Kural uygulanmaz, liste fiyatı kullanılır

6. **BrandDiscount_CurrencyFiltering_Respected**
   - USD kuralı var, TRY talep ediliyor
   - Sonuç: Kural uygulanmaz (para birimi uyumsuz)

7. **BrandDiscount_NoBrand_NoMatch**
   - Ürünün Brand'i null, "Total" için kural var
   - Sonuç: Kural uygulanmaz

### Test Sonuçları

**1. Çalıştırma:**
```
Test Çalıştırması Başarılı.
Toplam test sayısı: 15
     Geçti: 15
 Toplam süre: 10,0733 Saniye
```

**2. Çalıştırma (Deterministik Doğrulama):**
```
Test Çalıştırması Başarılı.
Toplam test sayısı: 15
     Geçti: 15
 Toplam süre: 7,9548 Saniye
```

✅ **Deterministik:** Her iki çalıştırmada da aynı sonuçlar

---

## 📊 Performans

### İndeks Stratejisi
- `ix_products_tenant_brand`: Brand sorguları için (TenantId + Brand)
- `ix_price_rules_tenant_brand_lookup`: **8 alanlı kompozit indeks**
  - TenantId, Scope, TargetId, BrandId
  - Currency, ValidFrom, ValidTo, Priority
  - Tek sorguda tüm filtreleme

### Sorgu Optimizasyonu
- Eski: 3 ayrı sorgu (Customer → Group → Product)
- Yeni: **1 sorgu**, bellekte hiyerarşik çözümleme
- Beklenen kazanç: ~60% DB round-trip azalması

---

## 📝 Kullanım Örnekleri

### Örnek 1: Tüm Bosch Ürünlerine İskonto
```csharp
var rule = new PriceRule
{
    Scope = "CUSTOMER",
    TargetId = customerId, // Specific customer
    VariantId = null,      // Applies to all variants
    BrandId = "Bosch",     // Only Bosch products
    RuleType = "DISCOUNT_PERCENT",
    Value = 20.00m,
    Currency = "TRY",
    ValidFrom = DateTime.UtcNow,
    ValidTo = DateTime.UtcNow.AddMonths(6),
    IsActive = true
};
```

### Örnek 2: Müşteri Grubu için NGK İskontosu
```csharp
var groupRule = new PriceRule
{
    Scope = "CUSTOMER_GROUP",
    TargetId = groupId,    // Customer group (e.g., VIP)
    BrandId = "NGK",       // NGK brand
    RuleType = "DISCOUNT_PERCENT",
    Value = 15.00m,
    Currency = "TRY",
    // ...
};
```

### Örnek 3: Varyant Önceliği
```csharp
// Senaryo: Aynı müşteri için iki kural
// 1. Brand rule: Bosch → %20 (applies to 100 products)
// 2. Variant rule: Specific spark plug → %15 (applies to 1 product)

// Sonuç: Spark plug için %15 uygulanır (daha spesifik)
//        Diğer 99 Bosch ürünü için %20 uygulanır
```

---

## 🎨 Tezgah Görünümü

### Marka İskontosu Aktif
```
┌─────────────────────────────────────────────┐
│ Ürün: Bosch Buji FR7DC                      │
│ Liste Fiyatı: 100.00 TL                     │
│ Marka iskontosu (Bosch): %20.00             │ ⬅️ YENİ
│ Net Fiyat: 80.00 TL                         │
│ Miktar: 2                                   │
│ Toplam: 160.00 TL                           │
└─────────────────────────────────────────────┘
```

### Varyant Özel Fiyat
```
┌─────────────────────────────────────────────┐
│ Ürün: Bosch Buji FR7DC                      │
│ Liste Fiyatı: 100.00 TL                     │
│ İndirim: %15.00                             │ ⬅️ Marka değil
│ Net Fiyat: 85.00 TL                         │
│ Miktar: 1                                   │
│ Toplam: 85.00 TL                            │
└─────────────────────────────────────────────┘
```

---

## 🚀 Deployment

### 1. Migration Uygula
```bash
cd src/Api
dotnet ef database update --context ErpDbContext
```

### 2. Veritabanı Doğrulama
```sql
-- Kolonların eklendiğini kontrol et
SELECT column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_name IN ('products', 'price_rules')
  AND column_name IN ('Brand', 'BrandId');

-- İndeksleri kontrol et
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename IN ('products', 'price_rules')
  AND indexname LIKE '%brand%';
```

### 3. Mevcut Verileri Güncelle (Opsiyonel)
```sql
-- Eğer Product.Name'de marka bilgisi varsa parse et
-- Örnek: "Bosch FR7DC Buji" → Brand = "Bosch"
UPDATE products
SET "Brand" = SPLIT_PART("Name", ' ', 1)
WHERE "Brand" IS NULL
  AND "Name" ~ '^(Bosch|NGK|Mobil|Castrol|Shell|Total)';
```

---

## 🔍 Hata Ayıklama

### Yaygın Sorunlar

**1. Marka iskontosu uygulanmıyor**
- Ürün Brand alanı null mu kontrol et
- PriceRule.BrandId doğru yazılmış mı (case-sensitive)
- Currency eşleşiyor mu
- ValidFrom/ValidTo aralığında mıyız

**2. Variant kuralı yerine brand kuralı uygulanıyor**
- PriceRule.VariantId doğru set edilmiş mi kontrol et
- Scope CUSTOMER mı (en yüksek öncelik)
- Priority değerleri karşılaştır

**3. Performans yavaş**
- `ix_price_rules_tenant_brand_lookup` indeksi var mı kontrol et
```sql
EXPLAIN ANALYZE
SELECT * FROM price_rules
WHERE "TenantId" = '...'
  AND ("BrandId" IS NULL OR "BrandId" = 'Bosch')
  AND "Currency" = 'TRY';
```

---

## 📚 İlgili Dosyalar

### Değiştirilen Dosyalar
- [src/Api/Entities/Product.cs](../../src/Api/Entities/Product.cs) - Brand alanı eklendi
- [src/Api/Entities/PriceRule.cs](../../src/Api/Entities/PriceRule.cs) - BrandId alanı eklendi
- [src/Api/Data/ErpDbContext.cs](../../src/Api/Data/ErpDbContext.cs) - Konfigürasyon ve indeksler
- [src/Api/Services/PricingService.cs](../../src/Api/Services/PricingService.cs) - Hiyerarşik çözümleme
- [tests/ErpCloud.Api.Tests/PricingModuleTests.cs](../../tests/ErpCloud.Api.Tests/PricingModuleTests.cs) - 7 yeni test

### Migration
- [src/Api/Data/Migrations/20260205223232_AddBrandBasedDiscounts.cs](../../src/Api/Data/Migrations/20260205223232_AddBrandBasedDiscounts.cs)

---

## 🎓 Lessons Learned

### Başarılar
✅ Single-query approach ile performans artışı  
✅ Explicit priority levels ile debugging kolaylaştı  
✅ 7 test ile edge case'ler kapsamlı test edildi  
✅ Migration index'leri otomatik oluşturdu

### İyileştirme Fırsatları
- Brand entity'si (normalized) düşünülebilir (şimdilik string yeterli)
- Bulk brand update scripti eklenebilir
- Admin UI'da brand auto-complete önerisi

---

## ✅ Kabul Kriterleri

- [x] BrandId field added to PriceRule entity
- [x] Brand field added to Product entity
- [x] Database indexes created (performance optimized)
- [x] Hierarchical resolution logic implemented (9 levels)
- [x] Frontend shows "Marka iskontosu (Brand): %X.XX"
- [x] 7 brand discount tests added
- [x] All tests pass deterministically (2 runs)
- [x] Migration generated successfully

---

**Sonraki Adımlar:**
- [ ] Admin UI'da brand management eklenmesi (SPRINT 3.4)
- [ ] Bulk import scriptleri (mevcut Product.Name → Brand parse)
- [ ] Raporlama: Marka bazlı iskonto analizi
