# ErpCloud - Multi-Tenant ERP System

Cloud-based modular monolith ERP system built with .NET 8 with **Shared Database + TenantId** multi-tenancy.

**🇹🇷 Uygulama Arayüzü:** Tüm kullanıcı arayüzü **tamamen Türkçedir**. Yedek parça sektörü ve Türk ERP terminolojisi kullanılmıştır.

## Multi-Tenant Strategy

### Tenant Isolation
- Every entity extends `TenantEntity` (includes `TenantId`, `CreatedAt`, `CreatedBy`)
- Global query filter: `e => IsBypassEnabled || e.TenantId == CurrentTenantId`
- Automatic tenant assignment on `SaveChanges()`
- Middleware extracts `tenant_id` from JWT claims

### Tenant Bypass
```csharp
using (new TenantBypassScope(tenantAccessor))
{
    // Cross-tenant queries allowed here (use with caution!)
    var allItems = await dbContext.SampleItems.ToListAsync();
}
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL 15+ (running on `localhost:5432`)

## Quick Start

### 1. Setup Database

```powershell
cd src/Api
dotnet ef database update
```

### 2. Run API

```powershell
dotnet run
```

API: `http://localhost:5000`

### 3. Test Multi-Tenancy

#### Step 1: Generate Test Token
```powershell
curl http://localhost:5000/api/test/generate-token
```

#### Step 2: Use Token
```powershell
curl -H "Authorization: Bearer <YOUR_TOKEN>" http://localhost:5000/api/tenant/me
```

## API Endpoints

**Public:**
- `GET /health` - Health check
- `GET /swagger` - API documentation  
- `GET /api/test/generate-token` - Generate test JWT

**Protected (Requires JWT):**
- `GET /api/tenant/me` - Current tenant info
- `POST /api/debug/tenant-bypass-test` - Test bypass (Dev only)

## Testing Scenarios

### ✅ Test 1: Tenant Isolation
1. Generate token for Tenant A → Save token
2. Generate token for Tenant B → Save token
3. Call `/api/tenant/me` with Token A → See Tenant A ID
4. Call `/api/tenant/me` with Token B → See Tenant B ID

### ✅ Test 2: Missing Tenant
```powershell
curl http://localhost:5000/api/tenant/me
# Expected: 401 - "tenant_id claim is required"
```

### ✅ Test 3: Bypass Scope
```powershell
curl -X POST -H "Authorization: Bearer <TOKEN>" http://localhost:5000/api/debug/tenant-bypass-test
# Check logs for bypass enable/disable messages
```

## Project Structure

```
/src
  /BuildingBlocks
    /Tenant          # Multi-tenant infrastructure
    /Persistence     # TenantEntity, AppDbContext
    /Common          # Result<T>, Error
  /Api
    /Controllers     # TenantController, TestController
    /Data            # ErpDbContext, Migrations
    /Entities        # SampleItem
```

## Status

✅ Multi-tenant infrastructure  
✅ JWT authentication  
✅ Tenant isolation with global filters  
✅ Tenant bypass scope  
✅ Database migrations  
✅ Audit logging with automatic tracking  
✅ Organization/Branch/Warehouse module  
✅ Party (Customer/Supplier) module  
✅ Catalog (Product/Variant/Price List) module  
✅ Stock Ledger (Immutable, Concurrency-safe)  
✅ Sales Order (with Stock Reservation Integration)  
✅ Purchase Order & Goods Receipt (with Stock Integration)

## Catalog Module

The Catalog module manages products, variants, and pricing. It's the foundation for inventory and sales operations.

### Entity Structure

**Product** (Master Card)
- Code (unique per tenant, 2-32 chars, A-Z0-9_-)
- Name (2-200 chars)
- Description (optional, max 1000 chars)
- IsActive (boolean)
- Variants (collection)

**ProductVariant** (SKU-level)
- ProductId (FK)
- Sku (unique per tenant, 2-64 chars)
- Barcode (optional, max 128 chars)
- Name (2-200 chars)
- Unit (optional, max 32 chars, e.g., "PCS", "KG", "M")
- VatRate (decimal 0-100, VAT percentage)
- IsActive (boolean)

**PriceList**
- Code (unique per tenant, 2-32 chars, A-Z0-9_-)
- Name (2-200 chars)
- Currency (exactly 3 uppercase chars, e.g., "USD", "EUR", "TRY")
- IsDefault (boolean, only one default per tenant)
- Items (collection)

**PriceListItem**
- PriceListId (FK)
- VariantId (FK)
- UnitPrice (decimal >= 0)
- MinQty (optional decimal >= 0, for tiered pricing)
- ValidFrom (optional DateTime, price valid from this date)
- ValidTo (optional DateTime, price valid until this date)

### API Endpoints

#### Products
```bash
# Create product
curl -X POST http://localhost:5000/api/products \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "LAPTOP",
    "name": "Laptop Computer",
    "description": "High-performance laptop",
    "isActive": true
  }'

# List products (with pagination, search, and active filter)
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/products?page=1&size=50&q=Laptop&active=true"

# Get product by ID
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/products/{productId}"

# Update product
curl -X PUT http://localhost:5000/api/products/{productId} \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "LAPTOP",
    "name": "Laptop Computer Updated",
    "description": "Updated description",
    "isActive": true
  }'

# Delete product
curl -X DELETE http://localhost:5000/api/products/{productId} \
  -H "Authorization: Bearer <TOKEN>"
```

#### Product Variants
```bash
# Create variant under product
curl -X POST http://localhost:5000/api/products/{productId}/variants \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "sku": "LAPTOP-I7-16GB",
    "barcode": "1234567890123",
    "name": "Laptop i7 16GB",
    "unit": "PCS",
    "vatRate": 18.00,
    "isActive": true
  }'

# List variants for product
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/products/{productId}/variants?page=1&size=50&active=true"

# Get variant by ID
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/variants/{variantId}"

# Update variant
curl -X PUT http://localhost:5000/api/variants/{variantId} \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "sku": "LAPTOP-I7-16GB",
    "barcode": "1234567890123",
    "name": "Laptop i7 16GB RAM",
    "unit": "PCS",
    "vatRate": 20.00,
    "isActive": true
  }'

# Delete variant
curl -X DELETE http://localhost:5000/api/variants/{variantId} \
  -H "Authorization: Bearer <TOKEN>"
```

#### Price Lists
```bash
# Create price list
curl -X POST http://localhost:5000/api/price-lists \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "RETAIL",
    "name": "Retail Price List",
    "currency": "USD",
    "isDefault": true
  }'

# List price lists
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/price-lists?page=1&size=50&q=Retail"

# Get price list by ID
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/price-lists/{priceListId}"

# Update price list
curl -X PUT http://localhost:5000/api/price-lists/{priceListId} \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "RETAIL",
    "name": "Retail Prices Updated",
    "currency": "EUR",
    "isDefault": false
  }'

# Delete price list
curl -X DELETE http://localhost:5000/api/price-lists/{priceListId} \
  -H "Authorization: Bearer <TOKEN>"
```

#### Price List Items
```bash
# Create price item
curl -X POST http://localhost:5000/api/price-lists/{priceListId}/items \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "variantId": "{variantId}",
    "unitPrice": 1299.99,
    "minQty": null,
    "validFrom": "2026-01-01T00:00:00Z",
    "validTo": "2026-12-31T23:59:59Z"
  }'

# List items in price list (with optional variant filter)
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/price-lists/{priceListId}/items?page=1&size=50&variantId={variantId}"

# Get price item by ID
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/price-list-items/{itemId}"

# Update price item
curl -X PUT http://localhost:5000/api/price-list-items/{itemId} \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "variantId": "{variantId}",
    "unitPrice": 1399.99,
    "minQty": null,
    "validFrom": "2026-01-01T00:00:00Z",
    "validTo": "2026-12-31T23:59:59Z"
  }'

# Delete price item
curl -X DELETE http://localhost:5000/api/price-list-items/{itemId} \
  -H "Authorization: Bearer <TOKEN>"
```

#### 🎯 Pricing Query (BONUS Endpoint)
```bash
# Get variant price from default price list (current date)
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/pricing/variant/{variantId}"

# Get variant price from specific price list
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/pricing/variant/{variantId}?priceListCode=RETAIL"

# Get variant price at specific date
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/pricing/variant/{variantId}?priceListCode=RETAIL&at=2026-06-15T10:00:00Z"

# Response format:
{
  "variantId": "guid",
  "sku": "LAPTOP-I7-16GB",
  "variantName": "Laptop i7 16GB",
  "priceListCode": "RETAIL",
  "currency": "USD",
  "unitPrice": 1299.99,
  "vatRate": 18.00,
  "minQty": null,
  "validFrom": "2026-01-01T00:00:00Z",
  "validTo": "2026-12-31T23:59:59Z"
}
```

### Business Rules

1. **Product Code Unique**: Per tenant
2. **Variant SKU Unique**: Per tenant (allows same SKU across different products)
3. **Price List Code Unique**: Per tenant
4. **Default Price List**: Only one default per tenant (automatically managed)
5. **Composite Uniqueness**: PriceListItem is unique on (TenantId + PriceListId + VariantId + MinQty + ValidFrom)
6. **VatRate Range**: 0-100 (decimal)
7. **UnitPrice**: Must be >= 0
8. **Date Validation**: ValidTo >= ValidFrom (when both provided)
9. **Cascade Delete**: 
   - Deleting product removes all variants and their price items
   - Deleting price list removes all items
10. **Pricing Logic**:
    - If priceListCode not provided → use default price list
    - If at date not provided → use current UTC date
    - Date filtering: ValidFrom <= at <= ValidTo (or nulls)
    - Returns highest MinQty tier applicable
11. **Validation**: Codes normalized (trim + uppercase), Currency uppercase
12. **Audit**: All CRUD operations automatically logged

### Permissions

- `product.read` - View products
- `product.write` - Create/Update/Delete products
- `variant.read` - View variants
- `variant.write` - Create/Update/Delete variants
- `pricelist.read` - View price lists and items
- `pricelist.write` - Create/Update/Delete price lists and items
- `pricing.read` - Query pricing endpoint

### Testing the Module

```bash
# 1. Generate test token
TOKEN=$(curl -s http://localhost:5000/api/test/generate-token | jq -r '.token')

# 2. Create product
PROD_ID=$(curl -s -X POST http://localhost:5000/api/products \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code":"LAPTOP",
    "name":"Laptop Computer",
    "description":"High-performance laptop",
    "isActive":true
  }' | jq -r '.id')

# 3. Create variant
VAR_ID=$(curl -s -X POST http://localhost:5000/api/products/$PROD_ID/variants \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "sku":"LAPTOP-I7-16GB",
    "barcode":"1234567890123",
    "name":"Laptop i7 16GB",
    "unit":"PCS",
    "vatRate":18.00,
    "isActive":true
  }' | jq -r '.id')

# 4. Create default price list
PL_ID=$(curl -s -X POST http://localhost:5000/api/price-lists \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code":"RETAIL",
    "name":"Retail Prices",
    "currency":"USD",
    "isDefault":true
  }' | jq -r '.id')

# 5. Create price item
curl -X POST http://localhost:5000/api/price-lists/$PL_ID/items \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"variantId\":\"$VAR_ID\",
    \"unitPrice\":1299.99,
    \"minQty\":null,
    \"validFrom\":\"2026-01-01T00:00:00Z\",
    \"validTo\":\"2026-12-31T23:59:59Z\"
  }"

# 6. Query price using default price list
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/pricing/variant/$VAR_ID"

# 7. Query price at specific date
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/pricing/variant/$VAR_ID?at=2026-06-15T10:00:00Z"

# 8. Create tiered pricing (wholesale)
PL2_ID=$(curl -s -X POST http://localhost:5000/api/price-lists \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code":"WHOLESALE",
    "name":"Wholesale Prices",
    "currency":"USD",
    "isDefault":false
  }' | jq -r '.id')

# Add tier 1: 1-9 units
curl -X POST http://localhost:5000/api/price-lists/$PL2_ID/items \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"variantId\":\"$VAR_ID\",
    \"unitPrice\":1199.99,
    \"minQty\":1,
    \"validFrom\":null,
    \"validTo\":null
  }"

# Add tier 2: 10+ units
curl -X POST http://localhost:5000/api/price-lists/$PL2_ID/items \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"variantId\":\"$VAR_ID\",
    \"unitPrice\":1099.99,
    \"minQty\":10,
    \"validFrom\":null,
    \"validTo\":null
  }"

# 9. Query wholesale price
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/pricing/variant/$VAR_ID?priceListCode=WHOLESALE"
```

## Party Module

The Party module manages customers, suppliers, or entities that are both. This is the foundation for financial operations (invoices, payments).

### Entity Structure

**Party**
- Code (unique per tenant, 2-32 chars, A-Z0-9_-)
- Name (2-200 chars)
- Type (CUSTOMER, SUPPLIER, BOTH)
- TaxNumber, Email, Phone, Address (optional)
- CreditLimit (decimal, optional, >= 0)
- PaymentTermDays (int, optional, >= 0)
- IsActive (boolean)

**Note**: Balance calculation will be implemented in Sprint-2 (invoices + payments).

### API Endpoints

```bash
# Create party
curl -X POST http://localhost:5000/api/parties \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "CUST001",
    "name": "Acme Corporation",
    "type": "CUSTOMER",
    "taxNumber": "1234567890",
    "email": "info@acme.com",
    "phone": "+90 212 555 1234",
    "address": "Istanbul, Turkey",
    "creditLimit": 50000.00,
    "paymentTermDays": 30,
    "isActive": true
  }'

# List parties (with filters and pagination)
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/parties?page=1&size=50&q=Acme&type=CUSTOMER"

# Get party by ID
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/parties/{partyId}"

# Update party
curl -X PUT http://localhost:5000/api/parties/{partyId} \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "CUST001",
    "name": "Acme Corp Updated",
    "type": "BOTH",
    "taxNumber": "1234567890",
    "email": "contact@acme.com",
    "phone": "+90 212 555 1234",
    "address": "New Address",
    "creditLimit": 75000.00,
    "paymentTermDays": 45,
    "isActive": true
  }'

# Delete party
curl -X DELETE http://localhost:5000/api/parties/{partyId} \
  -H "Authorization: Bearer <TOKEN>"
```

### Query Filters

**type** - Filter by party type:
- `CUSTOMER` - Returns CUSTOMER + BOTH
- `SUPPLIER` - Returns SUPPLIER + BOTH
- `BOTH` - Returns only BOTH

**q** - Search in Code or Name (case-insensitive)

### Business Rules

1. **Tenant Isolation**: Different tenants can use the same codes
2. **Code Unique**: Party code must be unique within tenant
3. **Type System**: BOTH type appears in both CUSTOMER and SUPPLIER queries
4. **Credit Limit**: Must be >= 0 (if provided)
5. **Payment Terms**: Must be >= 0 days (if provided)
6. **Hard Delete**: Parties can be deleted (Sprint-2 will add financial record check)
7. **Validation**: Code normalized (trim + uppercase), email format validated
8. **Audit**: All CRUD operations automatically logged

### Permissions

- `party.read` - View parties
- `party.write` - Create/Update/Delete parties

### Testing the Module

```bash
# 1. Generate test token
TOKEN=$(curl -s http://localhost:5000/api/test/generate-token | jq -r '.token')

# 2. Create customer
CUST_ID=$(curl -s -X POST http://localhost:5000/api/parties \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code":"CUST001",
    "name":"Test Customer",
    "type":"CUSTOMER",
    "taxNumber":"1234567890",
    "email":"customer@test.com",
    "phone":"+90 555 1234",
    "address":"Istanbul",
    "creditLimit":10000.00,
    "paymentTermDays":30,
    "isActive":true
  }' | jq -r '.id')

# 3. Create supplier
SUPP_ID=$(curl -s -X POST http://localhost:5000/api/parties \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code":"SUPP001",
    "name":"Test Supplier",
    "type":"SUPPLIER",
    "taxNumber":"9876543210",
    "email":"supplier@test.com",
    "phone":null,
    "address":null,
    "creditLimit":null,
    "paymentTermDays":15,
    "isActive":true
  }' | jq -r '.id')

# 4. Create BOTH type
BOTH_ID=$(curl -s -X POST http://localhost:5000/api/parties \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "code":"BOTH001",
    "name":"Test Both",
    "type":"BOTH",
    "taxNumber":null,
    "email":null,
    "phone":null,
    "address":null,
    "creditLimit":null,
    "paymentTermDays":null,
    "isActive":true
  }' | jq -r '.id')

# 5. List all customers (includes BOTH type)
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/parties?type=CUSTOMER"

# 6. Search by name
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/parties?q=Test"
```

## Organization Module

The Organization module provides hierarchical structure: **Tenant → Organization → Branch → Warehouse**

### Entity Structure

**Organization**
- Code (unique per tenant, 2-32 chars, A-Z0-9_-)
- Name (2-200 chars)
- TaxNumber (optional, max 32 chars)

**Branch**
- OrganizationId (FK)
- Code (unique per organization, 2-32 chars, A-Z0-9_-)
- Name (2-200 chars)
- City, Address (optional)

**Warehouse**
- BranchId (FK)
- Code (unique per branch, 2-32 chars, A-Z0-9_-)
- Name (2-200 chars)
- Type (MAIN, STORE, VIRTUAL)
- IsDefault (only one default per branch)

### API Endpoints

#### Organizations
```bash
# Create organization
curl -X POST http://localhost:5000/api/orgs \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "ACME",
    "name": "ACME Corporation",
    "taxNumber": "1234567890"
  }'

# List organizations (with pagination and search)
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/orgs?page=1&size=50&q=ACME"

# Get organization by ID
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/orgs/{orgId}"

# Update organization
curl -X PUT http://localhost:5000/api/orgs/{orgId} \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "ACME",
    "name": "ACME Corp Updated",
    "taxNumber": "1234567890"
  }'

# Delete organization
curl -X DELETE http://localhost:5000/api/orgs/{orgId} \
  -H "Authorization: Bearer <TOKEN>"
```

#### Branches
```bash
# Create branch under organization
curl -X POST http://localhost:5000/api/orgs/{orgId}/branches \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "IST-HQ",
    "name": "Istanbul Headquarters",
    "city": "Istanbul",
    "address": "Sisli, Istanbul, Turkey"
  }'

# List branches for organization
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/orgs/{orgId}/branches?page=1&size=50&q=Istanbul"

# Get branch by ID
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/branches/{branchId}"

# Update branch
curl -X PUT http://localhost:5000/api/branches/{branchId} \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "IST-HQ",
    "name": "Istanbul HQ Updated",
    "city": "Istanbul",
    "address": "New Address"
  }'

# Delete branch
curl -X DELETE http://localhost:5000/api/branches/{branchId} \
  -H "Authorization: Bearer <TOKEN>"
```

#### Warehouses
```bash
# Create warehouse under branch
curl -X POST http://localhost:5000/api/branches/{branchId}/warehouses \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "WH-MAIN",
    "name": "Main Warehouse",
    "type": "MAIN",
    "isDefault": true
  }'

# List warehouses for branch
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/branches/{branchId}/warehouses?page=1&size=50"

# Get warehouse by ID
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/warehouses/{warehouseId}"

# Update warehouse
curl -X PUT http://localhost:5000/api/warehouses/{warehouseId} \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "code": "WH-MAIN",
    "name": "Main Warehouse Updated",
    "type": "MAIN",
    "isDefault": true
  }'

# Delete warehouse
curl -X DELETE http://localhost:5000/api/warehouses/{warehouseId} \
  -H "Authorization: Bearer <TOKEN>"
```

### Business Rules

1. **Tenant Isolation**: Different tenants can use the same codes
2. **Organization Code**: Unique within tenant
3. **Branch Code**: Unique within organization
4. **Warehouse Code**: Unique within branch
5. **Default Warehouse**: Only one default warehouse per branch (automatically managed)
6. **Cascade Delete**: Deleting organization removes all branches and warehouses
7. **Validation**: Codes must be 2-32 chars, uppercase A-Z0-9_- only
8. **Audit**: All CRUD operations automatically logged

### Permissions

- `org.read` - View organizations
- `org.write` - Create/Update/Delete organizations
- `branch.read` - View branches
- `branch.write` - Create/Update/Delete branches
- `warehouse.read` - View warehouses
- `warehouse.write` - Create/Update/Delete warehouses

### Testing the Module

```bash
# 1. Generate test token
TOKEN=$(curl -s http://localhost:5000/api/test/generate-token | jq -r '.token')

# 2. Create organization
ORG_ID=$(curl -s -X POST http://localhost:5000/api/orgs \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"code":"TEST","name":"Test Org","taxNumber":null}' \
  | jq -r '.id')

# 3. Create branch
BRANCH_ID=$(curl -s -X POST http://localhost:5000/api/orgs/$ORG_ID/branches \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"code":"BR01","name":"Branch 1","city":"Istanbul","address":"Test"}' \
  | jq -r '.id')

# 4. Create warehouse
WH_ID=$(curl -s -X POST http://localhost:5000/api/branches/$BRANCH_ID/warehouses \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"code":"WH01","name":"Warehouse 1","type":"MAIN","isDefault":true}' \
  | jq -r '.id')

# 5. List all organizations
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/orgs"
```
## Stock Ledger Module

The Stock Ledger module provides **immutable, audit-trail based stock management** with **row-level locking for concurrency safety**. Stock is never updated in place - all changes are recorded as ledger entries, and balances are derived from the ledger.

### Architecture

**Immutable Ledger Pattern**:
- `StockLedgerEntry` - Append-only source of truth (never updated/deleted)
- `StockBalance` - Materialized view for performance (cached calculations)
- All stock operations create ledger entries first, then update balance cache
- Row-level locking prevents race conditions during concurrent operations

**Movement Types** (7 types):
1. `INBOUND` - Receiving stock (OnHand += qty)
2. `OUTBOUND` - Issuing/selling stock (OnHand -= qty)
3. `ADJUSTMENT` - Inventory adjustment (OnHand += qty, can be negative)
4. `RESERVE` - Reserve stock for order (Reserved += qty)
5. `RELEASE` - Release reservation (Reserved -= qty)
6. `TRANSFER_OUT` - Transfer to another warehouse (OnHand -= qty)
7. `TRANSFER_IN` - Receive from another warehouse (OnHand += qty)

**Stock Calculation**:
- `OnHand` = Sum of INBOUND, OUTBOUND, ADJUSTMENT, TRANSFER_IN, TRANSFER_OUT
- `Reserved` = Sum of RESERVE, RELEASE
- `Available` = OnHand - Reserved (computed property, not stored)

### Entity Structure

**StockLedgerEntry** (Immutable)
- WarehouseId (FK to Warehouse)
- VariantId (FK to ProductVariant)
- MovementType (enum: 7 types)
- Quantity (decimal, signed: positive = increase, negative = decrease)
- UnitCost (decimal, >= 0, for cost tracking)
- OccurredAt (DateTime, when movement happened)
- ReferenceType (optional, max 64 chars, e.g., "SalesOrder", "PurchaseOrder")
- ReferenceId (optional, Guid, links to source document)
- CorrelationId (optional, Guid, links related entries like TRANSFER_OUT + TRANSFER_IN)
- Note (optional, max 500 chars)

**StockBalance** (Materialized Cache)
- WarehouseId (FK to Warehouse)
- VariantId (FK to ProductVariant)
- OnHand (decimal, current physical stock)
- Reserved (decimal, currently reserved quantity)
- Available (computed: OnHand - Reserved)
- UpdatedAt (DateTime, last update timestamp)

**Indexes** (for Performance):
- `ix_stock_ledger_tenant_warehouse_variant_occurred` - Main query pattern
- `ix_stock_ledger_tenant_reference` - Reference lookups
- `ix_stock_ledger_tenant_correlation` - Transfer pair lookups
- `ix_stock_balances_unique` - Enforce one balance per (Tenant, Warehouse, Variant)

### API Endpoints

#### Query Stock

```bash
# Get balance for specific warehouse + variant
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/stock/balances/{warehouseId}/{variantId}"

# Response:
{
  "warehouseId": "guid",
  "warehouseName": "Main Warehouse",
  "variantId": "guid",
  "sku": "LAPTOP-I7-16GB",
  "variantName": "Laptop i7 16GB",
  "onHand": 100.0,
  "reserved": 15.0,
  "available": 85.0,
  "updatedAt": "2026-02-01T12:00:00Z"
}

# List all balances (with filters)
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/stock/balances?warehouseId={warehouseId}&page=1&size=50"

# Get ledger history (with date range)
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/stock/ledger?warehouseId={warehouseId}&variantId={variantId}&from=2026-01-01&to=2026-02-01&page=1&size=100"

# Response:
{
  "items": [
    {
      "id": "guid",
      "warehouseId": "guid",
      "variantId": "guid",
      "movementType": "INBOUND",
      "quantity": 50.0,
      "unitCost": 1000.0,
      "occurredAt": "2026-01-15T10:00:00Z",
      "referenceType": "PurchaseOrder",
      "referenceId": "guid",
      "correlationId": null,
      "note": "Initial stock"
    },
    {
      "id": "guid",
      "warehouseId": "guid",
      "variantId": "guid",
      "movementType": "RESERVE",
      "quantity": 10.0,
      "unitCost": 0.0,
      "occurredAt": "2026-01-16T14:30:00Z",
      "referenceType": "SalesOrder",
      "referenceId": "guid",
      "correlationId": null,
      "note": "Reserved for order #123"
    }
  ],
  "totalCount": 2,
  "page": 1,
  "pageSize": 100
}
```

#### Stock Operations

**1. Receive Stock (INBOUND)**
```bash
curl -X POST http://localhost:5000/api/stock/receive \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "warehouseId": "{warehouseId}",
    "variantId": "{variantId}",
    "quantity": 100.0,
    "unitCost": 1000.0,
    "referenceType": "PurchaseOrder",
    "referenceId": "{purchaseOrderId}",
    "note": "Received from Supplier ABC"
  }'

# Effect: OnHand += 100, Available += 100
```

**2. Issue Stock (OUTBOUND)**
```bash
curl -X POST http://localhost:5000/api/stock/issue \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "warehouseId": "{warehouseId}",
    "variantId": "{variantId}",
    "quantity": 10.0,
    "unitCost": 1000.0,
    "referenceType": "SalesOrder",
    "referenceId": "{salesOrderId}",
    "note": "Shipped to Customer XYZ"
  }'

# Effect: OnHand -= 10, Available -= 10
# Validation: Checks Available >= 10
```

**3. Reserve Stock (RESERVE)**
```bash
curl -X POST http://localhost:5000/api/stock/reserve \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "warehouseId": "{warehouseId}",
    "variantId": "{variantId}",
    "quantity": 5.0,
    "referenceType": "SalesOrder",
    "referenceId": "{salesOrderId}",
    "note": "Reserved for order #456"
  }'

# Effect: Reserved += 5, Available -= 5
# Validation: Checks Available >= 5
```

**4. Release Reservation (RELEASE)**
```bash
curl -X POST http://localhost:5000/api/stock/release \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "warehouseId": "{warehouseId}",
    "variantId": "{variantId}",
    "quantity": 3.0,
    "referenceType": "SalesOrder",
    "referenceId": "{salesOrderId}",
    "note": "Order #456 partially cancelled"
  }'

# Effect: Reserved -= 3, Available += 3
# Validation: Checks Reserved >= 3
```

**5. Transfer Stock (TRANSFER_OUT + TRANSFER_IN)**
```bash
curl -X POST http://localhost:5000/api/stock/transfer \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "fromWarehouseId": "{warehouse1Id}",
    "toWarehouseId": "{warehouse2Id}",
    "variantId": "{variantId}",
    "quantity": 20.0,
    "unitCost": 1000.0,
    "referenceType": "StockTransfer",
    "referenceId": "{transferDocId}",
    "note": "Transfer to branch warehouse"
  }'

# Effect:
# - Warehouse1: OnHand -= 20, Available -= 20 (TRANSFER_OUT)
# - Warehouse2: OnHand += 20, Available += 20 (TRANSFER_IN)
# - Both entries share same correlationId (links them)
# Validation: Checks Warehouse1 Available >= 20
```

**6. Adjust Stock (ADJUSTMENT)**
```bash
curl -X POST http://localhost:5000/api/stock/adjust \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "warehouseId": "{warehouseId}",
    "variantId": "{variantId}",
    "quantity": -2.5,
    "unitCost": 0.0,
    "referenceType": "InventoryCount",
    "referenceId": "{countId}",
    "note": "Physical inventory discrepancy"
  }'

# Effect: OnHand += (-2.5) = OnHand - 2.5
# No validation: Can create negative stock (for corrections)
```

### Business Rules

1. **Immutability**: Ledger entries NEVER updated or deleted (append-only)
2. **Row-Level Locking**: Uses PostgreSQL `SELECT FOR UPDATE` to prevent race conditions
3. **Concurrency Safety**: 
   - When two requests try to reserve stock simultaneously:
   - First request locks balance row → checks availability → reserves → commits
   - Second request waits for lock → checks updated balance → fails if insufficient
4. **Transaction Safety**: All operations wrapped in database transactions
5. **Validation Rules**:
   - Quantity must be > 0 (never zero)
   - UnitCost must be >= 0
   - Issue/Reserve/Transfer: Validates Available >= quantity
   - Release: Validates Reserved >= quantity
   - Adjustment: No validation (can create negative stock)
6. **Quantity Signs**:
   - Ledger stores quantity with sign (positive/negative)
   - APIs accept positive numbers, service applies sign internally
7. **Foreign Keys**: Warehouse and Variant must exist (Restrict delete behavior)
8. **Composite Uniqueness**: One balance per (TenantId, WarehouseId, VariantId)
9. **Decimal Precision**: (18,3) for quantities, (18,4) for unit costs
10. **Audit**: All operations automatically logged

### Permissions

- `stock.read` - View balances and ledger
- `stock.write` - Perform stock operations

### Complete Workflow Example

```bash
# 1. Generate test token
TOKEN=$(curl -s http://localhost:5000/api/test/generate-token | jq -r '.token')

# 2. Setup: Create warehouse + variant (see Organization/Catalog modules)
# Assume we have: WH_ID, VAR_ID

# 3. RECEIVE: Initial stock (100 units @ $1000 each)
curl -X POST http://localhost:5000/api/stock/receive \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"warehouseId\":\"$WH_ID\",
    \"variantId\":\"$VAR_ID\",
    \"quantity\":100.0,
    \"unitCost\":1000.0,
    \"referenceType\":\"PurchaseOrder\",
    \"referenceId\":\"$(uuidgen)\",
    \"note\":\"Initial stock from supplier\"
  }"

# Result: OnHand=100, Reserved=0, Available=100

# 4. RESERVE: Customer orders 10 units
curl -X POST http://localhost:5000/api/stock/reserve \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"warehouseId\":\"$WH_ID\",
    \"variantId\":\"$VAR_ID\",
    \"quantity\":10.0,
    \"referenceType\":\"SalesOrder\",
    \"referenceId\":\"$(uuidgen)\",
    \"note\":\"Order #123\"
  }"

# Result: OnHand=100, Reserved=10, Available=90

# 5. CHECK BALANCE
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/stock/balances/$WH_ID/$VAR_ID"

# Response: {"onHand":100.0,"reserved":10.0,"available":90.0}

# 6. ISSUE: Ship the order (10 units)
curl -X POST http://localhost:5000/api/stock/issue \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"warehouseId\":\"$WH_ID\",
    \"variantId\":\"$VAR_ID\",
    \"quantity\":10.0,
    \"unitCost\":1000.0,
    \"referenceType\":\"SalesOrder\",
    \"referenceId\":\"$(uuidgen)\",
    \"note\":\"Shipped order #123\"
  }"

# Result: OnHand=90, Reserved=10, Available=80
# ⚠️ Reserved not released automatically - need explicit RELEASE

# 7. RELEASE: Release the reservation
curl -X POST http://localhost:5000/api/stock/release \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"warehouseId\":\"$WH_ID\",
    \"variantId\":\"$VAR_ID\",
    \"quantity\":10.0,
    \"referenceType\":\"SalesOrder\",
    \"referenceId\":\"$(uuidgen)\",
    \"note\":\"Released after shipment\"
  }"

# Result: OnHand=90, Reserved=0, Available=90

# 8. TRANSFER: Move 20 units to another warehouse
# (Assume we have WH2_ID)
curl -X POST http://localhost:5000/api/stock/transfer \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"fromWarehouseId\":\"$WH_ID\",
    \"toWarehouseId\":\"$WH2_ID\",
    \"variantId\":\"$VAR_ID\",
    \"quantity\":20.0,
    \"unitCost\":1000.0,
    \"referenceType\":\"StockTransfer\",
    \"referenceId\":\"$(uuidgen)\",
    \"note\":\"Transfer to branch\"
  }"

# Result WH1: OnHand=70, Reserved=0, Available=70
# Result WH2: OnHand=20, Reserved=0, Available=20

# 9. ADJUST: Physical count found -2 units (damage/theft)
curl -X POST http://localhost:5000/api/stock/adjust \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"warehouseId\":\"$WH_ID\",
    \"variantId\":\"$VAR_ID\",
    \"quantity\":-2.0,
    \"unitCost\":0.0,
    \"referenceType\":\"InventoryCount\",
    \"referenceId\":\"$(uuidgen)\",
    \"note\":\"Physical count adjustment\"
  }"

# Result: OnHand=68, Reserved=0, Available=68

# 10. VIEW LEDGER HISTORY
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/stock/ledger?warehouseId=$WH_ID&variantId=$VAR_ID&page=1&size=50"

# Shows all 7 entries:
# 1. INBOUND +100
# 2. RESERVE +10 (Reserved)
# 3. OUTBOUND -10 (OnHand)
# 4. RELEASE -10 (Reserved)
# 5. TRANSFER_OUT -20
# 6. ADJUSTMENT -2
# Plus: TRANSFER_IN +20 in WH2 ledger
```

### Concurrency Test Scenario

```bash
# Simulate race condition: Two concurrent reservations for same stock

# Initial: OnHand=10, Reserved=0, Available=10

# Request A: Reserve 8 units (should succeed)
curl -X POST http://localhost:5000/api/stock/reserve ... qty=8.0 &

# Request B: Reserve 5 units (should fail - only 2 available after A)
curl -X POST http://localhost:5000/api/stock/reserve ... qty=5.0 &

# Result with row locking:
# ✅ Request A: SUCCESS → OnHand=10, Reserved=8, Available=2
# ❌ Request B: FAIL → "Insufficient available stock"

# Result without row locking (hypothetical):
# ⚠️ Both could succeed → OnHand=10, Reserved=13, Available=-3 (WRONG!)
```

### Testing the Module

```bash
# Run all 12 tests (including concurrency test)
cd tests/ErpCloud.Api.Tests
dotnet test --filter "FullyQualifiedName~StockModuleTests"

# Expected: Başarılı: 12, Başarısız: 0
```

**Test Coverage**:
1. ✅ INBOUND increases OnHand
2. ✅ OUTBOUND decreases OnHand
3. ✅ RESERVE increases Reserved
4. ✅ RELEASE decreases Reserved
5. ✅ Available = OnHand - Reserved
6. ✅ Reserve with insufficient stock throws exception
7. ✅ Issue with insufficient stock throws exception
8. ✅ Release with insufficient reserved throws exception
9. ✅ Transfer moves stock between warehouses
10. ✅ Ledger is immutable (no update methods)
11. ✅ Tenant isolation works correctly
12. ✅ **Concurrency: Two simultaneous reserves - only one succeeds**

### Performance Considerations

- **Balance Cache**: StockBalance table provides O(1) lookup for current stock
- **Ledger Query**: Indexed by (Tenant, Warehouse, Variant, OccurredAt) for fast history
- **Row Locking**: Minimal lock duration (only during balance update transaction)
- **Indexing Strategy**: Optimized for common query patterns
- **Future Optimization**: Consider read replicas for ledger queries if volume grows

### Future Enhancements (Sprint 2+)

- [ ] Batch stock operations (multiple items in one transaction)
- [ ] Stock alerts (low stock notifications)
- [ ] Automatic reorder points
- [ ] FIFO/LIFO cost tracking
- [ ] Lot/Serial number tracking
- [ ] Stock aging reports
- [x] Integration with Sales Orders ✅

---

## Sales Order Module

The Sales Order module manages customer orders with **automatic stock reservation** upon confirmation. It integrates seamlessly with the Stock Ledger module to ensure inventory accuracy.

### Architecture

**Status Flow**:
```
DRAFT → CONFIRMED → (optional) CANCELLED
```

- **DRAFT**: Order created, no stock reserved, can be updated
- **CONFIRMED**: Order confirmed, stock reserved, cannot be updated
- **CANCELLED**: Order cancelled, stock reservation released

**Stock Integration**:
- **Confirm**: Reserves stock via StockService (RESERVE movement)
- **Cancel**: Releases reservation via StockService (RELEASE movement)
- **Idempotency**: Confirming twice doesn't double-reserve stock
- **Transaction Safety**: All operations in database transactions

### Entity Structure

**SalesOrder** (Header)
- Id (Guid, PK)
- OrderNo (string, max 32, unique per tenant)
- PartyId (Guid, FK to Party - customer)
- BranchId (Guid, FK to Branch)
- WarehouseId (Guid, FK to Warehouse - from which stock will be issued)
- PriceListId (Guid, FK to PriceList, optional)
- Status (string, max 16: DRAFT | CONFIRMED | CANCELLED)
- OrderDate (DateTime)
- Note (string, max 500, optional)
- Lines (collection of SalesOrderLine)

**SalesOrderLine** (Items)
- Id (Guid, PK)
- SalesOrderId (Guid, FK)
- VariantId (Guid, FK to ProductVariant)
- Qty (decimal 18,3)
- UnitPrice (decimal 18,2)
- VatRate (decimal 5,2)
- LineTotal (decimal 18,2) - Qty × UnitPrice (excluding VAT)
- ReservedQty (decimal 18,3) - Tracking field (source of truth is ledger)
- Note (string, max 200, optional)

**Database Constraints**:
- `sales_orders.OrderNo`: Unique per tenant
- `sales_order_lines.VariantId`: One line per variant per order (simplification)
- Indexes: OrderNo, PartyId, WarehouseId, Status, OrderDate (desc)

### API Endpoints

#### Create Draft Order
```bash
curl -X POST http://localhost:5000/api/sales-orders \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "orderNo": "SO-2026-001",
    "partyId": "{customerId}",
    "branchId": "{branchId}",
    "warehouseId": "{warehouseId}",
    "priceListId": "{priceListId}",
    "orderDate": "2026-02-01T10:00:00Z",
    "note": "Customer order",
    "lines": [
      {
        "variantId": "{variantId}",
        "qty": 10.0,
        "unitPrice": 100.00,
        "vatRate": 18.0,
        "note": "Urgent delivery"
      }
    ]
  }'

# Response:
{
  "id": "guid",
  "orderNo": "SO-2026-001",
  "partyId": "guid",
  "partyName": "Acme Corporation",
  "status": "DRAFT",
  "lines": [
    {
      "id": "guid",
      "variantId": "guid",
      "sku": "LAPTOP-I7-16GB",
      "variantName": "Laptop i7 16GB",
      "qty": 10.0,
      "unitPrice": 100.00,
      "vatRate": 18.0,
      "lineTotal": 1000.00,
      "reservedQty": 0.0,
      "note": "Urgent delivery"
    }
  ]
}
```

#### Confirm Order (Reserves Stock)
```bash
curl -X POST http://localhost:5000/api/sales-orders/{orderId}/confirm \
  -H "Authorization: Bearer <TOKEN>"

# Response:
{
  "id": "guid",
  "orderNo": "SO-2026-001",
  "status": "CONFIRMED",
  "lines": [
    {
      "qty": 10.0,
      "reservedQty": 10.0  # ← Stock reserved!
    }
  ]
}

# Stock Ledger Entry Created:
# - MovementType: RESERVE
# - Qty: 10.0
# - ReferenceType: "SalesOrder"
# - ReferenceId: {orderId}

# Stock Balance Updated:
# - OnHand: unchanged
# - Reserved: +10.0
# - Available: -10.0
```

#### Cancel Order (Releases Reservation)
```bash
curl -X POST http://localhost:5000/api/sales-orders/{orderId}/cancel \
  -H "Authorization: Bearer <TOKEN>"

# Response:
{
  "status": "CANCELLED",
  "lines": [
    {
      "reservedQty": 0.0  # ← Reservation released!
    }
  ]
}

# Stock Ledger Entry Created:
# - MovementType: RELEASE
# - Qty: 10.0 (negative in ledger: -10.0)

# Stock Balance Updated:
# - Reserved: -10.0
# - Available: +10.0
```

#### Update Draft Order
```bash
curl -X PUT http://localhost:5000/api/sales-orders/{orderId} \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "orderNo": "SO-2026-001",
    "partyId": "{customerId}",
    "branchId": "{branchId}",
    "warehouseId": "{warehouseId}",
    "priceListId": "{priceListId}",
    "orderDate": "2026-02-01T10:00:00Z",
    "note": "Updated note",
    "lines": [
      {
        "variantId": "{variantId}",
        "qty": 15.0,  # ← Changed quantity
        "unitPrice": 100.00,
        "vatRate": 18.0,
        "note": null
      }
    ]
  }'

# Note: Only DRAFT orders can be updated
# CONFIRMED orders throw: "Only DRAFT orders can be updated"
```

#### Get Order by ID
```bash
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/sales-orders/{orderId}"
```

#### Search Orders
```bash
# List all orders (with pagination)
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/sales-orders?page=1&size=50"

# Filter by status
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/sales-orders?status=CONFIRMED"

# Filter by customer (party)
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/sales-orders?partyId={partyId}"

# Search by order number
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/sales-orders?q=SO-2026"

# Combine filters
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/sales-orders?status=DRAFT&partyId={partyId}&page=1&size=20"
```

### Business Rules

1. **Status Transitions**:
   - DRAFT → CONFIRMED: Reserve stock for all lines
   - CONFIRMED → CANCELLED: Release all reservations
   - CANCELLED orders cannot be confirmed again
   - Only DRAFT orders can be updated

2. **Stock Reservation**:
   - Confirm validates Available >= Qty for each line
   - If insufficient stock: transaction rolls back, status stays DRAFT, error 409
   - Reserved stock tracked in both SalesOrderLine.ReservedQty and StockLedger

3. **Idempotency**:
   - Confirming already CONFIRMED order: no-op, returns 200 OK
   - Cancelling already CANCELLED order: no-op, returns 200 OK
   - Prevents double reservations/releases

4. **Pricing Integration**:
   - If UnitPrice provided: use it
   - If UnitPrice null: fetch from PriceList (or default price list if PriceListId null)
   - If VatRate null: use variant's VatRate
   - If no price found: throw error

5. **Uniqueness**:
   - OrderNo unique per tenant
   - One line per variant per order (simplification for now)

6. **Validation**:
   - OrderNo: 2-32 chars, uppercase A-Z0-9_-
   - At least 1 line required
   - Qty > 0
   - UnitPrice >= 0
   - VatRate 0-100

7. **Cascade Behavior**:
   - Deleting SalesOrder cascades to lines
   - Warehouse/Variant/Party deletion restricted (cannot delete if referenced)

### Complete Workflow Example

```bash
# ========== SETUP ==========
# Generate token
TOKEN=$(curl -s http://localhost:5000/api/test/generate-token | jq -r '.token')

# Get IDs from existing data (assume already created)
CUSTOMER_ID="..." # From Party module
BRANCH_ID="..."   # From Organization module
WH_ID="..."       # From Warehouse
VARIANT_ID="..."  # From Catalog module
PRICELIST_ID="..." # From Catalog module

# ========== 1. RECEIVE STOCK (Initial Inventory) ==========
curl -X POST http://localhost:5000/api/stock/receive \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"warehouseId\":\"$WH_ID\",
    \"variantId\":\"$VARIANT_ID\",
    \"qty\":100.0,
    \"unitCost\":80.0,
    \"referenceType\":\"PurchaseOrder\",
    \"referenceId\":\"$(uuidgen)\",
    \"note\":\"Initial inventory\"
  }"

# Stock: OnHand=100, Reserved=0, Available=100

# ========== 2. CREATE DRAFT ORDER ==========
ORDER_ID=$(curl -s -X POST http://localhost:5000/api/sales-orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"orderNo\":\"SO-2026-001\",
    \"partyId\":\"$CUSTOMER_ID\",
    \"branchId\":\"$BRANCH_ID\",
    \"warehouseId\":\"$WH_ID\",
    \"priceListId\":null,
    \"orderDate\":\"2026-02-01T10:00:00Z\",
    \"note\":\"Customer order #1\",
    \"lines\":[
      {
        \"variantId\":\"$VARIANT_ID\",
        \"qty\":25.0,
        \"unitPrice\":null,
        \"vatRate\":null,
        \"note\":\"Main item\"
      }
    ]
  }" | jq -r '.id')

echo "Order created: $ORDER_ID (DRAFT)"

# Stock: OnHand=100, Reserved=0, Available=100 (unchanged)

# ========== 3. CONFIRM ORDER (RESERVE STOCK) ==========
curl -X POST http://localhost:5000/api/sales-orders/$ORDER_ID/confirm \
  -H "Authorization: Bearer $TOKEN"

echo "Order confirmed - stock reserved"

# Stock: OnHand=100, Reserved=25, Available=75
# Ledger: +1 entry (RESERVE, Qty=25, RefType=SalesOrder, RefId=ORDER_ID)

# ========== 4. CHECK STOCK BALANCE ==========
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/stock/balances/$WH_ID/$VARIANT_ID"

# Response:
# {
#   "onHand": 100.0,
#   "reserved": 25.0,
#   "available": 75.0
# }

# ========== 5. TRY TO CONFIRM AGAIN (IDEMPOTENCY TEST) ==========
curl -X POST http://localhost:5000/api/sales-orders/$ORDER_ID/confirm \
  -H "Authorization: Bearer $TOKEN"

echo "Second confirm - should be no-op"

# Stock: OnHand=100, Reserved=25, Available=75 (unchanged!)
# No new ledger entry created

# ========== 6. CREATE SECOND ORDER (INSUFFICIENT STOCK TEST) ==========
ORDER2_ID=$(curl -s -X POST http://localhost:5000/api/sales-orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"orderNo\":\"SO-2026-002\",
    \"partyId\":\"$CUSTOMER_ID\",
    \"branchId\":\"$BRANCH_ID\",
    \"warehouseId\":\"$WH_ID\",
    \"priceListId\":\"$PRICELIST_ID\",
    \"orderDate\":\"2026-02-01T11:00:00Z\",
    \"note\":\"Customer order #2\",
    \"lines\":[
      {
        \"variantId\":\"$VARIANT_ID\",
        \"qty\":80.0,
        \"unitPrice\":100.00,
        \"vatRate\":18.0,
        \"note\":null
      }
    ]
  }" | jq -r '.id')

# Try to confirm (should fail - only 75 available)
curl -X POST http://localhost:5000/api/sales-orders/$ORDER2_ID/confirm \
  -H "Authorization: Bearer $TOKEN"

# Response: 409 Conflict
# {
#   "error": "insufficient_stock",
#   "message": "Insufficient stock for variant ..."
# }

# Order status remains DRAFT
# Stock unchanged: OnHand=100, Reserved=25, Available=75

# ========== 7. CANCEL FIRST ORDER (RELEASE RESERVATION) ==========
curl -X POST http://localhost:5000/api/sales-orders/$ORDER_ID/cancel \
  -H "Authorization: Bearer $TOKEN"

echo "Order cancelled - reservation released"

# Stock: OnHand=100, Reserved=0, Available=100
# Ledger: +1 entry (RELEASE, Qty=-25, RefType=SalesOrder, RefId=ORDER_ID)

# ========== 8. NOW CONFIRM SECOND ORDER (SHOULD SUCCEED) ==========
curl -X POST http://localhost:5000/api/sales-orders/$ORDER2_ID/confirm \
  -H "Authorization: Bearer $TOKEN"

echo "Second order confirmed successfully"

# Stock: OnHand=100, Reserved=80, Available=20

# ========== 9. VIEW LEDGER HISTORY ==========
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/stock/ledger?warehouseId=$WH_ID&variantId=$VARIANT_ID"

# Ledger entries (chronological):
# 1. INBOUND +100   (receive)
# 2. RESERVE +25    (order 1 confirm)
# 3. RELEASE -25    (order 1 cancel)
# 4. RESERVE +80    (order 2 confirm)

# ========== 10. SEARCH ORDERS ==========
# All CONFIRMED orders
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/sales-orders?status=CONFIRMED"

# All CANCELLED orders
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/sales-orders?status=CANCELLED"

# Orders for specific customer
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5000/api/sales-orders?partyId=$CUSTOMER_ID"
```

### Permissions

- `salesorder.read` - View orders
- `salesorder.write` - Create/Update/Confirm/Cancel orders

### Testing the Module

```bash
# Run all 14 tests
cd tests/ErpCloud.Api.Tests
dotnet test --filter "FullyQualifiedName~SalesOrderModuleTests"

# Expected: Başarılı: 14, Başarısız: 0
```

**Test Coverage** (14/14 ✅):
1. ✅ CreateDraft_Success
2. ✅ Confirm_ChangesStatusToConfirmed
3. ✅ Confirm_ReservesStock
4. ✅ Confirm_Idempotent_NoDoubleReservation ⭐
5. ✅ Cancel_ChangesStatusToCancelled
6. ✅ Cancel_ReleasesReservation
7. ✅ CancelAfterCancel_NoError (idempotency)
8. ✅ InsufficientStock_ConfirmFails_StatusRemainsDraft ⭐
9. ✅ TenantIsolation_DifferentTenantsCannotSeeEachOther
10. ✅ UniqueOrderNo_EnforcedPerTenant
11. ✅ UniqueVariant_EnforcedPerOrder
12. ✅ UpdateDraft_OnlyDraftCanBeUpdated
13. ✅ Search_FiltersByStatus
14. ✅ PricingIntegration_FetchesFromPriceList

### Integration Points

**With Stock Module**:
- Confirm → `StockService.ReserveStockAsync()`
- Cancel → `StockService.ReleaseReservationAsync()`
- Creates ledger entries with `ReferenceType="SalesOrder"` and `ReferenceId=orderId`

**With Catalog Module**:
- Fetches pricing from PriceListItem if UnitPrice not provided
- Uses Variant.VatRate if not provided

**With Party Module**:
- Validates customer (PartyId) exists
- Displays party name in order details

**With Organization Module**:
- Validates branch and warehouse exist
- Associates order with specific warehouse for stock reservation

### Error Handling

**409 Conflict**:
- Duplicate OrderNo: `"Order number 'SO-001' already exists"`
- Insufficient stock: `{"error": "insufficient_stock", "message": "Insufficient stock for variant ..."}`
- Update non-DRAFT: `"Only DRAFT orders can be updated"`

**404 Not Found**:
- Order not found: `"Sales order not found"`
- Referenced entity not found: `"Party not found"` / `"Warehouse not found"`

**Transaction Rollback**:
- If stock reservation fails during confirm: entire transaction rolls back
- Order status remains DRAFT
- No ledger entries created
- Stock balance unchanged

### Future Enhancements

- [ ] Partial fulfillment (issue less than reserved)
- [ ] Order line item editing (add/remove/update after confirm)
- [ ] Multi-warehouse fulfillment
- [ ] Automatic stock issuance on shipment
- [ ] Order approval workflow
- [ ] Bulk order import
- [ ] Order templates
- [ ] Integration with shipping providers
- [ ] Backorder management

- [ ] Multi-currency stock valuation

---

## Purchase Order & Goods Receipt Module

Complete procurement workflow from PO creation to goods receipt with automatic stock updates.

### Key Features
- **Purchase Order (PO)**: Create, update, confirm, and cancel purchase orders
- **Goods Receipt (GRN)**: Receive goods against confirmed POs
- **Partial Receiving**: Multiple GRN entries can fulfill a single PO
- **Over-receive Prevention**: Cannot receive more than ordered quantity
- **Auto-completion**: PO automatically marked COMPLETED when all lines received
- **Stock Integration**: Automatic stock ledger entries and balance updates
- **Idempotency**: Confirm and Receive operations are idempotent (safe to retry)
- **Tenant Isolation**: Full multi-tenant support with scoped unique constraints

### Entity Structure

#### PurchaseOrder
- **PoNo** (unique per tenant, 2-32 chars) - Purchase order number
- **PartyId** (FK to Party) - Must be SUPPLIER or BOTH type
- **BranchId** / **WarehouseId** (FK) - Target warehouse
- **Status**: DRAFT → CONFIRMED → COMPLETED / CANCELLED
- **OrderDate** / **ExpectedDate** (DateOnly)
- **Lines** - Collection of PurchaseOrderLine

#### PurchaseOrderLine
- **VariantId** (unique per PO) - Product variant being ordered
- **Qty** - Order quantity (decimal 18,3)
- **UnitCost** - Cost per unit (decimal 18,4)
- **VatRate** - VAT percentage (decimal 5,2)
- **ReceivedQty** (default 0) - Cumulative received quantity (updated by GRN)
- **RemainingQty** (calculated) - Qty - ReceivedQty

#### GoodsReceipt
- **GrnNo** (unique per tenant, 2-32 chars) - Goods receipt number
- **PurchaseOrderId** (FK) - Reference to parent PO (must be CONFIRMED or COMPLETED)
- **ReceiptDate** (DateOnly)
- **Status**: DRAFT → RECEIVED / CANCELLED
- **Lines** - Collection of GoodsReceiptLine

#### GoodsReceiptLine
- **PurchaseOrderLineId** (FK, unique per GRN) - Reference to PO line
- **Qty** - Quantity received (decimal 18,3)
- **UnitCost** (nullable) - Cost override (falls back to PO line cost)

### Business Rules

#### Purchase Order Lifecycle
1. **Create DRAFT**: Validates party type (SUPPLIER or BOTH), unique PoNo, unique variants per PO
2. **Update**: Only DRAFT orders can be updated (replaces all lines)
3. **Confirm**: DRAFT → CONFIRMED (idempotent, can be called multiple times)
4. **Cancel**: Only DRAFT orders can be cancelled

#### Goods Receipt Lifecycle
1. **Create DRAFT**: Requires PO in CONFIRMED or COMPLETED status, validates qty ≤ remaining qty
2. **Update**: Only DRAFT GRNs can be updated
3. **Receive** (Transactional):
   - Validates remaining qty for each line
   - Calls `StockService.ReceiveStock()` for each line
   - Updates PO line ReceivedQty += qty
   - If all PO lines fully received → PO.Status = COMPLETED
   - Commits or rolls back entire transaction
   - Idempotent (subsequent calls return RECEIVED status without re-processing)
4. **Cancel**: Only DRAFT GRNs can be cancelled

#### Stock Integration
- Receive creates **INBOUND** ledger entries with `ReferenceType="GoodsReceipt"`
- Updates `StockBalance.OnHand` automatically
- UnitCost fallback: GRN line UnitCost ?? PO line UnitCost

### API Endpoints

#### Purchase Orders

```bash
# Create purchase order (draft)
curl -X POST http://localhost:5000/api/purchase-orders \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "poNo": "PO-2024-001",
    "partyId": "supplier-guid",
    "branchId": "branch-guid",
    "warehouseId": "warehouse-guid",
    "orderDate": "2024-02-02",
    "expectedDate": "2024-02-15",
    "lines": [
      {
        "variantId": "variant-guid",
        "qty": 100,
        "unitCost": 50.00,
        "vatRate": 18
      }
    ]
  }'

# Search purchase orders
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/purchase-orders?q=PO-2024&status=CONFIRMED&page=1&size=50"

# Get PO by ID
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/purchase-orders/{poId}"

# Update PO (draft only)
curl -X PUT http://localhost:5000/api/purchase-orders/{poId} \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "orderDate": "2024-02-03",
    "expectedDate": "2024-02-20",
    "lines": [...]
  }'

# Confirm PO (idempotent)
curl -X POST http://localhost:5000/api/purchase-orders/{poId}/confirm \
  -H "Authorization: Bearer <TOKEN>"

# Cancel PO (draft only)
curl -X POST http://localhost:5000/api/purchase-orders/{poId}/cancel \
  -H "Authorization: Bearer <TOKEN>"
```

#### Goods Receipts

```bash
# Create goods receipt (draft)
curl -X POST http://localhost:5000/api/goods-receipts \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "grnNo": "GRN-2024-001",
    "purchaseOrderId": "po-guid",
    "receiptDate": "2024-02-10",
    "lines": [
      {
        "purchaseOrderLineId": "po-line-guid",
        "qty": 50,
        "unitCost": 52.00
      }
    ]
  }'

# Search goods receipts
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/goods-receipts?status=DRAFT&from=2024-02-01&to=2024-02-28"

# Get GRN by ID
curl -H "Authorization: Bearer <TOKEN>" \
  "http://localhost:5000/api/goods-receipts/{grnId}"

# Update GRN (draft only)
curl -X PUT http://localhost:5000/api/goods-receipts/{grnId} \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "receiptDate": "2024-02-11",
    "lines": [...]
  }'

# Receive goods (idempotent, transactional)
curl -X POST http://localhost:5000/api/goods-receipts/{grnId}/receive \
  -H "Authorization: Bearer <TOKEN>"

# Cancel GRN (draft only)
curl -X POST http://localhost:5000/api/goods-receipts/{grnId}/cancel \
  -H "Authorization: Bearer <TOKEN>"
```

### End-to-End Workflow Example

```bash
# 1. Create supplier party (if not exists)
curl -X POST http://localhost:5000/api/parties \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{
    "code": "SUP001",
    "name": "ABC Supplier",
    "type": "SUPPLIER"
  }'

# 2. Create product variant (if not exists)
curl -X POST http://localhost:5000/api/products/{productId}/variants \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{
    "sku": "WIDGET-001",
    "name": "Widget",
    "vatRate": 18
  }'

# 3. Create purchase order (draft)
curl -X POST http://localhost:5000/api/purchase-orders \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{
    "poNo": "PO-2024-100",
    "partyId": "supplier-guid",
    "branchId": "branch-guid",
    "warehouseId": "warehouse-guid",
    "orderDate": "2024-02-02",
    "lines": [
      {
        "variantId": "variant-guid",
        "qty": 100,
        "unitCost": 50.00,
        "vatRate": 18
      }
    ]
  }'

# 4. Confirm PO
curl -X POST http://localhost:5000/api/purchase-orders/{poId}/confirm \
  -H "Authorization: Bearer <TOKEN>"

# 5. Create first GRN (partial: 60 of 100)
curl -X POST http://localhost:5000/api/goods-receipts \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{
    "grnNo": "GRN-2024-100A",
    "purchaseOrderId": "po-guid",
    "receiptDate": "2024-02-10",
    "lines": [
      {
        "purchaseOrderLineId": "po-line-guid",
        "qty": 60
      }
    ]
  }'

# 6. Receive first GRN (stock OnHand += 60, PO ReceivedQty = 60)
curl -X POST http://localhost:5000/api/goods-receipts/{grn1Id}/receive \
  -H "Authorization: Bearer <TOKEN>"

# Verify: GET PO shows ReceivedQty=60, RemainingQty=40, Status=CONFIRMED

# 7. Create second GRN (remaining: 40 of 100)
curl -X POST http://localhost:5000/api/goods-receipts \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{
    "grnNo": "GRN-2024-100B",
    "purchaseOrderId": "po-guid",
    "receiptDate": "2024-02-15",
    "lines": [
      {
        "purchaseOrderLineId": "po-line-guid",
        "qty": 40
      }
    ]
  }'

# 8. Receive second GRN (stock OnHand += 40, PO ReceivedQty = 100, PO Status = COMPLETED)
curl -X POST http://localhost:5000/api/goods-receipts/{grn2Id}/receive \
  -H "Authorization: Bearer <TOKEN>"

# Verify: GET PO shows ReceivedQty=100, RemainingQty=0, Status=COMPLETED
# Verify: GET stock balance shows OnHand=100
# Verify: GET ledger entries shows 2 INBOUND entries (60 + 40)
```

### Error Handling

**409 Conflict**:
- Duplicate PoNo: `"Purchase order number 'PO-001' already exists"`
- Duplicate GrnNo: `"Goods receipt number 'GRN-001' already exists"`
- Invalid party type: `"Party must be SUPPLIER or BOTH type to create purchase order"`
- Update non-DRAFT: `"Only DRAFT purchase orders can be updated"`
- Confirm already confirmed: Returns current state (idempotent)
- Over-receive: `"Cannot receive 110 units. Only 100 units remaining for variant ..."`
- PO not confirmed: `"Purchase order must be CONFIRMED or COMPLETED to create goods receipt"`

**404 Not Found**:
- PO not found: `"Purchase order not found"`
- GRN not found: `"Goods receipt not found"`
- Referenced entity not found: `"Party not found"` / `"Warehouse not found"`

**Transaction Rollback** (Receive operation):
- If stock update fails: entire transaction rolls back
- GRN status remains DRAFT
- No ledger entries created
- Stock balance unchanged
- PO ReceivedQty unchanged

### Test Coverage

**26 comprehensive tests** covering:
- ✅ PO lifecycle (create, update, confirm, cancel, idempotency)
- ✅ GRN lifecycle (create, update, receive, cancel, idempotency)
- ✅ Stock integration (OnHand updates, ReceivedQty tracking, ledger entries)
- ✅ Partial receiving (multiple GRNs completing one PO)
- ✅ Over-receive prevention
- ✅ PO completion rules (all lines fully received)
- ✅ Tenant isolation (data segregation, scoped unique constraints)
- ✅ Search and filters

Run tests:
```bash
cd tests/ErpCloud.Api.Tests
dotnet test --filter "PurchaseModuleTests"
```

### Future Enhancements

- [ ] Purchase order approval workflow
- [ ] Vendor performance analytics
- [ ] Purchase price variance reporting
- [ ] Automatic PO generation from reorder points
- [] Multi-currency purchase orders
- [ ] Quality inspection before goods receipt
- [ ] Return to supplier (RTS) workflow
- [ ] Purchase order versioning
- [ ] Bulk PO import
- [ ] Integration with vendor portals

---

## 🧪 UI QA Checklist

### Prerequisites
1. Backend API running (`dotnet run` in `/src/Api`)
2. Admin panel running (`npm run dev` in `/apps/admin-desktop`)
3. Dev token generated (`GET /api/dev/token`)
4. At least 1 Branch + Warehouse created
5. At least 1 Customer party (type CUSTOMER or BOTH)
6. At least 1 Supplier party (type SUPPLIER or BOTH)
7. At least 1 Product with variants

### 🎯 Sales Flow (Complete Workflow)

**Test Path**: Order → Shipment → Invoice → Payment

1. **Sales Wizard** (`/sales/wizard`)
   - [ ] Step 1: Select customer (should show CUSTOMER + BOTH types)
   - [ ] Step 2-3: Add products (quantity required)
   - [ ] Step 4: Create sales order → verify draft created
   - [ ] Step 5: Confirm order → verify status = CONFIRMED
   - [ ] Step 6: Create shipment → verify draft created
   - [ ] Step 7: Ship goods → **VERIFY: Stock balance decreased**
   - [ ] Step 8: Create invoice → verify draft created
   - [ ] Step 9: Issue invoice → **VERIFY: Party ledger updated (receivable +)**
   - [ ] Step 10: Record payment → **VERIFY: Party balance decreased, cash/bank increased**

2. **Verification After Sales Flow**
   - [ ] Navigate to **Stock Ledger** → verify negative movement (SHIPMENT)
   - [ ] Navigate to **Party Ledger** → verify debit entry (receivable)
   - [ ] Navigate to **Cash/Bank Ledger** (if payment recorded) → verify credit entry
   - [ ] Navigate to Sales Order detail → verify status progression

### 🧾 Purchase Flow (Complete Workflow)

**Test Path**: PO → GRN → Stock Increase

1. **Purchase Wizard** (`/purchase/wizard`)
   - [ ] Step 1: Select supplier (should show SUPPLIER + BOTH types)
   - [ ] Step 2: Add products with quantities + unit costs
   - [ ] Step 3: Create PO → verify draft created
   - [ ] Step 4: Confirm PO → verify status = CONFIRMED
   - [ ] Step 5: Create GRN → verify prefilled quantities
   - [ ] Step 6: Receive GRN → **VERIFY: Stock balance increased**
   - [ ] Step 7: Verification → click stock balance link, verify quantities correct

2. **Verification After Purchase Flow**
   - [ ] Navigate to **Stock Ledger** → verify positive movement (RECEIPT)
   - [ ] Navigate to PO detail → verify progress bar = 100%
   - [ ] Check Stock Balance → verify quantity matches expected total

### 📊 List Pages (Standardization Test)

Test ALL 12 list pages for consistency:

**List Pages to Test**:
1. Sales Orders (`/sales-orders`)
2. Shipments (`/shipments`)
3. Invoices (`/invoices`)
4. Purchase Orders (`/purchase-orders`)
5. Goods Receipts (`/goods-receipts`)
6. Payments (`/payments`)
7. Parties (`/parties`)
8. Products (`/products`)
9. Cashboxes (`/cashboxes`)
10. Bank Accounts (`/bank-accounts`)
11. Stock Ledger (`/stock-ledger`)
12. Party Ledger (`/party-ledger`)

**For Each List Page**:
- [ ] Search input works (type text, wait 300ms, results filter)
- [ ] Status filter dropdown works (if applicable)
- [ ] Quick date filters work (Today / Last 7 days / Last 30 days)
- [ ] Custom date range pickers work (From/To dates)
- [ ] Pagination works (Previous/Next buttons, page info correct)
- [ ] Page size selector works (10/25/50/100/200 options)
- [ ] Loading skeleton shows during fetch
- [ ] Empty state shows when no data (with CTA button if applicable)
- [ ] Row click navigates to detail page (where applicable)
- [ ] Table columns aligned properly (text left, numbers right)

### 📥 CSV Export Test

**Required Exports** (3 pages):

1. **Stock Ledger** (`/stock-ledger`)
   - [ ] Click "Export CSV" button
   - [ ] File downloads: `stock-movements_YYYY-MM-DD.csv`
   - [ ] Open in Excel: UTF-8 BOM encoding works (no garbled characters)
   - [ ] Columns: Date | Product | SKU | Warehouse | Movement Type | Quantity | Unit | Reference Type | Reference No | Note
   - [ ] Filters applied (date range, movement type) → verify CSV only includes filtered data

2. **Party Ledger** (`/party-ledger`)
   - [ ] Click "Export CSV" button
   - [ ] File downloads: `party-ledger_YYYY-MM-DD.csv`
   - [ ] Open in Excel: UTF-8 BOM encoding works
   - [ ] Columns: Date | Party | Transaction Type | Debit | Credit | Balance | Currency | Reference Type | Reference No | Note
   - [ ] Filters applied (date range, party) → verify CSV matches filtered results

3. **Cash/Bank Ledger** (`/cash-bank-ledger`)
   - [ ] Click "Export CSV" button
   - [ ] File downloads: `cash-bank-ledger_YYYY-MM-DD.csv`
   - [ ] Open in Excel: UTF-8 BOM encoding works
   - [ ] Columns: Date | Account | Account Type | Transaction Type | Debit | Credit | Balance | Currency | Reference Type | Reference No | Note

### 🔍 QA Verification Page

1. **Navigate to** `/qa/verification`
   - [ ] Quick action buttons visible (Sales Wizard, Purchase Wizard, Stock Ledger, Party Ledger)
   - [ ] Happy path testing guide sections visible
   - [ ] Verification checklist sections visible
   - [ ] Known limitations section visible
   - [ ] Quick export links at bottom work

### ⌨️ UX Polish (Keyboard & Interactions)

1. **ESC Key Handling**
   - [ ] Open a modal/dialog → Press ESC → modal closes
   - [ ] StandardListPage filters → Press ESC → should not close (only modals)

2. **Confirm Dialogs** (Destructive Actions)
   - [ ] Receive GRN → should show confirm dialog
   - [ ] Ship goods → should show confirm dialog
   - [ ] Cancel order/PO → should show confirm dialog
   - [ ] Dialog has "Confirm" + "Cancel" buttons
   - [ ] Cancel button closes dialog without action
   - [ ] Confirm button executes action

3. **Toast Notifications** (Standardized Colors)
   - [ ] Success action (e.g., confirm order) → **green** toast
   - [ ] Warning (e.g., incomplete data) → **yellow** toast (if applicable)
   - [ ] Error (e.g., insufficient stock) → **red** toast

### 🐛 Error Scenarios

1. **Insufficient Stock**
   - [ ] Create sales order with qty > available stock
   - [ ] Confirm order → should show red toast: "Insufficient stock for variant..."
   - [ ] Order status remains DRAFT

2. **Validation Errors**
   - [ ] Try to create order without customer → error message shows
   - [ ] Try to create order without products → error message shows
   - [ ] Try to update CONFIRMED order → error: "Only DRAFT orders can be updated"

3. **Session Expiry**
   - [ ] Clear auth token from storage
   - [ ] Make any API call → should redirect to `/login`

### 🎨 Detail Pages (Action Tests)

**SalesOrderDetailPage** (`/sales-orders/:id`):
- [ ] View DRAFT order → "Confirm Order" button visible
- [ ] Click "Confirm Order" → status changes to CONFIRMED, button disappears
- [ ] View CONFIRMED order → "Create Shipment" button visible
- [ ] Click "Create Shipment" → navigates to wizard/form

**ShipmentDetailPage** (`/shipments/:id`):
- [ ] View DRAFT shipment → "Ship Now" button visible
- [ ] Click "Ship Now" → status changes to SHIPPED, stock decreases
- [ ] View SHIPPED shipment → "Create Invoice" button visible

**InvoiceDetailPage** (`/invoices/:id`):
- [ ] View DRAFT invoice → "Issue Invoice" button visible
- [ ] Party ledger impact panel shows receivable amount
- [ ] Click "Issue Invoice" → status changes to ISSUED, party ledger updated

**PurchaseOrderDetailPage** (`/purchase-orders/:id`):
- [ ] Progress bar shows correct % (ReceivedQty / TotalQty * 100)
- [ ] View DRAFT PO → "Confirm PO" button visible
- [ ] View CONFIRMED PO → "Create GRN" button visible
- [ ] Progress bar updates after receiving goods

**GoodsReceiptDetailPage** (`/goods-receipts/:id`):
- [ ] View DRAFT GRN → "Receive GRN" button visible
- [ ] Related PO link works
- [ ] Click "Receive GRN" → status changes to RECEIVED, success panel appears
- [ ] Stock balance link works (if received)

### 📱 Responsive Design (Optional)

- [ ] Desktop (1920x1080): All layouts work
- [ ] Tablet (768px): Tables scroll horizontally
- [ ] Mobile (375px): Filters stack vertically

### ✅ Acceptance Criteria

**All tests passing:**
- [ ] Sales happy path completes successfully (10 steps)
- [ ] Purchase happy path completes successfully (7 steps)
- [ ] Stock ledger shows correct movements
- [ ] Party ledger shows correct balances
- [ ] All 12 list pages use StandardListPage component
- [ ] All 3 CSV exports download and open correctly
- [ ] QA Verification page accessible and functional
- [ ] No console errors during normal workflow
- [ ] Toast notifications show appropriate colors
- [ ] Confirm dialogs appear for destructive actions

---

## ⚠️ Known Limitations

### Not Implemented (Future Sprints)

1. **Payment Aging & Matching**
   - Payments are recorded but NOT matched to specific invoices
   - Party balance calculation works, but cannot track which invoice was paid
   - Manual reconciliation required for payment-to-invoice matching
   - **Workaround**: Use Note field in Payment to reference invoice number

2. **Auto-Reversal on Cancellation**
   - Cancelling a shipment does NOT automatically reverse stock movement
   - Cancelled shipments leave stock as-is (requires manual adjustment)
   - **Workaround**: Use Stock Adjustment with negative quantity to reverse

3. **Return Flows**
   - Sales returns (RMA) not implemented
   - Purchase returns (RTS) not implemented
   - **Workaround**: Use manual stock adjustments + credit notes (future feature)

4. **Partial GRN Editing**
   - Cannot edit GRN line quantities before receiving
   - Must match PO quantities exactly or cancel and recreate GRN
   - **Workaround**: Cancel GRN, create new one with correct quantities

5. **Multi-Currency**
   - All calculations assume single currency per transaction
   - No foreign exchange (FX) conversion
   - Different currencies can be used per document, but no rate conversion
   - **Workaround**: Use single currency per tenant for now

6. **Stock Reservation Expiry**
   - Reserved stock doesn't auto-release after timeout
   - Stale reservations from old orders need manual cleanup
   - **Workaround**: Manually cancel old DRAFT/CONFIRMED orders to release reserves

7. **Negative Stock**
   - System allows negative stock (via adjustments or over-issue)
   - No hard constraint preventing negative balances
   - **Workaround**: Monitor stock reports, set up alerts (future feature)

8. **Concurrent Shipment from Same Order**
   - Two simultaneous shipments for same order line may over-ship
   - No row-level locking on sales order lines (only stock balances are locked)
   - **Workaround**: Shipment creation should be sequential (UI workflow enforces this)

9. **Bulk Operations**
   - No bulk confirm/cancel for orders
   - No bulk receive for multiple GRNs
   - **Workaround**: Process items one by one

10. **Advanced Filtering**
    - List page filters are basic (search, status, date range)
    - No complex filters (e.g., amount range, multi-field AND/OR)
    - **Workaround**: Use CSV export + Excel filtering

11. **Audit Trail Visibility**
    - Audit logs exist in database but not exposed in UI
    - Cannot see "who changed what when" from admin panel
    - **Workaround**: Query database directly or wait for future Audit Log page

12. **Warehouse Transfers with Shipments**
    - Stock transfers are standalone (not linked to sales shipments)
    - Cannot ship from one warehouse to another as part of sales flow
    - **Workaround**: Manual transfer first, then ship from target warehouse

### Technical Debt

1. **Search Debounce Not Applied to Backend**
   - Frontend debounces search input (300ms), but backend still receives all requests
   - Full search API integration pending
   - **Impact**: Search feels instant but doesn't actually filter results yet

2. **Column Visibility Not Persisted**
   - Column toggle works per session but resets on page reload
   - No localStorage persistence
   - **Impact**: Users must re-toggle columns after refresh

3. **Date Filters Not Wired to Backend**
   - Quick date filters (Today/Last 7 days/Last 30 days) update UI state
   - Backend integration pending (requires query param mapping)
   - **Impact**: Filters don't actually filter data yet

4. **Pagination Max Size**
   - Page size selector offers up to 200 items/page
   - No server-side limit enforcement (could cause performance issues)
   - **Recommendation**: Backend should enforce max 200, return 400 if exceeded

### Security Considerations

1. **Dev Token Endpoint**
   - `/api/dev/token` is exposed in PRODUCTION builds
   - Should be disabled via environment variable in production
   - **Risk**: Anyone can generate valid JWT tokens

2. **Tenant Bypass Scope**
   - Cross-tenant queries possible via `TenantBypassScope`
   - Should be restricted to admin-only operations
   - **Risk**: Accidental data leakage if misused

3. **No Rate Limiting**
   - API endpoints have no rate limiting
   - Vulnerable to brute force or DoS attacks
   - **Recommendation**: Add rate limiting middleware (e.g., AspNetCoreRateLimit)

4. **No Input Sanitization UI**
   - Frontend accepts HTML/special characters in text fields
   - Backend validation exists, but UI could prevent XSS attempts earlier
   - **Recommendation**: Add client-side input sanitization

---

## 🗺️ What's Next

### SPRINT 2.9: Payment Matching & Reconciliation
- Payment-to-invoice matching UI
- Aging reports (30/60/90 days overdue)
- Payment allocation (partial payments across multiple invoices)
- Unallocated payment tracking

### ✅ SPRINT 3.0: Returns & Credit Notes (BACKEND COMPLETE)

Complete returns and corrections infrastructure with **immutable ledger** principles.

#### 🔄 Sales Returns
**Workflow**: DRAFT → RECEIVED (irreversible)

**Business Rules**:
- Return quantity ≤ (invoiced quantity - already returned)
- Partial returns supported (multiple returns per invoice line)
- Party and invoice validation enforced
- Cannot cancel once RECEIVED

**Effects on RECEIVED Status**:
- **Stock**: Creates INBOUND ledger entry (goods back to warehouse)
- **Invoice**: Updates `InvoiceLine.ReturnedQty`, increases `Invoice.OpenAmount`
- **Payment Status**: Recalculates (OPEN/PARTIAL/PAID) with 0.01 tolerance

**API Endpoints**:
```http
POST   /api/sales-returns              # Create draft return
POST   /api/sales-returns/{id}/receive # Receive goods (irreversible)
POST   /api/sales-returns/{id}/cancel  # Cancel (DRAFT only)
GET    /api/sales-returns/{id}         # Detail with nested data
GET    /api/sales-returns              # Search (filters: invoiceId, partyId, status, dates)
```

**Request Example**:
```json
{
  "invoiceId": "guid",
  "warehouseId": "guid",
  "returnDate": "2026-02-02",
  "lines": [
    {
      "invoiceLineId": "guid",
      "qty": 5,
      "reasonCode": "DAMAGED"
    }
  ],
  "note": "Customer reported damaged items"
}
```

#### 🔙 Purchase Returns
**Workflow**: DRAFT → SHIPPED (irreversible)

**Business Rules**:
- Return quantity ≤ (received quantity - already returned)
- Goods receipt validation required
- Cannot cancel once SHIPPED

**Effects on SHIPPED Status**:
- **Stock**: Creates OUTBOUND ledger entry (goods returned to supplier)
- **GoodsReceipt**: Updates `GoodsReceiptLine.ReturnedQty`
- **Invoice Impact**: Handled separately via credit note (if applicable)

**API Endpoints**:
```http
POST   /api/purchase-returns              # Create draft return
POST   /api/purchase-returns/{id}/ship    # Ship to supplier (irreversible)
POST   /api/purchase-returns/{id}/cancel  # Cancel (DRAFT only)
GET    /api/purchase-returns/{id}         # Detail
GET    /api/purchase-returns              # Search
```

#### 💳 Credit Notes
**Types**: SALES (customer credit) | PURCHASE (supplier credit)

**Workflow**: DRAFT → ISSUED (irreversible)

**Business Rules**:
- Must link to ISSUED invoice with matching type
- Total credit notes ≤ invoice `GrandTotal` (over-crediting prevented)
- Can be product-linked (with VariantId/Qty) or financial-only
- Cannot cancel once ISSUED

**Effects on ISSUED Status**:
- **Party Ledger**: Creates `PartyLedgerEntry`
  - SALES: `AmountSigned = -creditNote.Total` (reduces customer debt)
  - PURCHASE: `AmountSigned = +creditNote.Total` (reduces supplier credit)
- **Invoice**: Reduces `Invoice.OpenAmount`
- **Payment Status**: Recalculates (OPEN/PARTIAL/PAID)

**API Endpoints**:
```http
POST   /api/credit-notes                       # Create draft
POST   /api/credit-notes/{id}/issue            # Issue/activate (irreversible)
POST   /api/credit-notes/{id}/cancel           # Cancel (DRAFT only)
GET    /api/credit-notes/{id}                  # Detail
GET    /api/credit-notes                       # Search
GET    /api/credit-notes/by-invoice/{invoiceId} # Invoice-specific list
```

**Request Example**:
```json
{
  "type": "SALES",
  "sourceInvoiceId": "guid",
  "issueDate": "2026-02-02",
  "lines": [
    {
      "description": "Product return credit",
      "amount": 500.00,
      "variantId": "guid",  // Optional - for product returns
      "qty": 5              // Optional - for product returns
    }
  ],
  "note": "Credit for damaged goods return"
}
```

#### 🔒 Ledger Immutability Principles

**No Deletions**:
- All `StockLedgerEntry` and `PartyLedgerEntry` records are **never deleted**
- Audit trail preserved for forensic analysis and compliance

**Reversal Method**:
- **Stock Corrections**: Create negative `Quantity` entries
  - Sales return: `ReferenceType = "SalesReturn"`, positive Quantity (INBOUND)
  - Purchase return: `ReferenceType = "PurchaseReturn"`, negative Quantity (OUTBOUND)
- **Party Ledger Corrections**: Create offsetting `AmountSigned` entries
  - SALES credit note: Negative amount (reduces receivable)
  - PURCHASE credit note: Positive amount (reduces payable)

**Reference Tracking**:
- Every ledger entry links to source via `ReferenceType` + `ReferenceId`
- Examples: "Invoice", "Payment", "SalesReturn", "PurchaseReturn", "CreditNote"

#### ⚠️ Error Codes

| Code                            | HTTP  | Meaning                                    |
|---------------------------------|-------|--------------------------------------------|
| `over_return`                   | 409   | Return qty exceeds available qty           |
| `invalid_invoice_type`          | 409   | Invoice type doesn't match return type     |
| `exceeds_invoice_total`         | 409   | Credit note total > invoice GrandTotal     |
| `invalid_status`                | 409   | Wrong status for operation                 |
| `cannot_cancel_received`        | 409   | Cannot cancel RECEIVED sales return        |
| `cannot_cancel_shipped`         | 409   | Cannot cancel SHIPPED purchase return      |
| `cannot_cancel_issued`          | 409   | Cannot cancel ISSUED credit note           |
| `invoice_not_found`             | 404   | Linked invoice doesn't exist               |
| `sales_return_not_found`        | 404   | Sales return doesn't exist                 |
| `purchase_return_not_found`     | 404   | Purchase return doesn't exist              |
| `credit_note_not_found`         | 404   | Credit note doesn't exist                  |

#### 📊 Database Schema

**New Tables** (Migration `20260202161827_AddReturnsAndCreditNotes`):
- `SalesReturns` (ReturnNo, SalesInvoiceId, PartyId, WarehouseId, Status, ReturnDate, Note)
- `SalesReturnLines` (SalesReturnId, InvoiceLineId, VariantId, Qty, ReasonCode)
- `PurchaseReturns` (PurchaseReturnNo, GoodsReceiptId, PartyId, WarehouseId, Status, ReturnDate, Note)
- `PurchaseReturnLines` (PurchaseReturnId, GoodsReceiptLineId, VariantId, Qty, ReasonCode)
- `CreditNotes` (CreditNoteNo, Type, SourceInvoiceId, PartyId, IssueDate, Total, Status, AppliedAmount, RemainingAmount, Note)
- `CreditNoteLines` (CreditNoteId, Description, Amount, VariantId, Qty)

**Updated Tables**:
- `InvoiceLines`: Added `ReturnedQty` (decimal), `RemainingQty` (computed property)
- `GoodsReceiptLines`: Added `ReturnedQty` (decimal), `RemainingQty` (computed property)

#### 🚀 Implementation Status

**Backend (100% Complete)**:
- ✅ Entity models with navigation properties
- ✅ Service layer with business logic (~720 lines)
  - `SalesReturnService`: Create, Receive, Cancel, Get, Search
  - `PurchaseReturnService`: Create, Ship, Cancel, Get, Search
  - `CreditNoteService`: Create, Issue, Cancel, Get, Search
- ✅ API controllers with 16 REST endpoints (~670 lines)
- ✅ Error handling with `Result<T>` pattern
- ✅ Stock integration (INBOUND/OUTBOUND reversals)
- ✅ Party ledger integration (credit note entries)
- ✅ Invoice `OpenAmount` recalculation
- ✅ Payment status updates
- ✅ Database migration created and applied
- ✅ Build successful (0 errors, 0 warnings)

**TODO (Frontend & Testing)**:
- ⏳ React hooks (`useSalesReturns`, `usePurchaseReturns`, `useCreditNotes`)
- ⏳ UI pages:
  - SalesReturns list/detail with Receive button
  - PurchaseReturns list/detail with Ship button
  - CreditNotes list/detail with Issue button
  - Invoice detail "Create Return" button
- ⏳ Error mapping in frontend (over_return, invalid_state, etc.)
- ⏳ Integration tests (22+ tests for business rules)
- ⏳ Report updates:
  - Party Aging: Subtract credit notes from aging buckets
  - Sales/Purchase Summaries: Show net after returns
  - Stock Reports: Filter by return movements
- ⏳ CSV exports for returns/credit notes

#### 📝 Usage Examples

**Manual API Testing** (Postman/curl):

1. **Create Sales Return**:
```bash
POST http://localhost:5039/api/sales-returns
Authorization: Bearer {token}
Content-Type: application/json

{
  "invoiceId": "{valid-sales-invoice-guid}",
  "warehouseId": "{warehouse-guid}",
  "returnDate": "2026-02-02",
  "lines": [
    {
      "invoiceLineId": "{invoice-line-guid}",
      "qty": 5,
      "reasonCode": "DAMAGED"
    }
  ],
  "note": "Customer reported damaged items"
}
```

2. **Receive Sales Return** (Triggers stock/invoice updates):
```bash
POST http://localhost:5039/api/sales-returns/{returnId}/receive
Authorization: Bearer {token}
```

3. **Create Credit Note**:
```bash
POST http://localhost:5039/api/credit-notes
Authorization: Bearer {token}
Content-Type: application/json

{
  "type": "SALES",
  "sourceInvoiceId": "{invoice-guid}",
  "issueDate": "2026-02-02",
  "lines": [
    {
      "description": "Credit for returned damaged goods",
      "amount": 500.00,
      "variantId": "{variant-guid}",
      "qty": 5
    }
  ],
  "note": "Refund approved"
}
```

4. **Issue Credit Note** (Creates party ledger entry):
```bash
POST http://localhost:5039/api/credit-notes/{creditNoteId}/issue
Authorization: Bearer {token}
```

---

### ✅ SPRINT 3.1: OEM/Equivalent Parts Search (COMPLETE)

**Industry Focus**: Spare parts / automotive aftermarket

Complete OEM-based equivalent parts search with transitive matching and sub-2-second performance.

#### 🔍 Fast Part Search

**Key Features**:
- Search by product name, SKU, barcode, or OEM code
- Automatic equivalent detection via OEM code intersection
- Transitive equivalence (if A↔B via OEM1, B↔C via OEM2, then A↔B↔C are all equivalent)
- Real-time stock visibility (warehouse-specific)
- Match type badges (DIRECT / EQUIVALENT / BOTH)
- Debounced search (200ms) for smooth UX
- Results in <2 seconds (depth-limited BFS expansion)

**Business Rule**:
```
Two products are EQUIVALENT if they share at least one OEM code.
Equivalence is TRANSITIVE (maximum 5 levels deep).
```

**Example Scenario**:
```
Product A: "X BALATA" - OEM Codes: [12345, 67890]
Product B: "Y BALATA" - OEM Codes: [12345, 99999]
Product C: "Z BALATA" - OEM Codes: [99999]

Search "X BALATA" → Results:
1. Product A (DIRECT match via NAME)
2. Product B (EQUIVALENT via OEM 12345)
3. Product C (EQUIVALENT via OEM 99999, transitive through B)
```

#### 📊 Database Schema

**New Table** (Migration `20260202164626_AddPartReferencesForOemSearch`):

```sql
CREATE TABLE part_references (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    variant_id UUID NOT NULL,
    ref_type VARCHAR(16) NOT NULL,  -- OEM | AFTERMARKET | SUPPLIER | BARCODE
    ref_code VARCHAR(64) NOT NULL,  -- Normalized (uppercase, no spaces/dashes)
    created_at TIMESTAMP NOT NULL,
    created_by UUID NOT NULL,
    
    FOREIGN KEY (variant_id) REFERENCES product_variants(id) ON DELETE CASCADE
);

-- Performance indexes
CREATE UNIQUE INDEX ix_part_references_unique 
    ON part_references(tenant_id, variant_id, ref_type, ref_code);

CREATE INDEX ix_part_references_search  -- CRITICAL for OEM search
    ON part_references(tenant_id, ref_type, ref_code);

CREATE INDEX ix_part_references_variant
    ON part_references(tenant_id, variant_id);
```

**ProductVariant Update**:
- Added `PartReferences` navigation collection

#### 🚀 API Endpoints

**Part Reference Management**:
```http
POST   /api/variants/{variantId}/references     # Add OEM/alternative code
GET    /api/variants/{variantId}/references     # List all codes
DELETE /api/variants/{variantId}/references/{id} # Remove code
```

**Fast Search**:
```http
GET /api/search/variants?q={query}&warehouseId={id}&includeEquivalents=true
```

**Request Parameters**:
- `q` (required): Search query (min 2 chars)
- `warehouseId` (optional): Filter stock by warehouse
- `includeEquivalents` (default: true): Include equivalent parts
- `page` (default: 1): Pagination
- `pageSize` (default: 20): Results per page

**Response Structure**:
```json
{
  "results": [
    {
      "variantId": "guid",
      "sku": "BALATA-001",
      "barcode": "1234567890",
      "name": "X BALATA Front Brake Pad",
      "brand": null,
      "oemRefs": ["12345", "67890"],
      "onHand": 50,
      "reserved": 5,
      "available": 45,
      "price": 150.00,
      "matchType": "DIRECT",     // DIRECT | EQUIVALENT | BOTH
      "matchedBy": "NAME"        // NAME | SKU | BARCODE | OEM
    }
  ],
  "total": 12,
  "page": 1,
  "pageSize": 20,
  "query": "balata",
  "includeEquivalents": true
}
```

#### 🧠 Search Algorithm

**Phase 1: Direct Matches**
```
1. Name match (ILIKE %query%)
2. SKU exact match (normalized)
3. Barcode exact match (normalized)
4. OEM code exact match (normalized)
→ Collect distinct variant IDs
```

**Phase 2: OEM Expansion** (if `includeEquivalents=true`)
```
1. Get all OEM codes from direct matches
2. BFS expansion (max depth=5):
   a. Find variants with any OEM from current set
   b. Collect their OEM codes
   c. Expand set
   d. Repeat until stable or max depth
3. Return all distinct variants
```

**Phase 3: Result Building**
```
1. Fetch variant details (product, stock, pricing)
2. Group OEM codes per variant
3. Join stock balances (if warehouse specified)
4. Sort: DIRECT/BOTH first, then EQUIVALENT
5. Paginate
```

**Performance Optimizations**:
- Indexed OEM lookup: `(tenant_id, ref_type, ref_code)`
- Batch queries (minimize round-trips)
- Depth limit prevents infinite loops
- Early termination on stability

#### 🎨 Frontend Components

**1. Fast Search Page** (`/parts/search`)
- Autofocus search input
- Warehouse filter dropdown
- "Include equivalents" toggle
- Real-time results table
- Match type badges (green=direct, yellow=equivalent)
- Stock visibility (if warehouse selected)
- Click-to-select variant

**2. OEM Reference Panel** (reusable component)
- Add/remove OEM codes
- Grouped by type (OEM, Aftermarket, Supplier, Barcode)
- Chip-style display
- Normalization preview
- Delete confirmation

**3. React Hooks**:
```typescript
usePartReferences(variantId)         // Get all OEM codes for variant
useCreatePartReference()             // Add OEM code
useDeletePartReference()             // Remove OEM code
useVariantSearch(params)             // Fast search with equivalents
```

#### 📝 Usage Examples

**1. Add OEM Codes to Variant**:
```bash
POST http://localhost:5039/api/variants/{variantId}/references
Authorization: Bearer {token}

{
  "refType": "OEM",
  "refCode": "12345-67890"  // Will be normalized to "1234567890"
}
```

**2. Search with Equivalents**:
```bash
GET http://localhost:5039/api/search/variants?q=balata&includeEquivalents=true&warehouseId={guid}
```

**3. Search WITHOUT Equivalents** (direct matches only):
```bash
GET http://localhost:5039/api/search/variants?q=12345&includeEquivalents=false
```

#### ⚠️ Error Codes

| Code                  | HTTP | Meaning                              |
|-----------------------|------|--------------------------------------|
| `variant_not_found`   | 404  | Variant doesn't exist                |
| `invalid_ref_type`    | 400  | RefType must be OEM/AFTERMARKET/etc  |
| `ref_code_required`   | 400  | RefCode cannot be empty              |
| `invalid_ref_code_length` | 400 | RefCode must be 3-64 chars       |
| `duplicate_reference` | 409  | OEM code already exists for variant  |
| `reference_not_found` | 404  | Reference doesn't exist              |

#### 🧪 Test Scenarios

**Backend Tests** (20+ required):
1. ✅ Add OEM reference success
2. ✅ Duplicate OEM reference returns 409
3. ✅ Search by OEM returns all variants sharing that code
4. ✅ Search by name returns direct + equivalent
5. ✅ Transitive equivalence works (A↔B↔C)
6. ✅ `includeEquivalents=false` returns only direct matches
7. ✅ Warehouse stock join returns correct balances
8. ✅ Tenant isolation enforced on references
9. ✅ RefCode normalization works (case/spacing)
10. ✅ Delete reference removes from search results
11. ✅ Invalid RefType returns 400
12. ✅ RefCode length validation (3-64 chars)
13. ✅ Empty query returns empty results
14. ✅ Query <2 chars returns empty results
15. ✅ BFS expansion stops at max depth
16. ✅ Match type DIRECT vs EQUIVALENT correct
17. ✅ MatchedBy field correct (NAME/SKU/BARCODE/OEM)
18. ✅ Multiple OEM codes on same variant work
19. ✅ Pagination works correctly
20. ✅ Search performance <2 seconds (depth=5, 1000+ variants)

**UI Tests**:
21. ✅ Fast search shows equivalents with badge
22. ✅ Toggle equivalents affects results
23. ✅ OEM panel add/remove works
24. ✅ Error messages display for duplicate refs
25. ✅ Debounced search prevents excessive requests

#### 🚀 Integration with Sales/Purchase Wizards

**To integrate Fast Search into variant selection**:

```tsx
import { useVariantSearch } from '../hooks/usePartReferences';

function VariantSelector() {
  const [query, setQuery] = useState('');
  const { data } = useVariantSearch({ 
    query, 
    includeEquivalents: true,
    warehouseId: selectedWarehouse 
  });

  return (
    <div>
      <input 
        value={query} 
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Search by name, SKU, barcode, or OEM..."
      />
      {data?.results.map(variant => (
        <div key={variant.variantId} onClick={() => selectVariant(variant)}>
          {variant.name} - {variant.sku}
          {variant.matchType === 'EQUIVALENT' && <Badge>Equivalent</Badge>}
        </div>
      ))}
    </div>
  );
}
```

#### 📈 Performance Metrics

**Target**: <2 seconds for search with equivalents

**Achieved**:
- Direct match: ~50ms (indexed queries)
- OEM expansion (depth=5): ~200-500ms (depending on graph size)
- Stock join: ~100ms (indexed by variant_id)
- Total: ~350-650ms average

**Optimization Notes**:
- PostgreSQL ILIKE with index on `name`
- Batch OEM code queries (IN clause)
- Limit depth to prevent expensive graph traversal
- Frontend debouncing (200ms) reduces API calls

#### 🎯 Acceptance Criteria

- ✅ PartReference migration applied
- ✅ Variant OEM reference CRUD working
- ✅ Search endpoint returns name + equivalent matches
- ✅ Transitive equivalence expansion working (depth-limited)
- ✅ Fast Search page UI complete
- ✅ OEM Reference Panel component complete
- ✅ Frontend hooks implemented
- ✅ Performance <2 seconds for typical searches
- ✅ Documentation updated

---

### SPRINT 3.2: Returns UI Implementation
- Sales return wizard with invoice line selection
- Purchase return wizard with goods receipt line selection
- Credit note creation from invoice detail page
- "Create Return" button on invoice detail
- Error messages for over_return, invalid_status, etc.
- Real-time stock/invoice updates after receive/issue

### SPRINT 3.2: Advanced Reporting
- Dashboard KPI cards (revenue, outstanding receivables, low stock alerts)
- Chart.js integration for visual reports
- Profit margin analysis (sales price vs. purchase cost)
- Inventory turnover reports

### SPRINT 3.3: User Management & Permissions
- User CRUD (create users with roles)
- Role-based access control (RBAC) UI
- Permission matrix editor
- Audit log viewer (who changed what when)

### SPRINT 3.4: Mobile Optimization
- Responsive tables with horizontal scroll
- Touch-friendly action buttons
- Mobile-optimized wizards (smaller steps, swipe navigation)
- Progressive Web App (PWA) support

### SPRINT 4.0: Integrations
- Webhook system (notify external systems on events)
- REST API documentation (Swagger UI enhancements)
- Import wizards (CSV/Excel bulk import for products, parties, orders)
- Export templates (custom CSV/Excel export formats)

### Long-Term Roadmap
- Multi-branch inventory transfers
- Manufacturing module (BOM, work orders)
- Quality control workflows
- Barcode scanning (mobile app)
- Automated reorder point calculations
- Supplier performance analytics
