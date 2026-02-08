import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiClient } from '../lib/api-client';
import { Cashbox, BankAccount } from '../types/sales';
import { useToast } from './useToast';
import { ErrorMapper } from '../lib/error-mapper';
import { useNavigate } from 'react-router-dom';

export interface CreatePaymentRequest {
  partyId: string;
  amount: number;
  currency: string;
  paymentDate: string;
  cashboxId?: string | null;
  bankAccountId?: string | null;
  note?: string | null;
}

export interface Payment {
  id: string;
  partyId: string;
  amount: number;
  currency: string;
  paymentDate: string;
  cashboxId?: string | null;
  bankAccountId?: string | null;
  note?: string | null;
  createdAt: string;
}

// Cashboxes
export function useCashboxes() {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['cashboxes'],
    queryFn: async () => {
      try {
        return await ApiClient.get<Cashbox[]>('/api/cashboxes');
      } catch (error) {
        if (ErrorMapper.requiresLogin(error)) {
          toast({
            variant: "destructive",
            title: "Session Expired",
            description: "Please log in again.",
          });
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

// Bank Accounts
export function useBankAccounts() {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['bank-accounts'],
    queryFn: async () => {
      try {
        return await ApiClient.get<BankAccount[]>('/api/bank-accounts');
      } catch (error) {
        if (ErrorMapper.requiresLogin(error)) {
          toast({
            variant: "destructive",
            title: "Session Expired",
            description: "Please log in again.",
          });
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

// Create Payment
export function useCreatePayment() {
  const queryClient = useQueryClient();
  const { toast } = useToast();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: async (data: CreatePaymentRequest) => {
      return await ApiClient.post<Payment>('/api/payments', {
        body: JSON.stringify(data),
      });
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['payments'] });
      queryClient.invalidateQueries({ queryKey: ['cashboxes'] });
      queryClient.invalidateQueries({ queryKey: ['bank-accounts'] });
      queryClient.invalidateQueries({ queryKey: ['party-ledger'] });
      toast({
        title: "Payment Created",
        description: `Payment of ${data.amount} ${data.currency} created successfully.`,
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
