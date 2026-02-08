import { useState } from 'react';
import { Plus, Building2, MapPin } from 'lucide-react';
import { useOrganizations } from '../hooks/useOrganizations';
import { useBranches } from '../hooks/useBranches';
import { useWarehousesByBranch } from '../hooks/useWarehouses';

export default function OrganizationSetupPage() {
  const [selectedOrgId, setSelectedOrgId] = useState<string>('');
  const [selectedBranchId, setSelectedBranchId] = useState<string>('');
  
  // Organizations
  const [showOrgForm, setShowOrgForm] = useState(false);
  const [newOrgCode, setNewOrgCode] = useState('');
  const [newOrgName, setNewOrgName] = useState('');
  const [newOrgTaxNumber, setNewOrgTaxNumber] = useState('');
  
  // Branches
  const [showBranchForm, setShowBranchForm] = useState(false);
  const [newBranchCode, setNewBranchCode] = useState('');
  const [newBranchName, setNewBranchName] = useState('');
  const [newBranchAddress, setNewBranchAddress] = useState('');
  
  // Warehouses
  const [showWarehouseForm, setShowWarehouseForm] = useState(false);
  const [newWarehouseCode, setNewWarehouseCode] = useState('');
  const [newWarehouseName, setNewWarehouseName] = useState('');
  const [newWarehouseAddress, setNewWarehouseAddress] = useState('');

  const { data: organizations, isLoading: orgsLoading } = useOrganizations();
  const { data: branches, isLoading: branchesLoading } = useBranches(selectedOrgId);
  const { data: warehouses, isLoading: warehousesLoading } = useWarehousesByBranch(selectedBranchId);

  const handleCreateOrg = async () => {
    if (!newOrgCode || !newOrgName) {
      alert('Code and Name are required');
      return;
    }

    try {
      const token = localStorage.getItem('authToken');
      const response = await fetch('http://localhost:5039/api/orgs', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          code: newOrgCode,
          name: newOrgName,
          taxNumber: newOrgTaxNumber || null,
        }),
      });

      if (response.ok) {
        alert('Organization created successfully!');
        setShowOrgForm(false);
        setNewOrgCode('');
        setNewOrgName('');
        setNewOrgTaxNumber('');
        window.location.reload(); // Refresh to see new data
      } else {
        const error = await response.json();
        alert(`Error: ${error.error || 'Failed to create organization'}`);
      }
    } catch (error) {
      alert('Network error');
    }
  };

  const handleCreateBranch = async () => {
    if (!selectedOrgId) {
      alert('Please select an organization first');
      return;
    }
    if (!newBranchCode || !newBranchName) {
      alert('Code and Name are required');
      return;
    }

    try {
      const token = localStorage.getItem('authToken');
      const response = await fetch(`http://localhost:5039/api/orgs/${selectedOrgId}/branches`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          code: newBranchCode,
          name: newBranchName,
          address: newBranchAddress || null,
        }),
      });

      if (response.ok) {
        alert('Branch created successfully!');
        setShowBranchForm(false);
        setNewBranchCode('');
        setNewBranchName('');
        setNewBranchAddress('');
        window.location.reload();
      } else {
        const error = await response.json();
        alert(`Error: ${error.error || 'Failed to create branch'}`);
      }
    } catch (error) {
      alert('Network error');
    }
  };

  const handleCreateWarehouse = async () => {
    if (!selectedBranchId) {
      alert('Please select a branch first');
      return;
    }
    if (!newWarehouseCode || !newWarehouseName) {
      alert('Code and Name are required');
      return;
    }

    try {
      const token = localStorage.getItem('authToken');
      const response = await fetch(`http://localhost:5039/api/branches/${selectedBranchId}/warehouses`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          code: newWarehouseCode,
          name: newWarehouseName,
          address: newWarehouseAddress || null,
          type: 'MAIN', // Default warehouse type
        }),
      });

      if (response.ok) {
        alert('Warehouse created successfully!');
        setShowWarehouseForm(false);
        setNewWarehouseCode('');
        setNewWarehouseName('');
        setNewWarehouseAddress('');
        window.location.reload();
      } else {
        const error = await response.json();
        alert(`Error: ${error.error || 'Failed to create warehouse'}`);
      }
    } catch (error) {
      alert('Network error');
    }
  };

  return (
    <div className="max-w-7xl mx-auto p-6 space-y-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Organization Setup</h1>
        <p className="text-gray-600 mt-1">
          Manage organizations, branches, and warehouses
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Organizations */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200">
          <div className="px-4 py-3 border-b border-gray-200 flex items-center justify-between">
            <h2 className="font-semibold text-gray-900 flex items-center gap-2">
              <Building2 className="h-5 w-5" />
              Organizations
            </h2>
            <button
              onClick={() => setShowOrgForm(!showOrgForm)}
              className="p-1 text-blue-600 hover:bg-blue-50 rounded"
            >
              <Plus className="h-5 w-5" />
            </button>
          </div>

          {showOrgForm && (
            <div className="p-4 bg-gray-50 border-b border-gray-200 space-y-3">
              <input
                type="text"
                placeholder="Code (e.g., ORG001)"
                value={newOrgCode}
                onChange={(e) => setNewOrgCode(e.target.value)}
                className="w-full px-3 py-2 border rounded-md text-sm"
              />
              <input
                type="text"
                placeholder="Name"
                value={newOrgName}
                onChange={(e) => setNewOrgName(e.target.value)}
                className="w-full px-3 py-2 border rounded-md text-sm"
              />
              <input
                type="text"
                placeholder="Tax Number (optional)"
                value={newOrgTaxNumber}
                onChange={(e) => setNewOrgTaxNumber(e.target.value)}
                className="w-full px-3 py-2 border rounded-md text-sm"
              />
              <div className="flex gap-2">
                <button
                  onClick={handleCreateOrg}
                  className="flex-1 px-3 py-2 bg-blue-600 text-white rounded-md text-sm hover:bg-blue-700"
                >
                  Create
                </button>
                <button
                  onClick={() => setShowOrgForm(false)}
                  className="flex-1 px-3 py-2 bg-gray-200 text-gray-700 rounded-md text-sm hover:bg-gray-300"
                >
                  Cancel
                </button>
              </div>
            </div>
          )}

          <div className="p-4 space-y-2 max-h-96 overflow-y-auto">
            {orgsLoading && <p className="text-sm text-gray-500">Loading...</p>}
            {organizations?.map((org: any) => (
              <div
                key={org.id}
                onClick={() => setSelectedOrgId(org.id)}
                className={`p-3 border rounded-md cursor-pointer hover:bg-gray-50 ${
                  selectedOrgId === org.id ? 'border-blue-500 bg-blue-50' : 'border-gray-200'
                }`}
              >
                <div className="font-medium text-sm">{org.name}</div>
                <div className="text-xs text-gray-500">{org.code}</div>
              </div>
            ))}
            {!orgsLoading && organizations?.length === 0 && (
              <p className="text-sm text-gray-500 text-center py-4">No organizations yet</p>
            )}
          </div>
        </div>

        {/* Branches */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200">
          <div className="px-4 py-3 border-b border-gray-200 flex items-center justify-between">
            <h2 className="font-semibold text-gray-900 flex items-center gap-2">
              <MapPin className="h-5 w-5" />
              Branches
            </h2>
            <button
              onClick={() => setShowBranchForm(!showBranchForm)}
              disabled={!selectedOrgId}
              className="p-1 text-blue-600 hover:bg-blue-50 rounded disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <Plus className="h-5 w-5" />
            </button>
          </div>

          {showBranchForm && (
            <div className="p-4 bg-gray-50 border-b border-gray-200 space-y-3">
              <input
                type="text"
                placeholder="Code (e.g., BR001)"
                value={newBranchCode}
                onChange={(e) => setNewBranchCode(e.target.value)}
                className="w-full px-3 py-2 border rounded-md text-sm"
              />
              <input
                type="text"
                placeholder="Name"
                value={newBranchName}
                onChange={(e) => setNewBranchName(e.target.value)}
                className="w-full px-3 py-2 border rounded-md text-sm"
              />
              <input
                type="text"
                placeholder="Address (optional)"
                value={newBranchAddress}
                onChange={(e) => setNewBranchAddress(e.target.value)}
                className="w-full px-3 py-2 border rounded-md text-sm"
              />
              <div className="flex gap-2">
                <button
                  onClick={handleCreateBranch}
                  className="flex-1 px-3 py-2 bg-blue-600 text-white rounded-md text-sm hover:bg-blue-700"
                >
                  Create
                </button>
                <button
                  onClick={() => setShowBranchForm(false)}
                  className="flex-1 px-3 py-2 bg-gray-200 text-gray-700 rounded-md text-sm hover:bg-gray-300"
                >
                  Cancel
                </button>
              </div>
            </div>
          )}

          <div className="p-4 space-y-2 max-h-96 overflow-y-auto">
            {!selectedOrgId && (
              <p className="text-sm text-gray-500 text-center py-4">
                Select an organization first
              </p>
            )}
            {selectedOrgId && branchesLoading && <p className="text-sm text-gray-500">Loading...</p>}
            {selectedOrgId && branches?.items?.map((branch: any) => (
              <div
                key={branch.id}
                onClick={() => setSelectedBranchId(branch.id)}
                className={`p-3 border rounded-md cursor-pointer hover:bg-gray-50 ${
                  selectedBranchId === branch.id ? 'border-blue-500 bg-blue-50' : 'border-gray-200'
                }`}
              >
                <div className="font-medium text-sm">{branch.name}</div>
                <div className="text-xs text-gray-500">{branch.code}</div>
              </div>
            ))}
            {selectedOrgId && !branchesLoading && branches?.items?.length === 0 && (
              <p className="text-sm text-gray-500 text-center py-4">No branches yet</p>
            )}
          </div>
        </div>

        {/* Warehouses */}
        <div className="bg-white rounded-lg shadow-sm border border-gray-200">
          <div className="px-4 py-3 border-b border-gray-200 flex items-center justify-between">
            <h2 className="font-semibold text-gray-900 flex items-center gap-2">
              <Building2 className="h-5 w-5" />
              Warehouses
            </h2>
            <button
              onClick={() => setShowWarehouseForm(!showWarehouseForm)}
              disabled={!selectedBranchId}
              className="p-1 text-blue-600 hover:bg-blue-50 rounded disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <Plus className="h-5 w-5" />
            </button>
          </div>

          {showWarehouseForm && (
            <div className="p-4 bg-gray-50 border-b border-gray-200 space-y-3">
              <input
                type="text"
                placeholder="Code (e.g., WH001)"
                value={newWarehouseCode}
                onChange={(e) => setNewWarehouseCode(e.target.value)}
                className="w-full px-3 py-2 border rounded-md text-sm"
              />
              <input
                type="text"
                placeholder="Name"
                value={newWarehouseName}
                onChange={(e) => setNewWarehouseName(e.target.value)}
                className="w-full px-3 py-2 border rounded-md text-sm"
              />
              <input
                type="text"
                placeholder="Address (optional)"
                value={newWarehouseAddress}
                onChange={(e) => setNewWarehouseAddress(e.target.value)}
                className="w-full px-3 py-2 border rounded-md text-sm"
              />
              <div className="flex gap-2">
                <button
                  onClick={handleCreateWarehouse}
                  className="flex-1 px-3 py-2 bg-blue-600 text-white rounded-md text-sm hover:bg-blue-700"
                >
                  Create
                </button>
                <button
                  onClick={() => setShowWarehouseForm(false)}
                  className="flex-1 px-3 py-2 bg-gray-200 text-gray-700 rounded-md text-sm hover:bg-gray-300"
                >
                  Cancel
                </button>
              </div>
            </div>
          )}

          <div className="p-4 space-y-2 max-h-96 overflow-y-auto">
            {!selectedBranchId && (
              <p className="text-sm text-gray-500 text-center py-4">
                Select a branch first
              </p>
            )}
            {selectedBranchId && warehousesLoading && <p className="text-sm text-gray-500">Loading...</p>}
            {warehouses?.map((warehouse: any) => (
              <div
                key={warehouse.id}
                className="p-3 border border-gray-200 rounded-md hover:bg-gray-50"
              >
                <div className="font-medium text-sm">{warehouse.name}</div>
                <div className="text-xs text-gray-500">{warehouse.code}</div>
              </div>
            ))}
            {selectedBranchId && !warehousesLoading && warehouses?.length === 0 && (
              <p className="text-sm text-gray-500 text-center py-4">No warehouses yet</p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
