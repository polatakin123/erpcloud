import { useState, useEffect, useRef } from "react";
import { Search, Package, AlertCircle, Layers, MapPin } from "lucide-react";

interface Variant {
  variantId: string;
  sku: string;
  name: string;
  barcode?: string;
  oemCodes: string[];
  unit: string;
  isEquivalent?: boolean;
  equivalentTo?: string;
}

interface StockBalance {
  warehouseId: string;
  warehouseName: string;
  available: number;
  reserved: number;
  onOrder: number;
  total: number;
}

interface SearchResult {
  variant: Variant;
  stockBalances: StockBalance[];
  totalStock: number;
  equivalents: Variant[];
}

export default function StokSorguPage() {
  const [searchQuery, setSearchQuery] = useState("");
  const [searchResults, setSearchResults] = useState<SearchResult[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [includeEquivalents, setIncludeEquivalents] = useState(true);
  
  const searchInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    // Focus search input on mount
    searchInputRef.current?.focus();

    // Keyboard shortcuts
    const handleKeyPress = (e: KeyboardEvent) => {
      if (e.key === "F1") {
        e.preventDefault();
        searchInputRef.current?.focus();
      } else if (e.key === "F2") {
        e.preventDefault();
        setIncludeEquivalents(!includeEquivalents);
      } else if (e.key === "Enter" && searchQuery.trim()) {
        e.preventDefault();
        handleSearch();
      } else if (e.key === "Escape") {
        e.preventDefault();
        handleClear();
      }
    };

    window.addEventListener("keydown", handleKeyPress);
    return () => window.removeEventListener("keydown", handleKeyPress);
  }, [searchQuery, includeEquivalents]);

  const handleSearch = async () => {
    if (!searchQuery.trim()) {
      alert("Lütfen arama terimi girin (OEM, SKU, Barkod veya İsim)");
      return;
    }

    setIsSearching(true);
    try {
      const token = localStorage.getItem("token");
      const response = await fetch(
        `http://localhost:5039/api/search/variants?q=${encodeURIComponent(searchQuery)}&includeEquivalents=${includeEquivalents}&includeStock=true`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );
      const data = await response.json();
      
      // Transform results to include stock info
      const results: SearchResult[] = (data.results || []).map((item: any) => ({
        variant: {
          variantId: item.variantId,
          sku: item.sku,
          name: item.variantName || item.name,
          barcode: item.barcode,
          oemCodes: item.oemCodes || [],
          unit: item.unit || "ADET",
          isEquivalent: item.isEquivalent,
          equivalentTo: item.equivalentTo,
        },
        stockBalances: item.stockBalances || [],
        totalStock: item.stock || item.available || 0,
        equivalents: item.equivalents || [],
      }));
      
      setSearchResults(results);
    } catch (error) {
      console.error("Search error:", error);
      alert("Arama sırasında hata oluştu");
    } finally {
      setIsSearching(false);
    }
  };

  const handleClear = () => {
    setSearchQuery("");
    setSearchResults([]);
    searchInputRef.current?.focus();
  };

  const getStockStatusColor = (stock: number) => {
    if (stock <= 0) return "text-red-600";
    if (stock <= 5) return "text-orange-600";
    return "text-green-600";
  };

  const getStockStatusBg = (stock: number) => {
    if (stock <= 0) return "bg-red-50 border-red-200";
    if (stock <= 5) return "bg-orange-50 border-orange-200";
    return "bg-green-50 border-green-200";
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-slate-800 flex items-center gap-3">
            <Package className="w-8 h-8 text-blue-600" />
            Stok Sorgulama
          </h1>
          <p className="text-slate-600 mt-1">
            OEM, SKU, Barkod veya isim ile stok durumu sorgulayın
          </p>
        </div>

        {/* Keyboard Shortcuts */}
        <div className="bg-white rounded-lg shadow-md p-4 mb-6 border-l-4 border-blue-500">
          <div className="flex gap-6 text-sm text-slate-600">
            <span><kbd className="px-2 py-1 bg-slate-100 rounded">F1</kbd> Arama</span>
            <span><kbd className="px-2 py-1 bg-slate-100 rounded">F2</kbd> Muadil Aç/Kapa</span>
            <span><kbd className="px-2 py-1 bg-slate-100 rounded">Enter</kbd> Ara</span>
            <span><kbd className="px-2 py-1 bg-slate-100 rounded">ESC</kbd> Temizle</span>
          </div>
        </div>

        {/* Search Bar */}
        <div className="bg-white rounded-lg shadow-lg p-6 mb-6">
          <div className="flex gap-4">
            <div className="flex-1">
              <input
                ref={searchInputRef}
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                onKeyPress={(e) => e.key === "Enter" && handleSearch()}
                placeholder="OEM Kodu, SKU, Barkod veya Ürün İsmi..."
                className="w-full px-6 py-4 border-2 border-slate-300 rounded-lg focus:border-blue-500 focus:outline-none text-xl"
              />
            </div>
            <button
              onClick={handleSearch}
              disabled={isSearching || !searchQuery.trim()}
              className="px-8 py-4 bg-gradient-to-r from-blue-600 to-blue-700 text-white rounded-lg font-semibold text-lg hover:from-blue-700 hover:to-blue-800 disabled:opacity-50 disabled:cursor-not-allowed transition-all shadow-lg hover:shadow-xl flex items-center gap-2"
            >
              <Search className="w-6 h-6" />
              {isSearching ? "Aranıyor..." : "Ara"}
            </button>
            {searchResults.length > 0 && (
              <button
                onClick={handleClear}
                className="px-6 py-4 bg-slate-200 text-slate-700 rounded-lg font-semibold hover:bg-slate-300 transition-all"
              >
                Temizle
              </button>
            )}
          </div>

          {/* Options */}
          <div className="mt-4 flex items-center gap-4">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={includeEquivalents}
                onChange={(e) => setIncludeEquivalents(e.target.checked)}
                className="w-5 h-5 text-blue-600 rounded focus:ring-2 focus:ring-blue-500"
              />
              <span className="text-slate-700 font-medium">
                Muadil parçaları da göster (F2)
              </span>
            </label>
          </div>
        </div>

        {/* Results */}
        {searchResults.length > 0 && (
          <div className="space-y-4">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-xl font-semibold text-slate-800">
                {searchResults.length} sonuç bulundu
              </h2>
            </div>

            {searchResults.map((result) => (
              <div
                key={result.variant.variantId}
                className="bg-white rounded-lg shadow-lg overflow-hidden border-2 border-slate-200"
              >
                {/* Variant Header */}
                <div className="bg-gradient-to-r from-blue-50 to-slate-50 p-6 border-b-2 border-slate-200">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-3 mb-2">
                        <h3 className="text-2xl font-bold text-slate-800">
                          {result.variant.sku}
                        </h3>
                        {result.variant.isEquivalent && (
                          <span className="px-3 py-1 bg-orange-100 text-orange-700 rounded-full text-sm font-semibold">
                            Muadil
                          </span>
                        )}
                      </div>
                      <p className="text-lg text-slate-700 mb-2">
                        {result.variant.name}
                      </p>
                      <div className="flex gap-4 text-sm text-slate-600">
                        {result.variant.barcode && (
                          <span className="flex items-center gap-1">
                            <Package className="w-4 h-4" />
                            Barkod: {result.variant.barcode}
                          </span>
                        )}
                        {result.variant.oemCodes.length > 0 && (
                          <span className="flex items-center gap-1">
                            <Layers className="w-4 h-4" />
                            OEM: {result.variant.oemCodes.join(", ")}
                          </span>
                        )}
                        <span>Birim: {result.variant.unit}</span>
                      </div>
                    </div>
                    <div className={`px-6 py-4 rounded-lg border-2 ${getStockStatusBg(result.totalStock)}`}>
                      <div className="text-sm text-slate-600 mb-1">Toplam Stok</div>
                      <div className={`text-3xl font-bold ${getStockStatusColor(result.totalStock)}`}>
                        {result.totalStock}
                      </div>
                    </div>
                  </div>
                </div>

                {/* Stock by Warehouse */}
                {result.stockBalances.length > 0 && (
                  <div className="p-6">
                    <h4 className="text-lg font-semibold text-slate-800 mb-4 flex items-center gap-2">
                      <MapPin className="w-5 h-5" />
                      Depolar
                    </h4>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                      {result.stockBalances.map((balance) => (
                        <div
                          key={balance.warehouseId}
                          className="p-4 bg-slate-50 rounded-lg border border-slate-200"
                        >
                          <div className="font-semibold text-slate-800 mb-3">
                            {balance.warehouseName}
                          </div>
                          <div className="space-y-2 text-sm">
                            <div className="flex justify-between">
                              <span className="text-slate-600">Kullanılabilir:</span>
                              <span className={`font-bold ${getStockStatusColor(balance.available)}`}>
                                {balance.available}
                              </span>
                            </div>
                            {balance.reserved > 0 && (
                              <div className="flex justify-between">
                                <span className="text-slate-600">Rezerve:</span>
                                <span className="font-semibold text-orange-600">
                                  {balance.reserved}
                                </span>
                              </div>
                            )}
                            {balance.onOrder > 0 && (
                              <div className="flex justify-between">
                                <span className="text-slate-600">Siparişte:</span>
                                <span className="font-semibold text-blue-600">
                                  {balance.onOrder}
                                </span>
                              </div>
                            )}
                            <div className="flex justify-between pt-2 border-t border-slate-300">
                              <span className="text-slate-700 font-medium">Toplam:</span>
                              <span className="font-bold text-slate-800">
                                {balance.total}
                              </span>
                            </div>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* Equivalents */}
                {result.equivalents.length > 0 && (
                  <div className="p-6 bg-orange-50 border-t-2 border-orange-200">
                    <h4 className="text-lg font-semibold text-orange-800 mb-4 flex items-center gap-2">
                      <Layers className="w-5 h-5" />
                      Muadil Parçalar ({result.equivalents.length})
                    </h4>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                      {result.equivalents.map((eq) => (
                        <div
                          key={eq.variantId}
                          className="p-3 bg-white rounded-lg border border-orange-200"
                        >
                          <div className="font-semibold text-slate-800">
                            {eq.sku}
                          </div>
                          <div className="text-sm text-slate-600">
                            {eq.name}
                          </div>
                          {eq.oemCodes.length > 0 && (
                            <div className="text-xs text-slate-500 mt-1">
                              OEM: {eq.oemCodes.join(", ")}
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}

        {/* No Results */}
        {!isSearching && searchQuery && searchResults.length === 0 && (
          <div className="bg-white rounded-lg shadow-lg p-12 text-center">
            <AlertCircle className="w-16 h-16 mx-auto mb-4 text-slate-400" />
            <h3 className="text-xl font-semibold text-slate-700 mb-2">
              Sonuç bulunamadı
            </h3>
            <p className="text-slate-600">
              "<span className="font-semibold">{searchQuery}</span>" için sonuç bulunamadı.
            </p>
            <p className="text-sm text-slate-500 mt-2">
              Farklı bir arama terimi deneyin veya muadil aramasını açın.
            </p>
          </div>
        )}

        {/* Empty State */}
        {!searchQuery && searchResults.length === 0 && (
          <div className="bg-white rounded-lg shadow-lg p-12 text-center">
            <Search className="w-16 h-16 mx-auto mb-4 text-slate-400" />
            <h3 className="text-xl font-semibold text-slate-700 mb-2">
              Stok sorgulama için arama yapın
            </h3>
            <p className="text-slate-600">
              OEM kodu, SKU, barkod veya ürün ismi ile arayabilirsiniz
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
