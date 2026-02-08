import { useState, useEffect, useRef } from "react";
import { Search, Wallet, CreditCard, Banknote, Check, X, AlertCircle } from "lucide-react";

interface Party {
  id: string;
  code: string;
  name: string;
  balance: number;
  openInvoices: number;
}

interface OpenInvoice {
  id: string;
  invoiceNumber: string;
  date: string;
  amount: number;
  paid: number;
  remaining: number;
}

type PaymentMethod = "cash" | "card" | "bank";

export default function TahsilatPage() {
  const [selectedParty, setSelectedParty] = useState<Party | null>(null);
  const [partySearch, setPartySearch] = useState("");
  const [partyResults, setPartyResults] = useState<Party[]>([]);
  const [showPartyDropdown, setShowPartyDropdown] = useState(false);
  
  const [openInvoices, setOpenInvoices] = useState<OpenInvoice[]>([]);
  const [selectedInvoices, setSelectedInvoices] = useState<Set<string>>(new Set());
  
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>("cash");
  const [paymentAmount, setPaymentAmount] = useState("");
  const [paymentNote, setPaymentNote] = useState("");
  
  const searchInputRef = useRef<HTMLInputElement>(null);
  const amountInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    // Focus search on mount
    searchInputRef.current?.focus();

    // Keyboard shortcuts
    const handleKeyPress = (e: KeyboardEvent) => {
      if (e.key === "F1") {
        e.preventDefault();
        searchInputRef.current?.focus();
      } else if (e.key === "F9") {
        e.preventDefault();
        handlePayment();
      } else if (e.key === "Escape") {
        e.preventDefault();
        handleCancel();
      }
    };

    window.addEventListener("keydown", handleKeyPress);
    return () => window.removeEventListener("keydown", handleKeyPress);
  }, [selectedParty, paymentAmount, selectedInvoices]);

  useEffect(() => {
    if (partySearch.length >= 2) {
      searchParties(partySearch);
    } else {
      setPartyResults([]);
      setShowPartyDropdown(false);
    }
  }, [partySearch]);

  useEffect(() => {
    if (selectedParty) {
      fetchOpenInvoices(selectedParty.id);
    }
  }, [selectedParty]);

  const searchParties = async (query: string) => {
    try {
      const token = localStorage.getItem("token");
      const response = await fetch(
        `http://localhost:5039/api/parties?search=${encodeURIComponent(query)}`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );
      const data = await response.json();
      setPartyResults(data.items || []);
      setShowPartyDropdown(true);
    } catch (error) {
      console.error("Party search error:", error);
      setPartyResults([]);
    }
  };

  const fetchOpenInvoices = async (partyId: string) => {
    try {
      const token = localStorage.getItem("token");
      const response = await fetch(
        `http://localhost:5039/api/parties/${partyId}/open-invoices`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );
      const data = await response.json();
      setOpenInvoices(data || []);
    } catch (error) {
      console.error("Failed to fetch invoices:", error);
      setOpenInvoices([]);
    }
  };

  const selectParty = (party: Party) => {
    setSelectedParty(party);
    setPartySearch(party.name);
    setShowPartyDropdown(false);
    setSelectedInvoices(new Set());
    
    // Focus amount input after party selection
    setTimeout(() => amountInputRef.current?.focus(), 100);
  };

  const toggleInvoiceSelection = (invoiceId: string) => {
    const newSelection = new Set(selectedInvoices);
    if (newSelection.has(invoiceId)) {
      newSelection.delete(invoiceId);
    } else {
      newSelection.add(invoiceId);
    }
    setSelectedInvoices(newSelection);
  };

  const handlePayment = async () => {
    if (!selectedParty) {
      alert("Lütfen cari seçin");
      return;
    }

    const amount = parseFloat(paymentAmount);
    if (!amount || amount <= 0) {
      alert("Geçerli bir tutar girin");
      return;
    }

    try {
      const token = localStorage.getItem("token");
      
      const paymentData = {
        partyId: selectedParty.id,
        amount: amount,
        paymentMethod: paymentMethod,
        note: paymentNote,
        invoiceIds: Array.from(selectedInvoices),
        date: new Date().toISOString(),
      };

      const response = await fetch("http://localhost:5039/api/payments", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(paymentData),
      });

      if (response.ok) {
        alert(`✅ Tahsilat başarıyla kaydedildi!\n\nCari: ${selectedParty.name}\nTutar: ₺${amount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}\nYöntem: ${getPaymentMethodLabel(paymentMethod)}`);
        handleCancel();
      } else {
        const error = await response.text();
        alert(`❌ Hata: ${error}`);
      }
    } catch (error) {
      console.error("Payment error:", error);
      alert("❌ Tahsilat kaydedilemedi");
    }
  };

  const handleCancel = () => {
    setSelectedParty(null);
    setPartySearch("");
    setPartyResults([]);
    setOpenInvoices([]);
    setSelectedInvoices(new Set());
    setPaymentAmount("");
    setPaymentNote("");
    setPaymentMethod("cash");
    searchInputRef.current?.focus();
  };

  const getPaymentMethodLabel = (method: PaymentMethod) => {
    switch (method) {
      case "cash": return "Nakit";
      case "card": return "Kredi Kartı";
      case "bank": return "Banka Transferi";
    }
  };

  const selectedTotal = openInvoices
    .filter(inv => selectedInvoices.has(inv.id))
    .reduce((sum, inv) => sum + inv.remaining, 0);

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-slate-100 p-6">
      <div className="max-w-6xl mx-auto">
        {/* Header */}
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-slate-800 flex items-center gap-3">
            <Wallet className="w-8 h-8 text-blue-600" />
            Tahsilat İşlemi
          </h1>
          <p className="text-slate-600 mt-1">Cari hesaptan tahsilat yapın</p>
        </div>

        {/* Keyboard Shortcuts */}
        <div className="bg-white rounded-lg shadow-md p-4 mb-6 border-l-4 border-blue-500">
          <div className="flex gap-6 text-sm text-slate-600">
            <span><kbd className="px-2 py-1 bg-slate-100 rounded">F1</kbd> Cari Ara</span>
            <span><kbd className="px-2 py-1 bg-slate-100 rounded">F9</kbd> Tahsilat Kaydet</span>
            <span><kbd className="px-2 py-1 bg-slate-100 rounded">ESC</kbd> İptal</span>
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Left: Party Selection & Payment Details */}
          <div className="space-y-6">
            {/* Party Search */}
            <div className="bg-white rounded-lg shadow-lg p-6">
              <h2 className="text-lg font-semibold text-slate-800 mb-4 flex items-center gap-2">
                <Search className="w-5 h-5" />
                Cari Seçimi
              </h2>
              
              <div className="relative">
                <input
                  ref={searchInputRef}
                  type="text"
                  value={partySearch}
                  onChange={(e) => setPartySearch(e.target.value)}
                  onFocus={() => partySearch.length >= 2 && setShowPartyDropdown(true)}
                  placeholder="Cari kod veya isim ara..."
                  className="w-full px-4 py-3 border-2 border-slate-300 rounded-lg focus:border-blue-500 focus:outline-none text-lg"
                />
                
                {showPartyDropdown && partyResults.length > 0 && (
                  <div className="absolute z-10 w-full mt-2 bg-white border-2 border-slate-200 rounded-lg shadow-xl max-h-96 overflow-y-auto">
                    {partyResults.map((party) => (
                      <button
                        key={party.id}
                        onClick={() => selectParty(party)}
                        className="w-full px-4 py-3 text-left hover:bg-blue-50 border-b border-slate-100 last:border-b-0"
                      >
                        <div className="font-semibold text-slate-800">{party.code} - {party.name}</div>
                        <div className="text-sm text-slate-600">
                          Bakiye: <span className={party.balance > 0 ? "text-red-600 font-semibold" : "text-green-600"}>
                            ₺{Math.abs(party.balance).toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
                            {party.balance > 0 ? " (Alacak)" : " (Borç)"}
                          </span>
                        </div>
                      </button>
                    ))}
                  </div>
                )}
              </div>

              {/* Selected Party Info */}
              {selectedParty && (
                <div className="mt-4 p-4 bg-blue-50 rounded-lg border border-blue-200">
                  <div className="flex justify-between items-start">
                    <div>
                      <div className="font-semibold text-slate-800 text-lg">
                        {selectedParty.code} - {selectedParty.name}
                      </div>
                      <div className="mt-2 space-y-1">
                        <div className="text-sm text-slate-600">
                          Toplam Bakiye: <span className={selectedParty.balance > 0 ? "text-red-600 font-bold" : "text-green-600 font-bold"}>
                            ₺{Math.abs(selectedParty.balance).toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
                            {selectedParty.balance > 0 ? " (Alacak)" : " (Borç)"}
                          </span>
                        </div>
                        <div className="text-sm text-slate-600">
                          Açık Fatura: {selectedParty.openInvoices || openInvoices.length} adet
                        </div>
                      </div>
                    </div>
                    <button
                      onClick={handleCancel}
                      className="text-slate-400 hover:text-red-600"
                    >
                      <X className="w-5 h-5" />
                    </button>
                  </div>
                </div>
              )}
            </div>

            {/* Payment Details */}
            {selectedParty && (
              <div className="bg-white rounded-lg shadow-lg p-6">
                <h2 className="text-lg font-semibold text-slate-800 mb-4">
                  Tahsilat Detayları
                </h2>

                {/* Payment Method */}
                <div className="mb-4">
                  <label className="block text-sm font-medium text-slate-700 mb-2">
                    Ödeme Yöntemi
                  </label>
                  <div className="grid grid-cols-3 gap-2">
                    <button
                      onClick={() => setPaymentMethod("cash")}
                      className={`flex items-center justify-center gap-2 px-4 py-3 rounded-lg border-2 transition-all ${
                        paymentMethod === "cash"
                          ? "border-green-500 bg-green-50 text-green-700 font-semibold"
                          : "border-slate-300 bg-white text-slate-700 hover:border-green-300"
                      }`}
                    >
                      <Banknote className="w-5 h-5" />
                      Nakit
                    </button>
                    <button
                      onClick={() => setPaymentMethod("card")}
                      className={`flex items-center justify-center gap-2 px-4 py-3 rounded-lg border-2 transition-all ${
                        paymentMethod === "card"
                          ? "border-blue-500 bg-blue-50 text-blue-700 font-semibold"
                          : "border-slate-300 bg-white text-slate-700 hover:border-blue-300"
                      }`}
                    >
                      <CreditCard className="w-5 h-5" />
                      Kart
                    </button>
                    <button
                      onClick={() => setPaymentMethod("bank")}
                      className={`flex items-center justify-center gap-2 px-4 py-3 rounded-lg border-2 transition-all ${
                        paymentMethod === "bank"
                          ? "border-purple-500 bg-purple-50 text-purple-700 font-semibold"
                          : "border-slate-300 bg-white text-slate-700 hover:border-purple-300"
                      }`}
                    >
                      <Wallet className="w-5 h-5" />
                      Banka
                    </button>
                  </div>
                </div>

                {/* Payment Amount */}
                <div className="mb-4">
                  <label className="block text-sm font-medium text-slate-700 mb-2">
                    Tahsilat Tutarı (₺)
                  </label>
                  <input
                    ref={amountInputRef}
                    type="number"
                    step="0.01"
                    value={paymentAmount}
                    onChange={(e) => setPaymentAmount(e.target.value)}
                    placeholder="0.00"
                    className="w-full px-4 py-3 border-2 border-slate-300 rounded-lg focus:border-blue-500 focus:outline-none text-2xl font-bold text-right"
                  />
                </div>

                {/* Payment Note */}
                <div className="mb-4">
                  <label className="block text-sm font-medium text-slate-700 mb-2">
                    Açıklama (Opsiyonel)
                  </label>
                  <textarea
                    value={paymentNote}
                    onChange={(e) => setPaymentNote(e.target.value)}
                    placeholder="Tahsilat notu..."
                    rows={3}
                    className="w-full px-4 py-2 border-2 border-slate-300 rounded-lg focus:border-blue-500 focus:outline-none resize-none"
                  />
                </div>

                {/* Action Buttons */}
                <div className="flex gap-3">
                  <button
                    onClick={handlePayment}
                    disabled={!paymentAmount || parseFloat(paymentAmount) <= 0}
                    className="flex-1 flex items-center justify-center gap-2 px-6 py-4 bg-gradient-to-r from-green-600 to-green-700 text-white rounded-lg font-semibold text-lg hover:from-green-700 hover:to-green-800 disabled:opacity-50 disabled:cursor-not-allowed transition-all shadow-lg hover:shadow-xl"
                  >
                    <Check className="w-6 h-6" />
                    Tahsilat Kaydet (F9)
                  </button>
                  <button
                    onClick={handleCancel}
                    className="px-6 py-4 bg-slate-200 text-slate-700 rounded-lg font-semibold hover:bg-slate-300 transition-all"
                  >
                    <X className="w-6 h-6" />
                  </button>
                </div>
              </div>
            )}
          </div>

          {/* Right: Open Invoices */}
          <div>
            {selectedParty && (
              <div className="bg-white rounded-lg shadow-lg p-6">
                <h2 className="text-lg font-semibold text-slate-800 mb-4 flex items-center justify-between">
                  <span>Açık Faturalar</span>
                  {selectedInvoices.size > 0 && (
                    <span className="text-sm font-normal text-blue-600">
                      {selectedInvoices.size} fatura seçildi (₺{selectedTotal.toLocaleString('tr-TR', { minimumFractionDigits: 2 })})
                    </span>
                  )}
                </h2>

                {openInvoices.length === 0 ? (
                  <div className="text-center py-12 text-slate-500">
                    <AlertCircle className="w-12 h-12 mx-auto mb-3 opacity-50" />
                    <p>Bu carinin açık faturası bulunmuyor</p>
                  </div>
                ) : (
                  <div className="space-y-2 max-h-[600px] overflow-y-auto">
                    {openInvoices.map((invoice) => (
                      <div
                        key={invoice.id}
                        onClick={() => toggleInvoiceSelection(invoice.id)}
                        className={`p-4 border-2 rounded-lg cursor-pointer transition-all ${
                          selectedInvoices.has(invoice.id)
                            ? "border-blue-500 bg-blue-50"
                            : "border-slate-200 bg-white hover:border-blue-300"
                        }`}
                      >
                        <div className="flex items-start justify-between">
                          <div className="flex-1">
                            <div className="font-semibold text-slate-800">
                              {invoice.invoiceNumber}
                            </div>
                            <div className="text-sm text-slate-600 mt-1">
                              {new Date(invoice.date).toLocaleDateString('tr-TR')}
                            </div>
                          </div>
                          <div className="text-right">
                            <div className="font-bold text-lg text-red-600">
                              ₺{invoice.remaining.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
                            </div>
                            <div className="text-xs text-slate-500">
                              Toplam: ₺{invoice.amount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
                            </div>
                            {invoice.paid > 0 && (
                              <div className="text-xs text-green-600">
                                Ödenen: ₺{invoice.paid.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
                              </div>
                            )}
                          </div>
                        </div>
                        {selectedInvoices.has(invoice.id) && (
                          <div className="mt-2 flex items-center gap-2 text-sm text-blue-600 font-semibold">
                            <Check className="w-4 h-4" />
                            Seçildi
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}

                {openInvoices.length > 0 && (
                  <div className="mt-4 p-4 bg-slate-50 rounded-lg border border-slate-200">
                    <div className="text-sm text-slate-600 mb-1">
                      Tahsilat tutarı girdiğinizde, sistem otomatik olarak en eski faturalardan başlayarak eşleştirme yapacaktır.
                    </div>
                    <div className="text-xs text-slate-500">
                      İsterseniz manuel olarak faturaları seçebilirsiniz.
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
