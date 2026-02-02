# e-Belge Entegrasyon Modülü

## Genel Bakış

e-Belge modülü, ErpCloud içinde e-Arşiv ve e-Fatura süreçlerini yönetmek için geliştirilmiş altyapıdır.

**AMAÇ**: Bu sprint'te gerçek GİB entegrasyonu DEĞİL, değiştirilebilir/genişletilebilir bir altyapı kurmaktır.

## Temel Kavramlar

### 1. EDocument (e-Belge Kaydı)
ERP içindeki elektronik belge kaydı. Invoice'tan bağımsız lifecycle'a sahiptir.

**Statüler**:
- `DRAFT`: Oluşturuldu, henüz gönderilmedi
- `QUEUED`: Gönderim için kuyrukta (Outbox)
- `SENDING`: Provider'a gönderiliyor
- `SENT`: Provider'a başarıyla gönderildi
- `ACCEPTED`: GİB tarafından kabul edildi
- `REJECTED`: GİB tarafından reddedildi
- `CANCELLED`: İptal edildi
- `ERROR`: Hata oluştu (retry ile tekrar denenebilir)

### 2. Provider (Entegratör)
Gerçek e-fatura/e-arşiv ulaştırıcı firmalar (Nilvera, Uyumsoft, vb.)

### 3. Adapter Pattern
ERP ↔ Provider arası çevirici katman. Her provider için farklı adapter implement edilir.

## Mimari

```
Invoice (ISSUED)
    ↓
EDocument (DRAFT) → Outbox Event
    ↓
Worker (async)
    ↓
UBL-TR XML Generation
    ↓
Provider.SendAsync()
    ↓
SENT / ERROR
```

## Provider Mimarisi

### IEInvoiceProvider Interface
```csharp
public interface IEInvoiceProvider
{
    string Code { get; }
    Task<SendResult> SendAsync(EDocument doc, string ublXml);
    Task<StatusResult> CheckStatusAsync(EDocument doc);
    Task CancelAsync(EDocument doc);
}
```

### TestProvider
Geliştirme ve test için simülasyon provider:
- Rastgele başarı/hata döner (%70 başarı)
- 1-2 saniye delay ekler
- Gerçek hayattaki belirsizliği simüle eder

### Yeni Provider Ekleme
```csharp
// 1. IEInvoiceProvider implement et
public class NilveraProvider : IEInvoiceProvider
{
    public string Code => "NILVERA";
    // ... implement methods
}

// 2. Program.cs'te kaydet
builder.Services.AddSingleton<IEInvoiceProvider, NilveraProvider>();
var registry = serviceProvider.GetRequiredService<EInvoiceProviderRegistry>();
registry.Register(new NilveraProvider());
```

## UBL-TR XML Üretimi

Invoice entity'den UBL-TR 2.1 uyumlu XML üretilir:
- Namespace: `urn:oasis:names:specification:ubl:schema:xsd:Invoice-2`
- Supplier/Customer party bilgileri
- Invoice lines
- Tax totals
- Currency

## Async İşleyiş (Outbox Pattern)

**ŞU ANDA**: Outbox modülü henüz implemente edilmediği için TODO olarak bırakıldı.

**PLANLANAN AKIŞ**:
1. `EDocument.CreateAsync()` → Outbox event yaz
2. Worker event'i consume eder
3. UBL üretir, provider'a gönderir
4. Status günceller
5. Hata durumunda retry (max 3 kez)

## API Endpoints

### POST /api/e-documents
Issued invoice'tan e-document oluştur.

**Body**:
```json
{
  "invoiceId": "guid",
  "documentType": "EARCHIVE|EINVOICE",
  "scenario": "BASIC|COMMERCIAL"
}
```

**İdempotent**: Aynı (InvoiceId, DocumentType) için tekrar çağrılırsa var olanı döner.

### GET /api/e-documents/{id}
E-document detayı ve status history.

### GET /api/e-documents?invoiceId=&status=&type=
E-document listesi/arama.

### POST /api/e-documents/{id}/retry
ERROR durumundaki belgeyi tekrar dene (max 3 retry).

### POST /api/e-documents/{id}/cancel
DRAFT/QUEUED/SENDING durumundaki belgeyi iptal et.

## Veritabanı Yapısı

### e_documents
- **Unique**: (TenantId, InvoiceId, DocumentType)
- **Indexes**: Status, ProviderCode, DocumentType

### e_document_status_history
- Her status değişikliği kaydedilir
- Audit trail sağlar

## Test Senaryoları

1. ✅ ISSUED invoice → e-document creation
2. ✅ Unique constraint (idempotency)
3. ✅ DRAFT invoice → error
4. ✅ Status history tracking
5. ✅ Search by InvoiceId/Status/Type
6. ✅ Retry logic
7. ✅ Cancel logic
8. ✅ UBL XML generation
9. ✅ Tenant isolation
10. ✅ Provider registry

## Kapsam DIŞI (Bu Sprint'te)

- ❌ Gerçek GİB test/prod API çağrıları
- ❌ Mali mühür / e-imza entegrasyonu
- ❌ PDF görüntüleme / portal entegrasyonu
- ❌ Outbox worker implementation (TODO)
- ❌ e-Arşiv özel senaryolar
- ❌ İrsaliye e-belgesi

## Gelecek Adımlar

1. **Outbox Modülü**: Event-based async processing
2. **Gerçek Provider**: Nilvera/Uyumsoft adapter'ları
3. **GİB Test Entegrasyonu**: Test environment bağlantısı
4. **e-İmza**: Mali mühür entegrasyonu
5. **Monitoring**: Provider uptime, retry metrikleri
6. **Webhook**: GİB status güncellemeleri

## Örnek Kullanım

```csharp
// 1. Invoice issue et
var invoice = await invoiceService.IssueAsync(invoiceId);

// 2. e-Document oluştur
var edocDto = new CreateEDocumentDto(
    InvoiceId: invoice.Id,
    DocumentType: "EARCHIVE",
    Scenario: "BASIC"
);
var edoc = await edocumentService.CreateAsync(edocDto);

// 3. Status takip et
var withHistory = await edocumentService.GetByIdWithHistoryAsync(edoc.Id);

// 4. Hata durumunda retry
if (edoc.Status == "ERROR" && edoc.RetryCount < 3)
{
    await edocumentService.RetryAsync(edoc.Id);
}
```

## Kabul Kriterleri

✅ Migration başarılı  
✅ UBL XML üretiliyor  
✅ Provider/Adapter mimarisi temiz  
✅ Async akış planlandı (Outbox TODO)  
✅ Retry ve status tracking hazır  
✅ Testler yazıldı  
✅ README dokümantasyonu  

## Notlar

- **Provider seçimi**: Şu anda sabit "TEST", gelecekte tenant bazlı yapılandırılacak
- **Retry stratejisi**: Exponential backoff (30s, 60s, 120s)
- **Concurrency**: EDocument.Uuid unique UUID, duplicate gönderimi önler
- **Audit**: StatusHistory her değişikliği kaydeder
