# Transactional Outbox Pattern + RabbitMQ Implementation

## ✅ Tamamlanan İşler

### 1. Database Şeması (EF Migration)
- ✅ `outbox_messages` tablosu güncellendi (NextAttemptAt eklendi)
- ✅ `processed_messages` tablosu oluşturuldu (idempotency için)
- ✅ `demo_event_logs` tablosu oluşturuldu (test için)
- ✅ Migration'lar oluşturuldu ve uygulandı:
  - `20260201044951_OutboxAndDemoTables`
  - `20260201045213_AddProcessedMessages`

### 2. Outbox Pattern Implementation
- ✅ **OutboxMessage** entity'sine retry için `NextAttemptAt` property'si eklendi
- ✅ **ProcessedMessage** entity'si oluşturuldu (composite key: TenantId + MessageId)
- ✅ **AppDbContext** konfigürasyonu güncellendi (indexes, unique constraints)

### 3. Background Services
- ✅ **OutboxDispatcherService**: Her 2 saniyede 50 mesaj işleyen background worker
  - SELECT FOR UPDATE SKIP LOCKED (concurrent processing için)
  - Exponential backoff retry (2^attempts seconds, max 60s)
  - 10 denemeden sonra Failed olarak işaretlenir
  - Başarılı publish'lerde Sent status + SentAt timestamp

### 4. Idempotent Consumer
- ✅ **IdempotentConsumerBase**: Abstract base class
  - ProcessedMessage tablosunu kontrol eder
  - Duplicate message'ları ACK ile atlar
  - PostgreSQL unique constraint violation'ı yakalar (23505)
  - Transaction içinde ProcessedMessage + Handler çalıştırır

- ✅ **DemoEventConsumer**: Test consumer implementation
  - Queue: `erp.demo.events`
  - Routing key: `*.DemoEventCreated`
  - Console'a log yazar
  - `demo_event_logs` tablosuna kayıt atar

### 5. Demo Endpoint
- ✅ **POST /demo/publish**: Outbox pattern test endpoint
  - [Authorize] attribute
  - `DemoEventCreated` event'ini outbox'a ekler
  - Transaction içinde persist eder
  - Response: TenantId, OrderNo, Amount, Message

### 6. Unit Tests
- ✅ **OutboxWriterTests** (2 test)
  - `AddEventAsync_ShouldSerializeAndInsertToOutbox`: JSON serialization test
  - `AddEventAsync_ShouldSetCorrectTimestamp`: Timestamp validation test

- ✅ **IdempotencyTests** (2 test)
  - `ProcessedMessage_ShouldPreventDuplicateWithSameMessageId`: Idempotency test
  - `ProcessedMessage_ShouldAllowSameMessageIdForDifferentTenants`: Multi-tenant test

**Tüm testler başarılı: 4/4 ✅**

## 📋 Database Schema

### outbox_messages
```sql
CREATE TABLE outbox_messages (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  occurred_at TIMESTAMP NOT NULL,
  type VARCHAR(200) NOT NULL,
  payload JSONB NOT NULL,
  status INT NOT NULL,        -- 0:Pending, 1:Sent, 2:Failed
  attempts INT NOT NULL DEFAULT 0,
  last_error TEXT,
  sent_at TIMESTAMP,
  next_attempt_at TIMESTAMP,  -- Retry scheduling
  
  INDEX ix_outbox_messages_tenant_status (tenant_id, status, occurred_at),
  INDEX ix_outbox_messages_status_occurred (status, occurred_at)
);
```

### processed_messages
```sql
CREATE TABLE processed_messages (
  tenant_id UUID NOT NULL,
  id UUID NOT NULL,           -- MessageId from outbox
  processed_at TIMESTAMP NOT NULL,
  
  PRIMARY KEY (tenant_id, id),
  UNIQUE INDEX ix_processed_messages_tenant_message (tenant_id, id)
);
```

### demo_event_logs
```sql
CREATE TABLE demo_event_logs (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  message_id UUID NOT NULL,   -- MessageId from outbox
  payload JSONB NOT NULL,
  processed_at TIMESTAMP NOT NULL,
  created_at TIMESTAMP NOT NULL,
  created_by UUID NOT NULL,
  
  INDEX IX_demo_event_logs_TenantId (tenant_id),
  INDEX IX_demo_event_logs_TenantId_CreatedAt (tenant_id, created_at),
  UNIQUE INDEX ix_demo_event_logs_tenant_message (tenant_id, message_id)
);
```

## 🔄 Flow Diagram

### 1. Event Publishing (Transactional Outbox)
```
API Endpoint (POST /demo/publish)
  ↓
Begin Transaction
  ↓
Business Logic (örn: Order oluştur)
  ↓
OutboxWriter.AddEventAsync(tenantId, event)
  ↓
INSERT INTO outbox_messages (status=Pending)
  ↓
Commit Transaction
  ↓
Response to Client
```

### 2. Background Publishing
```
OutboxDispatcherService (2 saniye polling)
  ↓
SELECT * FROM outbox_messages 
WHERE status=0 AND (next_attempt_at IS NULL OR next_attempt_at <= NOW())
ORDER BY occurred_at LIMIT 50
FOR UPDATE SKIP LOCKED
  ↓
RabbitMQ Publish (exchange: erp.events, routing key: {tenantId}.{eventType})
  ↓
Success: UPDATE status=Sent, sent_at=NOW
  ↓
Failure: Increment attempts, calculate next_attempt_at (exponential backoff)
  ↓
Failed: If attempts > 10, UPDATE status=Failed
```

### 3. Idempotent Consumer
```
RabbitMQ Message Received (queue: erp.demo.events)
  ↓
Extract MessageId from headers
  ↓
Begin Transaction
  ↓
SELECT COUNT(*) FROM processed_messages 
WHERE tenant_id=@tenantId AND message_id=@messageId
  ↓
If EXISTS → ACK and skip (idempotency)
  ↓
INSERT INTO processed_messages (message_id, tenant_id, processed_at)
  ↓
Execute Handler (örn: INSERT INTO demo_event_logs)
  ↓
Commit Transaction
  ↓
ACK Message
  ↓
On Error: Rollback + NACK (requeue)
  ↓
On Unique Constraint Violation (23505): ACK and skip
```

## 🚀 Kullanım

### 1. Migrations Uygula
```bash
cd src/Api
dotnet ef database update --context ErpDbContext
```

### 2. RabbitMQ Ayarları (appsettings.json)
```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "Username": "guest",
    "Password": "guest",
    "Exchange": "erp.events",
    "ExchangeType": "topic"
  }
}
```

### 3. Uygulamayı Başlat
```bash
cd src/Api
dotnet run
```

Background services otomatik başlar:
- `OutboxDispatcherService`: Outbox'tan RabbitMQ'ya publish
- `DemoEventConsumerHostedService`: RabbitMQ'dan consume

### 4. Test Et

**Event Publish Et:**
```bash
curl -X POST https://localhost:5001/demo/publish \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "orderNo": "ORD-12345",
    "amount": 1250.75
  }'
```

**Response:**
```json
{
  "tenantId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "orderNo": "ORD-12345",
  "amount": 1250.75,
  "message": "Event enqueued to outbox"
}
```

**Kontrol Et:**
1. `outbox_messages` tablosunda Pending → Sent olduğunu gör
2. Console'da `[DEMO] Tenant=..., OrderNo=ORD-12345, Amount=1250.75` görünmeli
3. `processed_messages` tablosunda MessageId kaydedildi mi?
4. `demo_event_logs` tablosunda event loglandı mı?

### 5. Testleri Çalıştır
```bash
cd tests/ErpCloud.BuildingBlocks.Outbox.Tests
dotnet test
```

Sonuç:
```
Başarılı! - Başarısız: 0, Başarılı: 4, Toplam: 4
```

## 🛡️ Garantiler

### At-Least-Once Delivery
- Outbox pattern sayesinde event DB'ye yazıldıktan sonra asla kaybolmaz
- Retry mekanizması ile geçici hatalar tolere edilir
- Failed durumu ile DLQ benzeri izole edilebilir

### Exactly-Once Processing (Idempotency)
- `processed_messages` tablosu ile duplicate event'ler skip edilir
- Unique constraint (tenant_id, message_id) garanti sağlar
- PostgreSQL exception handling (SQLSTATE 23505)

### Multi-Tenant Isolation
- Her tenant için ayrı MessageId namespace'i
- Aynı MessageId farklı tenant'lar için kullanılabilir
- Row-level isolation

### Retry Logic
- Exponential backoff: 2^attempts seconds
- Max backoff: 60 seconds
- Max attempts: 10
- Failed event'ler manuel inceleme için işaretlenir

## 📂 Dosya Yapısı

```
src/
├── BuildingBlocks/
│   ├── Outbox/
│   │   ├── OutboxMessage.cs              (NextAttemptAt eklendi)
│   │   ├── IOutboxWriter.cs              (mevcut)
│   │   └── OutboxWriter.cs               (mevcut)
│   ├── Persistence/
│   │   ├── AppDbContext.cs               (konfigürasyon güncellendi)
│   │   └── ProcessedMessage.cs           (yeni)
│   └── Messaging/
│       ├── RabbitMqEventPublisher.cs     (mevcut)
│       └── RabbitMqConsumerBase.cs       (mevcut)
├── Api/
│   ├── Services/
│   │   ├── OutboxDispatcherService.cs    (yeni)
│   │   ├── IdempotentConsumerBase.cs     (yeni)
│   │   ├── DemoEventConsumer.cs          (yeni)
│   │   └── DemoEventConsumerHostedService.cs (yeni)
│   ├── Controllers/
│   │   └── DemoController.cs             (publish endpoint eklendi)
│   ├── Events/
│   │   └── DemoEventCreated.cs           (yeni)
│   ├── Entities/
│   │   └── DemoEventLog.cs               (yeni)
│   └── Data/
│       ├── ErpDbContext.cs               (DemoEventLogs eklendi)
│       └── Migrations/
│           ├── 20260201044951_OutboxAndDemoTables.cs
│           └── 20260201045213_AddProcessedMessages.cs
tests/
└── ErpCloud.BuildingBlocks.Outbox.Tests/
    ├── OutboxWriterTests.cs              (2 test)
    └── IdempotencyTests.cs               (2 test)
```

## ⚙️ Konfigürasyon

### OutboxDispatcher Settings
```csharp
PollingIntervalSeconds = 2     // Her 2 saniyede poll
BatchSize = 50                  // Batch processing
MaxAttempts = 10                // 10 denemeden sonra Failed
MaxBackoffSeconds = 60          // Maximum retry delay
```

### Consumer Settings
- Queue: `erp.demo.events`
- Routing Key: `*.DemoEventCreated`
- Exchange: `erp.events` (topic)
- Prefetch Count: 1 (QoS)

## 🎯 Next Steps (Opsiyonel)

1. **Dead Letter Queue (DLQ)**: Failed event'leri ayrı queue'ya taşı
2. **Monitoring**: Prometheus metrics ekle (outbox depth, publish rate, failure rate)
3. **Circuit Breaker**: RabbitMQ connection failure durumunda devre kesici
4. **Saga Pattern**: Multi-step workflow için orchestration/choreography
5. **Event Versioning**: Payload schema migration stratejisi

## 📊 Metrikler

Önerilen metrikler:
- `outbox_pending_count`: Bekleyen mesaj sayısı
- `outbox_failed_count`: Failed olarak işaretlenmiş mesaj sayısı
- `outbox_publish_rate`: Saniyedeki publish oranı
- `consumer_idempotency_skip_rate`: Skip edilen duplicate mesaj oranı
- `consumer_processing_duration`: Event işleme süresi

## 🔒 Güvenlik

- ✅ JWT Authentication (`[Authorize]` attribute)
- ✅ Tenant isolation (her sorgu tenant_id filtresi içermeli)
- ✅ Sensitive data logging kapalı
- ⚠️ RabbitMQ TLS/SSL (production için etkinleştir)
- ⚠️ RabbitMQ user permissions (production için role-based access)

---

**✅ Tüm gereksinimler tamamlandı!**
- ✅ Transactional Outbox Pattern
- ✅ RabbitMQ Integration
- ✅ Idempotent Consumer
- ✅ Retry + Exponential Backoff
- ✅ Background Worker
- ✅ Demo Endpoint + Consumer
- ✅ Unit Tests (4 adet, hepsi başarılı)
- ✅ Database Migrations
- ✅ Multi-tenant Support
