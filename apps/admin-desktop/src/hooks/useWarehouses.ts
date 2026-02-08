import { useQuery } from '@tanstack/react-query';
import { ApiClient } from '../lib/api-client';

export interface Warehouse {
  id: string;
  code: string;
  name: string;
  address?: string;
  isActive: boolean;
  branchId: string;
}

interface WarehousesResponse {
  items: Warehouse[];
  total: number;
}

/**
 * Hook to get all warehouses (across all branches)
 */
export function useWarehouses() {
  return useQuery({
    queryKey: ['warehouses'],
    queryFn: async () => {
      const response = await ApiClient.get<WarehousesResponse>('/api/warehouses?page=1&size=200');
      return response.items || [];
    },
  });
}

/**
 * Hook to get warehouses for a specific branch
 */
export function useWarehousesByBranch(branchId?: string) {
  return useQuery({
    queryKey: ['warehouses', branchId],
    queryFn: async () => {
      if (!branchId) return [];
      const response = await ApiClient.get<WarehousesResponse>(`/api/branches/${branchId}/warehouses?page=1&size=100`);
      return response.items;
    },
    enabled: !!branchId,
  });
}
