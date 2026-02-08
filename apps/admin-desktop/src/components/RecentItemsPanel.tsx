import { Plus } from 'lucide-react';
import { memo } from 'react';

interface RecentItem {
  variantId: string;
  sku: string;
  name: string;
  brand?: string;
  stock: number;
  lastAdded: number;
}

interface RecentItemsPanelProps {
  items: RecentItem[];
  onAddToCart: (item: RecentItem) => void;
}

export const RecentItemsPanel = memo(function RecentItemsPanel({ 
  items, 
  onAddToCart 
}: RecentItemsPanelProps) {
  if (items.length === 0) {
    return (
      <div className="p-4 text-center text-slate-400 text-sm">
        Henüz ürün eklenmedi
      </div>
    );
  }

  return (
    <div className="p-3 space-y-2">
      {items.slice(0, 5).map((item) => (
        <div
          key={item.variantId}
          className="flex items-center justify-between p-2 bg-slate-50 hover:bg-slate-100 rounded-lg transition-colors group"
        >
          <div className="flex-1 min-w-0">
            <div className="text-sm font-medium text-slate-800 truncate">
              {item.name}
            </div>
            <div className="flex items-center gap-2 text-xs text-slate-500">
              <span className="font-mono">{item.sku}</span>
              {item.brand && (
                <>
                  <span>•</span>
                  <span>{item.brand}</span>
                </>
              )}
              <span>•</span>
              <span className={item.stock <= 0 ? 'text-red-600' : item.stock < 5 ? 'text-yellow-600' : ''}>
                Stok: {item.stock}
              </span>
            </div>
          </div>
          <button
            onClick={() => onAddToCart(item)}
            className="ml-2 p-1.5 hover:bg-green-100 text-green-600 rounded opacity-0 group-hover:opacity-100 transition-opacity"
            title="Sepete ekle"
          >
            <Plus className="w-4 h-4" />
          </button>
        </div>
      ))}
    </div>
  );
});
