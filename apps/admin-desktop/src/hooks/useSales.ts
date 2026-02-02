import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiClient } from '../lib/api-client';
import { 
  SalesOrder, 
  CreateSalesOrderRequest, 
  Shipment, 
  CreateShipmentRequest,
  Invoice,
  PagedResult 
} from '../types/sales';
import { useToast } from './useToast';
import { ErrorMapper } from '../lib/error-mapper';
import { useNavigate } from 'react-router-dom';

// ============ Sales Orders ============

export function useSalesOrders(page: number = 1, pageSize: number = 50) {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['sales-orders', page, pageSize],
    queryFn: async () => {
      try {
        const response = await ApiClient.get<PagedResult<SalesOrder>>(
          `/api/sales-orders?page=${page}&pageSize=${pageSize}`
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

export function useSalesOrder(id: string | null) {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['sales-order', id],
    enabled: !!id,
    queryFn: async () => {
      if (!id) throw new Error('No ID provided');
      try {
        const response = await ApiClient.get<SalesOrder>(`/api/sales-orders/${id}`);
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

export function useCreateSalesOrder() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: async (data: CreateSalesOrderRequest) => {
      return await ApiClient.post<SalesOrder>('/api/sales-orders', {
        body: JSON.stringify(data),
      });
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['sales-orders'] });
      toast({
        title: "Sales Order Created",
        description: `Order ${data.orderNo} created successfully.`,
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

export function useConfirmSalesOrder() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: async (orderId: string) => {
      return await ApiClient.post<SalesOrder>(`/api/sales-orders/${orderId}/confirm`, {});
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['sales-orders'] });
      queryClient.invalidateQueries({ queryKey: ['sales-order', data.id] });
      toast({
        title: "Sales Order Confirmed",
        description: `Order ${data.orderNo} confirmed successfully.`,
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

// ============ Shipments ============

export function useShipments(page: number = 1, pageSize: number = 50) {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['shipments', page, pageSize],
    queryFn: async () => {
      try {
        const response = await ApiClient.get<PagedResult<Shipment>>(
          `/api/shipments?page=${page}&pageSize=${pageSize}`
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

export function useShipment(id: string | null) {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['shipment', id],
    enabled: !!id,
    queryFn: async () => {
      if (!id) throw new Error('No ID provided');
      try {
        const response = await ApiClient.get<Shipment>(`/api/shipments/${id}`);
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

export function useCreateShipment() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: async (data: CreateShipmentRequest) => {
      return await ApiClient.post<Shipment>('/api/shipments', {
        body: JSON.stringify(data),
      });
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['shipments'] });
      queryClient.invalidateQueries({ queryKey: ['sales-orders'] });
      toast({
        title: "Shipment Created",
        description: `Shipment ${data.shipmentNo} created successfully.`,
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

export function useShipShipment() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: async (shipmentId: string) => {
      return await ApiClient.post<Shipment>(`/api/shipments/${shipmentId}/ship`, {});
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['shipments'] });
      queryClient.invalidateQueries({ queryKey: ['shipment', data.id] });
      queryClient.invalidateQueries({ queryKey: ['sales-orders'] });
      toast({
        title: "Shipment Shipped",
        description: `Shipment ${data.shipmentNo} shipped successfully.`,
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

// ============ Invoices ============

export function useInvoices(page: number = 1, pageSize: number = 50) {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['invoices', page, pageSize],
    queryFn: async () => {
      try {
        const response = await ApiClient.get<PagedResult<Invoice>>(
          `/api/invoices?page=${page}&pageSize=${pageSize}`
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

export function useInvoice(id: string | null) {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['invoice', id],
    enabled: !!id,
    queryFn: async () => {
      if (!id) throw new Error('No ID provided');
      try {
        const response = await ApiClient.get<Invoice>(`/api/invoices/${id}`);
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

export function useInvoiceFromShipmentPreview(shipmentId: string | null) {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['invoice-preview', shipmentId],
    enabled: !!shipmentId,
    queryFn: async () => {
      if (!shipmentId) throw new Error('No shipmentId provided');
      try {
        const response = await ApiClient.get<Invoice>(`/api/shipments/${shipmentId}/invoice-preview`);
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

export function useCreateInvoiceFromShipment() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: async (shipmentId: string) => {
      return await ApiClient.post<Invoice>(`/api/shipments/${shipmentId}/invoice`, {});
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['invoices'] });
      queryClient.invalidateQueries({ queryKey: ['shipments'] });
      toast({
        title: "Invoice Created",
        description: `Invoice ${data.invoiceNo} created successfully.`,
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

export function useIssueInvoice() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: async (invoiceId: string) => {
      return await ApiClient.post<Invoice>(`/api/invoices/${invoiceId}/issue`, {});
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['invoices'] });
      queryClient.invalidateQueries({ queryKey: ['invoice', data.id] });
      toast({
        title: "Invoice Issued",
        description: `Invoice ${data.invoiceNo} issued successfully.`,
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
