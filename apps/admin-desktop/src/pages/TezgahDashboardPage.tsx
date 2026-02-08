import { Link } from "react-router-dom";
import { Search, CreditCard, Package, TrendingUp, Wallet, AlertTriangle, FileText, Users, Receipt, BarChart3, Clock } from "lucide-react";
import { useSalesSummary, useCashBankBalances, usePartyBalances } from "../hooks/useReports";
import { useInvoices } from "../hooks/useSales";

interface DailySummary {
  todaySalesCount: number;
  todaySalesAmount: number;
  cashSalesAmount: number;
  creditSalesAmount: number;
  cashBalance: number;
  openInvoicesCount: number;
}

export default function TezgahDashboardPage() {
  // Get today's date range
  const today = new Date().toISOString().split('T')[0];
  
  // Fetch today's sales summary
  const { data: salesData } = useSalesSummary(today, today, 'DAY');
  
  // Fetch cash/bank balances
  const { data: cashBankData } = useCashBankBalances();
  
  // Fetch open invoices (ISSUED status with openAmount > 0)
  const { data: invoicesData } = useInvoices(1, 1000);
  
  // Fetch party balances for customer debts
  const { data: partyBalancesData } = usePartyBalances(undefined, 'CUSTOMER', 1, 1000);

  // Calculate summary
  const todayData = salesData && salesData.length > 0 ? salesData[0] : null;
  const cashBalance = cashBankData?.filter(b => b.sourceType === 'CASHBOX').reduce((sum, b) => sum + b.balance, 0) || 0;
  const bankBalance = cashBankData?.filter(b => b.sourceType === 'BANK_ACCOUNT').reduce((sum, b) => sum + b.balance, 0) || 0;
  const openInvoices = invoicesData?.items?.filter(i => i.status === 'ISSUED' && (i as any).openAmount > 0) || [];
  const customersWithDebt = partyBalancesData?.items?.filter(p => p.balance > 0) || [];

  const summary: DailySummary = {
    todaySalesCount: todayData?.invoiceCount || 0,
    todaySalesAmount: todayData?.totalGross || 0,
    cashSalesAmount: todayData?.totalGross ? todayData.totalGross * 0.6 : 0, // Estimate - would need payment data
    creditSalesAmount: todayData?.totalGross ? todayData.totalGross * 0.4 : 0, // Estimate
    cashBalance: cashBalance + bankBalance,
    openInvoicesCount: openInvoices.length,
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 p-8">
      {/* Header */}
      <div className="mb-8">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-4xl font-bold text-slate-800 mb-2">
              🏪 Tezgâh Modu
            </h1>
            <p className="text-slate-600">Hızlı satış ve işlem yönetimi</p>
          </div>
          <div className="flex items-center gap-2 text-sm text-slate-500">
            <Clock className="w-4 h-4" />
            <span>{new Date().toLocaleDateString('tr-TR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}</span>
          </div>
        </div>
      </div>

      {/* Daily Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        {/* Today's Sales */}
        <div className="bg-white rounded-xl shadow-lg p-6 border-l-4 border-green-500">
          <div className="flex items-center justify-between mb-2">
            <div className="text-slate-600 text-sm font-medium">Bugünkü Satış</div>
            <TrendingUp className="w-5 h-5 text-green-500" />
          </div>
          <div className="text-3xl font-bold text-slate-800">
            ₺{summary.todaySalesAmount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
          </div>
          <div className="mt-2 text-xs text-slate-500">
            {summary.todaySalesCount} adet fatura
          </div>
        </div>

        {/* Cash Balance */}
        <div className="bg-white rounded-xl shadow-lg p-6 border-l-4 border-blue-500">
          <div className="flex items-center justify-between mb-2">
            <div className="text-slate-600 text-sm font-medium">Kasa/Banka Bakiye</div>
            <Wallet className="w-5 h-5 text-blue-500" />
          </div>
          <div className="text-3xl font-bold text-slate-800">
            ₺{summary.cashBalance.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
          </div>
          <div className="mt-2 text-xs text-slate-500">
            Nakit: ₺{cashBalance.toLocaleString('tr-TR', { minimumFractionDigits: 2 })} | Banka: ₺{bankBalance.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
          </div>
        </div>

        {/* Open Invoices */}
        <div className="bg-white rounded-xl shadow-lg p-6 border-l-4 border-orange-500">
          <div className="flex items-center justify-between mb-2">
            <div className="text-slate-600 text-sm font-medium">Açık Faturalar</div>
            <AlertTriangle className="w-5 h-5 text-orange-500" />
          </div>
          <div className="text-3xl font-bold text-slate-800">
            {summary.openInvoicesCount}
          </div>
          <div className="mt-2 text-xs text-slate-500">
            Tahsil edilmemiş
          </div>
        </div>

        {/* Customer Debts */}
        <div className="bg-white rounded-xl shadow-lg p-6 border-l-4 border-purple-500">
          <div className="flex items-center justify-between mb-2">
            <div className="text-slate-600 text-sm font-medium">Borçlu Müşteriler</div>
            <Users className="w-5 h-5 text-purple-500" />
          </div>
          <div className="text-3xl font-bold text-slate-800">
            {customersWithDebt.length}
          </div>
          <div className="mt-2 text-xs text-slate-500">
            Toplam: ₺{customersWithDebt.reduce((sum, p) => sum + p.balance, 0).toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
          </div>
        </div>
      </div>

      {/* Main Action Buttons */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        {/* Fast Cash Sale */}
        <Link
          to="/tezgah/satis?mode=cash"
          className="group bg-gradient-to-br from-green-500 to-green-600 rounded-2xl shadow-xl p-8 text-white hover:shadow-2xl hover:scale-105 transition-all duration-200"
        >
          <div className="flex flex-col items-center text-center">
            <div className="bg-white/20 rounded-full p-6 mb-4 group-hover:bg-white/30 transition-colors">
              <CreditCard className="w-12 h-12" />
            </div>
            <h2 className="text-2xl font-bold mb-2">Peşin Satış</h2>
            <p className="text-green-100 text-sm">
              Nakit / Kart / Banka
            </p>
            <div className="mt-4">
              <kbd className="bg-white/20 px-3 py-1 rounded text-xs">F1</kbd>
            </div>
          </div>
        </Link>

        {/* Fast Credit Sale */}
        <Link
          to="/tezgah/satis?mode=credit"
          className="group bg-gradient-to-br from-purple-500 to-purple-600 rounded-2xl shadow-xl p-8 text-white hover:shadow-2xl hover:scale-105 transition-all duration-200"
        >
          <div className="flex flex-col items-center text-center">
            <div className="bg-white/20 rounded-full p-6 mb-4 group-hover:bg-white/30 transition-colors">
              <FileText className="w-12 h-12" />
            </div>
            <h2 className="text-2xl font-bold mb-2">Veresiye Satış</h2>
            <p className="text-purple-100 text-sm">
              Cariye irsaliye
            </p>
            <div className="mt-4">
              <kbd className="bg-white/20 px-3 py-1 rounded text-xs">F2</kbd>
            </div>
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
            <div className="mt-4">
              <kbd className="bg-white/20 px-3 py-1 rounded text-xs">F3</kbd>
            </div>
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
            <div className="mt-4">
              <kbd className="bg-white/20 px-3 py-1 rounded text-xs">F4</kbd>
            </div>
          </div>
        </Link>
      </div>

      {/* Quick Reports Section */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
        {/* Today's Sales Details */}
        <div className="bg-white rounded-xl shadow-lg p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-slate-800 flex items-center gap-2">
              <Receipt className="w-5 h-5 text-green-600" />
              Bugünkü Satış Detayları
            </h3>
            <Link 
              to={`/reports/sales?from=${today}&to=${today}`}
              className="text-sm text-blue-600 hover:text-blue-800 hover:underline"
            >
              Detaylı Rapor →
            </Link>
          </div>
          {todayData ? (
            <div className="space-y-3">
              <div className="flex justify-between items-center p-3 bg-slate-50 rounded-lg">
                <span className="text-slate-600">Fatura Adedi</span>
                <span className="font-bold text-slate-800">{todayData.invoiceCount}</span>
              </div>
              <div className="flex justify-between items-center p-3 bg-slate-50 rounded-lg">
                <span className="text-slate-600">Net Tutar</span>
                <span className="font-bold text-slate-800">₺{todayData.totalNet.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</span>
              </div>
              <div className="flex justify-between items-center p-3 bg-slate-50 rounded-lg">
                <span className="text-slate-600">KDV</span>
                <span className="font-bold text-slate-800">₺{todayData.totalVat.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</span>
              </div>
              <div className="flex justify-between items-center p-3 bg-green-50 rounded-lg border border-green-200">
                <span className="text-green-700 font-semibold">Toplam (KDV Dahil)</span>
                <span className="font-bold text-green-800 text-xl">₺{todayData.totalGross.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</span>
              </div>
            </div>
          ) : (
            <div className="text-center py-8 text-slate-400">
              <Receipt className="w-12 h-12 mx-auto mb-3 opacity-50" />
              <p>Bugün henüz satış yapılmadı</p>
              <Link 
                to="/tezgah/satis"
                className="inline-block mt-4 px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors"
              >
                Yeni Satış Başlat
              </Link>
            </div>
          )}
        </div>

        {/* Quick Reports */}
        <div className="bg-white rounded-xl shadow-lg p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-slate-800 flex items-center gap-2">
              <BarChart3 className="w-5 h-5 text-blue-600" />
              Hızlı Raporlar
            </h3>
          </div>
          <div className="space-y-3">
            <Link 
              to={`/reports/sales?from=${today}&to=${today}`}
              className="flex items-center justify-between p-3 bg-slate-50 rounded-lg hover:bg-blue-50 hover:border-blue-200 border border-transparent transition-all group"
            >
              <span className="text-slate-700 group-hover:text-blue-700">📊 Bugün Satışlar</span>
              <span className="text-green-600 font-semibold">{summary.todaySalesCount} fatura</span>
            </Link>
            <Link 
              to="/invoices?status=ISSUED"
              className="flex items-center justify-between p-3 bg-slate-50 rounded-lg hover:bg-orange-50 hover:border-orange-200 border border-transparent transition-all group"
            >
              <span className="text-slate-700 group-hover:text-orange-700">📄 Açık Faturalar</span>
              <span className="text-orange-600 font-semibold">{summary.openInvoicesCount} adet</span>
            </Link>
            <Link 
              to="/reports/parties/balances?type=CUSTOMER"
              className="flex items-center justify-between p-3 bg-slate-50 rounded-lg hover:bg-purple-50 hover:border-purple-200 border border-transparent transition-all group"
            >
              <span className="text-slate-700 group-hover:text-purple-700">👥 Borçlu Müşteriler</span>
              <span className="text-purple-600 font-semibold">{customersWithDebt.length} müşteri</span>
            </Link>
            <Link 
              to="/reports/cashbank/balances"
              className="flex items-center justify-between p-3 bg-slate-50 rounded-lg hover:bg-blue-50 hover:border-blue-200 border border-transparent transition-all group"
            >
              <span className="text-slate-700 group-hover:text-blue-700">💰 Kasa/Banka Durumu</span>
              <span className="text-blue-600 font-semibold">₺{summary.cashBalance.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</span>
            </Link>
          </div>
        </div>
      </div>

      {/* Quick Links */}
      <div className="bg-white rounded-xl shadow-lg p-6">
        <h3 className="text-lg font-semibold text-slate-800 mb-4">Hızlı Erişim</h3>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <Link to="/parties" className="text-blue-600 hover:text-blue-800 hover:underline flex items-center gap-2">
            <Users className="w-4 h-4" />
            Cariler
          </Link>
          <Link to="/products" className="text-blue-600 hover:text-blue-800 hover:underline flex items-center gap-2">
            <Package className="w-4 h-4" />
            Ürünler
          </Link>
          <Link to="/reports/sales" className="text-blue-600 hover:text-blue-800 hover:underline flex items-center gap-2">
            <BarChart3 className="w-4 h-4" />
            Raporlar
          </Link>
          <Link to="/settings" className="text-blue-600 hover:text-blue-800 hover:underline flex items-center gap-2">
            <Package className="w-4 h-4" />
            Ayarlar
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
