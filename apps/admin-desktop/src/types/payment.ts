export interface Payment {
  id: string;
  paymentNo: string;
  partyId: string;
  partyName?: string;
  branchId: string;
  date: string;
  paymentDate: string;
  direction: string;
  paymentType: string;
  method: string;
  paymentMethod: string;
  currency: string;
  amount: number;
  note?: string;
  sourceType?: string;
  sourceId?: string;
  status: string;
}

export interface CreatePaymentDto {
  paymentNo: string;
  partyId: string;
  branchId: string;
  date: string;
  direction: string;
  method: string;
  currency: string;
  amount: number;
  note?: string;
  sourceType?: string;
  sourceId?: string;
}

export interface Cashbox {
  id: string;
  code: string;
  name: string;
  currency: string;
  isDefault: boolean;
  isActive: boolean;
}

export interface BankAccount {
  id: string;
  code: string;
  name: string;
  bankName?: string;
  iban?: string;
  currency: string;
  isDefault: boolean;
  isActive: boolean;
}

export interface CashBankLedgerEntry {
  id: string;
  occurredAt: string;
  sourceType: string;
  sourceId: string;
  paymentId?: string;
  description?: string;
  amountSigned: number;
  currency: string;
}

export interface CashBankBalance {
  balance: number;
  currency: string;
}
