# Sprint Backlog (Durum Tespiti Bazlı)

Bu backlog, mevcut kod tabanı ve dokümantasyondaki eksiklere göre önceliklendirilmiştir.

## Hedef
- Build kırıklarını kapatmak
- Test ve kalite kapısını çalışır hale getirmek
- Outbox/messaging akışını netleştirmek
- Dokümantasyonu kodla senkronize etmek
- UI'daki kritik TODO/placeholder alanlarını üretime yaklaştırmak

---

## P0 (Kritik) — Bu sprintte mutlaka tamamlanmalı

### 1) Admin Desktop build kırıklarının giderilmesi
**Neden:** `npm run build` başarısız, release/CI bloklayıcı.

**İşler:**
- `FastSalesPage` request tiplerini backend contract ile hizala.
  - `CreateSalesOrderRequest` için `issueDate`, `currency` alanlarını ekle.
  - `CreateShipmentRequest` için zorunlu `orderId` alanını doğru map et.
- Kullanılmayan import/değişken TypeScript hatalarını temizle.

**Kabul Kriteri:**
- `npm run build` başarılı.

**Tahmini Efor:** 0.5-1 gün

---

### 2) Backend test çalıştırılabilirliğinin sağlanması
**Neden:** Ortamda `dotnet` yokken durum doğrulanamıyor; ekip içinde tekrarlanabilir test akışı gerekli.

**İşler:**
- Geliştirme ortamı dokümantasyonuna .NET SDK doğrulama adımı ekle.
- CI veya local script ile `dotnet restore/build/test` standardı belirle.

**Kabul Kriteri:**
- En az bir standart komut seti ile backend testleri düzenli çalıştırılabilir.

**Tahmini Efor:** 0.5 gün

---

### 3) Test projelerinin solution/CI kapsamına alınması
**Neden:** Fiziksel olarak birden fazla test projesi var; solution içinde eksik referans riski var.

**İşler:**
- `ErpCloud.Api.Tests`, `Outbox.Tests`, `Auditing.Tests` projelerini solution'a ekle (veya CI'da pattern ile ayrı çalıştır).
- Tek komutta tüm testlerin koşulduğu akış oluştur.

**Kabul Kriteri:**
- Tüm test projeleri solution/CI tarafından kapsanıyor.

**Tahmini Efor:** 0.5 gün

---

## P1 (Yüksek) — Bu sprintte mümkünse tamamla

### 4) Outbox/Messaging aktivasyon kararı
**Neden:** Outbox kodu mevcut fakat Program.cs'te RabbitMQ/HostedService pasif.

**İşler:**
- Çalışma modu kararı:
  - A) Messaging aktif (RabbitMQ + OutboxDispatcher HostedService),
  - B) Feature flag ile kontrollü devreye alma.
- Seçilen mode göre konfigürasyon ve startup güncellemesi.

**Kabul Kriteri:**
- Outbox akışının üretimde/dev ortamda nasıl çalıştığı net ve uygulanmış.

**Tahmini Efor:** 1 gün

---

### 5) Dokümantasyon senkronizasyonu
**Neden:** E-Document dokümanında Outbox “yok” denirken kodda dispatcher var; güvenilirlik düşüyor.

**İşler:**
- `README.md`, `docs/E-DOCUMENT-MODULE.md`, `UI_SPRINT_STATUS.md` dosyalarını güncel gerçek duruma çek.
- “Tamamlandı / TODO / Gelecek adımlar” bölümlerini net ayrıştır.

**Kabul Kriteri:**
- Dokümanlar birbiriyle ve kodla çelişmiyor.

**Tahmini Efor:** 0.5 gün

---

### 6) UI kritik TODO alanları (placeholder kapatma)
**Neden:** Stok kartları ve context alanında disabled/query TODO'lar var.

**İşler:**
- `StockCardDetailPage`: stock levels API entegrasyonu.
- `StockCardsListPage`: tüm varyantları listeleyen gerçek endpoint veya backend uyumlu workaround.
- `ContextBar`: ayarlar dialogu placeholder yerine gerçek akış.

**Kabul Kriteri:**
- İlgili ekranlar placeholder/disabled olmadan işlevsel.

**Tahmini Efor:** 1-1.5 gün

---

## P2 (Orta) — Sonraki sprint(ler)

### 7) Returns/Credit Notes frontend kapsamı
**İşler:**
- Hooks: `useSalesReturns`, `usePurchaseReturns`, `useCreditNotes`
- List/detail sayfaları + aksiyon butonları (Receive/Ship/Issue)
- Frontend error mapping

**Kabul Kriteri:**
- Uçtan uca temel iade/alacak notu UI akışı çalışıyor.

**Tahmini Efor:** 2-3 gün

---

### 8) Raporlamalar ve export
**İşler:**
- Aging'e credit note etkisini yansıtma
- Sales/Purchase net hesaplar
- Stock report return movement filtreleri
- CSV export (returns/credit notes)

**Kabul Kriteri:**
- Raporlar iade/alacak notu senaryolarında doğru sonuç veriyor.

**Tahmini Efor:** 1.5-2 gün

---

### 9) Demo seed/onboarding stabilizasyonu
**İşler:**
- Demo user seed akışındaki FK sorununu çöz.
- Lokal ayağa kaldırma adımlarını tek akışta dokümante et.

**Kabul Kriteri:**
- Yeni geliştirici tek dokümanla sistemi ayağa kaldırabiliyor.

**Tahmini Efor:** 0.5-1 gün

---

## Sprint Önerisi (5 iş günü)

### Önerilen kapsam
- P0/1/2/3 (build + test + solution kapsamı)
- P1/4 (outbox çalışma kararı)
- P1/5 (dokümantasyon senkronizasyonu)
- P1/6'dan en kritik 1-2 ekran

### Beklenen sprint çıktısı
- Build kırığı olmayan, test kapsamı net, dokümantasyonu güncel bir baseline.
- Sonraki sprint için returns/reporting işlerinin önündeki teknik borç azaltılmış olur.
