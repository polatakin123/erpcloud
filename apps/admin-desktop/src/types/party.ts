// API Types
export interface Party {
  id: string;
  code: string;
  name: string;
  type: string;
  email?: string;
  phone?: string;
  taxNumber?: string;
  taxOffice?: string;
  isActive: boolean;
}

export interface CreatePartyDto {
  code: string;
  name: string;
  type: string;
  email?: string;
  phone?: string;
  taxNumber?: string;
  taxOffice?: string;
  isActive?: boolean;
}

export interface PartySearchResult {
  items: Party[];
  totalCount: number;
  page: number;
  size: number;
}
