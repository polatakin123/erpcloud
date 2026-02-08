import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../lib/api-client';

export interface PartReference {
  id: string;
  variantId: string;
  refType: string;
  refCode: string;
  createdAt: string;
}

export interface CreatePartReferenceRequest {
  refType: string;
  refCode: string;
}

export interface VariantSearchResult {
  variantId: string;
  sku: string;
  barcode?: string;
  name: string;
  variantName?: string;
  brand?: string;
  brandId?: string;
  brandCode?: string;
  brandLogoUrl?: string;
  isBrandActive?: boolean;
  oemRefs: string[];
  onHand?: number;
  reserved?: number;
  available?: number;
  price?: number;
  matchType: 'DIRECT' | 'EQUIVALENT' | 'BOTH';
  matchedBy: 'NAME' | 'SKU' | 'BARCODE' | 'OEM';
  fitmentPriority?: number; // 1=compatible+inStock, 2=compatible+equivalent+inStock, 3=compatible+outOfStock, 4=undefined
  isCompatible?: boolean;
  hasDefinedFitment?: boolean;
}

export interface VariantSearchResponse {
  results: VariantSearchResult[];
  total: number;
  page: number;
  pageSize: number;
  query: string;
  includeEquivalents: boolean;
}

/**
 * Hook to get part references (OEM codes) for a variant
 */
export function usePartReferences(variantId: string | undefined) {
  return useQuery({
    queryKey: ['part-references', variantId],
    queryFn: async () => {
      if (!variantId) return [];
      const response = await apiClient.get<PartReference[]>(
        `/api/variants/${variantId}/references`
      );
      return response; // ApiClient returns unwrapped data
    },
    enabled: !!variantId,
  });
}

/**
 * Hook to add a part reference (OEM code) to a variant
 */
export function useCreatePartReference() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      variantId,
      data,
    }: {
      variantId: string;
      data: CreatePartReferenceRequest;
    }) => {
      const response = await apiClient.post<PartReference>(
        `/api/variants/${variantId}/references`,
        data
      );
      return response; // ApiClient returns unwrapped data
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: ['part-references', variables.variantId],
      });
    },
  });
}

/**
 * Hook to delete a part reference
 */
export function useDeletePartReference() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      variantId,
      referenceId,
    }: {
      variantId: string;
      referenceId: string;
    }) => {
      await apiClient.delete(`/api/variants/${variantId}/references/${referenceId}`);
    },
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: ['part-references', variables.variantId],
      });
    },
  });
}

/**
 * Hook to search for variants with OEM-based equivalent detection and vehicle fitment filtering
 */
export function useVariantSearch(params: {
  query: string;
  warehouseId?: string;
  engineId?: string;
  includeEquivalents?: boolean;
  includeUndefinedFitment?: boolean;
  page?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: ['variant-search', params],
    queryFn: async () => {
      const searchParams = new URLSearchParams();
      if (params.query) searchParams.append('q', params.query);
      if (params.warehouseId) searchParams.append('warehouseId', params.warehouseId);
      if (params.engineId) searchParams.append('engineId', params.engineId);
      searchParams.append('includeEquivalents', String(params.includeEquivalents ?? true));
      if (params.engineId) {
        searchParams.append('includeUndefinedFitment', String(params.includeUndefinedFitment ?? false));
      }
      searchParams.append('page', String(params.page ?? 1));
      searchParams.append('pageSize', String(params.pageSize ?? 20));

      const response = await apiClient.get<VariantSearchResponse>(
        `/api/search/variants?${searchParams.toString()}`
      );
      return response; // ApiClient already returns unwrapped data
    },
    enabled: !!params.query && params.query.length >= 2,
    staleTime: 30000, // Cache for 30 seconds
  });
}
