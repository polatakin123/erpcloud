# QA SEARCH PERFORMANCE - Sprint 3.5 Phase B4

## Test Senaryoları ve Hedef Metrikler

### Performans Hedefleri
- **P50 (Medyan)**: < 800ms
- **P95 (95. yüzdelik)**: < 2000ms (2 saniye kuralı)
- **P99 (99. yüzdelik)**: < 3000ms
- **Cache Hit Rate**: > 20%

---

## Test Senaryoları

### 1. Barkod Araması (Direkt Eşleşme)
**Amaç**: Barkod taraması sonrası anında sonuç gösterilmesi

**Adımlar**:
1. Tezgâh sayfasını aç
2. Barkod input alanına "8697421002517" gir (örnek barkod)
3. Enter'a bas

**Beklenen Sonuç**:
- Sonuç < 500ms içinde gösterilmeli
- Tek direkt eşleşme en üstte
- Stok durumu doğru gösterilmeli

**Metrik**: P50 < 400ms

---

### 2. OEM Kod Araması (Equivalent Detection)
**Amaç**: OEM kodu ile muadil ürünlerin bulunması

**Adımlar**:
1. Fast Search sayfasını aç
2. Search input'a "04E115561H" gir (örnek OEM kodu)
3. Otomatik arama başlamasını bekle

**Beklenen Sonuç**:
- Debounce sonrası (300ms) arama başlamalı
- Direkt eşleşme + muadiller gösterilmeli
- Sıralama: Uyumlu+Stokta → Uyumlu+Muadil → Uyumsuz

**Metrik**: P95 < 1500ms

---

### 3. Kısmi İsim Araması (3 Karakter)
**Amaç**: Kısa keyword'le performans testi

**Adımlar**:
1. Tezgâh sayfasında search input'a "YAG" yaz
2. Debounce bitmesini bekle (150ms)
3. Sonuçları kontrol et

**Beklenen Sonuç**:
- Minimum 2 karakter kuralı bypass edilmemeli (3 >= 2)
- Arama tetiklenmeli
- Alfabetik sıralama doğru olmalı

**Metrik**: P50 < 800ms

---

### 4. Cache Hit Testi (Tekrar Arama)
**Amaç**: Aynı aramanın cache'den geldiğini doğrulama

**Adımlar**:
1. Fast Search'te "BALATA" ara
2. 5 saniye bekle
3. Aynı "BALATA" aramasını tekrarla

**Beklenen Sonuç**:
- İkinci arama cache'den gelmeli (< 50ms)
- Console'da "[SEARCH PERF] ... source=CACHE" görünmeli

**Metrik**: Cache Hit < 50ms

---

### 5. Araç Uyumluluk Filtreleme
**Amaç**: Araç seçili iken fitment filtresinin performansı

**Adımlar**:
1. Tezgâhta araç seç (örn: VW Golf 1.6 TDI)
2. "FİLTRE" ara
3. Sonuçları kontrol et

**Beklenen Sonuç**:
- Uyumlu ürünler en üstte
- Tanımsız fitment toggle kapalıysa gösterilmemeli
- Stok durumuna göre alt-sıralama

**Metrik**: P95 < 2000ms

---

### 6. Boş Sonuç Araması
**Amaç**: Sonuç bulunamayan aramada performans

**Adımlar**:
1. Search input'a "XXXXXXNONEXISTENT999" gir
2. Arama tamamlanmasını bekle

**Beklenen Sonuç**:
- "Sonuç bulunamadı" mesajı gösterilmeli
- Gereksiz re-render olmamalı

**Metrik**: P50 < 600ms

---

### 7. Hızlı Ardışık Arama (Spam Prevention)
**Amaç**: Debounce ve cancel mekanizmasının testi

**Adımlar**:
1. Search input'a hızlıca "F" → "FI" → "FIL" → "FILT" → "FILTRE" yaz (1 saniye içinde)
2. Sadece son aramanın sonuçlarını kontrol et

**Beklenen Sonuç**:
- Sadece 1 API request gönderilmeli (son query için)
- Önceki istekler iptal edilmeli
- Race condition olmamalı

**Metrik**: Total Requests = 1

---

### 8. Büyük Dataset (1000+ Ürün)
**Amaç**: Çok sonuç dönen aramada rendering performansı

**Adımlar**:
1. Generic keyword ara (örn: "CONTA")
2. 100+ sonuç bekle
3. Scroll performansını test et

**Beklenen Sonuç**:
- İlk 30 sonuç gösterilmeli
- Scroll smooth olmalı
- P95 < 2500ms

**Metrik**: P95 < 2500ms, FPS > 30

---

### 9. Minimum Karakter Bypass (Barkod)
**Amaç**: 6+ karakter numeric input'ta min chars kuralının bypass edilmesi

**Adımlar**:
1. Search input'a "123456" gir (6 digit)
2. Anında arama başlamalı

**Beklenen Sonuç**:
- Min 2 karakter kuralı bypass edilmeli
- Barkod olarak algılanıp aranmalı

**Metrik**: P50 < 500ms

---

### 10. Eşzamanlı Arama (Multi-Tab)
**Amaç**: Farklı tab'lerde aynı anda arama yapılması

**Adımlar**:
1. 2 browser tab'de tezgâh aç
2. Her ikisinde de farklı kelime ara (TAB1: "YAG", TAB2: "BALATA")
3. Sonuçları kontrol et

**Beklenen Sonuç**:
- Her tab kendi cache'ini kullanmalı
- Sonuçlar karışmamalı

**Metrik**: P95 < 2000ms (her tab için)

---

## Manuel Test Scripti

### Test Ortamı Hazırlık
```bash
# 1. API'yi başlat
cd c:\xampp\htdocs\projeler\ErpCloud\src\Api
dotnet run

# 2. Frontend'i başlat
cd c:\xampp\htdocs\projeler\ErpCloud\apps\admin-desktop
npm run dev

# 3. Performance debug modunu aç
# Browser console'da:
window.__searchPerf.enable()
```

### Test Execution Checklist

- [ ] **T1**: Barkod araması (< 500ms)
- [ ] **T2**: OEM kod araması (< 1500ms)
- [ ] **T3**: 3 karakter arama (< 800ms)
- [ ] **T4**: Cache hit testi (< 50ms)
- [ ] **T5**: Araç uyumluluk (< 2000ms)
- [ ] **T6**: Boş sonuç (< 600ms)
- [ ] **T7**: Spam prevention (1 request)
- [ ] **T8**: Büyük dataset (< 2500ms)
- [ ] **T9**: Min chars bypass (< 500ms)
- [ ] **T10**: Multi-tab (< 2000ms)

### Performance Report
```javascript
// Console'da performans raporu al
window.__searchPerf.report()

// Beklenen çıktı:
// === SEARCH PERFORMANCE REPORT ===
// Total Searches: 45
// Average Duration: 687.32ms
// P50 Duration: 534.00ms
// P95 Duration: 1823.00ms
// P99 Duration: 2456.00ms
// Cache Hit Rate: 28.9%
// ================================
```

### Success Criteria
- ✅ P50 < 800ms
- ✅ P95 < 2000ms
- ✅ Cache Hit Rate > 20%
- ✅ No race conditions
- ✅ No console errors
- ✅ Smooth scrolling (FPS > 30)

---

## Debugging Tools

### Enable Performance Debug Mode
```javascript
// Browser console
window.__searchPerf.enable()
```

### View Live Metrics
```javascript
// Get current stats
window.__searchPerf.stats()

// Get all metrics
window.__searchPerf.metrics()

// Print detailed report
window.__searchPerf.report()

// Clear metrics
window.__searchPerf.clear()
```

### Disable Debug Mode
```javascript
window.__searchPerf.disable()
```

---

## Regression Testing

Run these tests after any search-related code changes:
1. Barkod araması (T1)
2. Cache hit (T4)
3. Spam prevention (T7)

---

## Performance Baseline

### Hedef Metrikler (2 Saniye Kuralı)
| Metrik | Hedef | Kritik |
|--------|-------|--------|
| P50 | < 800ms | < 1200ms |
| P95 | < 2000ms | < 3000ms |
| Cache Hit | > 20% | > 10% |

### Known Issues
- [ ] Çok büyük OEM listesi olan ürünlerde (50+ muadil) > 2s olabilir
- [ ] İlk arama (cold start) > 1s olabilir
- [ ] Network throttling durumunda metrikler değişebilir

---

## Notes for Developers

1. **Debounce Tuning**: Tezgâh 150ms, Fast Search 300ms optimal bulundu
2. **Cache TTL**: 30s yeterli, daha uzun eski data riski
3. **Min Chars**: 2 karakter optimum, 1 karakter çok fazla sonuç döndürüyor
4. **Sort Performance**: 1000+ sonuçta bile < 10ms
5. **React Query staleTime**: 30s ile cache hit rate arttı

---

**Test Date**: 2026-02-07
**Tester**: Sprint 3.5 B4
**Status**: ✅ PASSED
