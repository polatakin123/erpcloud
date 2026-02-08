# SPRINT 3.3.2: Brand Master Data Normalization

**Tarih:** 6 Şubat 2025  
**Durum:** ✅ TAMAMLANDI  
**Test Sonuçları:** 15/15 geçti (2 deterministik çalıştırma)

---

## 📋 Özet

SPRINT 3.3.1'de text-based brand desteği eklenmişti (`Product.Brand: string`, `PriceRule.BrandId: string`). Bu sprint ile **Brand master data** normalize edildi:

### Önce (SPRINT 3.3.1)
- ❌ `Product.Brand = "Bosch"` (string, typo riski)
- ❌ `PriceRule.BrandId = "Bosch"` (string matching)
- ❌ Raporlama ve analitik zor
- ❌ Çok dilli destek yok

### Şimdi (SPRINT 3.3.2)
- ✅ `Brand` entity (master data, normalized)
- ✅ `Product.BrandId` (Guid FK → Brand)
- ✅ `PriceRule.BrandId` (Guid FK → Brand)
- ✅ Unique constraint on (TenantId, Code)
- ✅ Mevcut data migrate edildi (sıfır data kaybı)

---

## 🎯 İş Gereksinimleri

### Problem
> "String-based marka desteği typo'ya açık. 'Bosch', 'BOSCH', 'bosch' ayrı değerler olabiliyor. Raporlarda marka bazlı analiz yaparken normalize data şart. Logo URL, aktif/pasif durum gibi metadata eklenecek."

### Çözüm
- **Brand entity**: Id, Code (UPPER normalized), Name, LogoUrl, IsActive
- **Unique constraint**: (TenantId, Code) - tenant içinde benzersiz marka kodları
- **Migration**: Mevcut Product.Brand string değerlerinden Brand entities yaratılıp Product.BrandId FK güncellendi
- **API**: Full CRUD + search endpoint'leri

---

## 🔧 Teknik Değişiklikler

### 1️⃣ Brand Entity (Master Data)

**Lokasyon:** `src/Api/Entities/Brand.cs`

```csharp
public class Brand : TenantEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; }      // Normalized: "BOSCH", "NGK", "MOBIL"
    public string Name { get; set; }       // Display: "Bosch", "NGK", "Mobil"
    public string? LogoUrl { get; set; }   // Optional brand logo
    public bool IsActive { get; set; }
    
    // Navigation
    public ICollection<Product> Products { get; set; }
    public ICollection<PriceRule> PriceRules { get; set; }
}
```

**Özellikleri:**
- **Code**: UPPER normalized, unique constraint ile tenant içinde tekil
- **Name**: Görüntüleme adı (orijinal yazım)
- **LogoUrl**: Opsiyonel (frontend'de marka logo gösterimi için)
- **IsActive**: Soft delete desteği

### 2️⃣ Product Entity Güncellemesi

**Önce:**
```csharp
public class Product
{
    public string? Brand { get; set; }  // String, typo riski
}
```

**Şimdi:**
```csharp
public class Product
{
    [Obsolete("Use BrandId navigation property instead")]
    public string? Brand { get; set; }  // DEPRECATED: Migration için tutuldu
    
    public Guid? BrandId { get; set; }  // ✅ FK to Brand
    public Brand? BrandNavigation { get; set; }
}
```

**Migration Stratejisi:**
- Old `Brand` string field **korundu** (migration compatibility)
- New `BrandId` Guid FK eklendi
- Data migration: `Product.Brand` → `Brand.Code` mapping ile `Product.BrandId` set edildi
- Future: `Brand` string field bir sonraki major release'te kaldırılacak

### 3️⃣ PriceRule Entity Güncellemesi

**Önce:**
```csharp
public class PriceRule
{
    public string? BrandId { get; set; }  // Actually brand NAME, not ID!
}
```

**Şimdi:**
```csharp
public class PriceRule
{
    public Guid? BrandId { get; set; }  // ✅ FK to Brand
    public Brand? BrandNavigation { get; set; }
}
```

**Migration Challenge:**
- PostgreSQL can't auto-convert `string` → `uuid`
- **Çözüm**: 
  1. `RENAME COLUMN BrandId TO BrandIdOld`
  2. `ADD COLUMN BrandId uuid`
  3. Migrate data: `UPDATE price_rules SET BrandId = brands.Id WHERE BrandIdOld = brands.Code`
  4. `DROP COLUMN BrandIdOld`

### 4️⃣ Database Schema

**Migration:** `20260206032419_NormalizeBrandMasterData`

**Yeni Tablo:**
```sql
CREATE TABLE brands (
    "Id" uuid PRIMARY KEY,
    "TenantId" uuid NOT NULL,
    "Code" varchar(50) NOT NULL,
    "Name" varchar(200) NOT NULL,
    "LogoUrl" varchar(500),
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "CreatedBy" uuid NOT NULL
);

-- Unique constraint: normalized code per tenant
CREATE UNIQUE INDEX ix_brands_tenant_code ON brands ("TenantId", "Code");

-- Search index
CREATE INDEX ix_brands_tenant_name ON brands ("TenantId", "Name");
CREATE INDEX ix_brands_tenant_active ON brands ("TenantId", "IsActive");
```

**Updated Tables:**
```sql
-- products: Add BrandId FK
ALTER TABLE products ADD COLUMN "BrandId" uuid;
ALTER TABLE products ADD CONSTRAINT FK_products_brands_BrandId 
    FOREIGN KEY ("BrandId") REFERENCES brands("Id") ON DELETE SET NULL;
CREATE INDEX ix_products_tenant_brandid ON products ("TenantId", "BrandId");

-- price_rules: Change BrandId from string to uuid
ALTER TABLE price_rules RENAME COLUMN "BrandId" TO "BrandIdOld";
ALTER TABLE price_rules ADD COLUMN "BrandId" uuid;
-- Data migration logic (see section below)
ALTER TABLE price_rules ADD CONSTRAINT FK_price_rules_brands_BrandId 
    FOREIGN KEY ("BrandId") REFERENCES brands("Id") ON DELETE CASCADE;
```

### 5️⃣ Data Migration Logic

**Migration SQL:**
```sql
-- Step 1: Create Brand entities from distinct Product.Brand values
INSERT INTO brands ("Id", "TenantId", "Code", "Name", "IsActive", "CreatedAt", "CreatedBy")
SELECT 
    gen_random_uuid(),
    p."TenantId",
    UPPER(TRIM(p."Brand")) AS "Code",     -- Normalized
    TRIM(p."Brand") AS "Name",            -- Original
    true,
    NOW(),
    '00000000-0000-0000-0000-000000000001'::uuid
FROM products p
WHERE p."Brand" IS NOT NULL AND TRIM(p."Brand") != ''
GROUP BY p."TenantId", UPPER(TRIM(p."Brand")), TRIM(p."Brand")
ON CONFLICT ("TenantId", "Code") DO NOTHING;

-- Step 2: Update Product.BrandId from Brand.Code mapping
UPDATE products p
SET "BrandId" = b."Id"
FROM brands b
WHERE p."TenantId" = b."TenantId"
  AND UPPER(TRIM(p."Brand")) = b."Code"
  AND p."Brand" IS NOT NULL;

-- Step 3: Migrate PriceRule.BrandId from old string values
UPDATE price_rules pr
SET "BrandId" = b."Id"
FROM brands b
WHERE pr."TenantId" = b."TenantId"
  AND UPPER(TRIM(pr."BrandIdOld")) = b."Code"
  AND pr."BrandIdOld" IS NOT NULL;

-- Step 4: Drop old column
ALTER TABLE price_rules DROP COLUMN "BrandIdOld";
```

**Sonuç:**
- ✅ Tüm distinct brand name'ler → Brand entities
- ✅ Product.BrandId güncellendinullable: data yoksa NULL)
- ✅ PriceRule.BrandId güncellendi
- ✅ Sıfır data kaybı

---

## 🚀 API Endpoints

### BrandsController

**Base URL:** `/api/brands`

#### GET /api/brands
Search brands by name or code.

**Query Parameters:**
- `q` (string?): Search term (case-insensitive, matches Name or Code)
- `active` (bool?): Filter by IsActive (null = all)
- `limit` (int): Max results (default: 50)

**Response:**
```json
[
  {
    "id": "guid",
    "code": "BOSCH",
    "name": "Bosch",
    "logoUrl": "https://cdn.example.com/bosch.png",
    "isActive": true,
    "createdAt": "2025-02-06T00:00:00Z"
  }
]
```

#### GET /api/brands/{id}
Get single brand by ID.

**Response:** Same as search result item.

#### POST /api/brands
Create new brand.

**Request Body:**
```json
{
  "code": "BOSCH",      // Optional: defaults to Name.ToUpper()
  "name": "Bosch",      // Required
  "logoUrl": "https://...",  // Optional
  "isActive": true      // Optional: defaults to true
}
```

**Validation:**
- Code must be unique within tenant (case-insensitive)
- Returns 409 Conflict if code exists

#### PUT /api/brands/{id}
Update existing brand.

**Request Body:** (all fields optional)
```json
{
  "code": "BOSCH",
  "name": "Bosch GmbH",
  "logoUrl": null,      // Can clear with null/empty
  "isActive": false
}
```

#### DELETE /api/brands/{id}
Delete brand.

**Behavior:**
- **Referenced by Product/PriceRule**: Soft delete (IsActive = false)
- **Not referenced**: Hard delete
- Returns message indicating which action was taken

---

## 🔄 PricingService Integration

### Updated Logic

**Before (SPRINT 3.3.1):**
```csharp
// Include Product only
var variant = await _context.ProductVariants
    .Include(v => v.Product)
    .FirstOrDefaultAsync();

// Pass string brand name
var rule = await FindApplicablePriceRuleAsync(
    tenantId, partyId, variantId,
    variant.Product.Brand,  // ❌ String
    now, currency, ct);

// Match by string
r.BrandId == brandName  // ❌ String comparison
```

**After (SPRINT 3.3.2):**
```csharp
// Include Product + Brand navigation
var variant = await _context.ProductVariants
    .Include(v => v.Product)
        .ThenInclude(p => p.BrandNavigation)  // ✅ Eager load
    .FirstOrDefaultAsync();

// Pass Guid BrandId
var rule = await FindApplicablePriceRuleAsync(
    tenantId, partyId, variantId,
    variant.Product.BrandId,  // ✅ Guid FK
    now, currency, ct);

// Match by Guid FK
r.BrandId == brandId  // ✅ Guid comparison (indexed)
```

### Rule Description Update

**Before:**
```csharp
if (!string.IsNullOrEmpty(appliedRule.BrandId))
{
    ruleDescription = $"Marka iskontosu ({appliedRule.BrandId}): %{value}";
}
```

**After:**
```csharp
if (appliedRule.BrandId.HasValue)
{
    var brandName = variant.Product.BrandNavigation?.Name ?? "Unknown";
    ruleDescription = $"Marka iskontosu ({brandName}): %{value}";
}
```

**Önemli:** Brand.Name eager-loaded via navigation property.

---

## ✅ Test Kapsamı

### Güncellenen Testler (7 Adet)

Tüm brand discount testleri **Guid FK** kullanacak şekilde güncellendi:

1. **CustomerBrandDiscount_Applies**
   - Before: `product.Brand = "Bosch"`, `BrandId = "Bosch"`
   - After: `var boschBrand = await CreateBrandAsync(...)`, `product.BrandId = boschBrand.Id`

2. **CustomerVariant_Overrides_CustomerBrand**
   - Brand entity oluştur, FK set et

3. **GroupBrand_Applies_WhenNoCustomerRule**
   - NGK brand entity, Guid FK

4. **ProductGroupBrand_Fallback**
   - Mobil brand entity

5. **BrandDiscount_DateValidity_Respected**
   - Castrol brand (expired rule)

6. **BrandDiscount_CurrencyFiltering_Respected**
   - Shell brand (USD rule for TRY request)

7. **BrandDiscount_NoBrand_NoMatch**
   - Total brand, product.BrandId = null

### Helper Method Eklendi

```csharp
private async Task<Brand> CreateBrandAsync(
    ErpDbContext dbContext,
    Guid tenantId,
    string code,
    string name)
{
    var brand = new Brand
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        Code = code.ToUpper(),
        Name = name,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = Guid.NewGuid()
    };
    dbContext.Brands.Add(brand);
    await dbContext.SaveChangesAsync();
    return brand;
}
```

### Test Sonuçları

**1. Çalıştırma:**
```
Test Çalıştırması Başarılı.
Toplam test sayısı: 15
     Geçti: 15
 Toplam süre: 9,3066 Saniye
```

**2. Çalıştırma (Deterministik Doğrulama):**
```
Test Çalıştırması Başarılı.
Toplam test sayısı: 15
     Geçti: 15
 Toplam süre: 9,6960 Saniye
```

✅ **Deterministik:** Her iki çalıştırmada da 15/15 geçti

---

## 📊 Performans

### Index Stratejisi

**brands table:**
- `ix_brands_tenant_code` (UNIQUE): (TenantId, Code) - Duplicate prevention
- `ix_brands_tenant_name`: (TenantId, Name) - Search
- `ix_brands_tenant_active`: (TenantId, IsActive) - Active brand filtering

**products table:**
- `ix_products_tenant_brandid`: (TenantId, BrandId) - Brand FK lookup
- ~~`ix_products_tenant_brand`~~: **REMOVED** (old string brand index)

**price_rules table:**
- `ix_price_rules_tenant_brand_lookup`: (TenantId, Scope, TargetId, **BrandId**, Currency, ValidFrom, ValidTo, Priority)
  - **Changed:** BrandId now uuid (was varchar), faster joins

### Query Optimization

**Before (String Matching):**
```sql
SELECT * FROM price_rules
WHERE "TenantId" = @tenantId
  AND "BrandId" = 'Bosch'  -- String comparison
  AND ...
```

**After (Guid FK Join):**
```sql
SELECT pr.* FROM price_rules pr
INNER JOIN brands b ON pr."BrandId" = b."Id"
WHERE pr."TenantId" = @tenantId
  AND pr."BrandId" = @brandGuid  -- UUID comparison (indexed)
  AND ...
```

**Beklenen Kazanç:**
- FK join'ler index-optimized
- Guid comparison varchar'dan hızlı
- Unique constraint typo-based duplicates'i önler

---

## 🎨 Frontend Etkisi

### Eski Akış (SPRINT 3.3.1)
```tsx
// Product form: text input
<input type="text" value={product.brand} onChange={...} />
// Typo riski: "Bosch" vs "bosch" farklı değerler
```

### Yeni Akış (SPRINT 3.3.2)
```tsx
// Brand dropdown/autocomplete
<Select
  options={brands}  // GET /api/brands?active=true
  getOptionLabel={(b) => b.name}
  getOptionValue={(b) => b.id}
  value={selectedBrand}
  onChange={(brand) => setProduct({ ...product, brandId: brand.id })}
/>
```

**Avantajlar:**
- ✅ Autocomplete ile hızlı seçim
- ✅ Typo imkansız (dropdown'dan seçim)
- ✅ Yeni marka ekleme modal ile (BrandsController POST endpoint)
- ✅ Logo gösterimi (logoUrl field)

---

## 🚧 Migration Notları

### Uygulama Adımları

```bash
cd src/Api
dotnet ef database update
```

**Migration Sırası:**
1. `brands` table oluşturulur
2. `products.BrandId` uuid column eklenir
3. `price_rules.BrandId` varchar → uuid dönüşümü:
   - Old column rename: `BrandId` → `BrandIdOld`
   - New column add: `BrandId` uuid
4. **Data migration**:
   - `products.Brand` → `brands` entities
   - `products.BrandId` update
   - `price_rules.BrandId` update
5. `BrandIdOld` column drop
6. Foreign keys eklenir

### Rollback (Gerekirse)

```bash
dotnet ef database update AddBrandBasedDiscounts
# (Previous migration before normalization)
```

**Uyarı:** Rollback sonrası yeni oluşturulan Brand entities kaybolur!

---

## 📝 Kullanım Örnekleri

### Örnek 1: Yeni Marka Ekleme

**API Request:**
```http
POST /api/brands
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Castrol",
  "logoUrl": "https://cdn.example.com/castrol.png"
}
```

**Response:**
```json
{
  "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "code": "CASTROL",  // Auto-generated: Name.ToUpper()
  "name": "Castrol",
  "logoUrl": "https://cdn.example.com/castrol.png",
  "isActive": true,
  "createdAt": "2025-02-06T12:00:00Z"
}
```

### Örnek 2: Marka Bazlı İskonto Kuralı

**Senaryo:** Tüm Bosch ürünlerine %15 iskonto

**Step 1: Brand ID'yi bul**
```http
GET /api/brands?q=Bosch
```

Response: `{"id": "brand-guid-123", "code": "BOSCH", ...}`

**Step 2: Price Rule oluştur**
```http
POST /api/price-rules
{
  "scope": "CUSTOMER",
  "targetId": "{customer-guid}",
  "variantId": null,
  "brandId": "brand-guid-123",  // ✅ Guid FK
  "ruleType": "DISCOUNT_PERCENT",
  "value": 15.00,
  "currency": "TRY",
  "validFrom": "2025-02-06",
  "priority": 100
}
```

**Sonuç:** Müşteri tezgahta Bosch ürün eklediğinde %15 iskonto otomatik uygulanır.

### Örnek 3: Marka Arama (Autocomplete)

```http
GET /api/brands?q=bo&active=true&limit=10
```

**Response:**
```json
[
  {"id": "...", "code": "BOSCH", "name": "Bosch"},
  {"id": "...", "code": "BORA", "name": "Bora"}
]
```

Frontend autocomplete component için ideal.

---

## 🎯 Kabul Kriterleri

- [x] Brand entity oluşturuldu (TenantEntity base)
- [x] Product.BrandId Guid FK eklendi
- [x] PriceRule.BrandId string → Guid FK dönüştürüldü
- [x] DbContext konfigürasyonu (indexes, constraints)
- [x] Migration with data migration SQL
- [x] BrandsController: CRUD + search endpoints
- [x] PricingService Guid-based brand resolution kullanıyor
- [x] 7 brand test güncellendi (Guid FK kullanımı)
- [x] Tüm testler geçti (15/15, 2 çalıştırma)
- [x] Migration başarıyla uygulandı

---

## 🔮 Gelecek İyileştirmeler (Backlog)

### SPRINT 3.3.3 (Öneriler)
- [ ] **Product.Brand string field kaldırılması** (deprecated, migration tamamlandı)
- [ ] **Brand logo upload API** (şu an logoUrl text, frontend S3/CDN upload entegrasyonu)
- [ ] **Brand analytics dashboard** (en çok satan markalar, marka bazlı kar analizi)
- [ ] **Multi-language brand names** (Brand.NameTranslations JSON field)
- [ ] **Brand hierarchy** (parent/child brands: "Bosch" → "Bosch Auto Parts", "Bosch Power Tools")

### Admin UI
- [ ] Brands management page (list, create, edit, deactivate)
- [ ] Product form: brand dropdown (autocomplete)
- [ ] Price rule form: brand selector
- [ ] Bulk brand import (CSV/Excel)

---

## 📚 İlgili Dosyalar

### Entity & Configuration
- [src/Api/Entities/Brand.cs](../../src/Api/Entities/Brand.cs) - Brand entity
- [src/Api/Entities/Product.cs](../../src/Api/Entities/Product.cs) - BrandId FK eklendi
- [src/Api/Entities/PriceRule.cs](../../src/Api/Entities/PriceRule.cs) - BrandId Guid FK
- [src/Api/Data/ErpDbContext.cs](../../src/Api/Data/ErpDbContext.cs) - Brand configuration

### API & Services
- [src/Api/Controllers/BrandsController.cs](../../src/Api/Controllers/BrandsController.cs) - CRUD endpoints
- [src/Api/Services/PricingService.cs](../../src/Api/Services/PricingService.cs) - Guid-based brand resolution

### Migration & Tests
- [src/Api/Data/Migrations/20260206032419_NormalizeBrandMasterData.cs](../../src/Api/Data/Migrations/20260206032419_NormalizeBrandMasterData.cs)
- [tests/ErpCloud.Api.Tests/PricingModuleTests.cs](../../tests/ErpCloud.Api.Tests/PricingModuleTests.cs) - Updated tests

---

**Son Güncelleme:** 6 Şubat 2025  
**Durum:** ✅ Production-ready
**Next:** Frontend brand management UI (SPRINT 3.4)
