import { useQuery } from '@tanstack/react-query';
import { ApiClient } from '../lib/api-client';
import { useToast } from './useToast';
import { ErrorMapper } from '../lib/error-mapper';
import { useNavigate } from 'react-router-dom';

export interface StockBalance {
  variantId: string;
  sku: string;
  productName: string;
  variantName?: string;
  warehouseId: string;
  warehouseName: string;
  quantity: number;
  unit: string;
}

export interface StockBalanceFilters {
  warehouseId?: string;
  variantId?: string;
  sku?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedStockBalance {
  items: StockBalance[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export function useStockBalances(filters: StockBalanceFilters = {}) {
  const { toast } = useToast();
  const navigate = useNavigate();

  const queryParams = new URLSearchParams();
  if (filters.warehouseId) queryParams.append('warehouseId', filters.warehouseId);
  if (filters.variantId) queryParams.append('variantId', filters.variantId);
  if (filters.sku) queryParams.append('sku', filters.sku);
  queryParams.append('page', (filters.page || 1).toString());
  queryParams.append('pageSize', (filters.pageSize || 50).toString());

  return useQuery({
    queryKey: ['stock-balance', filters],
    queryFn: async () => {
      try {
        const response = await ApiClient.get<PagedStockBalance>(
          `/api/stock/balance?${queryParams.toString()}`
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
