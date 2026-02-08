import { useQueryClient } from '@tanstack/react-query';
import { useState, useEffect } from 'react';
import { ContextStore, AppContext } from '../lib/context-store';

/**
 * Hook for managing global app context (Branch/Warehouse selection)
 */
export function useAppContext() {
  const queryClient = useQueryClient();
  const [context, setContextState] = useState<AppContext>({ activeBranchId: null, activeWarehouseId: null });

  // Load context on mount
  useEffect(() => {
    ContextStore.getContext().then(setContextState);
  }, []);

  const setActiveBranch = async (branchId: string | null) => {
    await ContextStore.setActiveBranch(branchId);
    setContextState(prev => ({ ...prev, activeBranchId: branchId }));
    // Invalidate queries that depend on branch
    queryClient.invalidateQueries();
  };

  const setActiveWarehouse = async (warehouseId: string | null) => {
    await ContextStore.setActiveWarehouse(warehouseId);
    setContextState(prev => ({ ...prev, activeWarehouseId: warehouseId }));
    // Invalidate queries that depend on warehouse
    queryClient.invalidateQueries();
  };

  const clearContext = async () => {
    await ContextStore.clearContext();
    setContextState({ activeBranchId: null, activeWarehouseId: null });
    queryClient.invalidateQueries();
  };

  return {
    activeBranchId: context.activeBranchId,
    activeWarehouseId: context.activeWarehouseId,
    setActiveBranch,
    setActiveWarehouse,
    clearContext,
  };
}
