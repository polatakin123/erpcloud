import { Trash2, Plus, Minus, CheckCircle } from 'lucide-react';
import { memo } from 'react';

interface SalesLine {
  id: string;
  variantId: string;
  sku: string;
  name: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  totalPrice: number;
  stock: number;
  isCompatible?: boolean;
  fitmentPriority?: number;
  brand?: string;
  brandId?: string;
  brandCode?: string;
  brandLogoUrl?: string;
  isBrandActive?: boolean;
  listPrice?: number;
  discountPercent?: number;
  discountAmount?: number;
  netPrice?: number;
  unitCost?: number;
  profit?: number;
  profitPercent?: number;
  appliedRuleDescription?: string;
  pricingWarning?: string;
}

interface CartLineRowProps {
  line: SalesLine;
  isSelected: boolean;
  onSelect: () => void;
  onUpdateQuantity: (qty: number) => void;
  onUpdatePrice: (price: number) => void;
  onUpdateDiscount: (discount: number) => void;
  onRemove: () => void;
  onIncrement?: () => void;
  onDecrement?: () => void;
}

export const CartLineRow = memo(function CartLineRow({
  line,
  isSelected,
  onSelect,
  onUpdateQuantity,
  onUpdatePrice,
  onUpdateDiscount,
  onRemove,
  onIncrement,
  onDecrement
}: CartLineRowProps) {
  return (
    <tr 
      className={`border-b border-slate-100 hover:bg-slate-50 cursor-pointer transition-colors ${
        isSelected ? 'bg-blue-50 ring-2 ring-blue-400' : ''
      }`}
      onClick={onSelect}
    >
      <td className="px-4 py-3">
        {/* Brand Badge */}
        {line.brand && (
          <div className="flex items-center gap-1.5 mb-1">
            {line.brandLogoUrl ? (
              <img
                src={line.brandLogoUrl}
                alt={line.brand}
                className="h-4 w-4 rounded-sm object-contain"
                onError={(e) => {
                  (e.target as HTMLImageElement).style.display = 'none';
                }}
              />
            ) : line.brandCode ? (
              <div className="h-4 w-4 rounded-sm bg-blue-600 text-white flex items-center justify-center text-[8px] font-bold">
                {line.brandCode.charAt(0)}
              </div>
            ) : null}
            <span className={`text-xs font-medium px-1.5 py-0.5 rounded ${
              line.isBrandActive === false 
                ? 'bg-gray-200 text-gray-600' 
                : 'bg-blue-50 text-blue-700'
            }`}>
              {line.brandCode || line.brand}
            </span>
          </div>
        )}
        
        {/* Product Info */}
        <div className="font-medium text-slate-800">{line.name}</div>
        <div className="text-sm text-slate-500 font-mono">{line.sku}</div>
        
        {/* Badges */}
        <div className="flex items-center gap-2 mt-1.5 flex-wrap">
          {/* Stock Badge */}
          <span className={`text-xs px-2 py-0.5 rounded ${
            line.stock <= 0 
              ? 'bg-red-100 text-red-700 font-medium'
              : line.stock < 5
              ? 'bg-yellow-100 text-yellow-700'
              : 'bg-slate-100 text-slate-600'
          }`}>
            Stok: {line.stock}
          </span>
          
          {/* Fitment Badge */}
          {line.isCompatible !== undefined && (
            <span className="text-xs bg-green-100 text-green-800 px-2 py-0.5 rounded flex items-center gap-1">
              <CheckCircle className="h-3 w-3" />
              Uyumlu
            </span>
          )}
          
          {/* Pricing Rule Badge */}
          {line.appliedRuleDescription && (
            <span className="text-xs bg-blue-100 text-blue-800 px-2 py-0.5 rounded" title={line.appliedRuleDescription}>
              🏷️ {line.appliedRuleDescription}
            </span>
          )}
          
          {/* Profit Badge */}
          {line.profitPercent !== undefined && (
            <span 
              className={`text-xs px-2 py-0.5 rounded font-medium ${
                line.profitPercent < 0 
                  ? 'bg-red-100 text-red-800' 
                  : line.profitPercent < 10 
                  ? 'bg-yellow-100 text-yellow-800' 
                  : 'bg-green-100 text-green-800'
              }`}
              title={`Kar: ₺${line.profit?.toFixed(2) || 0} | Maliyet: ₺${line.unitCost?.toFixed(2) || 0}`}
            >
              {(line.profitPercent || 0) < 0 ? '⚠️' : '💰'} %{(line.profitPercent || 0).toFixed(1)}
            </span>
          )}
          
          {/* Warning Badge */}
          {line.pricingWarning && (
            <span className="text-xs bg-red-100 text-red-800 px-2 py-0.5 rounded" title={line.pricingWarning}>
              ⚠️ {line.pricingWarning}
            </span>
          )}
        </div>
      </td>
      
      {/* Quantity with +/- buttons */}
      <td className="px-4 py-3">
        <div className="flex items-center gap-1">
          <button
            onClick={(e) => { e.stopPropagation(); onDecrement ? onDecrement() : onUpdateQuantity(Math.max(1, line.quantity - 1)); }}
            className="p-1 hover:bg-slate-200 rounded"
            title="Azalt (-)"
          >
            <Minus className="w-4 h-4" />
          </button>
          <input
            type="number"
            min="1"
            max={line.stock}
            value={line.quantity}
            onChange={(e) => { e.stopPropagation(); onUpdateQuantity(parseInt(e.target.value) || 1); }}
            onClick={(e) => e.stopPropagation()}
            className="w-16 px-2 py-1 border border-slate-300 rounded text-center focus:ring-2 focus:ring-green-500"
          />
          <button
            onClick={(e) => { e.stopPropagation(); onIncrement ? onIncrement() : onUpdateQuantity(line.quantity + 1); }}
            className="p-1 hover:bg-slate-200 rounded"
            title="Artır (+)"
          >
            <Plus className="w-4 h-4" />
          </button>
        </div>
      </td>
      
      {/* Unit Price */}
      <td className="px-4 py-3">
        <input
          type="number"
          step="0.01"
          value={line.unitPrice}
          onChange={(e) => { e.stopPropagation(); onUpdatePrice(parseFloat(e.target.value) || 0); }}
          onClick={(e) => e.stopPropagation()}
          className="w-28 px-2 py-1 border border-slate-300 rounded text-right focus:ring-2 focus:ring-green-500"
        />
      </td>
      
      {/* Discount % */}
      <td className="px-4 py-3">
        <input
          type="number"
          min="0"
          max="100"
          step="0.1"
          value={line.discount}
          onChange={(e) => { e.stopPropagation(); onUpdateDiscount(parseFloat(e.target.value) || 0); }}
          onClick={(e) => e.stopPropagation()}
          className="w-20 px-2 py-1 border border-slate-300 rounded text-right focus:ring-2 focus:ring-yellow-500"
        />
      </td>
      
      {/* Total */}
      <td className="px-4 py-3 text-right font-semibold text-slate-800">
        ₺{line.totalPrice.toFixed(2)}
      </td>
      
      {/* Actions */}
      <td className="px-4 py-3">
        <button
          onClick={(e) => { e.stopPropagation(); onRemove(); }}
          className="p-2 hover:bg-red-100 text-red-600 rounded-lg transition-colors"
          title="Sil (Delete)"
        >
          <Trash2 className="w-4 h-4" />
        </button>
      </td>
    </tr>
  );
});
