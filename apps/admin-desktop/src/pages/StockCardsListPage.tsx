import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ApiClient } from '../lib/api-client';
import { Package, Search } from 'lucide-react';
import { Input } from '@/components/ui/input';

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
  };
}

export default function StockCardsListPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');

  // Fetch all variants with search
  const { data: _variants, isLoading } = useQuery<ProductVariant[]>({
    queryKey: ['all-variants', search],
    queryFn: async () => {
      // TODO: Implement proper backend endpoint for listing all variants
      // For now, this is a placeholder
      const response = await ApiClient.get<{ items: ProductVariant[] }>('/api/products/variants/all');
      return response.items || [];
    },
    enabled: false, // Disable until backend API ready
  });

  return (
    <div className="p-6">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-3xl font-bold mb-2">Stok Kartları</h1>
        <p className="text-gray-600">Tüm stok kartlarınızı görüntüleyin ve yönetin</p>
      </div>

      {/* Search */}
      <div className="mb-6">
        <div className="relative max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
          <Input
            placeholder="Stok kartı ara (SKU, ad, barkod)..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-10"
          />
        </div>
      </div>

      {/* Info Card - API Not Ready */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-6 mb-6">
        <div className="flex items-start gap-4">
          <Package className="h-8 w-8 text-blue-600 flex-shrink-0 mt-1" />
          <div>
            <h3 className="font-semibold text-blue-900 mb-2">Stok Kartı Listesi Geliştiriliyor</h3>
            <p className="text-sm text-blue-800 mb-3">
              Bu sayfa tüm stok kartlarınızı listeleyecek. Şu anda aşağıdaki yolları kullanabilirsiniz:
            </p>
            <ul className="space-y-2 text-sm text-blue-800">
              <li className="flex items-center gap-2">
                <span className="font-semibold">⚡ Hızlı Arama:</span>
                <span>OEM kodu veya ürün adı ile stok kartı arayın</span>
              </li>
              <li className="flex items-center gap-2">
                <span className="font-semibold">Ürünler (Yönetici):</span>
                <span>Ürün detayından stok kartlarına erişin</span>
              </li>
            </ul>
          </div>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <button
          onClick={() => navigate('/parts/search')}
          className="p-6 bg-white border-2 border-blue-200 rounded-lg hover:border-blue-400 hover:bg-blue-50 transition-all text-left"
        >
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center">
              <Search className="h-5 w-5 text-blue-600" />
            </div>
            <h3 className="font-semibold text-lg">Hızlı Arama</h3>
          </div>
          <p className="text-sm text-gray-600">
            OEM kodu, SKU veya ürün adı ile stok kartı arayın
          </p>
        </button>

        <button
          onClick={() => navigate('/products')}
          className="p-6 bg-white border-2 border-gray-200 rounded-lg hover:border-gray-400 hover:bg-gray-50 transition-all text-left"
        >
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 bg-gray-100 rounded-lg flex items-center justify-center">
              <Package className="h-5 w-5 text-gray-600" />
            </div>
            <h3 className="font-semibold text-lg">Ürünler</h3>
          </div>
          <p className="text-sm text-gray-600">
            Ürün bazlı stok kartlarını görüntüleyin
          </p>
        </button>

        <button
          onClick={() => navigate('/sales/wizard')}
          className="p-6 bg-white border-2 border-green-200 rounded-lg hover:border-green-400 hover:bg-green-50 transition-all text-left"
        >
          <div className="flex items-center gap-3 mb-2">
            <div className="w-10 h-10 bg-green-100 rounded-lg flex items-center justify-center">
              <span className="text-xl">🎯</span>
            </div>
            <h3 className="font-semibold text-lg">Hızlı Satış</h3>
          </div>
          <p className="text-sm text-gray-600">
            Satış işlemi başlatın
          </p>
        </button>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="text-center py-12">
          <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
          <p className="mt-2 text-gray-600">Yükleniyor...</p>
        </div>
      )}

      {/* Future: Table with variants will be shown here */}
    </div>
  );
}
