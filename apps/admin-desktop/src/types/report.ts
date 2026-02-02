export interface StockBalance {
  variantId: string;
  sku: string;
  variantName: string;
  unit: string;
  onHand: number;
  reserved: number;
  available: number;
}

export interface StockMovement {
  occurredAt: string;
  movementType: string;
  quantity: number;
  referenceType: string;
  referenceId: string;
  note?: string;
}

export interface SalesSummary {
  period: string;
  invoiceCount: number;
  totalNet: number;
  totalVat: number;
  totalGross: number;
}

export interface PartyBalance {
  partyId: string;
  code: string;
  name: string;
  type: string;
  balance: number;
  currency: string;
}

export interface PartyAging {
  partyId: string;
  code: string;
  name: string;
  bucket0_30: number;
  bucket31_60: number;
  bucket61_90: number;
  bucket90Plus: number;
  total: number;
}

export interface CashBankBalance {
  sourceType: string;
  sourceId: string;
  code: string;
  name: string;
  currency: string;
  balance: number;
}

export interface PagedReportResult<T> {
  page: number;
  size: number;
  total: number;
  items: T[];
}
