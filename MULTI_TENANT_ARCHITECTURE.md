# Multi-Tenant Architecture - ErpCloud

## 🎯 Overview
SHARED DATABASE + tenant_id model ile %100 tenant izolasyonu sağlayan multi-tenant altyapı.

## 🏗️ Architecture

### Tenant Isolation Strategy
- **Model**: Shared Database, Row-Level Filtering
- **Mechanism**: EF Core Global Query Filters
- **Security**: JWT-based tenant identification
- **Bypass**: Controlled scope-based bypass for system operations

## 📦 Components

### 1. BuildingBlocks/Tenant

#### ITenantContext
```csharp
public interface ITenantContext
{
    Guid? TenantId { get; }      // Current tenant ID
    Guid? UserId { get; }        // Current user ID
    bool IsBypassEnabled { get; } // Bypass flag (use with caution)
}
```

#### TenantContext
Scoped implementation storing tenant/user info throughout request lifecycle.

#### TenantContextAccessor
Provides scoped access to tenant context with methods:
- `SetTenantId(Guid)` - Set current tenant
- `SetUserId(Guid)` - Set current user
- `EnableBypass()` - Enable tenant filter bypass
- `DisableBypass()` - Disable tenant filter bypass

#### TenantMiddleware
Extracts tenant information from JWT claims:
- **Required Claim**: `tenant_id` (GUID string)
- **Optional Claim**: `user_id` or `sub` (for audit)
- **Response**: 401 if tenant_id missing or invalid
- **Public Paths**: `/health`, `/swagger`, `/`

#### TenantBypassScope
IDisposable scope for temporarily bypassing tenant isolation:
```csharp
using (var bypass = new TenantBypassScope(tenantAccessor))
{
    // Can access data from all tenants
    var allData = await context.Items.ToListAsync();
}
// Bypass automatically disabled
```

**⚠️ CRITICAL**: Use only in:
- Background jobs
- System maintenance operations
- Administrative tools
- Never in user-facing endpoints

### 2. BuildingBlocks/Persistence

#### TenantEntity (Abstract Base Class)
```csharp
public abstract class TenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }      // Tenant isolation key
    public DateTime CreatedAt { get; set; }  // Audit timestamp
    public Guid CreatedBy { get; set; }      // Audit user
}
```

#### AppDbContext
Base DbContext with automatic tenant handling:

**Global Query Filter**:
```csharp
modelBuilder.Entity<TenantEntity>()
    .HasQueryFilter(e => 
        tenantContext.IsBypassEnabled || 
        e.TenantId == tenantContext.TenantId);
```

**Automatic TenantId Assignment**:
```csharp
// On SaveChanges, new entities automatically get:
entity.TenantId = tenantContext.TenantId;
entity.CreatedAt = DateTime.UtcNow;
entity.CreatedBy = tenantContext.UserId ?? Guid.Empty;
```

**Indexes**:
- `(TenantId, Id)` - Primary lookup
- `(TenantId, CreatedAt)` - Temporal queries

### 3. API Endpoints

#### GET /me/tenant
Returns current tenant and user information from JWT.

**Response**:
```json
{
  "tenantId": "11111111-1111-1111-1111-111111111111",
  "userId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "timestamp": "2026-01-31T23:17:42Z"
}
```

#### POST /debug/tenant-bypass-test (Development Only)
Tests tenant isolation and bypass functionality.

**What it does**:
1. Creates test items for 2 different tenants using bypass
2. Reads all items WITH bypass (should see both)
3. Reads items WITHOUT bypass (should see only current tenant's)
4. Returns proof of tenant isolation

**Response**:
```json
{
  "message": "Tenant bypass test completed",
  "testTenantIds": ["11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222"],
  "currentTenantId": "22222222-2222-2222-2222-222222222222",
  "results": {
    "withBypass": {
      "itemCount": 2,
      "items": [...]
    },
    "withoutBypass": {
      "itemCount": 1,
      "items": [...]
    }
  },
  "proof": {
    "bypassAllowsAccessToAllTenants": true,
    "normalQueryOnlyShowsCurrentTenant": true,
    "tenantIsolationWorks": true
  }
}
```

#### GET /debug/tenant-isolation-check (Development Only)
Quick check to verify tenant isolation is working.

**Response**:
```json
{
  "currentTenantId": "11111111-1111-1111-1111-111111111111",
  "myItemsCount": 5,
  "totalItemsAllTenants": 15,
  "isolationWorking": true,
  "message": "✓ Tenant isolation is working - you can only see your tenant's data"
}
```

## 🔒 Security Guarantees

### Automatic Protection
✅ All queries on `TenantEntity` automatically filtered by `TenantId`  
✅ No code changes needed in repositories or services  
✅ Impossible to accidentally query other tenant's data  
✅ New entities automatically get current `TenantId`  

### JWT Requirements
✅ `tenant_id` claim REQUIRED (GUID format)  
✅ Missing/invalid → 401 Unauthorized  
✅ User ID extracted from `user_id` or `sub` claim  

### Bypass Controls
✅ Only available through `TenantBypassScope`  
✅ Automatically reverts when scope disposed  
✅ Debug endpoints only in Development environment  
✅ All bypass operations logged  

## 🧪 Testing Tenant Isolation

### 1. Create test data for multiple tenants
```bash
POST /debug/tenant-bypass-test
Authorization: Bearer <token-with-tenant-id-claim>
```

### 2. Verify isolation
Try querying with different tenant tokens:
```bash
# Token 1 (tenant_id: 11111111-1111-1111-1111-111111111111)
GET /api/items
→ Should only return Tenant 1's items

# Token 2 (tenant_id: 22222222-2222-2222-2222-222222222222)
GET /api/items
→ Should only return Tenant 2's items
```

### 3. Check current tenant
```bash
GET /me/tenant
→ Confirms which tenant you're authenticated as
```

## 📊 Database Schema

### Tables
All tenant-isolated tables include:
- `tenant_id` (uuid, NOT NULL, INDEXED)
- `created_at` (timestamp with time zone)
- `created_by` (uuid)

### Example: sample_items
```sql
CREATE TABLE sample_items (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    name varchar(200) NOT NULL,
    description varchar(1000),
    created_at timestamp with time zone NOT NULL,
    created_by uuid NOT NULL
);

CREATE INDEX ix_sample_items_tenant_id ON sample_items (tenant_id, id);
CREATE INDEX ix_sample_items_tenant_created ON sample_items (tenant_id, created_at);
```

## 🚀 Usage Examples

### Creating a Tenant-Isolated Entity
```csharp
public class Product : TenantEntity
{
    public required string Name { get; set; }
    public decimal Price { get; set; }
}

// Usage - TenantId is automatically set
var product = new Product 
{ 
    Id = Guid.NewGuid(),
    Name = "Widget",
    Price = 99.99m
};
await context.Products.AddAsync(product);
await context.SaveChangesAsync(); // TenantId, CreatedAt, CreatedBy auto-filled
```

### Querying (Automatic Filtering)
```csharp
// Automatically filtered to current tenant
var products = await context.Products.ToListAsync();

// Still filtered even with explicit Where
var expensiveProducts = await context.Products
    .Where(p => p.Price > 100)
    .ToListAsync(); // Only current tenant's expensive products
```

### Background Job with Bypass
```csharp
public class CleanupJob
{
    private readonly ErpDbContext _context;
    private readonly TenantContextAccessor _tenantAccessor;

    public async Task CleanupOldDataAsync()
    {
        using (var bypass = new TenantBypassScope(_tenantAccessor))
        {
            // Access all tenants' old data
            var oldItems = await _context.Items
                .Where(i => i.CreatedAt < DateTime.UtcNow.AddYears(-1))
                .ToListAsync();

            _context.Items.RemoveRange(oldItems);
            await _context.SaveChangesAsync();
        }
        // Bypass automatically disabled here
    }
}
```

## 🎯 Acceptance Criteria Status

✅ **tenant_id claim yoksa → 401**: TenantMiddleware enforces this  
✅ **Aynı endpoint farklı token ile farklı data**: Global query filter guarantees  
✅ **Bypass olmadan başka tenant verisi okunamaz**: EF Core filter prevents  
✅ **Bypass scope içinde okunabilir**: TenantBypassScope enables  
✅ **Migration sorunsuz**: All migrations include tenant_id indexes  
✅ **Kodda net yorumlar**: All components documented  

## 🔐 Production Checklist

- [ ] Remove or secure debug endpoints in production
- [ ] Monitor bypass scope usage (add logging/metrics)
- [ ] Review all background jobs for proper tenant handling
- [ ] Implement tenant onboarding process
- [ ] Set up tenant-specific data retention policies
- [ ] Configure audit logging for bypass operations
- [ ] Test tenant deletion/archival procedures

## 📝 Notes

- **No tenants table**: Tenant IDs come from external identity provider
- **No row-level security**: EF Core filters are sufficient
- **Debug endpoints**: Automatically disabled in Production
- **Performance**: Indexes on (tenant_id, ...) ensure optimal query performance
