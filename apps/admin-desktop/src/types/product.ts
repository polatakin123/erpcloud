// Product types
export interface Product {
  id: string;
  code: string;
  name: string;
  description?: string;
  isActive: boolean;
  brandId?: string;
  brand?: string; // Deprecated - for backward compatibility
}

export interface CreateProductDto {
  code: string;
  name: string;
  description?: string;
  isActive?: boolean;
  brandId?: string;
}

export interface ProductSearchResult {
  items: Product[];
  totalCount: number;
  page: number;
  size: number;
}

// Variant types
export interface Variant {
  id: string;
  productId: string;
  sku: string;
  name: string;
  description?: string;
  isActive: boolean;
  product?: Product;
}

export interface CreateVariantDto {
  productId: string;
  sku: string;
  name: string;
  description?: string;
  isActive?: boolean;
}

export interface VariantSearchResult {
  items: Variant[];
  totalCount: number;
  page: number;
  size: number;
}
