import { useQuery } from '@tanstack/react-query';
import { ApiClient } from '../lib/api-client';

interface Organization {
  id: string;
  code: string;
  name: string;
  taxNumber?: string;
}

interface OrganizationsResponse {
  items: Organization[];
  total: number;
  page: number;
  pageSize: number;
}

export function useOrganizations() {
  return useQuery({
    queryKey: ['organizations'],
    queryFn: async () => {
      const response = await ApiClient.get<OrganizationsResponse>('/api/orgs?page=1&size=100');
      return response.items; // Return only the items array
    },
  });
}
