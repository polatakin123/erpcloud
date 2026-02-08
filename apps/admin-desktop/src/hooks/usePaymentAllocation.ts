import { useMutation } from '@tanstack/react-query';
import { ApiClient } from '@/lib/api-client';

interface AutoAllocateRequest {
  invoiceIds?: string[];
}

interface AllocationInfo {
  invoiceId: string;
  invoiceNo: string;
  amount: number;
}

interface AutoAllocateResult {
  paymentId: string;
  allocatedTotal: number;
  remainingUnallocated: number;
  allocations: AllocationInfo[];
}

/**
 * Hook for auto-allocating payments to invoices.
 * Non-blocking operation for POS workflows.
 */
export function useAutoAllocatePayment() {
  return useMutation({
    mutationFn: async ({
      paymentId,
      invoiceIds,
    }: {
      paymentId: string;
      invoiceIds?: string[];
    }) => {
      const body: AutoAllocateRequest = invoiceIds ? { invoiceIds } : {};
      
      return await ApiClient.post<AutoAllocateResult>(
        `/api/payments/${paymentId}/auto-allocate`,
        body
      );
    },
  });
}
