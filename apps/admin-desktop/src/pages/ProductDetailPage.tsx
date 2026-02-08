import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Plus, Tag, AlertCircle, Edit } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import OemReferencePanel from '@/components/OemReferencePanel';
import { BrandSelect } from '@/components/brands/BrandSelect';
import { useBrand } from '@/hooks/useBrands';
import { useUpdateProduct } from '@/hooks/useProducts';
import { ApiClient } from '@/lib/api-client';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

interface Product {
  id: string;
  code: string;
  name: string;
  description?: string;
  isActive: boolean;
  brandId?: string;
  brand?: string; // Deprecated
}

interface Variant {
  id: string;
  productId: string;
  sku: string;
  name: string;
  barcode?: string;
  isActive: boolean;
}

export default function ProductDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [showVariantForm, setShowVariantForm] = useState(false);
  const [selectedVariantId, setSelectedVariantId] = useState<string | null>(null);
  const [isEditingProduct, setIsEditingProduct] = useState(false);

  // Fetch product
  const { data: product, isLoading: productLoading } = useQuery({
    queryKey: ['product', id],
    queryFn: async () => {
      return await ApiClient.get<Product>(`/api/products/${id}`);
    },
    enabled: !!id,
  });

  // Fetch brand information if brandId exists
  const { data: brandData } = useBrand(product?.brandId || '');

  // Fetch variants
  const { data: variants, isLoading: variantsLoading } = useQuery({
    queryKey: ['variants', id],
    queryFn: async () => {
      const response = await ApiClient.get<{ items: Variant[] }>(`/api/products/${id}/variants`);
      return response.items;
    },
    enabled: !!id,
  });

  if (productLoading) {
    return <div className="p-6">Yükleniy or...</div>;
  }

  if (!product) {
    return <div className="p-6">Ürün bulunamadı</div>;
  }

  return (
    <div className="p-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="mb-6">
        <Button
          variant="ghost"
          onClick={() => navigate('/products')}
          className="mb-4"
        >
          <ArrowLeft className="h-4 w-4 mr-2" />
          Ürünlere Dön
        </Button>
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold">{product.name}</h1>
            <p className="text-gray-600">{product.code}</p>
          </div>
          <div className={`px-3 py-1 rounded-full text-sm ${product.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-700'}`}>
            {product.isActive ? 'Aktif' : 'Pasif'}
          </div>
        </div>
      </div>

      {/* Product Details */}
      {product.description && (
        <Card className="mb-6">
          <CardHeader>
            <CardTitle>Açıklama</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-gray-600">{product.description}</p>
          </CardContent>
        </Card>
      )}

      {/* General Information with Brand */}
      <Card className="mb-6">
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Genel Bilgiler</CardTitle>
            {!isEditingProduct && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => setIsEditingProduct(true)}
              >
                <Edit className="h-4 w-4 mr-2" />
                Düzenle
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {isEditingProduct ? (
            <EditProductForm
              product={product}
              onSuccess={() => {
                setIsEditingProduct(false);
                queryClient.invalidateQueries({ queryKey: ['product', id] });
              }}
              onCancel={() => setIsEditingProduct(false)}
            />
          ) : (
            <>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium text-gray-500">Ürün Kodu</label>
                  <p className="mt-1 font-mono">{product.code}</p>
                </div>
                <div>
                  <label className="text-sm font-medium text-gray-500">Ürün Adı</label>
                  <p className="mt-1">{product.name}</p>
                </div>
              </div>

              {/* Brand Information */}
              <div>
                <label className="text-sm font-medium text-gray-500 mb-2 block">Marka</label>
                {product.brandId && brandData ? (
                  <div className="flex items-center gap-2">
                    {brandData.logoUrl ? (
                      <img
                        src={brandData.logoUrl}
                        alt={brandData.name}
                        className="h-6 w-6 rounded-full object-cover"
                        onError={(e) => {
                          const target = e.target as HTMLImageElement;
                          target.style.display = 'none';
                        }}
                      />
                    ) : (
                      <div className="h-6 w-6 rounded-full bg-blue-600 text-white flex items-center justify-center text-xs font-semibold">
                        {brandData.name.charAt(0).toUpperCase()}
                      </div>
                    )}
                    <span className="font-medium">{brandData.name}</span>
                    <span className="text-xs text-muted-foreground font-mono">({brandData.code})</span>
                  </div>
                ) : product.brand ? (
                  <div className="flex items-center gap-2 p-3 bg-orange-50 border border-orange-200 rounded">
                    <AlertCircle className="h-5 w-5 text-orange-600" />
                    <div className="flex-1">
                      <p className="text-sm font-medium text-orange-900">
                        Eski marka formatı: "{product.brand}"
                      </p>
                      <p className="text-xs text-orange-700 mt-1">
                        Bu ürün eski string-based marka formatı kullanıyor. Lütfen ürünü düzenleyip yeni marka seçin.
                      </p>
                    </div>
                  </div>
                ) : (
                  <div className="flex items-center gap-2 p-3 bg-yellow-50 border border-yellow-200 rounded">
                    <AlertCircle className="h-5 w-5 text-yellow-600" />
                    <p className="text-sm text-yellow-900">
                      Bu ürünün markası tanımlı değil.
                    </p>
                  </div>
                )}
              </div>
            </>
          )}
        </CardContent>
      </Card>

      {/* Variants Section */}
      <Card className="mb-6">
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Stok Kartları</CardTitle>
            <Button onClick={() => setShowVariantForm(!showVariantForm)} size="sm">
              <Plus className="h-4 w-4 mr-2" />
              Stok Kartı Ekle
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {showVariantForm && (
            <CreateVariantForm
              productId={id!}
              onSuccess={() => {
                setShowVariantForm(false);
                queryClient.invalidateQueries({ queryKey: ['variants', id] });
              }}
            />
          )}

          {variantsLoading && <div className="text-center py-4">Stok kartları yüklen iyor...</div>}

          {variants && variants.length === 0 && !showVariantForm && (
            <div className="text-center py-8 text-gray-500">
              Henüz stok kartı yok. Oluşturmak için "Stok Kartı Ekle" butonuna tıklayın.
            </div>
          )}

          <div className="space-y-4">
            {variants?.map((variant) => (
              <div
                key={variant.id}
                className={`border rounded-lg p-4 ${selectedVariantId === variant.id ? 'border-blue-500 bg-blue-50' : 'border-gray-200'}`}
              >
                <div className="flex items-start justify-between mb-4">
                  <div className="flex-1">
                    <h3 className="font-semibold text-lg">{variant.name}</h3>
                    <div className="flex gap-4 mt-2 text-sm text-gray-600">
                      <span>SKU: <span className="font-mono">{variant.sku}</span></span>
                      {variant.barcode && <span>Barcode: <span className="font-mono">{variant.barcode}</span></span>}
                    </div>
                  </div>
                  <Button
                    variant={selectedVariantId === variant.id ? 'default' : 'outline'}
                    size="sm"
                    onClick={() => setSelectedVariantId(selectedVariantId === variant.id ? null : variant.id)}
                    title="OEM kodları ve referans kodlarını yönetmek için yönetici aracı. Günlük satış işlemi için Hızlı Arama (⚡ Stok Kartı Ara) kullanın."
                  >
                    <Tag className="w-4 h-4 mr-1" />
                    {selectedVariantId === variant.id ? 'OEM Kodlarını Gizle (Yönetici)' : 'OEM Kodlarını Yönet (Yönetici)'}
                  </Button>
                </div>

                {selectedVariantId === variant.id && (
                  <div className="mt-4 pt-4 border-t">
                    <OemReferencePanel
                      variantId={variant.id}
                      variantName={variant.name}
                    />
                  </div>
                )}
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function CreateVariantForm({ productId, onSuccess }: { productId: string; onSuccess: () => void }) {
  const [sku, setSku] = useState('');
  const [name, setName] = useState('');
  const [barcode, setBarcode] = useState('');
  const [unit, setUnit] = useState('ADET');
  const [oemCodes, setOemCodes] = useState<string[]>([]);
  const [oemInput, setOemInput] = useState('');
  const [oemError, setOemError] = useState('');

  const createMutation = useMutation({
    mutationFn: async (data: { sku: string; name: string; unit: string; barcode?: string }) => {
      return await ApiClient.post<{ id: string }>(`/api/products/${productId}/variants`, data);
    },
    onSuccess: async (response: { id: string }) => {
      // If OEM codes provided, create references
      if (oemCodes.length > 0) {
        const variantId = response.id;
        try {
          for (const code of oemCodes) {
            await ApiClient.post(`/api/variants/${variantId}/references`, {
              refType: 'OEM',
              refCode: code,
            });
          }
        } catch (error) {
          console.error('Failed to create OEM references:', error);
        }
      }
      onSuccess();
      setSku('');
      setName('');
      setBarcode('');
      setUnit('ADET');
      setOemCodes([]);
      setOemInput('');
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    createMutation.mutate({
      sku,
      name,
      unit,
      barcode: barcode || undefined,
    });
  };

  return (
    <form onSubmit={handleSubmit} className="mb-6 p-4 bg-gray-50 rounded-lg space-y-4">
      <h4 className="font-semibold">Yeni Stok Kartı Oluştur</h4>
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div>
          <label className="block text-sm font-medium mb-2">SKU *</label>
          <Input
            required
            value={sku}
            onChange={(e) => setSku(e.target.value)}
            placeholder="Örn: BLT001-001"
          />
        </div>
        <div>
          <label className="block text-sm font-medium mb-2">Stok Kartı Adı *</label>
          <Input
            required
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Örn: Ön Balata"
          />
        </div>
        <div>
          <label className="block text-sm font-medium mb-2">Birim *</label>
          <select
            required
            value={unit}
            onChange={(e) => setUnit(e.target.value)}
            className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
          >
            <option value="ADET">Adet</option>
            <option value="KG">Kilogram (kg)</option>
            <option value="LITRE">Litre (L)</option>
            <option value="METRE">Metre (m)</option>
            <option value="KUTU">Kutu</option>
            <option value="PAKET">Paket</option>
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium mb-2">Barkod (isteğe bağlı)</label>
          <Input
            value={barcode}
            onChange={(e) => setBarcode(e.target.value)}
            placeholder="1234567890123"
          />
        </div>
      </div>
      
      {/* OEM Codes Section */}
      <div className="border-t pt-4">
        <label className="block text-sm font-medium mb-2">
          OEM Kodları (isteğe bağlı) - Yedek parça iş akışı için
        </label>
        <p className="text-xs text-gray-600 mb-2">
          OEM kodlarını virgül, boşluk ile ayırın veya Enter'a basın. Her kod 3-64 karakter olmalı.
        </p>
        <div className="flex gap-2 mb-2">
          <Input
            value={oemInput}
            onChange={(e) => {
              setOemInput(e.target.value);
              setOemError('');
            }}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ',') {
                e.preventDefault();
                const code = oemInput.trim();
                if (code.length >= 3 && code.length <= 64) {
                  if (!oemCodes.includes(code)) {
                    setOemCodes([...oemCodes, code]);
                  }
                  setOemInput('');
                  setOemError('');
                } else if (code.length > 0) {
                  setOemError('OEM kodu 3-64 karakter olmalı');
                }
              }
            }}
            placeholder="ABC123, XYZ-456, vb."
          />
          <Button
            type="button"
            variant="outline"
            onClick={() => {
              const codes = oemInput.split(/[,\s]+/).filter(c => c.trim().length >= 3);
              const validCodes = codes.filter(c => c.length >= 3 && c.length <= 64);
              const newCodes = validCodes.filter(c => !oemCodes.includes(c));
              if (newCodes.length > 0) {
                setOemCodes([...oemCodes, ...newCodes]);
                setOemInput('');
                setOemError('');
              } else if (oemInput.trim().length > 0 && oemInput.trim().length < 3) {
                setOemError('OEM kodları en az 3 karakter olmalı');
              }
            }}
          >
            Ekle
          </Button>
        </div>
        {oemError && <div className="text-destructive text-sm mb-2">{oemError}</div>}
        {oemCodes.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {oemCodes.map((code, idx) => (
              <div
                key={idx}
                className="inline-flex items-center gap-1 px-2 py-1 bg-blue-100 text-blue-800 rounded text-sm"
              >
                <Tag className="w-3 h-3" />
                {code}
                <button
                  type="button"
                  onClick={() => setOemCodes(oemCodes.filter((_, i) => i !== idx))}
                  className="ml-1 text-blue-600 hover:text-blue-800"
                >
                  ×
                </button>
              </div>
            ))}
          </div>
        )}
      </div>
      
      {createMutation.error && (
        <div className="text-destructive text-sm">Stok kartı oluşturulamadı</div>
      )}
      <div className="flex gap-2">
        <Button type="submit" disabled={createMutation.isPending}>
          {createMutation.isPending ? 'Oluşturuluyor...' : 'Stok Kartı Oluştur'}
        </Button>
        <Button type="button" variant="outline" onClick={onSuccess}>
          Vazgeç
        </Button>
      </div>
    </form>
  );
}

function EditProductForm({ 
  product, 
  onSuccess, 
  onCancel 
}: { 
  product: Product; 
  onSuccess: () => void; 
  onCancel: () => void;
}) {
  const [name, setName] = useState(product.name);
  const [description, setDescription] = useState(product.description || '');
  const [brandId, setBrandId] = useState(product.brandId);
  const [brandError, setBrandError] = useState('');
  const updateMutation = useUpdateProduct(product.id);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    // Validate brand is selected
    if (!brandId) {
      setBrandError('Marka seçilmelidir.');
      return;
    }
    
    try {
      await updateMutation.mutateAsync({
        name,
        description: description || undefined,
        brandId,
      });
      onSuccess();
    } catch (error) {
      console.error('Ürün güncellenemedi:', error);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div>
        <label className="block text-sm font-medium mb-2">Ürün Adı *</label>
        <Input
          required
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Ürün Adı"
        />
      </div>
      <div>
        <label className="block text-sm font-medium mb-2">Açıklama</label>
        <Input
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Ürün açıklaması (isteğe bağlı)"
        />
      </div>
      <div>
        <label className="block text-sm font-medium mb-2">Marka *</label>
        <BrandSelect
          value={brandId}
          onChange={(id) => {
            setBrandId(id);
            setBrandError('');
          }}
          error={brandError}
        />
      </div>
      {updateMutation.error && (
        <div className="text-destructive text-sm">Ürün güncellenemedi</div>
      )}
      <div className="flex gap-2">
        <Button type="submit" disabled={updateMutation.isPending}>
          {updateMutation.isPending ? 'Güncelleniyor...' : 'Güncelle'}
        </Button>
        <Button type="button" variant="outline" onClick={onCancel}>
          İptal
        </Button>
      </div>
    </form>
  );
}
