import { useMutation, useQuery } from '@tanstack/react-query';
import { ApiClient } from '../lib/api-client';
import { useToast } from './useToast';
import { ErrorMapper } from '../lib/error-mapper';
import { useNavigate } from 'react-router-dom';

/**
 * Pricing calculation request
 */
export interface PricingCalculationRequest {
  partyId: string;
  variantId: string;
  quantity: number;
  warehouseId?: string;
  currency?: string;
}

/**
 * Pricing calculation result with full breakdown
 */
export interface PricingCalculationResult {
  variantId: string;
  variantSku: string;
  variantName: string;
  quantity: number;
  currency: string;
  
  // Pricing breakdown
  listPrice: number;
  discountPercent?: number;
  discountAmount?: number;
  netPrice: number;
  lineTotal: number;
  
  // Cost & profit
  unitCost?: number;
  profit?: number;
  profitPercent?: number;
  
  // Rule information
  appliedRuleId?: string;
  appliedRuleScope?: string;
  appliedRuleType?: string;
  ruleDescription?: string;
  
  // Warnings
  hasWarning: boolean;
  warningMessage?: string;
}

/**
 * Hook for calculating pricing with discount rules and profit analysis
 */
export function usePricingCalculation() {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: async (request: PricingCalculationRequest) => {
      try {
        return await ApiClient.post<PricingCalculationResult>(
          '/api/pricing/calculate',
          request
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

/**
 * Hook for batch pricing calculation
 */
export function usePricingCalculationBatch() {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: async (requests: PricingCalculationRequest[]) => {
      try {
        return await ApiClient.post<PricingCalculationResult[]>(
          '/api/pricing/calculate/batch',
          requests
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

/**
 * Auto-calculate pricing when line item changes (for real-time pricing display)
 */
export function useAutoPricing(
  partyId: string | undefined,
  variantId: string | undefined,
  quantity: number,
  warehouseId?: string,
  currency: string = 'TRY'
) {
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['pricing', partyId, variantId, quantity, warehouseId, currency],
    enabled: !!partyId && !!variantId && quantity > 0,
    queryFn: async () => {
      if (!partyId || !variantId) {
        throw new Error('Party ID and Variant ID are required');
      }

      try {
        const request: PricingCalculationRequest = {
          partyId,
          variantId,
          quantity,
          warehouseId,
          currency,
        };

        return await ApiClient.post<PricingCalculationResult>(
          '/api/pricing/calculate',
          request
        );
      } catch (error) {
        if (ErrorMapper.requiresLogin(error)) {
          navigate('/login');
          throw error;
        }
        // Don't show toast for auto-pricing errors to avoid spam
        throw error;
      }
    },
    // Refetch less aggressively for auto-pricing
    staleTime: 30000, // 30 seconds
    gcTime: 60000, // 1 minute
    retry: 1,
  });
}
