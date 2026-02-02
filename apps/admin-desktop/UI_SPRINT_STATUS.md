# ErpCloud Admin UI - Production Sprint

## Implementation Status

### ✅ Phase 1: Infrastructure (COMPLETED)
- [x] Global Context Store (`lib/context-store.ts`)
- [x] Error Mapping Utility (`lib/error-mapper.ts`)
- [x] CSV Export Utility (`lib/csv-exporter.ts`)
- [x] App Context Hook (`hooks/useAppContext.ts`)
- [x] Sales/Purchase Types (`types/sales.ts`)

### 🚧 Phase 2: Core UI Components (IN PROGRESS)
- [ ] Context Bar Component (top bar with branch/warehouse selection)
- [ ] Toast/Notification System
- [ ] Table Component with filters/pagination/sort
- [ ] Error Boundary Component
- [ ] Loading States & Skeletons
- [ ] Confirmation Dialogs

### 📋 Phase 3: API Hooks (TODO)
Need to create hooks for:
- [ ] Branches (`hooks/useBranches.ts`)
- [ ] Warehouses (`hooks/useWarehouses.ts`)
- [ ] Sales Orders (`hooks/useSalesOrders.ts`)
- [ ] Shipments (`hooks/useShipments.ts`)
- [ ] Invoices (`hooks/useInvoices.ts`)
- [ ] Purchase Orders (`hooks/usePurchaseOrders.ts`)
- [ ] Goods Receipts (`hooks/useGoodsReceipts.ts`)
- [ ] Cashboxes/Banks (`hooks/useCashBank.ts`)
- [ ] Ledgers (`hooks/useLedgers.ts`)

### 📄 Phase 4: List Pages (TODO)
Standardize with filters, search, pagination:
- [ ] Sales Orders List
- [ ] Shipments List
- [ ] Invoices List
- [ ] Purchase Orders List
- [ ] Goods Receipts List
- [ ] Stock Ledger List
- [ ] Party Ledger List
- [ ] Cash/Bank Ledger List
- [ ] Cashboxes List
- [ ] Bank Accounts List

### 📋 Phase 5: Detail Pages (TODO)
With actions and links:
- [ ] Sales Order Detail
- [ ] Shipment Detail
- [ ] Invoice Detail
- [ ] Purchase Order Detail
- [ ] Goods Receipt Detail
- [ ] Payment Detail

### 🧙 Phase 6: Wizards (TODO)
- [ ] Sales Wizard (10 steps)
  1. Select/Create Customer
  2. Add Products
  3. Create Draft Order
  4. Confirm Order
  5. Create Shipment
  6. Ship Shipment
  7. Create Invoice
  8. Issue Invoice
  9. Create Payment
  10. Verification Summary
  
- [ ] Purchase Wizard (6 steps)
  1. Select/Create Supplier
  2. Create PO + Lines
  3. Confirm PO
  4. Create GRN
  5. Receive GRN
  6. Verification

### 🛠️ Phase 7: Settings & Misc (TODO)
- [ ] Settings Page Enhancement
- [ ] Clear Cache Functionality
- [ ] Token Debug Info

### 📚 Phase 8: Documentation (TODO)
- [ ] README: UI Workflows
- [ ] README: QA Checklist
- [ ] DEVELOPMENT.md: Component Guide

---

## Quick Start for Implementation

Due to the massive scope (40+ components/pages), this will be delivered in phases.

**Priority Order:**
1. **Context Bar** - Essential for all operations
2. **Core Hooks** - API access layer
3. **Sales Wizard** - Primary user flow
4. **Purchase Wizard** - Secondary flow
5. **List Pages** - Browse functionality
6. **Detail Pages** - Deep dive
7. **Documentation** - QA checklist

**Estimated Effort:**
- Infrastructure: ✅ Done (1 hour)
- Context Bar + Hooks: 2-3 hours
- Sales Wizard: 3-4 hours  
- Purchase Wizard: 2-3 hours
- List Pages: 4-5 hours
- Detail Pages: 3-4 hours
- Documentation: 1 hour

**Total:** ~15-20 hours of focused development

---

## Next Immediate Steps

1. Create Context Bar component
2. Integrate into MainLayout
3. Create core API hooks (branches, warehouses, sales, purchase)
4. Build Sales Wizard skeleton
5. Implement step-by-step

---

## Architecture Decisions

**Routing:**
- `/sales/wizard` - Sales flow
- `/purchase/wizard` - Purchase flow
- `/sales-orders`, `/sales-orders/:id` - List & detail
- `/shipments`, `/shipments/:id` - List & detail
- `/invoices`, `/invoices/:id` - List & detail
- `/purchase-orders`, `/purchase-orders/:id` - List & detail
- `/goods-receipts`, `/goods-receipts/:id` - List & detail

**State Management:**
- TanStack Query for server state
- Context Store for branch/warehouse (persistent)
- Local state for form data (React Hook Form)

**Error Handling:**
- Global error boundary
- 401 → redirect to /login
- 403/409/400 → toast with mapped message
- Field errors → show inline

**CSV Export:**
- Stock Movements
- Party Ledger
- Cash/Bank Ledger
- Frontend-generated CSV

---

## Known Backend Constraints

Most endpoints exist. If missing:
- Use existing CRUD patterns
- Minimal backend changes
- Focus on UI composition

