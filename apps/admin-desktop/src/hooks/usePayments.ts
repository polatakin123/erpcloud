import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiClient } from '@/lib/api-client';
import type { Cashbox, BankAccount, CreatePaymentDto, Payment } from '@/types/payment';

export function useCashboxes() {
  return useQuery<{ items: Cashbox[] }>({
    queryKey: ['cashboxes'],
    queryFn: () => ApiClient.get('/api/cashboxes?page=1&size=100'),
  });
}

export function useBankAccounts() {
  return useQuery<{ items: BankAccount[] }>({
    queryKey: ['bankAccounts'],
    queryFn: () => ApiClient.get('/api/bank-accounts?page=1&size=100'),
  });
}

export function useCreatePayment() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (data: CreatePaymentDto) => {
      return ApiClient.post<Payment>('/api/payments', data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['payments'] });
    },
  });
}
