import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiClient } from '@/lib/api-client';
import type { Party, CreatePartyDto, PartySearchResult } from '@/types/party';

export function useParties(q?: string, page = 1, size = 50) {
  return useQuery<PartySearchResult>({
    queryKey: ['parties', q, page, size],
    queryFn: async () => {
      const params = new URLSearchParams({
        page: page.toString(),
        size: size.toString(),
      });
      if (q) params.append('q', q);
      
      return ApiClient.get(`/api/parties?${params.toString()}`);
    },
  });
}

export function useCreateParty() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (data: CreatePartyDto) => {
      return ApiClient.post<Party>('/api/parties', data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['parties'] });
    },
  });
}

export function useParty(id: string) {
  return useQuery<Party>({
    queryKey: ['party', id],
    queryFn: () => ApiClient.get(`/api/parties/${id}`),
    enabled: !!id,
  });
}
