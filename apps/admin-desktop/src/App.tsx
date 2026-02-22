import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MainLayout } from './components/MainLayout';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';
import { SettingsPage } from './pages/SettingsPage';
import { PartiesPage } from './pages/PartiesPage';
import { ProductsPage } from './pages/ProductsPage';
import ProductDetailPage from './pages/ProductDetailPage';
import { StockReportsPage } from './pages/reports/StockReportsPage';
import { SalesReportsPage } from './pages/reports/SalesReportsPage';
import { PartiesReportsPage } from './pages/reports/PartiesReportsPage';
import { CashBankReportsPage } from './pages/reports/CashBankReportsPage';
import { SalesWizardPage } from './pages/sales/SalesWizardPage';
import { PurchaseWizardPage } from './pages/purchase/PurchaseWizardPage';
import { SalesOrdersListPage } from './pages/sales/SalesOrdersListPage';
import { SalesOrderDetailPage } from './pages/sales/SalesOrderDetailPage';
import { ShipmentsListPage } from './pages/sales/ShipmentsListPage';
import { ShipmentDetailPage } from './pages/sales/ShipmentDetailPage';
import { InvoicesListPage } from './pages/sales/InvoicesListPage';
import { InvoiceDetailPage } from './pages/sales/InvoiceDetailPage';
import { PurchaseOrdersListPage } from './pages/purchase/PurchaseOrdersListPage';
import { PurchaseOrderDetailPage } from './pages/purchase/PurchaseOrderDetailPage';
import { GoodsReceiptsListPage } from './pages/purchase/GoodsReceiptsListPage';
import { GoodsReceiptDetailPage } from './pages/purchase/GoodsReceiptDetailPage';
import { PaymentsListPage } from './pages/finance/PaymentsListPage';
import { StockLedgerPage } from './pages/reports/StockLedgerPage';
import { PartyLedgerPage } from './pages/reports/PartyLedgerPage';
import { CashBankLedgerPage } from './pages/reports/CashBankLedgerPage';
import FastSearchPage from './pages/FastSearchPage';
import StockCardDetailPage from './pages/StockCardDetailPage';
import StockCardsListPage from './pages/StockCardsListPage';
import { QAVerificationPage } from './pages/QAVerificationPage';
import OrganizationSetupPage from './pages/OrganizationSetupPage';
import TezgahDashboardPage from './pages/TezgahDashboardPage';
import FastSalesPage from './pages/FastSalesPage';
import TahsilatPage from './pages/TahsilatPage';
import StokSorguPage from './pages/StokSorguPage';
import { BrandListPage } from './pages/admin/BrandListPage';
import { useEffect, useState } from 'react';
import { ApiClient } from './lib/api-client';
import { SettingsService } from './lib/settings';
import { useAuthStore } from './lib/auth-store';
import { RequirePerm } from './components/RequirePerm';

// Placeholder pages
const PlaceholderPage = ({ title }: { title: string }) => (
  <div className="p-6">
    <h1 className="text-3xl font-bold mb-4">{title}</h1>
    <p className="text-muted-foreground">This page is under construction.</p>
  </div>
);

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

function DefaultRedirect() {
  const { hasPerm } = useAuthStore();
  // Admin users go to dashboard, others go to tezgah
  if (hasPerm('ADMIN.SETTINGS')) {
    return <Navigate to="/dashboard" replace />;
  }
  return <Navigate to="/tezgah" replace />;
}

function App() {
  const [isInitialized, setIsInitialized] = useState(false);
  const hydrated = useAuthStore((state) => state.hydrated);
  const token = useAuthStore((state) => state.token);
  const setAuth = useAuthStore((state) => state.setAuth);

  useEffect(() => {
    async function init() {
      await ApiClient.initialize();
      const settings = await SettingsService.getSettings();
      const storedToken = localStorage.getItem('accessToken') || settings.authToken;
      
      if (storedToken) {
        setAuth(storedToken);
      } else {
        // No token, mark as hydrated anyway
        useAuthStore.setState({ hydrated: true });
      }
      
      setIsInitialized(true);
    }
    init();
  }, [setAuth]);

  const handleLoginSuccess = () => {
    // No-op, navigation handled by LoginPage
  };

  if (!isInitialized || !hydrated) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-gray-600">Yükleniyor...</div>
      </div>
    );
  }

  const hasToken = !!token;

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter
        future={{
          v7_startTransition: true,
          v7_relativeSplatPath: true,
        }}
      >
        <Routes>
          <Route path="/login" element={<LoginPage onLoginSuccess={handleLoginSuccess} />} />
          
          <Route
            path="/"
            element={hasToken ? <MainLayout /> : <Navigate to="/login" replace />}
          >
            <Route index element={<DefaultRedirect />} />
            <Route path="dashboard" element={<DashboardPage />} />
            
            {/* Tezgah Mode - Operation UI */}
            <Route path="tezgah" element={<RequirePerm perm="POS.VIEW" fallback="/dashboard"><TezgahDashboardPage /></RequirePerm>} />
            <Route path="tezgah/satis" element={<RequirePerm perm="POS.SELL" fallback="/tezgah"><FastSalesPage /></RequirePerm>} />
            <Route path="tezgah/fast-search" element={<RequirePerm perm="POS.VIEW" fallback="/tezgah"><FastSearchPage /></RequirePerm>} />
            <Route path="tezgah/tahsilat" element={<RequirePerm perm="FINANCE.COLLECT" fallback="/tezgah"><TahsilatPage /></RequirePerm>} />
            <Route path="tezgah/stok-sorgu" element={<RequirePerm perm="STOCK.VIEW" fallback="/tezgah"><StokSorguPage /></RequirePerm>} />
            <Route path="tezgah/raporlar" element={<RequirePerm perm="POS.VIEW" fallback="/tezgah"><PlaceholderPage title="Raporlar" /></RequirePerm>} />
            
            <Route path="settings" element={<SettingsPage />} />
            <Route path="setup/organization" element={<RequirePerm perm="ADMIN.SETTINGS" fallback="/tezgah"><OrganizationSetupPage /></RequirePerm>} />
            
            {/* Admin - Only for Admin permission */}
            <Route path="admin/brands" element={<RequirePerm perm="ADMIN.SETTINGS" fallback="/tezgah"><BrandListPage /></RequirePerm>} />
            
            {/* Catalog */}
            <Route path="products" element={<ProductsPage />} />
            <Route path="products/:id" element={<ProductDetailPage />} />
            <Route path="price-lists" element={<PlaceholderPage title="Price Lists" />} />
            <Route path="parts/search" element={<FastSearchPage />} />
            <Route path="fast-search" element={<FastSearchPage />} />
            <Route path="stock-cards" element={<StockCardsListPage />} />
            <Route path="stock-cards/:id" element={<StockCardDetailPage />} />
            
            {/* Sales & Purchase */}
            <Route path="parties" element={<PartiesPage />} />
            <Route path="sales/wizard" element={<SalesWizardPage />} />
            <Route path="purchase/wizard" element={<PurchaseWizardPage />} />
            <Route path="sales-orders" element={<SalesOrdersListPage />} />
            <Route path="sales-orders/:id" element={<SalesOrderDetailPage />} />
            <Route path="shipments" element={<ShipmentsListPage />} />
            <Route path="shipments/:id" element={<ShipmentDetailPage />} />
            <Route path="invoices" element={<InvoicesListPage />} />
            <Route path="invoices/:id" element={<InvoiceDetailPage />} />
            <Route path="purchase-orders" element={<PurchaseOrdersListPage />} />
            <Route path="purchase-orders/:id" element={<PurchaseOrderDetailPage />} />
            <Route path="goods-receipts" element={<GoodsReceiptsListPage />} />
            <Route path="goods-receipts/:id" element={<GoodsReceiptDetailPage />} />
            
            {/* Stock */}
            <Route path="stock-balance" element={<PlaceholderPage title="Stock Balance" />} />
            <Route path="stock-ledger" element={<StockLedgerPage />} />
            
            {/* Accounting */}
            <Route path="party-ledger" element={<PartyLedgerPage />} />
            <Route path="payments" element={<PaymentsListPage />} />
            <Route path="cashboxes" element={<PlaceholderPage title="Cashboxes" />} />
            <Route path="bank-accounts" element={<PlaceholderPage title="Bank Accounts" />} />
            <Route path="cash-bank-ledger" element={<CashBankLedgerPage />} />
            
            {/* QA & Tools */}
            <Route path="qa/verification" element={<QAVerificationPage />} />
            
            {/* Reports */}
            <Route path="reports/stock" element={<StockReportsPage />} />
            <Route path="reports/sales" element={<SalesReportsPage />} />
            <Route path="reports/parties" element={<PartiesReportsPage />} />
            <Route path="reports/cashbank" element={<CashBankReportsPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  );
}

export default App;
