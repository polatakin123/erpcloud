import { useQuery } from '@tanstack/react-query';
import { ApiClient } from '../lib/api-client';
import { useToast } from './useToast';
import { ErrorMapper } from '../lib/error-mapper';
import { useNavigate } from 'react-router-dom';

export interface ProductVariant {
  id: string;
  sku: string;
  productId: string;
  productName: string;
  variantName?: string;
  price: number;
  currency: string;
  unit: string;
  stockQty: number;
}

export interface PagedVariantResult {
  items: ProductVariant[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export function useProductVariants(query?: string, page: number = 1, pageSize: number = 50) {
  const { toast } = useToast();
  const navigate = useNavigate();

  const queryString = new URLSearchParams();
  if (query) {
    queryString.append('q', query);
  }
  queryString.append('page', page.toString());
  queryString.append('pageSize', pageSize.toString());

  return useQuery({
    queryKey: ['product-variants', query, page, pageSize],
    queryFn: async () => {
      try {
        const response = await ApiClient.get<PagedVariantResult>(
          `/api/product-variants?${queryString.toString()}`
        );
        return response;
      } catch (error) {
        if (ErrorMapper.requiresLogin(error)) {
          navigate('/login');
          throw error;
        }
        const mappedError = ErrorMapper.mapError(error);
        toast({
          variant: "destructive",
          title: mappedError.title,
          description: mappedError.message,
        });
        throw error;
      }
    },
  });
}

export function useProductVariant(id: string | null) {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['product-variant', id],
    enabled: !!id,
    queryFn: async () => {
      if (!id) throw new Error('No ID provided');
      try {
        const response = await ApiClient.get<ProductVariant>(`/api/product-variants/${id}`);
        return response;
      } catch (error) {
        if (ErrorMapper.requiresLogin(error)) {
          navigate('/login');
          throw error;
        }
        const mappedError = ErrorMapper.mapError(error);
        toast({
          variant: "destructive",
          title: mappedError.title,
          description: mappedError.message,
        });
        throw error;
      }
    },
  });
}
