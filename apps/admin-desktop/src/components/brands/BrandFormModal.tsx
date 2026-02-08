import { useState, useEffect } from 'react';
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { useCreateBrand, useUpdateBrand } from '@/hooks/useBrands';
import type { Brand, CreateBrandRequest, UpdateBrandRequest } from '@/types/brand';
import { useToast } from '@/hooks/useToast';
import { ApiError } from '@/lib/api-client';

interface BrandFormModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  brand?: Brand;
  onSuccess?: () => void;
}

export function BrandFormModal({
  open,
  onOpenChange,
  brand,
  onSuccess,
}: BrandFormModalProps) {
  const { toast } = useToast();
  const createMutation = useCreateBrand();
  const updateMutation = useUpdateBrand(brand?.id || '');
  
  const [formData, setFormData] = useState({
    name: '',
    code: '',
    logoUrl: '',
    isActive: true,
  });

  // Initialize form data when brand prop changes
  useEffect(() => {
    if (brand) {
      setFormData({
        name: brand.name,
        code: brand.code,
        logoUrl: brand.logoUrl || '',
        isActive: brand.isActive,
      });
    } else {
      setFormData({
        name: '',
        code: '',
        logoUrl: '',
        isActive: true,
      });
    }
  }, [brand, open]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      const payload = {
        name: formData.name.trim(),
        code: formData.code.trim() || undefined,
        logoUrl: formData.logoUrl.trim() || undefined,
        isActive: formData.isActive,
      };

      if (brand) {
        // Update existing brand
        await updateMutation.mutateAsync(payload as UpdateBrandRequest);
        toast({
          title: 'Marka güncellendi',
          description: `${formData.name} başarıyla güncellendi.`,
        });
      } else {
        // Create new brand
        await createMutation.mutateAsync(payload as CreateBrandRequest);
        toast({
          title: 'Marka oluşturuldu',
          description: `${formData.name} başarıyla oluşturuldu.`,
        });
      }
      
      onSuccess?.();
      onOpenChange(false);
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        toast({
          variant: 'destructive',
          title: 'Hata',
          description: 'Bu marka zaten mevcut.',
        });
      } else {
        toast({
          variant: 'destructive',
          title: 'Hata',
          description: brand
            ? 'Marka güncellenirken bir hata oluştu.'
            : 'Marka oluşturulurken bir hata oluştu.',
        });
      }
    }
  };

  const isLoading = createMutation.isPending || updateMutation.isPending;

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent className="max-w-md">
        <form onSubmit={handleSubmit}>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {brand ? 'Marka Düzenle' : 'Yeni Marka'}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {brand
                ? 'Marka bilgilerini güncelleyin.'
                : 'Yeni bir marka oluşturun.'}
            </AlertDialogDescription>
          </AlertDialogHeader>

          <div className="space-y-4 py-4">
            <div>
              <label className="block text-sm font-medium mb-2">
                Marka Adı *
              </label>
              <Input
                required
                value={formData.name}
                onChange={(e) =>
                  setFormData({ ...formData, name: e.target.value })
                }
                placeholder="Bosch"
                disabled={isLoading}
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">
                Marka Kodu
              </label>
              <Input
                value={formData.code}
                onChange={(e) =>
                  setFormData({ ...formData, code: e.target.value.toUpperCase() })
                }
                placeholder="Otomatik oluşturulur"
                disabled={isLoading}
              />
              <p className="text-xs text-muted-foreground mt-1">
                Boş bırakılırsa otomatik olarak oluşturulur
              </p>
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">
                Logo URL
              </label>
              <Input
                type="url"
                value={formData.logoUrl}
                onChange={(e) =>
                  setFormData({ ...formData, logoUrl: e.target.value })
                }
                placeholder="https://example.com/logo.png"
                disabled={isLoading}
              />
            </div>

            <div className="flex items-center space-x-2">
              <Checkbox
                id="isActive"
                checked={formData.isActive}
                onCheckedChange={(checked) =>
                  setFormData({ ...formData, isActive: checked as boolean })
                }
                disabled={isLoading}
              />
              <label
                htmlFor="isActive"
                className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
              >
                Aktif
              </label>
            </div>
          </div>

          <AlertDialogFooter>
            <AlertDialogCancel type="button" disabled={isLoading}>
              İptal
            </AlertDialogCancel>
            <Button type="submit" disabled={isLoading}>
              {isLoading ? 'Kaydediliyor...' : 'Kaydet'}
            </Button>
          </AlertDialogFooter>
        </form>
      </AlertDialogContent>
    </AlertDialog>
  );
}
