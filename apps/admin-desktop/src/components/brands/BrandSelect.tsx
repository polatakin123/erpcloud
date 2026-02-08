import { useState, useEffect } from 'react';
import { useBrands } from '@/hooks/useBrands';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Plus, Check } from 'lucide-react';
import { BrandFormModal } from './BrandFormModal';
import type { Brand } from '@/types/brand';

interface BrandSelectProps {
  value?: string; // Brand ID (Guid)
  onChange: (brandId: string | undefined) => void;
  disabled?: boolean;
  error?: string;
}

export function BrandSelect({
  value,
  onChange,
  disabled = false,
  error,
}: BrandSelectProps) {
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [isOpen, setIsOpen] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [selectedBrand, setSelectedBrand] = useState<Brand | undefined>();

  // Debounce search (300ms)
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
    }, 300);
    return () => clearTimeout(timer);
  }, [search]);

  const { data: brands, isLoading } = useBrands(debouncedSearch, true, 50);

  // Find selected brand from value prop
  useEffect(() => {
    if (value && brands) {
      const brand = brands.find((b) => b.id === value);
      setSelectedBrand(brand);
      if (brand) {
        setSearch(''); // Clear search when brand is selected
      }
    } else {
      setSelectedBrand(undefined);
    }
  }, [value, brands]);

  const handleSelect = (brand: Brand) => {
    onChange(brand.id);
    setIsOpen(false);
    setSearch('');
  };

  const handleClear = () => {
    onChange(undefined);
    setSearch('');
  };

  const handleCreateSuccess = () => {
    setShowCreateModal(false);
    // The newly created brand will be auto-selected via query invalidation
  };

  return (
    <div className="relative">
      {/* Selected Brand Display / Search Input */}
      <div className="relative">
        {selectedBrand && !isOpen ? (
          <div className="flex items-center gap-2 h-10 w-full rounded-md border border-input bg-background px-3 py-2">
            {selectedBrand.logoUrl ? (
              <img
                src={selectedBrand.logoUrl}
                alt={selectedBrand.name}
                className="h-5 w-5 rounded-full object-cover"
                onError={(e) => {
                  const target = e.target as HTMLImageElement;
                  target.style.display = 'none';
                }}
              />
            ) : (
              <div className="h-5 w-5 rounded-full bg-blue-600 text-white flex items-center justify-center text-xs font-semibold">
                {selectedBrand.name.charAt(0).toUpperCase()}
              </div>
            )}
            <span className="flex-1 text-sm">{selectedBrand.name}</span>
            <span className="text-xs text-muted-foreground font-mono">{selectedBrand.code}</span>
            {!disabled && (
              <button
                type="button"
                onClick={handleClear}
                className="ml-2 text-gray-400 hover:text-gray-600"
                title="Temizle"
              >
                ×
              </button>
            )}
          </div>
        ) : (
          <Input
            type="text"
            placeholder="Marka ara (ad veya kod)..."
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setIsOpen(true);
            }}
            onFocus={() => setIsOpen(true)}
            disabled={disabled}
            className={error ? 'border-red-500' : ''}
          />
        )}
      </div>

      {/* Error Message */}
      {error && <div className="text-sm text-red-600 mt-1">{error}</div>}

      {/* Dropdown */}
      {isOpen && !disabled && (
        <>
          {/* Backdrop */}
          <div
            className="fixed inset-0 z-10"
            onClick={() => setIsOpen(false)}
          />

          {/* Dropdown Panel */}
          <div className="absolute z-20 mt-1 w-full bg-white border border-gray-200 rounded-md shadow-lg max-h-80 overflow-auto">
            {isLoading ? (
              <div className="p-4 text-center text-sm text-gray-500">
                Yükleniyor...
              </div>
            ) : brands && brands.length > 0 ? (
              <>
                {brands.map((brand) => (
                  <button
                    key={brand.id}
                    type="button"
                    onClick={() => handleSelect(brand)}
                    className="w-full flex items-center gap-3 px-3 py-2 hover:bg-gray-50 text-left"
                  >
                    {brand.logoUrl ? (
                      <img
                        src={brand.logoUrl}
                        alt={brand.name}
                        className="h-6 w-6 rounded-full object-cover"
                        onError={(e) => {
                          const target = e.target as HTMLImageElement;
                          target.style.display = 'none';
                        }}
                      />
                    ) : (
                      <div className="h-6 w-6 rounded-full bg-blue-600 text-white flex items-center justify-center text-xs font-semibold">
                        {brand.name.charAt(0).toUpperCase()}
                      </div>
                    )}
                    <div className="flex-1">
                      <div className="text-sm font-medium">{brand.name}</div>
                      <div className="text-xs text-gray-500 font-mono">{brand.code}</div>
                    </div>
                    {value === brand.id && (
                      <Check className="h-4 w-4 text-green-600" />
                    )}
                  </button>
                ))}
                {/* Add New Brand Button */}
                <div className="border-t">
                  <button
                    type="button"
                    onClick={() => {
                      setShowCreateModal(true);
                      setIsOpen(false);
                    }}
                    className="w-full flex items-center gap-2 px-3 py-2 hover:bg-blue-50 text-blue-600 text-sm font-medium"
                  >
                    <Plus className="h-4 w-4" />
                    Yeni Marka Ekle
                  </button>
                </div>
              </>
            ) : (
              <div className="p-4">
                <div className="text-center text-sm text-gray-500 mb-3">
                  Marka bulunamadı
                </div>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    setShowCreateModal(true);
                    setIsOpen(false);
                  }}
                  className="w-full"
                >
                  <Plus className="h-4 w-4 mr-2" />
                  Yeni Marka Ekle
                </Button>
              </div>
            )}
          </div>
        </>
      )}

      {/* Create Brand Modal */}
      <BrandFormModal
        open={showCreateModal}
        onOpenChange={setShowCreateModal}
        onSuccess={handleCreateSuccess}
      />
    </div>
  );
}
