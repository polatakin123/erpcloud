import { useState, FormEvent } from 'react';
import { Search, Package, Tag, TrendingUp, ShoppingCart, FileText, Info, AlertCircle, CheckCircle, HelpCircle, Receipt, Truck } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useVariantSearch } from '../hooks/usePartReferences';
import { useWarehouses, type Warehouse } from '../hooks/useWarehouses';
import { useVehicleContext } from '../hooks/useVehicleContext';
import { VehicleFilterBar } from '../components/VehicleFilterBar';
import { StockCardDialog } from '../components/StockCardDialog';
import { ContextMenu, ContextMenuContent, ContextMenuItem, ContextMenuTrigger } from '../components/ui/context-menu';

export default function FastSearchPage() {
  const [inputValue, setInputValue] = useState('');
  const [submittedQuery, setSubmittedQuery] = useState('');
  const [selectedWarehouse, setSelectedWarehouse] = useState<string>('');
  const [includeEquivalents, setIncludeEquivalents] = useState(true);
  const [includeUndefinedFitment, setIncludeUndefinedFitment] = useState(false);
  const [showStockCard, setShowStockCard] = useState(false);
  const [selectedVariant, setSelectedVariant] = useState<any>(null);
  const [selectedVariants, setSelectedVariants] = useState<Set<string>>(new Set());
  const navigate = useNavigate();

  const { data: warehouses } = useWarehouses();
  const { selectedEngineId } = useVehicleContext();
  
  const { data: searchResults, isLoading, error } = useVariantSearch({
    query: submittedQuery,
    warehouseId: selectedWarehouse || undefined,
    engineId: selectedEngineId || undefined,
    includeEquivalents,
    includeUndefinedFitment,
    page: 1,
    pageSize: 20,
  });

  // Log search params for debugging
  if (submittedQuery && submittedQuery.length >= 2) {
    console.log('[FastSearch] Query:', submittedQuery, 'Warehouse:', selectedWarehouse, 'Results:', searchResults?.results?.length || 0);
  }
  if (error) {
    console.error('[FastSearch] Error:', error);
  }

  // Handle search submit (Enter or Ara button)
  const handleSearchSubmit = (e?: FormEvent) => {
    e?.preventDefault();
    const trimmed = inputValue.trim();
    if (trimmed.length >= 2) {
      setSubmittedQuery(trimmed);
    }
  };

  // Handle ESC key to clear
  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Escape') {
      setInputValue('');
      setSubmittedQuery('');
    } else if (e.key === 'Enter') {
      handleSearchSubmit();
    }
  };

  // Context menu handlers
  const handleOpenStockCard = (variant: any) => {
    setSelectedVariant(variant);
    setShowStockCard(true);
  };

  const handleToggleSelection = (variantId: string) => {
    setSelectedVariants(prev => {
      const newSet = new Set(prev);
      if (newSet.has(variantId)) {
        newSet.delete(variantId);
      } else {
        newSet.add(variantId);
      }
      return newSet;
    });
  };

  const handleInvoiceSale = () => {
    if (selectedVariants.size === 0) return;
    const variants = searchResults?.results
      ?.filter(r => selectedVariants.has(r.variantId))
      .map(result => ({
        variantId: result.variantId,
        sku: result.sku,
        barcode: result.barcode,
        name: result.name,
        brand: result.brand,
        brandCode: result.brandCode,
        stock: result.available || 0,
        available: result.available || 0,
        price: result.price || 0,
        oemRefs: result.oemRefs || [],
      }));
    navigate('/tezgah/satis', { state: { addToCart: variants } });
  };

  const handleInvoicePurchase = () => {
    alert("Faturalı alış özelliği yakında eklenecek");
  };

  const handleShipmentSale = () => {
    if (selectedVariants.size === 0) return;
    const variants = searchResults?.results
      ?.filter(r => selectedVariants.has(r.variantId))
      .map(result => ({
        variantId: result.variantId,
        sku: result.sku,
        barcode: result.barcode,
        name: result.name,
        brand: result.brand,
        brandCode: result.brandCode,
        stock: result.available || 0,
        available: result.available || 0,
        price: result.price || 0,
        oemRefs: result.oemRefs || [],
      }));
    navigate('/tezgah/satis', { state: { addToCart: variants } });
  };

  const handleShipmentPurchase = () => {
    alert("İrsaliyeli alış özelliği yakında eklenecek");
  };

  const handleStockMovements = () => {
    alert("Stok hareketleri özelliği yakında eklenecek");
  };

  return (
    <div className="max-w-7xl mx-auto p-6">
      {/* Header */}
      <div className="mb-6 bg-gradient-to-r from-blue-600 to-blue-700 text-white rounded-lg p-6 shadow-lg">
        <h1 className="text-3xl font-bold mb-2">
          ⚡ Hızlı Stok Kartı Arama
        </h1>
        <p className="text-blue-100 text-lg">
          OEM kodu, ürün adı, SKU veya barkod ile arayın. Muadil parçaları ve stok durumunu anında görün.
        </p>
      </div>

      {/* Vehicle Filter Bar */}
      <VehicleFilterBar />

      {/* Search Controls */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6 mb-6 mt-6">
        <form onSubmit={handleSearchSubmit}>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {/* Search Input with Ara Button */}
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Arama
              </label>
              <div className="flex gap-2">
                <div className="relative flex-1">
                  <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" />
                  <input
                    type="text"
                    value={inputValue}
                    onChange={(e) => setInputValue(e.target.value)}
                    onKeyDown={handleKeyDown}
                    placeholder="Ürün adı, SKU, barkod veya OEM kodu... (En az 2 karakter)"
                    className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                    autoFocus
                  />
                </div>
                <button
                  type="submit"
                  disabled={inputValue.trim().length < 2}
                  className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:bg-gray-300 disabled:cursor-not-allowed font-medium transition-colors"
                >
                  Ara
                </button>
              </div>
              {inputValue.trim().length > 0 && inputValue.trim().length < 2 && (
                <p className="text-xs text-red-600 mt-1">
                  En az 2 karakter girin
                </p>
              )}
            </div>

          {/* Warehouse Filter */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Depo (Stok)
            </label>
            <select
              value={selectedWarehouse}
              onChange={(e) => setSelectedWarehouse(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            >
              <option value="">Tüm Depolar</option>
              {warehouses?.map((w: Warehouse) => (
                <option key={w.id} value={w.id}>
                  {w.name}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Checkboxes */}
        <div className="flex flex-wrap gap-6 mt-4">
          <div className="flex items-center">
            <input
              type="checkbox"
              id="includeEquivalents"
              checked={includeEquivalents}
              onChange={(e) => setIncludeEquivalents(e.target.checked)}
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
            <label htmlFor="includeEquivalents" className="ml-2 text-sm text-gray-700">
              Muadil parçaları göster (OEM kodlarına göre)
            </label>
          </div>

          {selectedEngineId && (
            <div className="flex items-center">
              <input
                type="checkbox"
                id="includeUndefinedFitment"
                checked={includeUndefinedFitment}
                onChange={(e) => setIncludeUndefinedFitment(e.target.checked)}
                className="h-4 w-4 text-purple-600 focus:ring-purple-500 border-gray-300 rounded"
              />
              <label htmlFor="includeUndefinedFitment" className="ml-2 text-sm text-gray-700">
                Uyumu tanımsız olanları da göster
              </label>
            </div>
          )}
        </div>
      </form>
      </div>

      {/* Error State */}
      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
          <div className="flex items-center gap-2 text-red-800">
            <AlertCircle className="h-5 w-5" />
            <span className="font-medium">Arama Hatası</span>
          </div>
          <p className="text-sm text-red-700 mt-1">
            Arama başarısız oldu. Lütfen bağlantınızı kontrol edip tekrar deneyin.
          </p>
        </div>
      )}

      {/* Warehouse Selection Prompt */}
      {!selectedWarehouse && submittedQuery.length >= 2 && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
          <div className="flex items-center gap-2 text-blue-800">
            <Info className="h-5 w-5" />
            <span className="font-medium">İpucu: Stok durumunu görmek için bir depo seçin</span>
          </div>
        </div>
      )}

      {/* Loading State */}
      {isLoading && (
        <div className="text-center py-12">
          <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
          <p className="mt-2 text-gray-600">Aranıyor...</p>
        </div>
      )}

      {/* Results */}
      {!isLoading && searchResults && searchResults.results.length > 0 && (
        <div className="bg-white rounded-lg shadow-sm border border-gray-200">
          {/* Results Header */}
          <div className="px-6 py-4 border-b border-gray-200">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold text-gray-900">
                Search Results ({searchResults.total})
              </h2>
              <span className="text-sm text-gray-500">
                Query: "{searchResults.query}"
              </span>
            </div>
          </div>

          {/* Results Table */}
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Stok Kartı
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    SKU
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    OEM Kodları
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Eşleşme
                  </th>
                  {selectedWarehouse && (
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Stok
                    </th>
                  )}
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    İşlemler
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {searchResults.results.map((result) => {
                  const isUndefinedFitment = selectedEngineId && result.fitmentPriority === 4;
                  const isCompatible = selectedEngineId && result.isCompatible;
                  
                  const variant = {
                    variantId: result.variantId,
                    sku: result.sku,
                    barcode: result.barcode,
                    name: result.name,
                    brand: result.brand,
                    brandCode: result.brandCode,
                    stock: result.available || 0,
                    available: result.available || 0,
                    price: result.price || 0,
                    oemRefs: result.oemRefs || [],
                  };

                  return (
                    <ContextMenu key={result.variantId}>
                      <ContextMenuTrigger asChild>
                        <tr
                          className={`cursor-pointer ${
                            isUndefinedFitment 
                              ? 'opacity-60 bg-gray-50' 
                              : selectedVariants.has(result.variantId)
                                ? 'bg-green-50 hover:bg-green-100'
                                : 'hover:bg-blue-50'
                          }`}
                          onClick={() => handleToggleSelection(result.variantId)}
                          onDoubleClick={(e) => {
                            e.stopPropagation();
                            handleOpenStockCard(variant);
                          }}
                        >
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-2">
                          <Package className="h-5 w-5 text-gray-400" />
                          <div>
                            {/* Brand Badge */}
                            {result.brand && (
                              <div className="flex items-center gap-1.5 mb-1">
                                {result.brandLogoUrl ? (
                                  <img
                                    src={result.brandLogoUrl}
                                    alt={result.brand}
                                    className="h-4 w-4 rounded-sm object-contain"
                                    onError={(e) => {
                                      (e.target as HTMLImageElement).style.display = 'none';
                                    }}
                                  />
                                ) : result.brandCode ? (
                                  <div className="h-4 w-4 rounded-sm bg-blue-600 text-white flex items-center justify-center text-[8px] font-bold">
                                    {result.brandCode.charAt(0)}
                                  </div>
                                ) : null}
                                <span className={`text-xs font-medium px-1.5 py-0.5 rounded ${
                                  result.isBrandActive === false 
                                    ? 'bg-gray-200 text-gray-600' 
                                    : 'bg-blue-50 text-blue-700'
                                }`}>
                                  {result.brandCode || result.brand}
                                </span>
                              </div>
                            )}
                            <div className="text-sm font-medium text-gray-900">
                              {result.name}
                            </div>
                            {result.barcode && (
                              <div className="text-sm text-gray-500">
                                Barcode: {result.barcode}
                              </div>
                            )}
                            {/* Fitment badges */}
                            {selectedEngineId && (
                              <div className="flex gap-1 mt-1">
                                {isCompatible ? (
                                  <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800">
                                    <CheckCircle className="h-3 w-3 mr-1" />
                                    Araç uyumlu
                                  </span>
                                ) : (
                                  <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-600">
                                    <HelpCircle className="h-3 w-3 mr-1" />
                                    Uyum tanımsız
                                  </span>
                                )}
                              </div>
                            )}
                          </div>
                        </div>
                      </td>
                    <td className="px-6 py-4 text-sm text-gray-900">
                      {result.sku}
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-wrap gap-1">
                        {result.oemRefs.slice(0, 3).map((oem, idx) => (
                          <span
                            key={idx}
                            className="inline-flex items-center px-2 py-1 rounded-md text-xs font-medium bg-blue-100 text-blue-800"
                          >
                            <Tag className="h-3 w-3 mr-1" />
                            {oem}
                          </span>
                        ))}
                        {result.oemRefs.length > 3 && (
                          <span className="text-xs text-gray-500">
                            +{result.oemRefs.length - 3} more
                          </span>
                        )}
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-col gap-1">
                        <span
                          className={`inline-flex items-center px-2 py-1 rounded-md text-xs font-medium ${
                            result.matchType === 'DIRECT' || result.matchType === 'BOTH'
                              ? 'bg-green-100 text-green-800'
                              : 'bg-yellow-100 text-yellow-800'
                          }`}
                        >
                          {result.matchType === 'EQUIVALENT' && (
                            <TrendingUp className="h-3 w-3 mr-1" />
                          )}
                          {result.matchType}
                        </span>
                        <span className="text-xs text-gray-500">
                          via {result.matchedBy}
                        </span>
                      </div>
                    </td>
                    {selectedWarehouse && (
                      <td className="px-6 py-4">
                        {result.available !== undefined ? (
                          <div className="text-sm">
                            <div className={`font-medium ${result.available > 0 ? 'text-green-600' : 'text-red-600'}`}>
                              {result.available} available
                            </div>
                            <div className="text-gray-500 text-xs">
                              {result.onHand} on hand, {result.reserved} reserved
                            </div>
                          </div>
                        ) : (
                          <span className="text-sm text-gray-500">No stock</span>
                        )}
                      </td>
                    )}
                    <td className="px-6 py-4">
                      <div className="flex gap-2">
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            navigate('/tezgah/satis', { state: { 
                              addToCart: {
                                variantId: result.variantId,
                                sku: result.sku,
                                variantName: result.variantName,
                                stock: result.available || 0,
                                oemCodes: result.oemRefs,
                                // Brand information
                                brand: result.brand,
                                brandId: result.brandId,
                                brandCode: result.brandCode,
                                brandLogoUrl: result.brandLogoUrl,
                                isBrandActive: result.isBrandActive,
                              }
                            }});
                          }}
                          className="inline-flex items-center px-3 py-1 border border-transparent text-xs font-medium rounded-md text-white bg-green-600 hover:bg-green-700"
                          title="Satışa ekle"
                        >
                          <ShoppingCart className="h-3 w-3 mr-1" />
                          Satışa Ekle
                        </button>
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            navigate('/purchase/wizard', { state: { selectedVariantId: result.variantId } });
                          }}
                          className="inline-flex items-center px-3 py-1 border border-gray-300 text-xs font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
                          title="Sipariş ver"
                        >
                          <FileText className="h-3 w-3 mr-1" />
                          Sipariş
                        </button>
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            navigate(`/stock-cards/${result.variantId}`);
                          }}
                          className="inline-flex items-center px-3 py-1 border border-gray-300 text-xs font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
                          title="Stok kartı detayı"
                        >
                          <Info className="h-3 w-3" />
                        </button>
                      </div>
                    </td>
                        </tr>
                      </ContextMenuTrigger>
                      <ContextMenuContent className="w-56">
                        <ContextMenuItem onClick={handleInvoiceSale} className="cursor-pointer" disabled={selectedVariants.size === 0}>
                          <Receipt className="mr-2 h-4 w-4" />
                          <span>Faturalı Satış {selectedVariants.size > 0 && `(${selectedVariants.size})`}</span>
                        </ContextMenuItem>
                        <ContextMenuItem onClick={handleInvoicePurchase} className="cursor-pointer" disabled={selectedVariants.size === 0}>
                          <ShoppingCart className="mr-2 h-4 w-4" />
                          <span>Faturalı Alış {selectedVariants.size > 0 && `(${selectedVariants.size})`}</span>
                        </ContextMenuItem>
                        <ContextMenuItem onClick={handleShipmentSale} className="cursor-pointer" disabled={selectedVariants.size === 0}>
                          <Truck className="mr-2 h-4 w-4" />
                          <span>İrsaliyeli Satış {selectedVariants.size > 0 && `(${selectedVariants.size})`}</span>
                        </ContextMenuItem>
                        <ContextMenuItem onClick={handleShipmentPurchase} className="cursor-pointer" disabled={selectedVariants.size === 0}>
                          <Package className="mr-2 h-4 w-4" />
                          <span>İrsaliyeli Alış {selectedVariants.size > 0 && `(${selectedVariants.size})`}</span>
                        </ContextMenuItem>
                        <ContextMenuItem onClick={handleStockMovements} className="cursor-pointer" disabled={selectedVariants.size === 0}>
                          <FileText className="mr-2 h-4 w-4" />
                          <span>Stok Hareketleri {selectedVariants.size > 0 && `(${selectedVariants.size})`}</span>
                        </ContextMenuItem>
                      </ContextMenuContent>
                    </ContextMenu>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* No Results */}
      {!isLoading && searchResults && searchResults.results.length === 0 && submittedQuery && (
        <div className="text-center py-12 bg-white rounded-lg shadow-sm border border-gray-200">
          <Package className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">
            Sonuç bulunamadı
          </h3>
          <p className="text-gray-600">
            Farklı bir arama terimi deneyin veya muadil arama seçeneğini kapatın
          </p>
        </div>
      )}

      {/* Empty State */}
      {!submittedQuery && (
        <div className="text-center py-12 bg-white rounded-lg shadow-sm border border-gray-200">
          <Search className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">
            Aramaya başlayın
          </h3>
          <p className="text-gray-600">
            Arama yapmak için en az 2 karakter girin ve Enter'a basın veya "Ara" butonuna tıklayın
          </p>
        </div>
      )}

      {/* Stock Card Dialog */}
      {selectedVariant && (
        <StockCardDialog
          open={showStockCard}
          onOpenChange={setShowStockCard}
          variant={selectedVariant}
        />
      )}
    </div>
  );
}
