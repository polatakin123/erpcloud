import { useQuery } from '@tanstack/react-query';
import { ApiClient } from '../lib/api-client';
import type {
  StockBalance,
  StockMovement,
  SalesSummary,
  PartyBalance,
  PartyAging,
  CashBankBalance,
  PagedReportResult,
} from '../types/report';

export function useStockBalances(warehouseId: string, q?: string, page = 1, size = 50) {
  return useQuery({
    queryKey: ['reports', 'stock', 'balances', warehouseId, q, page, size],
    queryFn: () =>
      ApiClient.request<PagedReportResult<StockBalance>>(
        `/reports/stock/balances?warehouseId=${warehouseId}&q=${q || ''}&page=${page}&size=${size}`
      ),
    enabled: !!warehouseId,
  });
}

export function useStockMovements(
  warehouseId?: string,
  variantId?: string,
  movementType?: string,
  from?: string,
  to?: string,
  page = 1,
  size = 50
) {
  return useQuery({
    queryKey: ['reports', 'stock', 'movements', warehouseId, variantId, movementType, from, to, page, size],
    queryFn: () => {
      const params = new URLSearchParams();
      if (warehouseId) params.append('warehouseId', warehouseId);
      if (variantId) params.append('variantId', variantId);
      if (movementType) params.append('movementType', movementType);
      if (from) params.append('from', from);
      if (to) params.append('to', to);
      params.append('page', page.toString());
      params.append('size', size.toString());
      
      return ApiClient.request<PagedReportResult<StockMovement>>(
        `/reports/stock/movements?${params.toString()}`
      );
    },
  });
}

export function useSalesSummary(from?: string, to?: string, groupBy = 'DAY') {
  return useQuery({
    queryKey: ['reports', 'sales', 'summary', from, to, groupBy],
    queryFn: () => {
      const params = new URLSearchParams();
      if (from) params.append('from', from);
      if (to) params.append('to', to);
      params.append('groupBy', groupBy);
      
      return ApiClient.request<SalesSummary[]>(
        `/reports/sales/summary?${params.toString()}`
      );
    },
  });
}

export function usePurchaseSummary(from?: string, to?: string, groupBy = 'DAY') {
  return useQuery({
    queryKey: ['reports', 'purchase', 'summary', from, to, groupBy],
    queryFn: () => {
      const params = new URLSearchParams();
      if (from) params.append('from', from);
      if (to) params.append('to', to);
      params.append('groupBy', groupBy);
      
      return ApiClient.request<SalesSummary[]>(
        `/reports/purchase/summary?${params.toString()}`
      );
    },
  });
}

export function usePartyBalances(q?: string, type?: string, page = 1, size = 50, at?: string) {
  return useQuery({
    queryKey: ['reports', 'parties', 'balances', q, type, page, size, at],
    queryFn: () => {
      const params = new URLSearchParams();
      if (q) params.append('q', q);
      if (type) params.append('type', type);
      if (at) params.append('at', at);
      params.append('page', page.toString());
      params.append('size', size.toString());
      
      return ApiClient.request<PagedReportResult<PartyBalance>>(
        `/reports/parties/balances?${params.toString()}`
      );
    },
  });
}

export function usePartyAging(q?: string, type?: string, page = 1, size = 50, at?: string) {
  return useQuery({
    queryKey: ['reports', 'parties', 'aging', q, type, page, size, at],
    queryFn: () => {
      const params = new URLSearchParams();
      if (q) params.append('q', q);
      if (type) params.append('type', type);
      if (at) params.append('at', at);
      params.append('page', page.toString());
      params.append('size', size.toString());
      
      return ApiClient.request<PagedReportResult<PartyAging>>(
        `/reports/parties/aging?${params.toString()}`
      );
    },
  });
}

export function useCashBankBalances(at?: string) {
  return useQuery({
    queryKey: ['reports', 'cashbank', 'balances', at],
    queryFn: () => {
      const params = new URLSearchParams();
      if (at) params.append('at', at);
      
      return ApiClient.request<CashBankBalance[]>(
        `/reports/cashbank/balances?${params.toString()}`
      );
    },
  });
}
