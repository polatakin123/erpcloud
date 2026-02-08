export interface Brand {
  id: string;
  tenantId: string;
  code: string;
  name: string;
  logoUrl?: string;
  isActive: boolean;
  createdAt: string;
  createdBy: string;
}

export interface CreateBrandRequest {
  code?: string;
  name: string;
  logoUrl?: string;
  isActive?: boolean;
}

export interface UpdateBrandRequest {
  code?: string;
  name?: string;
  logoUrl?: string;
  isActive?: boolean;
}

export interface BrandSearchResult {
  items: Brand[];
  total: number;
}
