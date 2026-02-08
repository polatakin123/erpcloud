import { useState, useEffect, useRef } from "react";
import { Search, X, User, Clock, Plus } from "lucide-react";
import { ApiClient } from "../lib/api-client";
import { useToast } from "../hooks/useToast";

interface Party {
  id: string;
  code: string;
  name: string;
  phone?: string;
  email?: string;
  balance: number;
  creditLimit: number;
  type: string;
}

interface CustomerPickerModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSelect: (party: Party) => void;
}

export default function CustomerPickerModal({ isOpen, onClose, onSelect }: CustomerPickerModalProps) {
  const [searchQuery, setSearchQuery] = useState("");
  const [searchResults, setSearchResults] = useState<Party[]>([]);
  const [recentCustomers, setRecentCustomers] = useState<Party[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  
  const searchInputRef = useRef<HTMLInputElement>(null);
  const searchTimeoutRef = useRef<number | null>(null);
  const { toast } = useToast();

  useEffect(() => {
    if (isOpen) {
      searchInputRef.current?.focus();
      loadRecentCustomers();
      setSearchQuery("");
      setSearchResults([]);
      setSelectedIndex(0);
    }
  }, [isOpen]);

  useEffect(() => {
    if (!isOpen) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      const currentList = searchQuery ? searchResults : recentCustomers;
      
      if (e.key === "Escape") {
        e.preventDefault();
        onClose();
      } else if (e.key === "ArrowDown") {
        e.preventDefault();
        setSelectedIndex((prev) => (prev + 1) % currentList.length);
      } else if (e.key === "ArrowUp") {
        e.preventDefault();
        setSelectedIndex((prev) => (prev - 1 + currentList.length) % currentList.length);
      } else if (e.key === "Enter") {
        e.preventDefault();
        if (currentList.length > 0 && selectedIndex >= 0) {
          onSelect(currentList[selectedIndex]);
          onClose();
        }
      }
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isOpen, searchQuery, searchResults, recentCustomers, selectedIndex, onClose, onSelect]);

  useEffect(() => {
    // Debounced search
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }

    if (searchQuery.trim().length < 2) {
      setSearchResults([]);
      return;
    }

    searchTimeoutRef.current = setTimeout(() => {
      performSearch(searchQuery);
    }, 300);

    return () => {
      if (searchTimeoutRef.current) {
        clearTimeout(searchTimeoutRef.current);
      }
    };
  }, [searchQuery]);

  const loadRecentCustomers = async () => {
    try {
      // Get recent customers from localStorage or API
      const recent = localStorage.getItem("recentCustomers");
      if (recent) {
        setRecentCustomers(JSON.parse(recent).slice(0, 5));
      }
    } catch (error) {
      console.error("Failed to load recent customers:", error);
    }
  };

  const performSearch = async (query: string) => {
    setIsSearching(true);
    try {
      const response = await ApiClient.get<{ items: Party[] }>(
        `/api/parties?search=${encodeURIComponent(query)}&type=CUSTOMER&page=1&pageSize=20`
      );
      setSearchResults(response.items || []);
      setSelectedIndex(0);
    } catch (error) {
      console.error("Search error:", error);
      setSearchResults([]);
    } finally {
      setIsSearching(false);
    }
  };

  const handleSelect = (party: Party) => {
    // Save to recent customers
    const recent = localStorage.getItem("recentCustomers");
    let recentList: Party[] = recent ? JSON.parse(recent) : [];
    recentList = [party, ...recentList.filter(p => p.id !== party.id)].slice(0, 10);
    localStorage.setItem("recentCustomers", JSON.stringify(recentList));

    onSelect(party);
    onClose();
  };

  const handleCreateCustomer = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    
    setIsCreating(true);
    try {
      const newParty = await ApiClient.post<Party>('/api/parties', {
        type: 'CUSTOMER',
        code: formData.get('code'),
        name: formData.get('name'),
        phone: formData.get('phone') || undefined,
        email: formData.get('email') || undefined,
        taxNumber: formData.get('taxNumber') || undefined,
        taxOffice: formData.get('taxOffice') || undefined,
        address: formData.get('address') || undefined,
      });

      toast({
        title: "Cari Oluşturuldu",
        description: `${newParty.code} - ${newParty.name} başarıyla oluşturuldu`,
      });

      handleSelect(newParty);
    } catch (error: any) {
      toast({
        variant: "destructive",
        title: "Hata",
        description: error.message || "Cari oluşturulamadı",
      });
    } finally {
      setIsCreating(false);
    }
  };

  if (!isOpen) return null;

  const displayList = searchQuery ? searchResults : recentCustomers;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" onClick={onClose}>
      <div
        className="bg-white rounded-xl shadow-2xl w-full max-w-2xl mx-4 max-h-[80vh] flex flex-col"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-slate-200">
          <h2 className="text-2xl font-bold text-slate-800 flex items-center gap-2">
            <User className="w-6 h-6 text-blue-600" />
            Cari Seç
          </h2>
          <button
            onClick={onClose}
            className="text-slate-400 hover:text-slate-600 transition-colors"
          >
            <X className="w-6 h-6" />
          </button>
        </div>

        {/* Search Input */}
        <div className="p-6 border-b border-slate-200">
          <div className="relative">
            <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-slate-400" />
            <input
              ref={searchInputRef}
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Cari kod, isim veya telefon numarası ara..."
              className="w-full pl-12 pr-4 py-3 border-2 border-slate-300 rounded-lg text-lg focus:border-blue-500 focus:outline-none"
            />
          </div>
          <div className="mt-2 flex items-center justify-between">
            <div className="flex items-center gap-4 text-sm text-slate-600">
              <span><kbd className="px-2 py-1 bg-slate-100 rounded">↑↓</kbd> Gezin</span>
              <span><kbd className="px-2 py-1 bg-slate-100 rounded">Enter</kbd> Seç</span>
              <span><kbd className="px-2 py-1 bg-slate-100 rounded">ESC</kbd> Kapat</span>
            </div>
            <button
              onClick={() => setShowCreateForm(true)}
              className="flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors text-sm font-medium"
            >
              <Plus className="w-4 h-4" />
              Yeni Cari
            </button>
          </div>
        </div>

        {/* Results/Recent */}
        <div className="flex-1 overflow-y-auto p-6">
          {!searchQuery && recentCustomers.length > 0 && (
            <div className="mb-4">
              <div className="flex items-center gap-2 text-sm font-semibold text-slate-600 mb-3">
                <Clock className="w-4 h-4" />
                Son Kullanılan Cariler
              </div>
            </div>
          )}

          {isSearching && (
            <div className="text-center py-12">
              <div className="animate-spin w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full mx-auto mb-3"></div>
              <p className="text-slate-600">Aranıyor...</p>
            </div>
          )}

          {!isSearching && displayList.length === 0 && (
            <div className="text-center py-12">
              <User className="w-16 h-16 mx-auto mb-3 text-slate-300" />
              <p className="text-slate-600">
                {searchQuery ? "Sonuç bulunamadı" : "Henüz cari kullanılmamış"}
              </p>
              <p className="text-sm text-slate-500 mt-1">
                {searchQuery ? "Farklı bir arama terimi deneyin" : "Arama yaparak cari bulabilirsiniz"}
              </p>
            </div>
          )}

          {!isSearching && displayList.length > 0 && (
            <div className="space-y-2">
              {displayList.map((party, index) => (
                <button
                  key={party.id}
                  onClick={() => handleSelect(party)}
                  className={`w-full text-left p-4 rounded-lg border-2 transition-all ${
                    index === selectedIndex
                      ? "border-blue-500 bg-blue-50"
                      : "border-slate-200 bg-white hover:border-blue-300 hover:bg-blue-50"
                  }`}
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="font-semibold text-slate-800 text-lg">
                        {party.code} - {party.name}
                      </div>
                      {(party.phone || party.email) && (
                        <div className="text-sm text-slate-600 mt-1">
                          {party.phone && <span>{party.phone}</span>}
                          {party.phone && party.email && <span className="mx-2">•</span>}
                          {party.email && <span>{party.email}</span>}
                        </div>
                      )}
                    </div>
                    <div className="text-right ml-4">
                      <div className="text-sm text-slate-600">Bakiye</div>
                      <div className={`font-bold ${party.balance > 0 ? "text-red-600" : "text-green-600"}`}>
                        ₺{Math.abs(party.balance).toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
                      </div>
                      <div className="text-xs text-slate-500">
                        {party.balance > 0 ? "Alacak" : "Borç"}
                      </div>
                    </div>
                  </div>
                  {party.creditLimit > 0 && (
                    <div className="mt-2 text-xs text-slate-500">
                      Kredi Limiti: ₺{party.creditLimit.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
                    </div>
                  )}
                </button>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Quick Create Form Modal */}
      {showCreateForm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" onClick={() => setShowCreateForm(false)}>
          <div
            className="bg-white rounded-xl shadow-2xl w-full max-w-md mx-4"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between p-6 border-b border-slate-200">
              <h3 className="text-xl font-bold text-slate-800 flex items-center gap-2">
                <Plus className="w-5 h-5 text-green-600" />
                Yeni Cari Oluştur
              </h3>
              <button
                onClick={() => setShowCreateForm(false)}
                className="text-slate-400 hover:text-slate-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleCreateCustomer} className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Cari Kodu <span className="text-red-500">*</span>
                </label>
                <input
                  name="code"
                  type="text"
                  required
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500"
                  placeholder="C001"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Cari Adı <span className="text-red-500">*</span>
                </label>
                <input
                  name="name"
                  type="text"
                  required
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500"
                  placeholder="Ahmet Yılmaz"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Telefon
                </label>
                <input
                  name="phone"
                  type="tel"
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500"
                  placeholder="0532 123 45 67"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  E-posta
                </label>
                <input
                  name="email"
                  type="email"
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500"
                  placeholder="ornek@email.com"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">
                    Vergi No
                  </label>
                  <input
                    name="taxNumber"
                    type="text"
                    className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500"
                    placeholder="1234567890"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-slate-700 mb-1">
                    Vergi Dairesi
                  </label>
                  <input
                    name="taxOffice"
                    type="text"
                    className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500"
                    placeholder="Kadıköy"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  Adres
                </label>
                <textarea
                  name="address"
                  rows={2}
                  className="w-full px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-green-500 focus:border-green-500"
                  placeholder="Tam adres..."
                />
              </div>

              <div className="flex gap-3 pt-4">
                <button
                  type="button"
                  onClick={() => setShowCreateForm(false)}
                  className="flex-1 px-4 py-2 border border-slate-300 text-slate-700 rounded-lg hover:bg-slate-50"
                >
                  İptal
                </button>
                <button
                  type="submit"
                  disabled={isCreating}
                  className="flex-1 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 disabled:opacity-50 flex items-center justify-center gap-2"
                >
                  {isCreating ? (
                    <>
                      <div className="animate-spin w-4 h-4 border-2 border-white border-t-transparent rounded-full"></div>
                      Oluşturuluyor...
                    </>
                  ) : (
                    <>
                      <Plus className="w-4 h-4" />
                      Oluştur
                    </>
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
