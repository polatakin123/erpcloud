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
  branchId: string;
  warehouseId: string;
  partyId: string;
  issueDate: string;
  currency: string;
  subtotal: number;
  vatTotal: number;
  grandTotal: number;
  note?: string;
  lines: SalesOrderLineDetail[];
  createdAt: string;
  createdBy: string;
}

export interface SalesOrderLineDetail {
  id: string;
  variantId: string;
  variantSku: string;
  variantName: string;
  quantity: number;
  unitPrice: number;
  vatRate: number;
  discountPercent: number;
  lineTotal: number;
  reservedQty: number;
  shippedQty: number;
  remainingQty: number;
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
  orderId: string;
  orderNo: string;
  branchId: string;
  warehouseId: string;
  partyId: string;
  shipDate?: string;
  lines: ShipmentLineDetail[];
  note?: string;
  createdAt: string;
  createdBy: string;
}

export interface ShipmentLineDetail {
  id: string;
  orderLineId: string;
  variantId: string;
  variantSku: string;
  variantName: string;
  quantity: number;
  invoicedQty: number;
  remainingQty: number;
}

// Invoice types
export interface Invoice {
  id: string;
  invoiceNo: string;
  type: 'SALES' | 'PURCHASE';
  status: 'DRAFT' | 'ISSUED' | 'CANCELLED';
  sourceType?: string;
  sourceId?: string;
  branchId: string;
  partyId: string;
  issueDate: string;
  dueDate?: string;
  currency: string;
  subtotal: number;
  vatTotal: number;
  grandTotal: number;
  lines: InvoiceLineDetail[];
  note?: string;
  createdAt: string;
  createdBy: string;
}

export interface InvoiceLineDetail {
  id: string;
  variantId: string;
  variantSku: string;
  variantName: string;
  quantity: number;
  unitPrice: number;
  vatRate: number;
  discountPercent: number;
  lineTotal: number;
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
  orderNo: string;
  status: 'DRAFT' | 'CONFIRMED' | 'RECEIVED' | 'CANCELLED';
  branchId: string;
  warehouseId: string;
  partyId: string;
  issueDate: string;
  currency: string;
  subtotal: number;
  vatTotal: number;
  grandTotal: number;
  note?: string;
  lines: PurchaseOrderLineDetail[];
  createdAt: string;
  createdBy: string;
}

export interface PurchaseOrderLineDetail {
  id: string;
  variantId: string;
  variantSku: string;
  variantName: string;
  quantity: number;
  unitPrice: number;
  vatRate: number;
  discountPercent: number;
  lineTotal: number;
  receivedQty: number;
  remainingQty: number;
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
  receiptNo: string;
  status: 'DRAFT' | 'RECEIVED' | 'CANCELLED';
  purchaseOrderId: string;
  poNo: string;
  branchId: string;
  warehouseId: string;
  partyId: string;
  receiveDate?: string;
  lines: GoodsReceiptLineDetail[];
  note?: string;
  createdAt: string;
  createdBy: string;
}

export interface GoodsReceiptLineDetail {
  id: string;
  poLineId: string;
  variantId: string;
  variantSku: string;
  variantName: string;
  quantity: number;
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
