# ErpCloud - Transactional Outbox + RabbitMQ Mesajlaşma Altyapısı

## 🎯 Hedef
Domain event'leri transaction içinde outbox'a yazılıp, background worker ile RabbitMQ'ya publish edilecek. Consumer tarafında idempotent işlem garantisi sağlanacak.

## ✅ Tamamlanan Bileşenler

### 1. BuildingBlocks.Outbox
**OutboxMessage.cs** - Outbox entity:
```csharp
- Id (Guid)
- TenantId (Guid)
- OccurredAt (DateTime)
- Type (string) - Event tip bilgisi
- Payload (string, JSON)
- Status (Pending/Sent/Failed)
- Attempts (int)
- LastError (string?)
- SentAt (DateTime?)
```

**IOutboxWriter.cs** + **OutboxWriter.cs**:
- `AddAsync(OutboxMessage)` - Transaction içinde outbox'a ekleme
- `AddEventAsync<TEvent>(tenantId, event)` - Generic event ekleme

### 2. BuildingBlocks.Messaging
**RabbitMqOptions.cs** - Konfigürasyon:
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

**RabbitMqConnectionFactory.cs**:
- Singleton connection yönetimi
- Auto recovery enabled
- Exchange declaration

**RabbitMqEventPublisher.cs** - Publisher:
- Exchange: `erp.events` (topic)
- Routing key: `{tenantId}.{eventType}`
- Persistent messages
- Headers: tenant_id, event_type

**RabbitMqConsumerBase.cs** - Consumer base class:
- Queue: `erp.<module>.events`
- Manual ACK
- QoS: prefetchCount = 1
- Abstract `ProcessMessageAsync(EventMessage)`

### 3. BuildingBlocks.Persistence
**ProcessedMessage.cs** - Idempotency tracking:
```csharp
- MessageId (Guid)
- TenantId (Guid)  
- ProcessedAt (DateTime)
- Composite PK: (TenantId, MessageId)
- Unique index
```

**AppDbContext.cs** updates:
- `ConfigureOutboxMessages()` - outbox_messages table config
- `ConfigureProcessedMessages()` - processed_messages table config

## 📋 Kalan İşler

### 1. Migration Oluştur
```bash
cd src/Api
dotnet ef migrations add OutboxAndProcessedMessages
dotnet ef database update
```

Tablolar:
- `outbox_messages` (id, tenant_id, occurred_at, type, payload JSONB, status, attempts, last_error, sent_at)
- `processed_messages` (message_id, tenant_id, processed_at)

### 2. OutboxPublisherService (Background Worker)
**src/Api/Workers/OutboxPublisherService.cs**:
```csharp
public class OutboxPublisherService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // 1. outbox_messages'tan status=Pending al (LIMIT 50)
            // 2. Her mesaj için:
            //    - EventMessage oluştur
            //    - IEventPublisher.PublishAsync() çağır
            //    - Başarılı: Status=Sent, SentAt=now
            //    - Hata: Attempts++, LastError set
            //    - Attempts > 10: Status=Failed
            // 3. SaveChangesAsync()
            // 4. 2 saniye bekle
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
```

Register in Program.cs:
```csharp
builder.Services.AddHostedService<OutboxPublisherService>();
```

### 3. Idempotent Consumer Helper
**BuildingBlocks.Messaging/IdempotentConsumerBase.cs**:
```csharp
protected override async Task ProcessMessageAsync(EventMessage message)
{
    // 1. processed_messages'ta var mı kontrol et
    var exists = await _context.Set<ProcessedMessage>()
        .AnyAsync(x => x.TenantId == message.TenantId && x.MessageId == message.MessageId);
    
    if (exists)
    {
        _logger.LogInformation("Message already processed: {MessageId}", message.MessageId);
        return; // ACK edilecek ama işlem yapılmayacak
    }
    
    // 2. Transaction başlat
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        // 3. İşlemi yap (abstract method)
        await HandleEventAsync(message);
        
        // 4. processed_messages'a kaydet
        await _context.Set<ProcessedMessage>().AddAsync(new ProcessedMessage
        {
            MessageId = message.MessageId,
            TenantId = message.TenantId
        });
        
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}

protected abstract Task HandleEventAsync(EventMessage message);
```

### 4. Demo Controller
**src/Api/Controllers/DemoController.cs**:
```csharp
[ApiController]
[Route("demo")]
public class DemoController : ControllerBase
{
    private readonly ErpDbContext _context;
    private readonly IOutboxWriter _outboxWriter;

    [HttpPost("publish")]
    public async Task<IActionResult> PublishEvent([FromBody] DemoEventRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // 1. Business logic (örn: SampleItem oluştur)
            var item = new SampleItem
            {
                Name = request.ItemName,
                Description = "Created via demo"
            };
            
            _context.SampleItems.Add(item);
            
            // 2. Event oluştur ve outbox'a yaz
            var orderCreated = new OrderCreatedEvent
            {
                OrderId = item.Id,
                ItemName = item.Name,
                CreatedAt = DateTime.UtcNow
            };
            
            var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            await _outboxWriter.AddEventAsync(tenantId, orderCreated);
            
            // 3. Commit - outbox da kaydedilir
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return Ok(new { Message = "Event published to outbox", ItemId = item.Id });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

public record OrderCreatedEvent(Guid OrderId, string ItemName, DateTime CreatedAt);
```

### 5. Program.cs Güncellemeleri
```csharp
// Outbox + Messaging
builder.Services.AddOutbox();
builder.Services.AddRabbitMq(builder.Configuration);

// Background worker
builder.Services.AddHostedService<OutboxPublisherService>();
```

appsettings.json'a ekle:
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

### 6. Test Consumer (Optional)
**src/Api/Consumers/OrderCreatedConsumer.cs**:
```csharp
public class OrderCreatedConsumer : IdempotentConsumerBase
{
    protected override string QueueName => "erp.api.events";
    protected override string[] RoutingKeys => new[] { "*.OrderCreatedEvent" };

    protected override async Task HandleEventAsync(EventMessage message)
    {
        var orderCreated = JsonSerializer.Deserialize<OrderCreatedEvent>(message.Payload);
        _logger.LogInformation("Processing order: {OrderId}", orderCreated.OrderId);
        
        // Business logic here
    }
}

// Program.cs'de başlat
var consumer = app.Services.GetRequiredService<OrderCreatedConsumer>();
consumer.StartConsuming();
```

## 🧪 Test Senaryosu

### 1. RabbitMQ Başlat
```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
# UI: http://localhost:15672 (guest/guest)
```

### 2. Migration Çalıştır
```bash
dotnet ef database update
```

### 3. API Başlat
```bash
dotnet run --project src/Api/ErpCloud.Api.csproj
```

### 4. Event Publish Et
```bash
POST http://localhost:5039/demo/publish
{
  "itemName": "Test Item"
}
```

### 5. Outbox Kontrol Et
```sql
SELECT * FROM outbox_messages WHERE status = 0; -- Pending
SELECT * FROM outbox_messages WHERE status = 1; -- Sent
```

### 6. RabbitMQ Management UI'da Kontrol Et
- Exchanges: `erp.events` görünmeli
- Messages: publish edilen mesajlar

### 7. Idempotency Testi
Aynı MessageId'yi iki kere consume et:
```sql
-- processed_messages tablosunda tek kayıt olmalı
SELECT * FROM processed_messages;
```

## 📦 Kurulu Paketler

### BuildingBlocks.Outbox:
- Microsoft.EntityFrameworkCore 8.0.11
- Microsoft.EntityFrameworkCore.Relational 8.0.11

### BuildingBlocks.Messaging:
- RabbitMQ.Client 6.8.1
- Microsoft.Extensions.Logging.Abstractions 8.0.2
- Microsoft.Extensions.Configuration.Abstractions 8.0.0
- Microsoft.Extensions.Configuration.Binder 8.0.2

### BuildingBlocks.Persistence:
- Microsoft.EntityFrameworkCore 8.0.11
- Referans: BuildingBlocks.Outbox

## ✅ Kabul Kriterleri

- [x] OutboxMessage entity ve writer
- [x] RabbitMQ connection + publisher
- [x] Consumer base class
- [x] ProcessedMessage entity
- [ ] Migration (outbox_messages + processed_messages)
- [ ] OutboxPublisher background worker
- [ ] Idempotent consumer helper
- [ ] Demo controller
- [ ] Transaction içinde outbox yazma testi
- [ ] Worker publish test
- [ ] Idempotency test (aynı message iki kere)

## 🎯 Sonraki Adımlar

1. **Önce API Process Kapat**: Running process var (ID: 86384)
2. **Build Çalıştır**: `dotnet build` - hataları gider
3. **Migration Ekle**: outbox + processed_messages tabloları
4. **Worker Ekle**: OutboxPublisherService
5. **Demo Controller**: Transaction + outbox test
6. **RabbitMQ Docker**: Container başlat
7. **End-to-End Test**: Publish → Outbox → RabbitMQ → Consumer → Idempotency

## 📝 Notlar

- Outbox pattern transactional garantiyi sağlar (at-least-once delivery)
- Worker 2 saniyede bir polling yapar (production'da daha optimize edilebilir)
- Idempotency processed_messages ile garantilenir
- Failed mesajlar manuel retry gerektirir (Attempts > 10)
- RabbitMQ topic exchange ile flexible routing
- Consumer QoS=1 ile tek tek işler, performans artırılabilir
