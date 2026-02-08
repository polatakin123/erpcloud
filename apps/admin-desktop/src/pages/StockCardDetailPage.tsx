import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiClient } from '../lib/api-client';
import { ArrowLeft, Package, Tag, Hash, Building2, TrendingUp, ScanLine } from 'lucide-react';
import { useState } from 'react';
import VehicleFitmentTab from '../components/VehicleFitmentTab';

interface ProductVariant {
  id: string;
  productId: string;
  sku: string;
  name: string;
  barcode?: string;
  price: number;
  currency: string;
  isActive: boolean;
  product?: {
    id: string;
    name: string;
    code: string;
    categoryId?: string;
  };
}

interface PartReference {
  id: string;
  variantId: string;
  refType: string;
  refCode: string;
  createdAt: string;
}

interface StockLevel {
  warehouseId: string;
  warehouseName: string;
  onHand: number;
  reserved: number;
  available: number;
}

export default function StockCardDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [newOemCode, setNewOemCode] = useState('');
  const [newBarcode, setNewBarcode] = useState('');
  const [activeTab, setActiveTab] = useState<'info' | 'fitment'>('info');

  // Fetch variant details
  const { data: variant, isLoading: loadingVariant } = useQuery<ProductVariant>({
    queryKey: ['variant', id],
    queryFn: async () => {
      return await ApiClient.get<ProductVariant>(`/api/products/variants/${id}`);
    },
  });

  // Fetch OEM codes and references
  const { data: references = [], isLoading: loadingRefs } = useQuery<PartReference[]>({
    queryKey: ['variant-references', id],
    queryFn: async () => {
      const response = await ApiClient.get<PartReference[]>(`/api/variants/${id}/references`);
      return response || [];
    },
  });

  // Fetch stock levels (placeholder - implement when stock service ready)
  const { data: stockLevels = [] } = useQuery<StockLevel[]>({
    queryKey: ['variant-stock', id],
    queryFn: async () => {
      // TODO: Implement stock levels API
      return [];
    },
    enabled: false, // Disable until API ready
  });

  // Add OEM code mutation
  const addOemMutation = useMutation({
    mutationFn: async (code: string) => {
      return ApiClient.post(`/api/variants/${id}/references`, {
        refType: 'OEM',
        refCode: code,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['variant-references', id] });
      setNewOemCode('');
    },
  });

  // Delete reference mutation
  const deleteRefMutation = useMutation({
    mutationFn: async (refId: string) => {
      return ApiClient.delete(`/api/variants/references/${refId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['variant-references', id] });
    },
  });

  // Update variant mutation
  const updateVariantMutation = useMutation({
    mutationFn: async (data: Partial<ProductVariant>) => {
      return ApiClient.put(`/api/products/variants/${id}`, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['variant', id] });
    },
  });

  const handleAddOemCode = () => {
    if (newOemCode.trim()) {
      addOemMutation.mutate(newOemCode.trim());
    }
  };

  const handleUpdateBarcode = () => {
    if (variant && newBarcode !== variant.barcode) {
      updateVariantMutation.mutate({ barcode: newBarcode });
    }
  };

  if (loadingVariant || loadingRefs) {
    return (
      <div className="p-6">
        <div className="animate-pulse space-y-4">
          <div className="h-8 bg-gray-200 rounded w-1/4"></div>
          <div className="h-64 bg-gray-200 rounded"></div>
        </div>
      </div>
    );
  }

  if (!variant) {
    return (
      <div className="p-6">
        <div className="text-center py-12">
          <Package className="h-16 w-16 text-gray-400 mx-auto mb-4" />
          <h2 className="text-xl font-semibold text-gray-900 mb-2">Stok Kartı Bulunamadı</h2>
          <p className="text-gray-600 mb-4">Bu stok kartı sistemde mevcut değil.</p>
          <button
            onClick={() => navigate('/products')}
            className="text-blue-600 hover:text-blue-800"
          >
            Ürünlere Dön
          </button>
        </div>
      </div>
    );
  }

  const oemCodes = references.filter((r) => r.refType === 'OEM');

  return (
    <div className="p-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="mb-6 flex items-center gap-4">
        <button
          onClick={() => navigate(-1)}
          className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
          title="Geri dön"
        >
          <ArrowLeft className="h-5 w-5" />
        </button>
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Stok Kartı Detayı</h1>
          <p className="text-sm text-gray-600 mt-1">
            {variant.product?.name} - {variant.name}
          </p>
        </div>
      </div>
{/* Tabs */}
      <div className="mb-6 border-b border-gray-200">
        <nav className="flex space-x-8">
          <button
            onClick={() => setActiveTab('info')}
            className={`py-4 px-1 border-b-2 font-medium text-sm transition-colors ${
              activeTab === 'info'
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            Temel Bilgiler
          </button>
          <button
            onClick={() => setActiveTab('fitment')}
            className={`py-4 px-1 border-b-2 font-medium text-sm transition-colors ${
              activeTab === 'fitment'
                ? 'border-blue-600 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            Uyumlu Araçlar
          </button>
        </nav>
      </div>

      {activeTab === 'info' && (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Left Column - Master Info */}
          {/* Left Column - Master Info */}
        <div className="lg:col-span-2 space-y-6">
          {/* Basic Info Card */}
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
              <Package className="h-5 w-5 text-blue-600" />
              Temel Bilgiler
            </h2>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  <Hash className="h-4 w-4 inline mr-1" />
                  SKU
                </label>
                <div className="text-lg font-mono font-semibold text-gray-900">
                  {variant.sku}
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  <Tag className="h-4 w-4 inline mr-1" />
                  Stok Kartı Adı
                </label>
                <div className="text-lg font-semibold text-gray-900">
                  {variant.name}
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  <Building2 className="h-4 w-4 inline mr-1" />
                  Ana Ürün
                </label>
                <button
                  onClick={() => navigate(`/products/${variant.productId}`)}
                  className="text-blue-600 hover:text-blue-800 font-medium"
                >
                  {variant.product?.code} - {variant.product?.name}
                </button>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  <TrendingUp className="h-4 w-4 inline mr-1" />
                  Fiyat
                </label>
                <div className="text-lg font-semibold text-gray-900">
                  {variant.price.toFixed(2)} {variant.currency}
                </div>
              </div>
            </div>
          </div>

          {/* OEM Codes Card */}
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
              <Tag className="h-5 w-5 text-green-600" />
              OEM Kodları
            </h2>
            
            {/* Add new OEM code */}
            <div className="mb-4 flex gap-2">
              <input
                type="text"
                value={newOemCode}
                onChange={(e) => setNewOemCode(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') handleAddOemCode();
                }}
                placeholder="Yeni OEM kodu ekle (Enter veya virgül ile)"
                className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <button
                onClick={handleAddOemCode}
                disabled={!newOemCode.trim() || addOemMutation.isPending}
                className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Ekle
              </button>
            </div>

            {/* OEM codes list */}
            <div className="space-y-2">
              {oemCodes.length === 0 ? (
                <p className="text-sm text-gray-500 italic">Henüz OEM kodu eklenmemiş</p>
              ) : (
                oemCodes.map((ref) => (
                  <div
                    key={ref.id}
                    className="flex items-center justify-between p-3 bg-green-50 border border-green-200 rounded-md"
                  >
                    <span className="font-mono font-semibold text-green-900">
                      {ref.refCode}
                    </span>
                    <button
                      onClick={() => deleteRefMutation.mutate(ref.id)}
                      className="text-red-600 hover:text-red-800 text-sm"
                    >
                      Kaldır
                    </button>
                  </div>
                ))
              )}
            </div>
          </div>

          {/* Barcode Card */}
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <h2 className="text-lg font-semibold mb-4 flex items-center gap-2">
              <ScanLine className="h-5 w-5 text-purple-600" />
              Barkod
            </h2>
            <div className="flex gap-2">
              <input
                type="text"
                value={newBarcode || variant.barcode || ''}
                onChange={(e) => setNewBarcode(e.target.value)}
                onBlur={handleUpdateBarcode}
                placeholder="Barkod numarası"
                className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 font-mono"
              />
            </div>
            {variant.barcode && (
              <p className="mt-2 text-sm text-gray-600">
                Mevcut barkod: <span className="font-mono font-semibold">{variant.barcode}</span>
              </p>
            )}
          </div>
        </div>

        {/* Right Column - Stock & Quick Links */}
        <div className="space-y-6">
          {/* Stock Levels Card */}
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <h2 className="text-lg font-semibold mb-4">Stok Durumu</h2>
            {stockLevels.length === 0 ? (
              <div className="text-center py-8">
                <Package className="h-12 w-12 text-gray-400 mx-auto mb-2" />
                <p className="text-sm text-gray-500">Stok bilgisi yükleniyor...</p>
              </div>
            ) : (
              <div className="space-y-3">
                {stockLevels.map((stock) => (
                  <div key={stock.warehouseId} className="border-b pb-3 last:border-b-0">
                    <div className="font-medium text-sm text-gray-700 mb-1">
                      {stock.warehouseName}
                    </div>
                    <div className="grid grid-cols-3 gap-2 text-xs">
                      <div>
                        <span className="text-gray-600">Eldeki:</span>
                        <div className="font-semibold">{stock.onHand}</div>
                      </div>
                      <div>
                        <span className="text-gray-600">Rezerve:</span>
                        <div className="font-semibold text-yellow-600">{stock.reserved}</div>
                      </div>
                      <div>
                        <span className="text-gray-600">Kullanılabilir:</span>
                        <div className="font-semibold text-green-600">{stock.available}</div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Quick Actions */}
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
            <h2 className="text-lg font-semibold mb-4">Hızlı İşlemler</h2>
            <div className="space-y-2">
              <button
                onClick={() => navigate('/sales/wizard', { state: { selectedVariantId: id } })}
                className="w-full px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 text-sm font-medium"
              >
                Satış Yap
              </button>
              <button
                onClick={() => navigate('/purchase/wizard', { state: { selectedVariantId: id } })}
                className="w-full px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700 text-sm font-medium"
              >
                Sipariş Ver
              </button>
              <button
                onClick={() => navigate(`/reports/stock-ledger?sku=${variant.sku}`)}
                className="w-full px-4 py-2 border border-gray-300 text-gray-700 rounded-md hover:bg-gray-50 text-sm font-medium"
              >
                Stok Hareketleri
              </button>
            </div>
          </div>
        </div>
        </div>
      )}

      {activeTab === 'fitment' && (
        <VehicleFitmentTab variantId={id!} />
      )}
    </div>
  );
}
