import { Link, Outlet, useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { SettingsService } from '@/lib/settings';
import { ApiClient } from '@/lib/api-client';
import { useState, useEffect } from 'react';
import { ContextBar } from './ContextBar';
import { Toaster } from './ui/toaster';

export function MainLayout() {
  const navigate = useNavigate();
  const [apiBaseUrl, setApiBaseUrl] = useState('');

  useEffect(() => {
    SettingsService.getSettings().then((settings) => {
      setApiBaseUrl(settings.apiBaseUrl);
    });
  }, []);

  const handleLogout = async () => {
    await SettingsService.clearAuthToken();
    ApiClient.setToken(null);
    navigate('/login');
  };

  return (
    <div className="flex h-screen bg-gray-50">
      {/* Sidebar */}
      <div className="w-64 bg-white border-r flex flex-col">
        <div className="p-4 border-b">
          <h1 className="text-xl font-bold text-primary">ERP Cloud</h1>
          <p className="text-xs text-muted-foreground">Admin Console</p>
        </div>

        <nav className="flex-1 overflow-y-auto p-4">
          <div className="space-y-1">
            <NavLink to="/">Dashboard</NavLink>
            
            <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
              CATALOG
            </div>
            <NavLink to="/products">Products</NavLink>
            <NavLink to="/price-lists">Price Lists</NavLink>
            
            <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
              SALES & PURCHASES
            </div>
            <NavLink to="/parties">Parties</NavLink>
            <NavLink to="/sales/wizard">🎯 Sales Wizard</NavLink>
            <NavLink to="/purchase/wizard">🧾 Purchase Wizard</NavLink>
            <NavLink to="/sales-orders">Sales Orders</NavLink>
            <NavLink to="/shipments">Shipments</NavLink>
            <NavLink to="/invoices">Invoices</NavLink>
            <NavLink to="/purchase-orders">Purchase Orders</NavLink>
            <NavLink to="/goods-receipts">Goods Receipts</NavLink>
            
            <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
              STOCK
            </div>
            <NavLink to="/stock-balance">Stock Balance</NavLink>
            <NavLink to="/stock-ledger">Stock Ledger</NavLink>
            
            <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
              ACCOUNTING
            </div>
            <NavLink to="/party-ledger">Party Ledger</NavLink>
            <NavLink to="/payments">Payments</NavLink>
            <NavLink to="/cashboxes">Cashboxes</NavLink>
            <NavLink to="/bank-accounts">Bank Accounts</NavLink>
            <NavLink to="/cash-bank-ledger">Cash/Bank Ledger</NavLink>
            
            <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
              REPORTS
            </div>
            <NavLink to="/reports/stock">Stock Reports</NavLink>
            <NavLink to="/reports/sales">Sales & Purchase Reports</NavLink>
            <NavLink to="/reports/parties">Party Reports</NavLink>
            <NavLink to="/reports/cashbank">Cash & Bank Reports</NavLink>
            
            <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
              QA & TOOLS
            </div>
            <NavLink to="/qa/verification">🔍 QA Verification</NavLink>
          </div>
        </nav>

        <div className="p-4 border-t space-y-2">
          <div className="text-xs text-muted-foreground">
            <div className="font-semibold mb-1">API Server</div>
            <div className="truncate">{apiBaseUrl}</div>
          </div>
          
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              className="flex-1"
              onClick={() => navigate('/settings')}
            >
              Settings
            </Button>
            <Button
              variant="ghost"
              size="sm"
              className="flex-1"
              onClick={handleLogout}
            >
              Logout
            </Button>
          </div>
        </div>
      </div>

      {/* Main content */}
      <div className="flex-1 flex flex-col">
        <ContextBar />
        <div className="flex-1 overflow-y-auto">
          <Outlet />
        </div>
      </div>
      
      <Toaster />
    </div>
  );
}

function NavLink({ to, children }: { to: string; children: React.ReactNode }) {
  return (
    <Link
      to={to}
      className="block px-3 py-2 rounded-md text-sm hover:bg-gray-100 transition-colors"
    >
      {children}
    </Link>
  );
}
