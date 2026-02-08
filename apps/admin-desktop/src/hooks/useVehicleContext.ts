import { useState, useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { VehicleContextStore, VehicleContext } from '../lib/vehicle-context-store';

/**
 * Hook for managing global vehicle selection context
 * Similar pattern to useAppContext for Branch/Warehouse
 */
export function useVehicleContext() {
  const queryClient = useQueryClient();
  const [context, setContextState] = useState<VehicleContext>({
    selectedBrandId: null,
    selectedModelId: null,
    selectedYearId: null,
    selectedEngineId: null,
  });

  // Load context on mount
  useEffect(() => {
    VehicleContextStore.getContext().then(setContextState);
  }, []);

  const setSelectedBrand = async (brandId: string | null) => {
    await VehicleContextStore.setSelectedBrand(brandId);
    setContextState(prev => ({ 
      ...prev, 
      selectedBrandId: brandId,
      selectedModelId: null,
      selectedYearId: null,
      selectedEngineId: null,
    }));
    queryClient.invalidateQueries({ queryKey: ['vehicle-models'] });
  };

  const setSelectedModel = async (modelId: string | null) => {
    await VehicleContextStore.setSelectedModel(modelId);
    setContextState(prev => ({ 
      ...prev, 
      selectedModelId: modelId,
      selectedYearId: null,
      selectedEngineId: null,
    }));
    queryClient.invalidateQueries({ queryKey: ['vehicle-years'] });
  };

  const setSelectedYear = async (yearId: string | null) => {
    await VehicleContextStore.setSelectedYear(yearId);
    setContextState(prev => ({ 
      ...prev, 
      selectedYearId: yearId,
      selectedEngineId: null,
    }));
    queryClient.invalidateQueries({ queryKey: ['vehicle-engines'] });
  };

  const setSelectedEngine = async (engineId: string | null) => {
    await VehicleContextStore.setSelectedEngine(engineId);
    setContextState(prev => ({ ...prev, selectedEngineId: engineId }));
  };

  const clearVehicleSelection = async () => {
    await VehicleContextStore.clearContext();
    setContextState({
      selectedBrandId: null,
      selectedModelId: null,
      selectedYearId: null,
      selectedEngineId: null,
    });
    queryClient.invalidateQueries({ queryKey: ['vehicle-models'] });
    queryClient.invalidateQueries({ queryKey: ['vehicle-years'] });
    queryClient.invalidateQueries({ queryKey: ['vehicle-engines'] });
  };

  return {
    selectedBrandId: context.selectedBrandId,
    selectedModelId: context.selectedModelId,
    selectedYearId: context.selectedYearId,
    selectedEngineId: context.selectedEngineId,
    setSelectedBrand,
    setSelectedModel,
    setSelectedYear,
    setSelectedEngine,
    clearVehicleSelection,
  };
}
