import { Receipt, Eye } from 'lucide-react';
import { memo, useEffect, useState } from 'react';

interface RecentSale {
  id: string;
  invoiceNo: string;
  date: string;
  customerName?: string;
  total: number;
  type: 'cash' | 'credit';
  itemCount: number;
}

interface RecentSalesPanelProps {
  onView?: (saleId: string) => void;
}

// LocalStorage key for recent sales
const STORAGE_KEY = 'fastSale_recentSales';

export const RecentSalesPanel = memo(function RecentSalesPanel({ 
  onView 
}: RecentSalesPanelProps) {
  const [sales, setSales] = useState<RecentSale[]>([]);

  useEffect(() => {
    // Load from localStorage
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        setSales(JSON.parse(stored));
      }
    } catch (error) {
      console.error('Failed to load recent sales:', error);
    }
  }, []);

  if (sales.length === 0) {
    return (
      <div className="p-4 text-center text-slate-400 text-sm">
        Henüz satış yapılmadı
      </div>
    );
  }

  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    
    if (diffMins < 1) return 'Şimdi';
    if (diffMins < 60) return `${diffMins} dk önce`;
    if (diffMins < 1440) return `${Math.floor(diffMins / 60)} sa önce`;
    return date.toLocaleDateString('tr-TR');
  };

  return (
    <div className="p-3 space-y-2">
      {sales.slice(0, 10).map((sale) => (
        <div
          key={sale.id}
          className="flex items-center justify-between p-2 bg-slate-50 hover:bg-slate-100 rounded-lg transition-colors group"
        >
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <Receipt className="w-3.5 h-3.5 text-slate-400" />
              <span className="text-sm font-medium text-slate-800 font-mono">
                {sale.invoiceNo}
              </span>
              <span className={`text-xs px-1.5 py-0.5 rounded ${
                sale.type === 'cash' 
                  ? 'bg-green-100 text-green-700' 
                  : 'bg-purple-100 text-purple-700'
              }`}>
                {sale.type === 'cash' ? 'Peşin' : 'Veresiye'}
              </span>
            </div>
            <div className="flex items-center gap-2 text-xs text-slate-500">
              <span>{formatDate(sale.date)}</span>
              {sale.customerName && (
                <>
                  <span>•</span>
                  <span className="truncate max-w-[120px]">{sale.customerName}</span>
                </>
              )}
              <span>•</span>
              <span>{sale.itemCount} ürün</span>
              <span>•</span>
              <span className="font-medium text-slate-700">₺{sale.total.toFixed(2)}</span>
            </div>
          </div>
          {onView && (
            <button
              onClick={() => onView(sale.id)}
              className="ml-2 p-1.5 hover:bg-blue-100 text-blue-600 rounded opacity-0 group-hover:opacity-100 transition-opacity"
              title="Görüntüle"
            >
              <Eye className="w-4 h-4" />
            </button>
          )}
        </div>
      ))}
    </div>
  );
});

// Helper function to save a new sale to recent sales
export function saveRecentSale(sale: Omit<RecentSale, 'id'>) {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    const sales: RecentSale[] = stored ? JSON.parse(stored) : [];
    
    const newSale: RecentSale = {
      ...sale,
      id: `sale_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
    };
    
    // Add to beginning, keep only last 50
    const updated = [newSale, ...sales].slice(0, 50);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(updated));
  } catch (error) {
    console.error('Failed to save recent sale:', error);
  }
}
