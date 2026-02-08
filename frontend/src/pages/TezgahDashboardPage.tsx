import { Link } from "react-router-dom";
import { Zap, Search, CreditCard, Package, TrendingUp, Wallet, AlertTriangle, FileText } from "lucide-react";
import { useState, useEffect } from "react";

interface DailySummary {
  todaySales: number;
  todayPayments: number;
  criticalStock: number;
  openInvoices: number;
}

export default function TezgahDashboardPage() {
  const [summary, setSummary] = useState<DailySummary>({
    todaySales: 0,
    todayPayments: 0,
    criticalStock: 0,
    openInvoices: 0,
  });

  useEffect(() => {
    // TODO: Fetch real data from API
    // For now, mock data
    setSummary({
      todaySales: 15420.50,
      todayPayments: 12300.00,
      criticalStock: 8,
      openInvoices: 12,
    });
  }, []);

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-4xl font-bold text-slate-800 mb-2">
          🏪 Tezgâh Modu
        </h1>
        <p className="text-slate-600">Hızlı satış ve işlem yönetimi</p>
      </div>

      {/* Daily Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
        <div className="bg-white rounded-xl shadow-lg p-6 border-l-4 border-green-500">
          <div className="flex items-center justify-between mb-2">
            <div className="text-slate-600 text-sm font-medium">Bugünkü Satış</div>
            <TrendingUp className="w-5 h-5 text-green-500" />
          </div>
          <div className="text-3xl font-bold text-slate-800">
            ₺{summary.todaySales.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
          </div>
        </div>

        <div className="bg-white rounded-xl shadow-lg p-6 border-l-4 border-blue-500">
          <div className="flex items-center justify-between mb-2">
            <div className="text-slate-600 text-sm font-medium">Bugünkü Tahsilat</div>
            <Wallet className="w-5 h-5 text-blue-500" />
          </div>
          <div className="text-3xl font-bold text-slate-800">
            ₺{summary.todayPayments.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
          </div>
        </div>

        <div className="bg-white rounded-xl shadow-lg p-6 border-l-4 border-orange-500">
          <div className="flex items-center justify-between mb-2">
            <div className="text-slate-600 text-sm font-medium">Kritik Stoklar</div>
            <AlertTriangle className="w-5 h-5 text-orange-500" />
          </div>
          <div className="text-3xl font-bold text-slate-800">
            {summary.criticalStock}
          </div>
        </div>

        <div className="bg-white rounded-xl shadow-lg p-6 border-l-4 border-purple-500">
          <div className="flex items-center justify-between mb-2">
            <div className="text-slate-600 text-sm font-medium">Açık İrsaliyeler</div>
            <FileText className="w-5 h-5 text-purple-500" />
          </div>
          <div className="text-3xl font-bold text-slate-800">
            {summary.openInvoices}
          </div>
        </div>
      </div>

      {/* Main Action Buttons */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {/* Fast Sales */}
        <Link
          to="/tezgah/satis"
          className="group bg-gradient-to-br from-green-500 to-green-600 rounded-2xl shadow-xl p-8 text-white hover:shadow-2xl hover:scale-105 transition-all duration-200"
        >
          <div className="flex flex-col items-center text-center">
            <div className="bg-white/20 rounded-full p-6 mb-4 group-hover:bg-white/30 transition-colors">
              <Zap className="w-12 h-12" />
            </div>
            <h2 className="text-2xl font-bold mb-2">Hızlı Satış</h2>
            <p className="text-green-100 text-sm">
              Peşin / Veresiye Satış
            </p>
          </div>
        </Link>

        {/* OEM / Equivalent Search */}
        <Link
          to="/fast-search"
          className="group bg-gradient-to-br from-blue-500 to-blue-600 rounded-2xl shadow-xl p-8 text-white hover:shadow-2xl hover:scale-105 transition-all duration-200"
        >
          <div className="flex flex-col items-center text-center">
            <div className="bg-white/20 rounded-full p-6 mb-4 group-hover:bg-white/30 transition-colors">
              <Search className="w-12 h-12" />
            </div>
            <h2 className="text-2xl font-bold mb-2">OEM / Muadil Arama</h2>
            <p className="text-blue-100 text-sm">
              Hızlı stok sorgulama
            </p>
          </div>
        </Link>

        {/* Payment Collection */}
        <Link
          to="/tezgah/tahsilat"
          className="group bg-gradient-to-br from-purple-500 to-purple-600 rounded-2xl shadow-xl p-8 text-white hover:shadow-2xl hover:scale-105 transition-all duration-200"
        >
          <div className="flex flex-col items-center text-center">
            <div className="bg-white/20 rounded-full p-6 mb-4 group-hover:bg-white/30 transition-colors">
              <CreditCard className="w-12 h-12" />
            </div>
            <h2 className="text-2xl font-bold mb-2">Tahsilat</h2>
            <p className="text-purple-100 text-sm">
              Nakit / Banka / Kart
            </p>
          </div>
        </Link>

        {/* Stock Inquiry */}
        <Link
          to="/tezgah/stok-sorgu"
          className="group bg-gradient-to-br from-orange-500 to-orange-600 rounded-2xl shadow-xl p-8 text-white hover:shadow-2xl hover:scale-105 transition-all duration-200"
        >
          <div className="flex flex-col items-center text-center">
            <div className="bg-white/20 rounded-full p-6 mb-4 group-hover:bg-white/30 transition-colors">
              <Package className="w-12 h-12" />
            </div>
            <h2 className="text-2xl font-bold mb-2">Stok Sorgu</h2>
            <p className="text-orange-100 text-sm">
              Depo stok kontrol
            </p>
          </div>
        </Link>
      </div>

      {/* Quick Links */}
      <div className="mt-8 bg-white rounded-xl shadow-lg p-6">
        <h3 className="text-lg font-semibold text-slate-800 mb-4">Hızlı Erişim</h3>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <Link to="/parties" className="text-blue-600 hover:text-blue-800 hover:underline">
            → Cariler
          </Link>
          <Link to="/products" className="text-blue-600 hover:text-blue-800 hover:underline">
            → Ürünler
          </Link>
          <Link to="/tezgah/raporlar" className="text-blue-600 hover:text-blue-800 hover:underline">
            → Raporlar
          </Link>
          <Link to="/settings" className="text-blue-600 hover:text-blue-800 hover:underline">
            → Ayarlar
          </Link>
        </div>
      </div>

      {/* Keyboard Shortcuts Info */}
      <div className="mt-6 bg-slate-800 rounded-xl shadow-lg p-6 text-white">
        <h3 className="text-sm font-semibold mb-3">⌨️ Klavye Kısayolları</h3>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
          <div>
            <kbd className="bg-slate-700 px-2 py-1 rounded">F1</kbd> Hızlı Satış
          </div>
          <div>
            <kbd className="bg-slate-700 px-2 py-1 rounded">F2</kbd> OEM Arama
          </div>
          <div>
            <kbd className="bg-slate-700 px-2 py-1 rounded">F3</kbd> Tahsilat
          </div>
          <div>
            <kbd className="bg-slate-700 px-2 py-1 rounded">F4</kbd> Stok Sorgu
          </div>
        </div>
      </div>
    </div>
  );
}
