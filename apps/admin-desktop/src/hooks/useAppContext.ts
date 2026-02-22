import { useQueryClient } from '@tanstack/react-query';
import { useState, useEffect } from 'react';
import { ContextStore, AppContext } from '../lib/context-store';

/**
 * Hook for managing global app context (Branch/Warehouse selection)
 */
export function useAppContext() {
  const queryClient = useQueryClient();
  const [context, setContextState] = useState<AppContext>({ activeOrgId: null, activeBranchId: null, activeWarehouseId: null });

  // Load context on mount
  useEffect(() => {
    ContextStore.getContext().then(setContextState);
  }, []);

  const setActiveOrg = async (orgId: string | null) => {
    await ContextStore.setActiveOrg(orgId);
    setContextState(prev => ({ ...prev, activeOrgId: orgId, activeBranchId: null, activeWarehouseId: null }));
    queryClient.invalidateQueries();
  };

  const setActiveBranch = async (branchId: string | null) => {
    await ContextStore.setActiveBranch(branchId);
    setContextState(prev => ({ ...prev, activeBranchId: branchId, activeWarehouseId: null }));
    queryClient.invalidateQueries();
  };

  const setActiveWarehouse = async (warehouseId: string | null) => {
    await ContextStore.setActiveWarehouse(warehouseId);
    setContextState(prev => ({ ...prev, activeWarehouseId: warehouseId }));
    queryClient.invalidateQueries();
  };

  const clearContext = async () => {
    await ContextStore.clearContext();
    setContextState({ activeOrgId: null, activeBranchId: null, activeWarehouseId: null });
    queryClient.invalidateQueries();
  };

  return {
    activeOrgId: context.activeOrgId,
    activeBranchId: context.activeBranchId,
    activeWarehouseId: context.activeWarehouseId,
    setActiveOrg,
    setActiveBranch,
    setActiveWarehouse,
    clearContext,
  };
}
