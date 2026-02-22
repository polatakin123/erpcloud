import { useAppContext } from '../hooks/useAppContext';
import { useWarehousesByBranch } from '../hooks/useWarehouses';
import { useOrganizations } from '../hooks/useOrganizations';
import { useBranches } from '../hooks/useBranches';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from './ui/select';
import { AlertCircle, Settings } from 'lucide-react';

export function ContextBar() {
  const { activeOrgId, activeBranchId, activeWarehouseId, setActiveOrg, setActiveBranch, setActiveWarehouse } = useAppContext();
  const { data: organizations, isLoading: orgsLoading } = useOrganizations();
  
  // Get branches for selected organization
  const { data: branchesData, isLoading: branchesLoading } = useBranches(activeOrgId || undefined);
  const branches = branchesData?.items || [];
  
  // Get warehouses for selected branch
  const { data: warehouses, isLoading: warehousesLoading } = useWarehousesByBranch(activeBranchId || undefined);

  const showWarning = !activeBranchId || !activeWarehouseId;
  const apiUrl = (import.meta as any).env.VITE_API_BASE_URL || 'http://localhost:5039';

  return (
    <div className="flex items-center gap-4 px-4 py-2 bg-white border-b">
      {showWarning && (
        <div className="flex items-center gap-2 text-orange-600">
          <AlertCircle className="h-4 w-4" />
          <span className="text-sm font-medium">Please select Organization, Branch & Warehouse</span>
        </div>
      )}

      <div className="flex items-center gap-2">
        <label className="text-sm font-medium text-gray-700">Organization:</label>
        <Select
          value={activeOrgId || undefined}
          onValueChange={(value: string) => setActiveOrg(value)}
          disabled={orgsLoading}
        >
          <SelectTrigger className="w-[200px]">
            <SelectValue placeholder="Select Organization" />
          </SelectTrigger>
          <SelectContent>
            {organizations?.map((org) => (
              <SelectItem key={org.id} value={org.id}>
                {org.name} ({org.code})
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex items-center gap-2">
        <label className="text-sm font-medium text-gray-700">Branch:</label>
        <Select
          value={activeBranchId || undefined}
          onValueChange={(value: string) => setActiveBranch(value)}
          disabled={branchesLoading || !activeOrgId}
        >
          <SelectTrigger className="w-[200px]">
            <SelectValue placeholder="Select Branch" />
          </SelectTrigger>
          <SelectContent>
            {branches?.map((branch) => (
              <SelectItem key={branch.id} value={branch.id}>
                {branch.name} ({branch.code})
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex items-center gap-2">
        <label className="text-sm font-medium text-gray-700">Warehouse:</label>
        <Select
          value={activeWarehouseId || undefined}
          onValueChange={(value: string) => setActiveWarehouse(value)}
          disabled={warehousesLoading || !activeBranchId}
        >
          <SelectTrigger className="w-[200px]">
            <SelectValue placeholder="Select Warehouse" />
          </SelectTrigger>
          <SelectContent>
            {warehouses?.map((warehouse) => (
              <SelectItem key={warehouse.id} value={warehouse.id}>
                {warehouse.name} ({warehouse.code})
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="ml-auto flex items-center gap-4">
        <span className="text-xs text-gray-500">{apiUrl}</span>
        <button
          className="flex items-center gap-2 text-sm text-gray-600 hover:text-gray-900"
          onClick={() => {
            // TODO: Open settings dialog
            alert('Settings dialog coming soon');
          }}
        >
          <Settings className="h-4 w-4" />
          Settings
        </button>
      </div>
    </div>
  );
}
