import { useState, useEffect, useRef } from "react";
import { Search, Plus, Trash2, CreditCard, FileText, Save, X } from "lucide-react";

interface SalesLine {
  id: string;
  variantId: string;
  sku: string;
  name: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  totalPrice: number;
  stock: number;
}

interface Party {
  id: string;
  name: string;
  code: string;
  balance: number;
  creditLimit: number;
}

type SaleType = "cash" | "credit";
type PaymentMethod = "cash" | "card" | "bank";

export default function FastSalesPage() {
  const [saleType, setSaleType] = useState<SaleType>("cash");
  const [salesLines, setSalesLines] = useState<SalesLine[]>([]);
  const [selectedParty, setSelectedParty] = useState<Party | null>(null);
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>("cash");
  
  // Search state
  const [searchQuery, setSearchQuery] = useState("");
  const [searchResults, setSearchResults] = useState<any[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  
  // Refs for keyboard navigation
  const searchInputRef = useRef<HTMLInputElement>(null);
  const barcodeInputRef = useRef<HTMLInputElement>(null);

  // Calculate totals
  const subtotal = salesLines.reduce((sum, line) => sum + line.totalPrice, 0);
  const totalDiscount = salesLines.reduce((sum, line) => sum + (line.unitPrice * line.quantity * line.discount / 100), 0);
  const grandTotal = subtotal;

  useEffect(() => {
    // Focus barcode input on mount
    barcodeInputRef.current?.focus();

    // Keyboard shortcuts
    const handleKeyPress = (e: KeyboardEvent) => {
      if (e.key === "F1") {
        e.preventDefault();
        barcodeInputRef.current?.focus();
      } else if (e.key === "F2") {
        e.preventDefault();
        searchInputRef.current?.focus();
      } else if (e.key === "F9") {
        e.preventDefault();
        handleSale();
      } else if (e.key === "Escape") {
        e.preventDefault();
        handleCancel();
      }
    };

    window.addEventListener("keydown", handleKeyPress);
    return () => window.removeEventListener("keydown", handleKeyPress);
  }, [salesLines, saleType, selectedParty]);

  const handleSearch = async () => {
    if (!searchQuery.trim()) return;

    setIsSearching(true);
    try {
      const token = localStorage.getItem("token");
      const response = await fetch(
        `http://localhost:5039/api/search/variants?q=${encodeURIComponent(searchQuery)}&includeEquivalents=true`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );
      const data = await response.json();
      setSearchResults(data.results || []);
    } catch (error) {
      console.error("Search error:", error);
    } finally {
      setIsSearching(false);
    }
  };

  const addToCart = (variant: any) => {
    const existing = salesLines.find(line => line.variantId === variant.variantId);
    
    if (existing) {
      // Increase quantity
      setSalesLines(salesLines.map(line =>
        line.variantId === variant.variantId
          ? { ...line, quantity: line.quantity + 1, totalPrice: (line.quantity + 1) * line.unitPrice * (1 - line.discount / 100) }
          : line
      ));
    } else {
      // Add new line
      const newLine: SalesLine = {
        id: Math.random().toString(36).substr(2, 9),
        variantId: variant.variantId,
        sku: variant.sku,
        name: variant.variantName,
        quantity: 1,
        unitPrice: variant.price || 0, // TODO: Get price from API
        discount: 0,
        totalPrice: variant.price || 0,
        stock: variant.stock || 0,
      };
      setSalesLines([...salesLines, newLine]);
    }

    // Clear search
    setSearchQuery("");
    setSearchResults([]);
    barcodeInputRef.current?.focus();
  };

  const updateLine = (id: string, field: keyof SalesLine, value: any) => {
    setSalesLines(salesLines.map(line => {
      if (line.id !== id) return line;
      
      const updated = { ...line, [field]: value };
      
      // Recalculate total
      if (field === "quantity" || field === "unitPrice" || field === "discount") {
        updated.totalPrice = updated.quantity * updated.unitPrice * (1 - updated.discount / 100);
      }
      
      return updated;
    }));
  };

  const removeLine = (id: string) => {
    setSalesLines(salesLines.filter(line => line.id !== id));
  };

  const handleSale = async () => {
    if (salesLines.length === 0) {
      alert("Satır ekleyiniz!");
      return;
    }

    if (saleType === "credit" && !selectedParty) {
      alert("Cari seçiniz!");
      return;
    }

    // TODO: Submit sale to backend
    console.log("Processing sale:", {
      type: saleType,
      party: selectedParty,
      lines: salesLines,
      total: grandTotal,
      paymentMethod: saleType === "cash" ? paymentMethod : null,
    });

    alert(`Satış tamamlandı!\nToplam: ₺${grandTotal.toFixed(2)}`);
    
    // Clear cart
    setSalesLines([]);
    setSelectedParty(null);
    barcodeInputRef.current?.focus();
  };

  const handleCancel = () => {
    if (salesLines.length > 0) {
      if (confirm("Satışı iptal etmek istediğinize emin misiniz?")) {
        setSalesLines([]);
        setSelectedParty(null);
      }
    }
  };

  return (
    <div className="h-screen flex flex-col bg-slate-50">
      {/* Header */}
      <div className="bg-gradient-to-r from-green-600 to-green-700 text-white px-6 py-4 shadow-lg">
        <h1 className="text-2xl font-bold">⚡ Hızlı Satış</h1>
      </div>

      <div className="flex-1 flex overflow-hidden">
        {/* LEFT: Search Panel */}
        <div className="w-1/3 border-r border-slate-200 bg-white flex flex-col">
          <div className="p-4 border-b border-slate-200 bg-slate-50">
            <h2 className="font-semibold text-slate-700 mb-3">Ürün Arama</h2>
            
            {/* Barcode Input */}
            <div className="mb-3">
              <label className="block text-sm text-slate-600 mb-1">
                <kbd className="bg-slate-200 px-2 py-1 rounded text-xs">F1</kbd> Barkod
              </label>
              <input
                ref={barcodeInputRef}
                type="text"
                placeholder="Barkod okutun veya girin..."
                className="w-full px-4 py-3 border border-slate-300 rounded-lg font-mono text-lg focus:ring-2 focus:ring-green-500 focus:border-green-500"
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    setSearchQuery(e.currentTarget.value);
                    handleSearch();
                    e.currentTarget.value = "";
                  }
                }}
              />
            </div>

            {/* OEM / Name Search */}
            <div className="mb-3">
              <label className="block text-sm text-slate-600 mb-1">
                <kbd className="bg-slate-200 px-2 py-1 rounded text-xs">F2</kbd> OEM / Ürün Adı
              </label>
              <div className="flex gap-2">
                <input
                  ref={searchInputRef}
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  onKeyDown={(e) => e.key === "Enter" && handleSearch()}
                  placeholder="OEM kodu veya ürün adı..."
                  className="flex-1 px-4 py-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
                <button
                  onClick={handleSearch}
                  disabled={isSearching}
                  className="px-4 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
                >
                  <Search className="w-5 h-5" />
                </button>
              </div>
            </div>
          </div>

          {/* Search Results */}
          <div className="flex-1 overflow-y-auto p-4">
            {searchResults.length === 0 && !isSearching && (
              <div className="text-center text-slate-400 mt-8">
                <Search className="w-12 h-12 mx-auto mb-2 opacity-50" />
                <p>Aramaya başlayın</p>
              </div>
            )}

            {isSearching && (
              <div className="text-center text-slate-400 mt-8">
                <div className="animate-spin w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full mx-auto"></div>
                <p className="mt-2">Aranıyor...</p>
              </div>
            )}

            <div className="space-y-2">
              {searchResults.map((result) => (
                <div
                  key={result.variantId}
                  className="border border-slate-200 rounded-lg p-3 hover:bg-green-50 hover:border-green-300 cursor-pointer transition-colors"
                  onClick={() => addToCart(result)}
                >
                  <div className="flex justify-between items-start mb-1">
                    <div className="font-medium text-slate-800">{result.variantName}</div>
                    <div className="text-xs bg-slate-100 px-2 py-1 rounded">
                      Stok: {result.stock || 0}
                    </div>
                  </div>
                  <div className="text-sm text-slate-600">{result.sku}</div>
                  {result.oemCodes && result.oemCodes.length > 0 && (
                    <div className="text-xs text-blue-600 mt-1">
                      OEM: {result.oemCodes.join(", ")}
                    </div>
                  )}
                  {result.matchType === "equivalent" && (
                    <div className="text-xs text-orange-600 mt-1">
                      ⚡ Muadil
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* CENTER: Sales Lines */}
        <div className="flex-1 flex flex-col bg-white">
          <div className="p-4 border-b border-slate-200 bg-slate-50">
            <h2 className="font-semibold text-slate-700">Satış Kalemleri</h2>
          </div>

          <div className="flex-1 overflow-y-auto">
            {salesLines.length === 0 ? (
              <div className="text-center text-slate-400 mt-16">
                <Plus className="w-16 h-16 mx-auto mb-3 opacity-50" />
                <p className="text-lg">Satış kalemi ekleyiniz</p>
                <p className="text-sm">Barkod okutun veya OEM arayın</p>
              </div>
            ) : (
              <table className="w-full">
                <thead className="bg-slate-100 sticky top-0">
                  <tr className="text-left text-sm text-slate-600">
                    <th className="px-4 py-3">SKU / Ürün</th>
                    <th className="px-4 py-3 w-24">Miktar</th>
                    <th className="px-4 py-3 w-32">Birim Fiyat</th>
                    <th className="px-4 py-3 w-24">İskonto %</th>
                    <th className="px-4 py-3 w-32">Toplam</th>
                    <th className="px-4 py-3 w-16"></th>
                  </tr>
                </thead>
                <tbody>
                  {salesLines.map((line) => (
                    <tr key={line.id} className="border-b border-slate-100 hover:bg-slate-50">
                      <td className="px-4 py-3">
                        <div className="font-medium text-slate-800">{line.name}</div>
                        <div className="text-sm text-slate-500">{line.sku}</div>
                        <div className="text-xs text-slate-400">Stok: {line.stock}</div>
                      </td>
                      <td className="px-4 py-3">
                        <input
                          type="number"
                          min="1"
                          max={line.stock}
                          value={line.quantity}
                          onChange={(e) => updateLine(line.id, "quantity", parseInt(e.target.value) || 1)}
                          className="w-20 px-2 py-1 border border-slate-300 rounded text-center focus:ring-2 focus:ring-green-500"
                        />
                      </td>
                      <td className="px-4 py-3">
                        <input
                          type="number"
                          step="0.01"
                          value={line.unitPrice}
                          onChange={(e) => updateLine(line.id, "unitPrice", parseFloat(e.target.value) || 0)}
                          className="w-28 px-2 py-1 border border-slate-300 rounded text-right focus:ring-2 focus:ring-green-500"
                        />
                      </td>
                      <td className="px-4 py-3">
                        <input
                          type="number"
                          min="0"
                          max="100"
                          step="0.1"
                          value={line.discount}
                          onChange={(e) => updateLine(line.id, "discount", parseFloat(e.target.value) || 0)}
                          className="w-20 px-2 py-1 border border-slate-300 rounded text-center focus:ring-2 focus:ring-green-500"
                        />
                      </td>
                      <td className="px-4 py-3 font-semibold text-slate-800 text-right">
                        ₺{line.totalPrice.toFixed(2)}
                      </td>
                      <td className="px-4 py-3">
                        <button
                          onClick={() => removeLine(line.id)}
                          className="text-red-600 hover:text-red-800 p-1"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {/* Totals */}
          {salesLines.length > 0 && (
            <div className="border-t-2 border-slate-300 bg-slate-50 p-4">
              <div className="flex justify-end space-y-2">
                <div className="w-64">
                  <div className="flex justify-between text-slate-600 mb-1">
                    <span>Ara Toplam:</span>
                    <span>₺{subtotal.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-slate-600 mb-1">
                    <span>Toplam İskonto:</span>
                    <span className="text-red-600">-₺{totalDiscount.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-2xl font-bold text-slate-800 pt-2 border-t border-slate-300">
                    <span>TOPLAM:</span>
                    <span>₺{grandTotal.toFixed(2)}</span>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* RIGHT: Payment Panel */}
        <div className="w-80 border-l border-slate-200 bg-white flex flex-col">
          <div className="p-4 border-b border-slate-200 bg-slate-50">
            <h2 className="font-semibold text-slate-700 mb-3">Ödeme Türü</h2>
            
            {/* Sale Type Toggle */}
            <div className="grid grid-cols-2 gap-2 mb-4">
              <button
                onClick={() => setSaleType("cash")}
                className={`px-4 py-3 rounded-lg font-medium transition-colors ${
                  saleType === "cash"
                    ? "bg-green-600 text-white"
                    : "bg-slate-100 text-slate-600 hover:bg-slate-200"
                }`}
              >
                Peşin Satış
              </button>
              <button
                onClick={() => setSaleType("credit")}
                className={`px-4 py-3 rounded-lg font-medium transition-colors ${
                  saleType === "credit"
                    ? "bg-purple-600 text-white"
                    : "bg-slate-100 text-slate-600 hover:bg-slate-200"
                }`}
              >
                Veresiye
              </button>
            </div>
          </div>

          {/* Payment Method (Cash Sale) */}
          {saleType === "cash" && (
            <div className="p-4 border-b border-slate-200">
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Ödeme Şekli
              </label>
              <div className="space-y-2">
                <button
                  onClick={() => setPaymentMethod("cash")}
                  className={`w-full px-4 py-2 rounded-lg text-left transition-colors ${
                    paymentMethod === "cash"
                      ? "bg-green-100 border-2 border-green-600 text-green-800"
                      : "bg-slate-50 border border-slate-300 text-slate-600 hover:bg-slate-100"
                  }`}
                >
                  💵 Nakit
                </button>
                <button
                  onClick={() => setPaymentMethod("card")}
                  className={`w-full px-4 py-2 rounded-lg text-left transition-colors ${
                    paymentMethod === "card"
                      ? "bg-green-100 border-2 border-green-600 text-green-800"
                      : "bg-slate-50 border border-slate-300 text-slate-600 hover:bg-slate-100"
                  }`}
                >
                  💳 Kredi Kartı
                </button>
                <button
                  onClick={() => setPaymentMethod("bank")}
                  className={`w-full px-4 py-2 rounded-lg text-left transition-colors ${
                    paymentMethod === "bank"
                      ? "bg-green-100 border-2 border-green-600 text-green-800"
                      : "bg-slate-50 border border-slate-300 text-slate-600 hover:bg-slate-100"
                  }`}
                >
                  🏦 Banka Transferi
                </button>
              </div>
            </div>
          )}

          {/* Party Selection (Credit Sale) */}
          {saleType === "credit" && (
            <div className="p-4 border-b border-slate-200">
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Cari Seçimi
              </label>
              {!selectedParty ? (
                <div>
                  <input
                    type="text"
                    placeholder="Cari ara..."
                    className="w-full px-3 py-2 border border-slate-300 rounded-lg mb-2 focus:ring-2 focus:ring-purple-500"
                  />
                  <button className="w-full px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700">
                    Cari Seç
                  </button>
                  <p className="text-xs text-slate-500 mt-2">
                    TODO: Cari arama popup'ı eklenecek
                  </p>
                </div>
              ) : (
                <div className="bg-purple-50 border border-purple-200 rounded-lg p-3">
                  <div className="flex justify-between items-start mb-2">
                    <div>
                      <div className="font-medium text-purple-900">{selectedParty.name}</div>
                      <div className="text-sm text-purple-600">{selectedParty.code}</div>
                    </div>
                    <button
                      onClick={() => setSelectedParty(null)}
                      className="text-purple-600 hover:text-purple-800"
                    >
                      <X className="w-4 h-4" />
                    </button>
                  </div>
                  <div className="text-sm">
                    <div className="flex justify-between mb-1">
                      <span className="text-slate-600">Bakiye:</span>
                      <span className={selectedParty.balance > 0 ? "text-red-600 font-medium" : "text-green-600"}>
                        ₺{Math.abs(selectedParty.balance).toFixed(2)}
                        {selectedParty.balance > 0 ? " (Borç)" : " (Alacak)"}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-600">Limit:</span>
                      <span className="text-slate-800">₺{selectedParty.creditLimit.toFixed(2)}</span>
                    </div>
                  </div>
                  {(selectedParty.balance + grandTotal) > selectedParty.creditLimit && (
                    <div className="mt-2 text-xs bg-orange-100 border border-orange-300 text-orange-800 px-2 py-1 rounded">
                      ⚠️ Limit aşılacak!
                    </div>
                  )}
                </div>
              )}
            </div>
          )}

          {/* Action Buttons */}
          <div className="flex-1"></div>
          <div className="p-4 space-y-3 border-t border-slate-200">
            <button
              onClick={handleSale}
              disabled={salesLines.length === 0}
              className="w-full px-6 py-4 bg-gradient-to-r from-green-600 to-green-700 text-white rounded-xl font-bold text-lg hover:from-green-700 hover:to-green-800 disabled:opacity-50 disabled:cursor-not-allowed shadow-lg hover:shadow-xl transition-all"
            >
              <div className="flex items-center justify-center gap-2">
                {saleType === "cash" ? <CreditCard className="w-6 h-6" /> : <FileText className="w-6 h-6" />}
                <span>{saleType === "cash" ? "Fatura Kes" : "İrsaliye Kes"}</span>
              </div>
              <div className="text-sm opacity-90 mt-1">
                <kbd className="bg-white/20 px-2 py-0.5 rounded">F9</kbd>
              </div>
            </button>

            <button
              onClick={handleCancel}
              className="w-full px-6 py-3 bg-slate-200 text-slate-700 rounded-lg font-medium hover:bg-slate-300 transition-colors"
            >
              <div className="flex items-center justify-center gap-2">
                <X className="w-5 h-5" />
                <span>İptal</span>
              </div>
              <div className="text-xs opacity-70 mt-1">
                <kbd className="bg-slate-300 px-2 py-0.5 rounded">ESC</kbd>
              </div>
            </button>
          </div>

          {/* Keyboard Shortcuts */}
          <div className="p-4 bg-slate-800 text-white text-xs">
            <div className="space-y-1">
              <div><kbd className="bg-slate-700 px-1.5 py-0.5 rounded">F1</kbd> Barkod</div>
              <div><kbd className="bg-slate-700 px-1.5 py-0.5 rounded">F2</kbd> Arama</div>
              <div><kbd className="bg-slate-700 px-1.5 py-0.5 rounded">F9</kbd> Satış</div>
              <div><kbd className="bg-slate-700 px-1.5 py-0.5 rounded">ESC</kbd> İptal</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
