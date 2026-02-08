import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../lib/api-client';

// ===== DTOs =====
export interface VehicleBrandDto {
  id: string;
  code: string;
  name: string;
}

export interface CreateVehicleBrandDto {
  code: string;
  name: string;
}

export interface UpdateVehicleBrandDto {
  code: string;
  name: string;
}

export interface VehicleModelDto {
  id: string;
  brandId: string;
  brandName: string;
  name: string;
}

export interface CreateVehicleModelDto {
  brandId: string;
  name: string;
}

export interface UpdateVehicleModelDto {
  brandId: string;
  name: string;
}

export interface VehicleYearRangeDto {
  id: string;
  modelId: string;
  yearFrom: number;
  yearTo: number;
  displayName: string;
}

export interface CreateVehicleYearRangeDto {
  modelId: string;
  yearFrom: number;
  yearTo: number;
}

export interface UpdateVehicleYearRangeDto {
  modelId: string;
  yearFrom: number;
  yearTo: number;
}

export interface VehicleEngineDto {
  id: string;
  yearRangeId: string;
  code: string;
  fuelType: string;
  displayName: string;
}

export interface CreateVehicleEngineDto {
  yearRangeId: string;
  code: string;
  fuelType: string;
}

export interface UpdateVehicleEngineDto {
  yearRangeId: string;
  code: string;
  fuelType: string;
}

export interface StockCardFitmentDto {
  id: string;
  variantId: string;
  vehicleEngineId: string;
  brandName: string;
  modelName: string;
  yearRange: string;
  engineCode: string;
  fuelType: string;
  fullDisplay: string;
  notes?: string;
}

export interface CreateStockCardFitmentDto {
  vehicleEngineId: string;
  notes?: string;
}

// ===== BRANDS =====
export function useVehicleBrands() {
  return useQuery({
    queryKey: ['vehicle-brands'],
    queryFn: async () => {
      return await apiClient.get<VehicleBrandDto[]>('/api/vehicles/brands');
    }
  });
}

export function useVehicleBrand(id: string | undefined) {
  return useQuery({
    queryKey: ['vehicle-brands', id],
    queryFn: async () => {
      return await apiClient.get<VehicleBrandDto>(`/api/vehicles/brands/${id}`);
    },
    enabled: !!id
  });
}

export function useCreateVehicleBrand() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (dto: CreateVehicleBrandDto) => {
      return await apiClient.post<VehicleBrandDto>('/api/vehicles/brands', dto);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicle-brands'] });
    }
  });
}

export function useUpdateVehicleBrand() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, dto }: { id: string; dto: UpdateVehicleBrandDto }) => {
      await apiClient.put(`/api/vehicles/brands/${id}`, dto);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicle-brands'] });
    }
  });
}

export function useDeleteVehicleBrand() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/api/vehicles/brands/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicle-brands'] });
    }
  });
}

// ===== MODELS =====
export function useVehicleModels(brandId?: string) {
  return useQuery({
    queryKey: ['vehicle-models', brandId],
    queryFn: async () => {
      const url = brandId 
        ? `/api/vehicles/models?brandId=${brandId}`
        : '/api/vehicles/models';
      return await apiClient.get<VehicleModelDto[]>(url);
    }
  });
}

export function useVehicleModel(id: string | undefined) {
  return useQuery({
    queryKey: ['vehicle-models', id],
    queryFn: async () => {
      return await apiClient.get<VehicleModelDto>(`/api/vehicles/models/${id}`);
    },
    enabled: !!id
  });
}

export function useCreateVehicleModel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (dto: CreateVehicleModelDto) => {
      return await apiClient.post<VehicleModelDto>('/api/vehicles/models', dto);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicle-models'] });
    }
  });
}

export function useUpdateVehicleModel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, dto }: { id: string; dto: UpdateVehicleModelDto }) => {
      await apiClient.put(`/api/vehicles/models/${id}`, dto);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicle-models'] });
    }
  });
}

export function useDeleteVehicleModel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/api/vehicles/models/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicle-models'] });
    }
  });
}

// ===== YEAR RANGES =====
export function useVehicleYearRanges(modelId?: string) {
  return useQuery({
    queryKey: ['vehicle-years', modelId],
    queryFn: async () => {
      const url = modelId 
        ? `/api/vehicles/years?modelId=${modelId}`
        : '/api/vehicles/years';
      return await apiClient.get<VehicleYearRangeDto[]>(url);
    }
  });
}

export function useVehicleYearRange(id: string | undefined) {
  return useQuery({
    queryKey: ['vehicle-years', id],
    queryFn: async () => {
      return await apiClient.get<VehicleYearRangeDto>(`/api/vehicles/years/${id}`);
    },
    enabled: !!id
  });
}

export function useCreateVehicleYearRange() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (dto: CreateVehicleYearRangeDto) => {
      return await apiClient.post<VehicleYearRangeDto>('/api/vehicles/years', dto);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicle-years'] });
    }
  });
}

export function useUpdateVehicleYearRange() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, dto }: { id: string; dto: UpdateVehicleYearRangeDto }) => {
      await apiClient.put(`/api/vehicles/years/${id}`, dto);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicle-years'] });
    }
  });
}

export function useDeleteVehicleYearRange() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/api/vehicles/years/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicle-years'] });
    }
  });
}

// ===== ENGINES =====
export function useVehicleEngines(yearRangeId?: string) {
  return useQuery({
    queryKey: ['vehicle-engines', yearRangeId],
    queryFn: async () => {
      const url = yearRangeId 
        ? `/api/vehicles/engines?yearRangeId=${yearRangeId}`
        : '/api/vehicles/engines';
      return await apiClient.get<VehicleEngineDto[]>(url);
    }
  });
}

export function useVehicleEngine(id: string | undefined) {
  return useQuery({
    queryKey: ['vehicle-engines', id],
    queryFn: async () => {
      return await apiClient.get<VehicleEngineDto>(`/api/vehicles/engines/${id}`);
    },
    enabled: !!id
  });
}

export function useCreateVehicleEngine() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (dto: CreateVehicleEngineDto) => {
      return await apiClient.post<VehicleEngineDto>('/api/vehicles/engines', dto);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicle-engines'] });
    }
  });
}

export function useUpdateVehicleEngine() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, dto }: { id: string; dto: UpdateVehicleEngineDto }) => {
      await apiClient.put(`/api/vehicles/engines/${id}`, dto);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicle-engines'] });
    }
  });
}

export function useDeleteVehicleEngine() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/api/vehicles/engines/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vehicle-engines'] });
    }
  });
}

// ===== FITMENTS =====
export function useStockCardFitments(variantId: string | undefined) {
  return useQuery({
    queryKey: ['stock-card-fitments', variantId],
    queryFn: async () => {
      return await apiClient.get<StockCardFitmentDto[]>(`/api/vehicles/fitments/variant/${variantId}`);
    },
    enabled: !!variantId
  });
}

export function useCreateStockCardFitment(variantId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (dto: CreateStockCardFitmentDto) => {
      return await apiClient.post<StockCardFitmentDto>(`/api/vehicles/fitments/variant/${variantId}`, dto);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stock-card-fitments', variantId] });
    }
  });
}

export function useDeleteStockCardFitment(variantId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (fitmentId: string) => {
      await apiClient.delete(`/api/vehicles/fitments/variant/${variantId}/${fitmentId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stock-card-fitments', variantId] });
    }
  });
}
