// Branch types
export interface Branch {
  id: string;
  organizationId: string;
  code: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  createdBy: string;
}

// Warehouse types
export interface Warehouse {
  id: string;
  branchId: string;
  code: string;
  name: string;
  type: 'MAIN' | 'TRANSIT' | 'VIRTUAL';
  isDefault: boolean;
  createdAt: string;
  createdBy: string;
}

// Sales Order types
export interface SalesOrderLine {
  variantId: string;
  quantity: number;
  unitPrice: number;
  vatRate: number;
  discountPercent?: number;
}

export interface CreateSalesOrderRequest {
  branchId: string;
  warehouseId: string;
  partyId: string;
  issueDate: string;
  currency: string;
  lines: SalesOrderLine[];
  note?: string;
}

export interface SalesOrder {
  id: string;
  orderNo: string;
  status: 'DRAFT' | 'CONFIRMED' | 'SHIPPED' | 'INVOICED' | 'CANCELLED';
  partyId: string;
  partyName: string;
  branchId: string;
  branchName: string;
  warehouseId: string;
  warehouseName: string;
  priceListId?: string;
  priceListCode?: string;
  orderDate: string;
  totalAmount: number;
  currency: string;
  note?: string;
  lines: SalesOrderLineDetail[];
  createdAt: string;
}

export interface SalesOrderLineDetail {
  id: string;
  variantId: string;
  sku: string;
  variantName: string;
  qty: number;
  quantity: number;
  unitPrice: number;
  price: number;
  vatRate: number;
  lineTotal: number;
  reservedQty: number;
  note?: string;
}

// Shipment types
export interface ShipmentLine {
  orderLineId: string;
  quantity: number;
}

export interface CreateShipmentRequest {
  orderId: string;
  lines: ShipmentLine[];
  note?: string;
}

export interface Shipment {
  id: string;
  shipmentNo: string;
  status: 'DRAFT' | 'SHIPPED' | 'INVOICED' | 'CANCELLED';
  salesOrderId: string;
  branchId: string;
  warehouseId: string;
  shipmentDate: string;
  lines: ShipmentLineDetail[];
  note?: string;
  createdAt: string;
  createdBy: string;
}

export interface ShipmentLineDetail {
  id: string;
  shipmentId: string;
  salesOrderLineId: string;
  variantId: string;
  qty: number;
  note?: string;
}

// Invoice types
export interface Invoice {
  id: string;
  invoiceNo: string;
  type: 'SALES' | 'PURCHASE';
  status: 'DRAFT' | 'ISSUED' | 'CANCELLED';
  partyId: string;
  partyName: string;
  branchId: string;
  branchName: string;
  issueDate: string;
  dueDate?: string;
  currency: string;
  subtotal: number;
  vatTotal: number;
  grandTotal: number;
  lines: InvoiceLineDetail[];
  note?: string;
  createdAt: string;
  paidAmount: number;
  openAmount: number;
  paymentStatus: 'OPEN' | 'PARTIAL' | 'PAID';
}

export interface InvoiceLineDetail {
  id: string;
  variantId?: string;
  sku?: string;
  description: string;
  qty?: number;
  unitPrice?: number;
  vatRate: number;
  lineTotal: number;
  vatAmount: number;
}

// Purchase Order types
export interface PurchaseOrderLine {
  variantId: string;
  quantity: number;
  unitPrice: number;
  vatRate: number;
  discountPercent?: number;
}

export interface CreatePurchaseOrderRequest {
  branchId: string;
  warehouseId: string;
  partyId: string;
  issueDate: string;
  currency: string;
  lines: PurchaseOrderLine[];
  note?: string;
}

export interface PurchaseOrder {
  id: string;
  poNo: string;
  status: 'DRAFT' | 'CONFIRMED' | 'RECEIVED' | 'CANCELLED';
  partyId: string;
  partyName: string;
  branchId: string;
  branchName: string;
  warehouseId: string;
  warehouseName: string;
  orderDate: string;
  expectedDate?: string;
  note?: string;
  totalAmount: number;
  receivedAmount: number;
  lines: PurchaseOrderLineDetail[];
  createdAt: string;
}

export interface PurchaseOrderLineDetail {
  id: string;
  variantId: string;
  variantSku: string;
  variantName: string;
  qty: number;
  receivedQty: number;
  remainingQty: number;
  unitCost?: number;
  vatRate?: number;
  lineTotal: number;
  note?: string;
}

// Goods Receipt types
export interface GoodsReceiptLine {
  poLineId: string;
  quantity: number;
}

export interface CreateGoodsReceiptRequest {
  purchaseOrderId: string;
  lines: GoodsReceiptLine[];
  note?: string;
}

export interface GoodsReceipt {
  id: string;
  grnNo: string;
  status: 'DRAFT' | 'RECEIVED' | 'CANCELLED';
  purchaseOrderId: string;
  poNo: string;
  branchId: string;
  branchName: string;
  warehouseId: string;
  warehouseName: string;
  receiptDate: string;
  note?: string;
  totalAmount: number;
  lines: GoodsReceiptLineDetail[];
  createdAt: string;
}

export interface GoodsReceiptLineDetail {
  id: string;
  purchaseOrderLineId: string;
  variantId: string;
  variantSku: string;
  variantName: string;
  qty: number;
  unitCost?: number;
  lineTotal: number;
  note?: string;
}

// Stock Ledger types
export interface StockLedgerEntry {
  id: string;
  warehouseId: string;
  warehouseName: string;
  variantId: string;
  variantSku: string;
  variantName: string;
  occurredAt: string;
  movementType: 'IN' | 'OUT';
  quantity: number;
  referenceType?: string;
  referenceId?: string;
  note?: string;
}

// Cashbox/Bank types
export interface Cashbox {
  id: string;
  code: string;
  name: string;
  currency: string;
  isActive: boolean;
  isDefault: boolean;
  createdAt: string;
}

export interface BankAccount {
  id: string;
  code: string;
  name: string;
  bankName: string;
  iban?: string;
  currency: string;
  isActive: boolean;
  isDefault: boolean;
  createdAt: string;
}

// Party Ledger types
export interface PartyLedgerEntry {
  id: string;
  partyId: string;
  partyCode: string;
  partyName: string;
  branchId: string;
  occurredAt: string;
  sourceType: string;
  sourceId: string;
  description: string;
  amountSigned: number;
  currency: string;
  openAmountSigned: number;
}

// Cash/Bank Ledger types
export interface CashBankLedgerEntry {
  id: string;
  sourceType: 'CASHBOX' | 'BANK';
  sourceId: string;
  sourceName: string;
  occurredAt: string;
  description: string;
  amountSigned: number;
  currency: string;
}

// Pagination helper
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
}
