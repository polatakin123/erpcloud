import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiClient } from '@/lib/api-client';
import type { Brand, CreateBrandRequest, UpdateBrandRequest } from '@/types/brand';

export function useBrands(q?: string, active?: boolean, limit = 50) {
  return useQuery<Brand[]>({
    queryKey: ['brands', q, active, limit],
    queryFn: async () => {
      const params = new URLSearchParams({
        limit: limit.toString(),
      });
      if (q) params.append('q', q);
      if (active !== undefined) params.append('active', active.toString());
      
      return ApiClient.get(`/api/brands?${params.toString()}`);
    },
  });
}

export function useBrand(id: string) {
  return useQuery<Brand>({
    queryKey: ['brand', id],
    queryFn: () => ApiClient.get(`/api/brands/${id}`),
    enabled: !!id,
  });
}

export function useCreateBrand() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (data: CreateBrandRequest) => {
      return ApiClient.post<Brand>('/api/brands', data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['brands'] });
    },
  });
}

export function useUpdateBrand(id: string) {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (data: UpdateBrandRequest) => {
      return ApiClient.put<Brand>(`/api/brands/${id}`, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['brands'] });
      queryClient.invalidateQueries({ queryKey: ['brand', id] });
    },
  });
}

export function useDeleteBrand() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (id: string) => {
      return ApiClient.delete<{ message: string; wasSoftDeleted?: boolean }>(`/api/brands/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['brands'] });
    },
  });
}
