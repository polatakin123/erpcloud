import { useState } from 'react';
import { useBrands, useDeleteBrand } from '@/hooks/useBrands';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card } from '@/components/ui/card';
import { Search, Plus, Edit, Power, Trash2 } from 'lucide-react';
import { BrandFormModal } from '@/components/brands/BrandFormModal';
import { ConfirmDialog } from '@/components/shared/ConfirmDialog';
import type { Brand } from '@/types/brand';
import { useToast } from '@/hooks/useToast';

export function BrandListPage() {
  const { toast } = useToast();
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [showFormModal, setShowFormModal] = useState(false);
  const [selectedBrand, setSelectedBrand] = useState<Brand | undefined>();
  const [brandToDelete, setBrandToDelete] = useState<Brand | null>(null);

  // Debounce search (300ms)
  React.useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
    }, 300);
    return () => clearTimeout(timer);
  }, [search]);

  const { data: brands, isLoading } = useBrands(debouncedSearch, undefined, 100);
  const deleteMutation = useDeleteBrand();

  const handleEdit = (brand: Brand) => {
    setSelectedBrand(brand);
    setShowFormModal(true);
  };

  const handleCreate = () => {
    setSelectedBrand(undefined);
    setShowFormModal(true);
  };

  const handleDelete = async () => {
    if (!brandToDelete) return;

    try {
      const result = await deleteMutation.mutateAsync(brandToDelete.id);
      
      if (result.wasSoftDeleted) {
        toast({
          title: 'Marka pasif hale getirildi',
          description: 'Bu marka kullanımda olduğu için silinemez. Pasif hale getirildi.',
        });
      } else {
        toast({
          title: 'Marka silindi',
          description: `${brandToDelete.name} başarıyla silindi.`,
        });
      }
    } catch (error) {
      toast({
        variant: 'destructive',
        title: 'Hata',
        description: 'Marka silinirken bir hata oluştu.',
      });
    } finally {
      setBrandToDelete(null);
    }
  };

  const handleToggleActive = async (brand: Brand) => {
    // Toggle by editing
    setSelectedBrand({
      ...brand,
      isActive: !brand.isActive,
    });
    setShowFormModal(true);
  };

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Markalar</h1>
        <Button onClick={handleCreate}>
          <Plus className="h-4 w-4 mr-2" />
          Yeni Marka
        </Button>
      </div>

      {/* Search */}
      <Card className="p-4 mb-6">
        <div className="flex gap-4 items-center">
          <div className="flex-1 relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
            <Input
              type="text"
              placeholder="Marka ara..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-10"
            />
          </div>
        </div>
      </Card>

      {/* Table */}
      <Card>
        {isLoading ? (
          <div className="p-12 text-center">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900"></div>
          </div>
        ) : brands && brands.length > 0 ? (
          <table className="w-full">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="text-left py-3 px-4 font-semibold text-sm">Logo</th>
                <th className="text-left py-3 px-4 font-semibold text-sm">Kod</th>
                <th className="text-left py-3 px-4 font-semibold text-sm">Ad</th>
                <th className="text-left py-3 px-4 font-semibold text-sm">Durum</th>
                <th className="text-right py-3 px-4 font-semibold text-sm">İşlemler</th>
              </tr>
            </thead>
            <tbody>
              {brands.map((brand) => (
                <tr key={brand.id} className="border-b hover:bg-gray-50">
                  <td className="py-3 px-4">
                    {brand.logoUrl ? (
                      <img
                        src={brand.logoUrl}
                        alt={brand.name}
                        className="h-8 w-8 rounded-full object-cover"
                        onError={(e) => {
                          // Fallback to initials on error
                          const target = e.target as HTMLImageElement;
                          target.style.display = 'none';
                          const fallback = target.nextElementSibling as HTMLDivElement;
                          if (fallback) fallback.style.display = 'flex';
                        }}
                      />
                    ) : null}
                    <div
                      className={`h-8 w-8 rounded-full bg-blue-600 text-white flex items-center justify-center font-semibold text-sm ${
                        brand.logoUrl ? 'hidden' : 'flex'
                      }`}
                    >
                      {brand.name.charAt(0).toUpperCase()}
                    </div>
                  </td>
                  <td className="py-3 px-4 font-mono text-sm">{brand.code}</td>
                  <td className="py-3 px-4 font-medium">{brand.name}</td>
                  <td className="py-3 px-4">
                    {brand.isActive ? (
                      <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                        Aktif
                      </span>
                    ) : (
                      <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
                        Pasif
                      </span>
                    )}
                  </td>
                  <td className="py-3 px-4">
                    <div className="flex justify-end gap-2">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleEdit(brand)}
                        title="Düzenle"
                      >
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleToggleActive(brand)}
                        title={brand.isActive ? 'Pasif Yap' : 'Aktif Yap'}
                      >
                        <Power className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => setBrandToDelete(brand)}
                        title="Sil"
                        className="text-red-600 hover:text-red-700 hover:bg-red-50"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <div className="p-12 text-center">
            <p className="text-gray-500 mb-4">Marka bulunamadı</p>
            <Button onClick={handleCreate}>
              <Plus className="h-4 w-4 mr-2" />
              İlk Markayı Oluştur
            </Button>
          </div>
        )}
      </Card>

      {/* Form Modal */}
      <BrandFormModal
        open={showFormModal}
        onOpenChange={setShowFormModal}
        brand={selectedBrand}
        onSuccess={() => {
          setShowFormModal(false);
          setSelectedBrand(undefined);
        }}
      />

      {/* Delete Confirmation */}
      <ConfirmDialog
        open={!!brandToDelete}
        onOpenChange={(open) => !open && setBrandToDelete(null)}
        title="Markayı Sil"
        description={`${brandToDelete?.name} markasını silmek istediğinizden emin misiniz? Bu marka kullanımda ise pasif hale getirilecektir.`}
        confirmText="Sil"
        cancelText="İptal"
        onConfirm={handleDelete}
        variant="destructive"
      />
    </div>
  );
}

// Import React for useEffect
import * as React from 'react';
