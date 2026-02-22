import { Link, Outlet, useNavigate, useLocation } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { SettingsService } from '@/lib/settings';
import { ApiClient } from '@/lib/api-client';
import { useAuthStore } from '@/lib/auth-store';
import { useState, useEffect } from 'react';
import { ContextBar } from './ContextBar';
import { Toaster } from './ui/toaster';
import { Store, Settings as SettingsIcon } from 'lucide-react';

export function MainLayout() {
  const navigate = useNavigate();
  const location = useLocation();
  const [apiBaseUrl, setApiBaseUrl] = useState('');
  const { isAdmin, hasPerm, clearAuth } = useAuthStore();
  
  // Detect if we're in Tezgâh mode
  const isTezgahMode = location.pathname.startsWith('/tezgah');

  useEffect(() => {
    SettingsService.getSettings().then((settings) => {
      setApiBaseUrl(settings.apiBaseUrl);
    });
  }, []);

  const handleLogout = async () => {
    await SettingsService.clearAuthToken();
    ApiClient.setToken(null);
    clearAuth();
    navigate('/login');
  };

  return (
    <div className="flex h-screen bg-gray-50">
      {/* Sidebar */}
      <div className="w-64 bg-white border-r flex flex-col">
        <div className="p-4 border-b">
          <h1 className="text-xl font-bold text-primary">ERP Cloud</h1>
          <p className="text-xs text-muted-foreground">
            {isTezgahMode ? '🏪 Tezgâh Modu' : '⚙️ Yönetim Modu'}
          </p>
        </div>

        {/* Mode Toggle */}
        <div className="p-4 border-b bg-slate-50">
          <div className="flex gap-2">
            {hasPerm('POS.VIEW') && (
              <button
                onClick={() => navigate('/tezgah')}
                className={`flex-1 px-3 py-2 rounded-lg text-sm font-medium transition-colors flex items-center justify-center gap-1 ${
                  isTezgahMode
                    ? 'bg-green-600 text-white shadow-md'
                    : 'bg-white border border-slate-300 text-slate-600 hover:bg-slate-100'
                }`}
              >
                <Store className="w-4 h-4" />
                Tezgâh
              </button>
            )}
            {hasPerm('ADMIN.SETTINGS') && (
              <button
                onClick={() => navigate('/dashboard')}
                className={`flex-1 px-3 py-2 rounded-lg text-sm font-medium transition-colors flex items-center justify-center gap-1 ${
                  !isTezgahMode
                    ? 'bg-blue-600 text-white shadow-md'
                    : 'bg-white border border-slate-300 text-slate-600 hover:bg-slate-100'
                }`}
              >
                <SettingsIcon className="w-4 h-4" />
                Yönetim
              </button>
            )}
          </div>
        </div>

        <nav className="flex-1 overflow-y-auto p-4">
          {isTezgahMode ? (
            // TEZGAH MODE MENU
            <div className="space-y-1">
              {hasPerm('POS.VIEW') && <NavLink to="/tezgah">🏠 Tezgâh Ana Sayfa</NavLink>}
              
              <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
                HIZLI İŞLEMLER
              </div>
              {hasPerm('POS.SELL') && (
                <NavLink to="/tezgah/satis" className="!bg-gradient-to-r !from-green-600 !to-green-700 !text-white font-semibold">
                  ⚡ Hızlı Satış
                </NavLink>
              )}
              {hasPerm('STOCK.VIEW') && (
                <NavLink to="/tezgah/fast-search" className="!bg-gradient-to-r !from-blue-600 !to-blue-700 !text-white font-semibold">
                  🔍 OEM / Muadil Arama
                </NavLink>
              )}
              {hasPerm('FINANCE.COLLECT') && <NavLink to="/tezgah/tahsilat">💳 Tahsilat</NavLink>}
              {hasPerm('STOCK.VIEW') && <NavLink to="/tezgah/stok-sorgu">📦 Stok Sorgu</NavLink>}
              
              {hasPerm('POS.VIEW') && (
                <>
                  <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
                    RAPORLAR
                  </div>
                  <NavLink to="/tezgah/raporlar">Günlük Raporlar</NavLink>
                </>
              )}
            </div>
          ) : (
            // ADMIN MODE MENU
            <div className="space-y-1">
              <NavLink to="/dashboard">Kontrol Paneli</NavLink>
              
              <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
                HIZLI ERİŞİM
              </div>
              {hasPerm('STOCK.VIEW') && (
                <NavLink to="/fast-search" className="!bg-gradient-to-r !from-blue-600 !to-blue-700 !text-white font-semibold">
                  ⚡ Stok Kartı Ara
                </NavLink>
              )}
              
              {hasPerm('ADMIN.SETTINGS') && (
                <>
                  <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
                    KURULUM
                  </div>
                  <NavLink to="/setup/organization">🏢 Organizasyon & Şubeler</NavLink>
                  
                  <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
                    YÖNETİM
                  </div>
                  <NavLink to="/admin/brands">Markalar</NavLink>
                </>
              )}
              
              {hasPerm('STOCK.VIEW') && (
                <>
                  <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
                    KATALOG
                  </div>
                  <NavLink to="/stock-cards">Stok Kartları</NavLink>
                  <NavLink to="/products">Ürünler (Yönetici)</NavLink>
                  <NavLink to="/price-lists">Fiyat Listeleri</NavLink>
                </>
              )}
              
              {hasPerm('POS.SELL') && (
                <>
                  <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
                    SATIŞ & SATIN ALMA
                  </div>
                  <NavLink to="/parties">Cariler</NavLink>
                  <NavLink to="/sales/wizard">🎯 Hızlı Satış</NavLink>
                  <NavLink to="/purchase/wizard">🧾 Hızlı Satın Alma</NavLink>
                  <NavLink to="/sales-orders">Satış Siparişleri</NavLink>
                  <NavLink to="/shipments">Sevkiyatlar</NavLink>
                  <NavLink to="/invoices">Faturalar</NavLink>
                  <NavLink to="/purchase-orders">Satın Alma Siparişleri</NavLink>
                  <NavLink to="/goods-receipts">Mal Kabul</NavLink>
                </>
              )}
              
              {hasPerm('STOCK.VIEW') && (
                <>
                  <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
                    STOK
                  </div>
                  <NavLink to="/stock-balance">Stok Bakiyeleri</NavLink>
                  <NavLink to="/stock-ledger">Stok Hareketleri</NavLink>
                </>
              )}
              
              {hasPerm('FINANCE.VIEW') && (
                <>
                  <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
                    MUHASEBE
                  </div>
                  <NavLink to="/party-ledger">Cari Hareketler</NavLink>
                  <NavLink to="/payments">Tahsilat & Ödemeler</NavLink>
                </>
              )}
              <NavLink to="/cashboxes">Kasalar</NavLink>
              <NavLink to="/bank-accounts">Banka Hesapları</NavLink>
              <NavLink to="/cash-bank-ledger">Kasa/Banka Hareketleri</NavLink>
              
              <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
                RAPORLAR
              </div>
              <NavLink to="/reports/stock">Stok Raporları</NavLink>
              <NavLink to="/reports/sales">Satış & Satın Alma Raporları</NavLink>
              <NavLink to="/reports/parties">Cari Raporları</NavLink>
              <NavLink to="/reports/cashbank">Kasa & Banka Raporları</NavLink>
              
              <div className="pt-4 pb-2 text-xs font-semibold text-muted-foreground">
                ARAÇLAR
              </div>
              <NavLink to="/qa/verification">🔍 Kalite Kontrol</NavLink>
            </div>
          )}
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
              Ayarlar
            </Button>
            <Button
              variant="ghost"
              size="sm"
              className="flex-1"
              onClick={handleLogout}
            >
              Çıkış
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

function NavLink({ to, children, className }: { to: string; children: React.ReactNode; className?: string }) {
  return (
    <Link
      to={to}
      className={`block px-3 py-2 rounded-md text-sm hover:bg-gray-100 transition-colors ${className || ''}`}
    >
      {children}
    </Link>
  );
}
