import { useState } from 'react';
import { Plus, Tag, Trash2, X } from 'lucide-react';
import {
  usePartReferences,
  useCreatePartReference,
  useDeletePartReference,
} from '../hooks/usePartReferences';

interface OemReferencePanelProps {
  variantId: string;
  variantName?: string;
}

const REF_TYPES = [
  { value: 'OEM', label: 'OEM' },
  { value: 'AFTERMARKET', label: 'Aftermarket' },
  { value: 'SUPPLIER', label: 'Supplier' },
  { value: 'BARCODE', label: 'Barcode' },
];

export default function OemReferencePanel({ variantId, variantName }: OemReferencePanelProps) {
  const [isAdding, setIsAdding] = useState(false);
  const [newRefType, setNewRefType] = useState('OEM');
  const [newRefCode, setNewRefCode] = useState('');
  const [error, setError] = useState('');

  const { data: references, isLoading } = usePartReferences(variantId);
  const createMutation = useCreatePartReference();
  const deleteMutation = useDeletePartReference();

  const handleAddReference = async () => {
    setError('');

    if (!newRefCode.trim()) {
      setError('Reference code is required');
      return;
    }

    if (newRefCode.length < 3) {
      setError('Reference code must be at least 3 characters');
      return;
    }

    try {
      await createMutation.mutateAsync({
        variantId,
        data: {
          refType: newRefType,
          refCode: newRefCode.trim(),
        },
      });

      // Reset form
      setNewRefCode('');
      setIsAdding(false);
      setError('');
    } catch (err: any) {
      const errorMsg = err.response?.data?.message || 'Failed to add reference';
      setError(errorMsg);
    }
  };

  const handleDelete = async (referenceId: string) => {
    if (!confirm('Are you sure you want to delete this reference?')) {
      return;
    }

    try {
      await deleteMutation.mutateAsync({ variantId, referenceId });
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to delete reference');
    }
  };

  if (isLoading) {
    return (
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <div className="animate-pulse">
          <div className="h-4 bg-gray-200 rounded w-1/4 mb-4"></div>
          <div className="space-y-2">
            <div className="h-8 bg-gray-200 rounded"></div>
            <div className="h-8 bg-gray-200 rounded"></div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200">
      {/* Header */}
      <div className="px-6 py-4 border-b border-gray-200">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-lg font-semibold text-gray-900">
              OEM & Alternative References
            </h3>
            {variantName && (
              <p className="text-sm text-gray-600 mt-1">{variantName}</p>
            )}
          </div>
          {!isAdding && (
            <button
              onClick={() => setIsAdding(true)}
              className="inline-flex items-center px-3 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            >
              <Plus className="h-4 w-4 mr-1" />
              Add Reference
            </button>
          )}
        </div>
      </div>

      {/* Add Reference Form */}
      {isAdding && (
        <div className="px-6 py-4 bg-gray-50 border-b border-gray-200">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Type
              </label>
              <select
                value={newRefType}
                onChange={(e) => setNewRefType(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
              >
                {REF_TYPES.map((type) => (
                  <option key={type.value} value={type.value}>
                    {type.label}
                  </option>
                ))}
              </select>
            </div>
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Reference Code
              </label>
              <div className="flex gap-2">
                <input
                  type="text"
                  value={newRefCode}
                  onChange={(e) => setNewRefCode(e.target.value)}
                  onKeyPress={(e) => {
                    if (e.key === 'Enter') handleAddReference();
                  }}
                  placeholder="e.g., 12345-67890"
                  className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
                  autoFocus
                />
                <button
                  onClick={handleAddReference}
                  disabled={createMutation.isPending}
                  className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
                >
                  {createMutation.isPending ? 'Adding...' : 'Add'}
                </button>
                <button
                  onClick={() => {
                    setIsAdding(false);
                    setNewRefCode('');
                    setError('');
                  }}
                  className="px-4 py-2 bg-gray-200 text-gray-700 rounded-md hover:bg-gray-300"
                >
                  <X className="h-4 w-4" />
                </button>
              </div>
            </div>
          </div>
          {error && (
            <div className="mt-2 text-sm text-red-600">
              {error}
            </div>
          )}
        </div>
      )}

      {/* References List */}
      <div className="px-6 py-4">
        {!references || references.length === 0 ? (
          <div className="text-center py-8 text-gray-500">
            <Tag className="h-12 w-12 mx-auto mb-3 text-gray-400" />
            <p>No references added yet</p>
            <p className="text-sm mt-1">
              Add OEM or alternative part codes to enable equivalent part search
            </p>
          </div>
        ) : (
          <div className="space-y-3">
            {REF_TYPES.map((type) => {
              const typeRefs = references.filter((r) => r.refType === type.value);
              if (typeRefs.length === 0) return null;

              return (
                <div key={type.value} className="border-l-4 border-blue-500 pl-4">
                  <div className="text-sm font-medium text-gray-700 mb-2">
                    {type.label} ({typeRefs.length})
                  </div>
                  <div className="flex flex-wrap gap-2">
                    {typeRefs.map((ref) => (
                      <div
                        key={ref.id}
                        className="inline-flex items-center gap-2 px-3 py-1.5 bg-blue-50 border border-blue-200 rounded-md group"
                      >
                        <Tag className="h-4 w-4 text-blue-600" />
                        <span className="text-sm font-mono text-gray-900">
                          {ref.refCode}
                        </span>
                        <button
                          onClick={() => handleDelete(ref.id)}
                          disabled={deleteMutation.isPending}
                          className="opacity-0 group-hover:opacity-100 transition-opacity ml-1 text-red-600 hover:text-red-700 disabled:opacity-50"
                          title="Delete reference"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </div>
                    ))}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Info Footer */}
      <div className="px-6 py-3 bg-gray-50 border-t border-gray-200">
        <p className="text-xs text-gray-600">
          <strong>Note:</strong> Parts sharing the same OEM code are considered equivalent.
          Use Fast Search to find all equivalent parts automatically.
        </p>
      </div>
    </div>
  );
}
