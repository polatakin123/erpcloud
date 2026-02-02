
---

## Sprint 2.3: Shipment-Based Invoicing

**Status**:  COMPLETE (Tests: 16/16 passing, Migration applied)

### Overview

Allows creating **Sales Invoices** directly from **SHIPPED shipments**, maintaining full traceability between:
- Invoice  Shipment (via `SourceType`/`SourceId`)
- InvoiceLine  ShipmentLine (via `ShipmentLineId`)
- InvoiceLine  SalesOrderLine (via `SalesOrderLineId`)

**Key Features**:
-  Full & partial invoicing from shipment
-  Prevents double-invoicing (unique constraint on ShipmentLineId)
-  Tracks `InvoicedQty` on each shipment line
-  **Transactional integrity**: Issue invoice  creates ledger entry + updates `InvoicedQty` atomically
-  **Idempotency**: Issue same invoice twice = no-op, no double-increment

### Database Schema

**Invoice**:
- `SourceType` (varchar 32, nullable) - \"SHIPMENT\" for shipment-based invoices
- `SourceId` (uuid, nullable) - References Shipment.Id
- Index: `(TenantId, SourceType, SourceId)`

**InvoiceLine**:
- `ShipmentLineId` (uuid, nullable, FK  shipment_lines ON DELETE RESTRICT)
- `SalesOrderLineId` (uuid, nullable, FK  sales_order_lines ON DELETE RESTRICT)
- **Unique constraint**: `(TenantId, ShipmentLineId)` WHERE `ShipmentLineId IS NOT NULL` 
- Index: `(TenantId, ShipmentLineId)`

**ShipmentLine**:
- `InvoicedQty` (decimal 18,3, default 0) - Cumulative invoiced quantity
- Constraint: `0 <= InvoicedQty <= Qty` (enforced in business logic)

### API Endpoints

#### 1. Preview Invoice from Shipment
```http
POST /api/shipments/{shipmentId}/invoice/preview
Authorization: Bearer {token}
Content-Type: application/json

{
  \"invoiceNo\": \"INV-2024-001\",
  \"issueDate\": \"2024-02-02T10:00:00Z\",
  \"dueDate\": \"2024-03-02T10:00:00Z\",
  \"note\": \"Payment terms: Net 30\",
  \"lines\": [  // Optional - omit for full invoice
    {
      \"shipmentLineId\": \"3fa85f64-5717-4562-b3fc-2c963f66afa6\",
      \"qty\": 5
    }
  ]
}
```

**Response**:
```json
{
  \"subtotal\": 15000.00,
  \"vatTotal\": 2700.00,
  \"grandTotal\": 17700.00,
  \"lines\": [
    {
      \"shipmentLineId\": \"3fa85f64-...\",
      \"variantName\": \"Widget A\",
      \"qty\": 10,
      \"unitPrice\": 1500.00,
      \"vatRate\": 18,
      \"lineTotal\": 15000.00,
      \"vatAmount\": 2700.00
    }
  ]
}
```

**Validations**:
-  Shipment must be `SHIPPED`
-  Qty  (ShipmentLine.Qty - InvoicedQty)
-  No database write (safe for calculation)

#### 2. Create Draft Invoice from Shipment
```http
POST /api/shipments/{shipmentId}/invoice
Authorization: Bearer {token}

{
  \"invoiceNo\": \"INV-2024-001\",
  \"issueDate\": \"2024-02-02T10:00:00Z\",
  \"dueDate\": \"2024-03-02T10:00:00Z\",
  \"note\": \"Payment terms: Net 30\"
  // Omit \"lines\" for full invoice (all remaining qty)
}
```

**Response** (201 Created):
```json
{
  \"id\": \"7c9e6679-...\",
  \"invoiceNo\": \"INV-2024-001\",
  \"status\": \"DRAFT\",
  \"sourceType\": \"SHIPMENT\",
  \"sourceId\": \"3fa85f64-...\",
  \"lines\": [
    {
      \"shipmentLineId\": \"...\",
      \"salesOrderLineId\": \"...\",
      \"qty\": 10,
      \"unitPrice\": 1500.00
    }
  ]
}
```

**Effects**:
- Creates `DRAFT` invoice with `SourceType=\"SHIPMENT\"`, `SourceId={shipmentId}`
- Sets `InvoiceLine.ShipmentLineId` and `SalesOrderLineId` for traceability
- **Does NOT** update `InvoicedQty` yet (only when issued)

**Errors**:
- `400`: Shipment not `SHIPPED`, qty validation failed
- `404`: Shipment not found
- `409`: Unique constraint violation (shipment line already invoiced)

#### 3. Issue Invoice (Enhanced)
```http
POST /api/invoices/{invoiceId}/issue
```

**Effects for Shipment-Based Invoices**:
1. Changes invoice status to `ISSUED` 
2. Creates `PartyLedgerEntry` (existing Sprint-2.1 behavior) 
3. **NEW**: Updates `ShipmentLine.InvoicedQty += InvoiceLine.Qty` 
4. All within **single transaction** (atomic commit)

**Idempotency**: Calling `Issue` on already-issued invoice = no-op, `InvoicedQty` does NOT increment again 

#### 4. List Shipment Invoices
```http
GET /api/shipments/{shipmentId}/invoices
```

#### 5. Get Shipment Invoicing Status
```http
GET /api/shipments/{shipmentId}/invoicing-status
```

**Response**:
```json
{
  \"isFullyInvoiced\": false,
  \"totalShippedQty\": 10,
  \"totalInvoicedQty\": 5,
  \"totalRemainingQty\": 5
}
```

### End-to-End Workflow Example

```powershell
# Step 1: Receive stock
curl -X POST http://localhost:5000/api/stock-entries -d '{
  \"entryNo\": \"REC-001\",
  \"warehouseId\": \"...\",
  \"lines\": [{\"variantId\": \"...\", \"qty\": 100, \"unitCost\": 1000}]
}'

# Step 2: Create & confirm sales order
curl -X POST http://localhost:5000/api/sales-orders -d '{
  \"orderNo\": \"SO-001\",
  \"partyId\": \"...\",
  \"lines\": [{\"variantId\": \"...\", \"qty\": 10, \"unitPrice\": 1500}]
}'
curl -X POST http://localhost:5000/api/sales-orders/{orderId}/confirm

# Step 3: Create & ship shipment
curl -X POST http://localhost:5000/api/shipments -d '{
  \"shipmentNo\": \"SHIP-001\",
  \"salesOrderId\": \"{orderId}\",
  \"lines\": [{\"salesOrderLineId\": \"...\", \"qty\": 10}]
}'
curl -X POST http://localhost:5000/api/shipments/{shipmentId}/ship

# Step 4: Preview invoice
curl -X POST http://localhost:5000/api/shipments/{shipmentId}/invoice/preview -d '{
  \"invoiceNo\": \"INV-001\",
  \"issueDate\": \"2024-02-02T10:00:00Z\",
  \"dueDate\": \"2024-03-02T10:00:00Z\"
}'
#  Returns: {\"subtotal\": 15000, \"vatTotal\": 2700, \"grandTotal\": 17700}

# Step 5: Create PARTIAL invoice (5 out of 10)
curl -X POST http://localhost:5000/api/shipments/{shipmentId}/invoice -d '{
  \"invoiceNo\": \"INV-001\",
  \"lines\": [{\"shipmentLineId\": \"...\", \"qty\": 5}],
  \"issueDate\": \"2024-02-02T10:00:00Z\",
  \"dueDate\": \"2024-03-02T10:00:00Z\"
}'

# Step 6: Issue invoice (updates InvoicedQty + creates ledger entry)
curl -X POST http://localhost:5000/api/invoices/{invoiceId}/issue
#  ShipmentLine.InvoicedQty: 0  5
#  PartyLedgerEntry created with AmountSigned = 8850

# Step 7: Check status
curl http://localhost:5000/api/shipments/{shipmentId}/invoicing-status
#  {\"isFullyInvoiced\": false, \"totalRemainingQty\": 5}

# Step 8: Create second invoice for remaining qty
curl -X POST http://localhost:5000/api/shipments/{shipmentId}/invoice -d '{
  \"invoiceNo\": \"INV-002\",
  \"issueDate\": \"2024-02-05T10:00:00Z\",
  \"dueDate\": \"2024-03-05T10:00:00Z\"
  // Omit \"lines\"  auto-invoices remaining 5 qty
}'
curl -X POST http://localhost:5000/api/invoices/{invoice2Id}/issue
#  ShipmentLine.InvoicedQty: 5  10
#  {\"isFullyInvoiced\": true}

# Step 9: Verify double-invoicing prevented
curl -X POST http://localhost:5000/api/shipments/{shipmentId}/invoice -d '{...}'
#  400 Bad Request: \"No lines available to invoice\"
```

### Business Rules

**Invoicing Constraints**:
-  Only `SHIPPED` shipments can be invoiced
-  Each `ShipmentLine` can appear in **max 1 InvoiceLine** (unique constraint)
-  Partial invoicing: Specify `lines[]` with `qty  (Qty - InvoicedQty)`
-  Full invoicing: Omit `lines[]`  invoices all remaining qty
-  Over-invoicing validation at **create** (preventive) and **issue** (defensive)

**Transactional Integrity**:
- When invoice issued: Creates `PartyLedgerEntry` + updates `ShipmentLine.InvoicedQty` **atomically**
- Both succeed or both rollback (single `DbTransaction`)

**Idempotency**:
- Issue already-issued invoice  no-op, safe to retry

### Test Coverage (16/16 )

1.  Create invoice from shipment
2.  Invoice totals calculated correctly
3.  Preview doesn't create invoice
4.  Partial invoice allows remaining qty
5.  Partial invoice over-qty fails
6.  Issue updates InvoicedQty
7.  Issue idempotent
8.  Unique constraint prevents double-invoicing
9.  Non-shipped shipment rejected
10.  Wrong shipment line ID fails
11.  Tenant isolation
12.  List shipment invoices
13.  Invoice contains source info
14.  InvoicedQty constraint 0..Qty
15.  Transactional integrity (ledger + invoicing) 
16.  SalesOrderLineId traceability

### Integration Points

- **Invoice Module**: Reuses `InvoiceService.IssueAsync()` with hook pattern
- **Shipment Module**: Reads status/qty, updates `InvoicedQty`
- **Sales Order Module**: Copies `SalesOrderLineId` for traceability
- **Party Module**: Inherits `PartyId` for ledger entries

### Permissions

- `invoicing.write` - Required for all invoice operations

### Future Enhancements

- [ ] Credit notes from invoices
- [ ] Automatic invoice on shipment
- [ ] Multi-currency invoicing
- [ ] Payment allocation
- [ ] Invoice PDF generation
