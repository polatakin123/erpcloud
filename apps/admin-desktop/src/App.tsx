import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MainLayout } from './components/MainLayout';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';
import { SettingsPage } from './pages/SettingsPage';
import { PartiesPage } from './pages/PartiesPage';
import { ProductsPage } from './pages/ProductsPage';
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
import { QAVerificationPage } from './pages/QAVerificationPage';
import { useEffect, useState } from 'react';
import { ApiClient } from './lib/api-client';
import { SettingsService } from './lib/settings';

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

function App() {
  const [isInitialized, setIsInitialized] = useState(false);
  const [hasToken, setHasToken] = useState(false);

  useEffect(() => {
    async function init() {
      await ApiClient.initialize();
      const settings = await SettingsService.getSettings();
      setHasToken(!!settings.authToken);
      setIsInitialized(true);
    }
    init();
  }, []);

  const handleLoginSuccess = () => {
    setHasToken(true);
  };

  if (!isInitialized) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div>Loading...</div>
      </div>
    );
  }

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage onLoginSuccess={handleLoginSuccess} />} />
          
          <Route
            path="/"
            element={hasToken ? <MainLayout /> : <Navigate to="/login" replace />}
          >
            <Route index element={<DashboardPage />} />
            <Route path="settings" element={<SettingsPage />} />
            
            {/* Catalog */}
            <Route path="products" element={<ProductsPage />} />
            <Route path="price-lists" element={<PlaceholderPage title="Price Lists" />} />
            
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
