import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiClient } from '../lib/api-client';
import { 
  PurchaseOrder, 
  CreatePurchaseOrderRequest,
  GoodsReceipt,
  CreateGoodsReceiptRequest,
  PagedResult 
} from '../types/sales';
import { useToast } from './useToast';
import { ErrorMapper } from '../lib/error-mapper';
import { useNavigate } from 'react-router-dom';

// ============ Purchase Orders ============

export function usePurchaseOrders(page: number = 1, pageSize: number = 50) {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['purchase-orders', page, pageSize],
    queryFn: async () => {
      try {
        return await ApiClient.get<PagedResult<PurchaseOrder>>(
          `/api/purchase-orders?page=${page}&pageSize=${pageSize}`
        );
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

export function usePurchaseOrder(id: string | null) {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['purchase-order', id],
    enabled: !!id,
    queryFn: async () => {
      if (!id) throw new Error('No ID provided');
      try {
        return await ApiClient.get<PurchaseOrder>(`/api/purchase-orders/${id}`);
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

export function useCreatePurchaseOrder() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: async (data: CreatePurchaseOrderRequest) => {
      return await ApiClient.post<PurchaseOrder>('/api/purchase-orders', {
        body: JSON.stringify(data),
      });
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['purchase-orders'] });
      toast({
        title: "Purchase Order Created",
        description: `PO ${data.poNo} created successfully.`,
      });
      return data;
    },
    onError: (error) => {
      if (ErrorMapper.requiresLogin(error)) {
        navigate('/login');
        return;
      }
      const mappedError = ErrorMapper.mapError(error);
      toast({
        variant: "destructive",
        title: mappedError.title,
        description: mappedError.message,
      });
    },
  });
}

export function useConfirmPurchaseOrder() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: async (orderId: string) => {
      return await ApiClient.post<PurchaseOrder>(`/api/purchase-orders/${orderId}/confirm`, {});
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['purchase-orders'] });
      queryClient.invalidateQueries({ queryKey: ['purchase-order', data.id] });
      toast({
        title: "Purchase Order Confirmed",
        description: `PO ${data.poNo} confirmed successfully.`,
      });
      return data;
    },
    onError: (error) => {
      if (ErrorMapper.requiresLogin(error)) {
        navigate('/login');
        return;
      }
      const mappedError = ErrorMapper.mapError(error);
      toast({
        variant: "destructive",
        title: mappedError.title,
        description: mappedError.message,
      });
    },
  });
}

// ============ Goods Receipts ============

export function useGoodsReceipts(page: number = 1, pageSize: number = 50) {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['goods-receipts', page, pageSize],
    queryFn: async () => {
      try {
        return await ApiClient.get<PagedResult<GoodsReceipt>>(
          `/api/goods-receipts?page=${page}&pageSize=${pageSize}`
        );
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

export function useGoodsReceipt(id: string | null) {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['goods-receipt', id],
    enabled: !!id,
    queryFn: async () => {
      if (!id) throw new Error('No ID provided');
      try {
        return await ApiClient.get<GoodsReceipt>(`/api/goods-receipts/${id}`);
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

export function useCreateGoodsReceipt() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: async (data: CreateGoodsReceiptRequest) => {
      return await ApiClient.post<GoodsReceipt>('/api/goods-receipts', {
        body: JSON.stringify(data),
      });
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['goods-receipts'] });
      queryClient.invalidateQueries({ queryKey: ['purchase-orders'] });
      toast({
        title: "Goods Receipt Created",
        description: `GRN ${data.grnNo} created successfully.`,
      });
      return data;
    },
    onError: (error) => {
      if (ErrorMapper.requiresLogin(error)) {
        navigate('/login');
        return;
      }
      const mappedError = ErrorMapper.mapError(error);
      toast({
        variant: "destructive",
        title: mappedError.title,
        description: mappedError.message,
      });
    },
  });
}

export function useReceiveGoodsReceipt() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: async (receiptId: string) => {
      return await ApiClient.post<GoodsReceipt>(`/api/goods-receipts/${receiptId}/receive`, {});
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['goods-receipts'] });
      queryClient.invalidateQueries({ queryKey: ['goods-receipt', data.id] });
      queryClient.invalidateQueries({ queryKey: ['purchase-orders'] });
      queryClient.invalidateQueries({ queryKey: ['stock-balance'] });
      toast({
        title: "Goods Receipt Received",
        description: `GRN ${data.grnNo} received successfully. Stock updated.`,
      });
      return data;
    },
    onError: (error) => {
      if (ErrorMapper.requiresLogin(error)) {
        navigate('/login');
        return;
      }
      const mappedError = ErrorMapper.mapError(error);
      toast({
        variant: "destructive",
        title: mappedError.title,
        description: mappedError.message,
      });
    },
  });
}
