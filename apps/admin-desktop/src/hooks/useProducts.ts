import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiClient } from '@/lib/api-client';
import type { Product, CreateProductDto, ProductSearchResult } from '@/types/product';

export function useProducts(q?: string, page = 1, size = 50) {
  return useQuery<ProductSearchResult>({
    queryKey: ['products', q, page, size],
    queryFn: async () => {
      const params = new URLSearchParams({
        page: page.toString(),
        size: size.toString(),
      });
      if (q) params.append('q', q);
      
      return ApiClient.get(`/api/products?${params.toString()}`);
    },
  });
}

export function useCreateProduct() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (data: CreateProductDto) => {
      return ApiClient.post<Product>('/api/products', data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['products'] });
    },
  });
}

export function useProduct(id: string) {
  return useQuery<Product>({
    queryKey: ['product', id],
    queryFn: () => ApiClient.get(`/api/products/${id}`),
    enabled: !!id,
  });
}
