import { useAppContext } from '../hooks/useAppContext';
import { useBranches, useWarehouses } from '../hooks/useBranches';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from './ui/select';
import { AlertCircle, Settings } from 'lucide-react';

export function ContextBar() {
  const { activeBranchId, activeWarehouseId, setActiveBranch, setActiveWarehouse } = useAppContext();
  const { data: branches, isLoading: branchesLoading } = useBranches();
  const { data: warehouses, isLoading: warehousesLoading } = useWarehouses();

  const showWarning = !activeBranchId || !activeWarehouseId;
  const apiUrl = (import.meta as any).env.VITE_API_BASE_URL || 'http://localhost:5039';

  return (
    <div className="flex items-center gap-4 px-4 py-2 bg-white border-b">
      {showWarning && (
        <div className="flex items-center gap-2 text-orange-600">
          <AlertCircle className="h-4 w-4" />
          <span className="text-sm font-medium">Please select Branch & Warehouse</span>
        </div>
      )}

      <div className="flex items-center gap-2">
        <label className="text-sm font-medium text-gray-700">Branch:</label>
        <Select
          value={activeBranchId || undefined}
          onValueChange={(value: string) => setActiveBranch(value)}
          disabled={branchesLoading}
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
          disabled={warehousesLoading}
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
