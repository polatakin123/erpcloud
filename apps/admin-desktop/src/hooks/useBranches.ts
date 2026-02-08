import { useQuery } from '@tanstack/react-query';
import { ApiClient } from '../lib/api-client';
import { Branch, Warehouse } from '../types/sales';
import { useToast } from './useToast';
import { ErrorMapper } from '../lib/error-mapper';
import { useNavigate } from 'react-router-dom';

interface BranchesResponse {
  items: Branch[];
  total: number;
}

// Branches - for specific organization
export function useBranches(organizationId?: string) {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['branches', organizationId],
    queryFn: async () => {
      try {
        if (!organizationId) return { items: [], total: 0 };
        const response = await ApiClient.get<BranchesResponse>(`/api/orgs/${organizationId}/branches`);
        return response;
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
    enabled: !!organizationId,
  });
}

// Warehouses
export function useWarehouses() {
  const { toast } = useToast();
  const navigate = useNavigate();

  return useQuery({
    queryKey: ['warehouses'],
    queryFn: async () => {
      try {
        const response = await ApiClient.get<Warehouse[]>('/api/warehouses');
        return response;
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
