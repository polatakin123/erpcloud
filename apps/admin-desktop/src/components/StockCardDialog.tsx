import { Dialog, DialogContent, DialogHeader, DialogTitle } from "./ui/dialog";
import { Package, TrendingUp, Calendar, DollarSign } from "lucide-react";

interface StockCardDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  variant: {
    variantId: string;
    sku: string;
    name: string;
    brand?: string;
    brandCode?: string;
    stock?: number;
    available?: number;
    price?: number;
    barcode?: string;
    oemRefs?: string[];
  };
}

export function StockCardDialog({ open, onOpenChange, variant }: StockCardDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Package className="h-5 w-5" />
            Stok Kartı
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-6">
          {/* Basic Info */}
          <div className="border border-slate-200 rounded-lg p-4 bg-slate-50">
            <h3 className="font-semibold text-slate-700 mb-3">Ürün Bilgileri</h3>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="text-sm text-slate-600">SKU</label>
                <div className="font-mono font-medium">{variant.sku}</div>
              </div>
              <div>
                <label className="text-sm text-slate-600">Barkod</label>
                <div className="font-mono font-medium">{variant.barcode || "-"}</div>
              </div>
              <div className="col-span-2">
                <label className="text-sm text-slate-600">Ürün Adı</label>
                <div className="font-medium">{variant.name}</div>
              </div>
              <div>
                <label className="text-sm text-slate-600">Marka</label>
                <div className="font-medium">{variant.brand || "-"}</div>
              </div>
              <div>
                <label className="text-sm text-slate-600">Marka Kodu</label>
                <div className="font-medium">{variant.brandCode || "-"}</div>
              </div>
            </div>
          </div>

          {/* Stock Info */}
          <div className="border border-slate-200 rounded-lg p-4">
            <h3 className="font-semibold text-slate-700 mb-3 flex items-center gap-2">
              <TrendingUp className="h-4 w-4" />
              Stok Durumu
            </h3>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="text-sm text-slate-600">Toplam Stok</label>
                <div className="text-2xl font-bold text-slate-900">
                  {variant.stock ?? 0}
                </div>
              </div>
              <div>
                <label className="text-sm text-slate-600">Kullanılabilir</label>
                <div className="text-2xl font-bold text-green-600">
                  {variant.available ?? 0}
                </div>
              </div>
            </div>
          </div>

          {/* Pricing Info */}
          <div className="border border-slate-200 rounded-lg p-4">
            <h3 className="font-semibold text-slate-700 mb-3 flex items-center gap-2">
              <DollarSign className="h-4 w-4" />
              Fiyat Bilgisi
            </h3>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="text-sm text-slate-600">Satış Fiyatı</label>
                <div className="text-2xl font-bold text-blue-600">
                  ₺{(variant.price ?? 0).toFixed(2)}
                </div>
              </div>
            </div>
          </div>

          {/* OEM References */}
          {variant.oemRefs && variant.oemRefs.length > 0 && (
            <div className="border border-slate-200 rounded-lg p-4">
              <h3 className="font-semibold text-slate-700 mb-3">OEM Referansları</h3>
              <div className="flex flex-wrap gap-2">
                {variant.oemRefs.map((oem, index) => (
                  <span
                    key={index}
                    className="bg-blue-100 text-blue-800 text-sm px-3 py-1 rounded-full font-mono"
                  >
                    {oem}
                  </span>
                ))}
              </div>
            </div>
          )}

          {/* Future: Stock Movements */}
          <div className="border border-slate-200 rounded-lg p-4 bg-slate-50">
            <h3 className="font-semibold text-slate-700 mb-3 flex items-center gap-2">
              <Calendar className="h-4 w-4" />
              Son Hareketler
            </h3>
            <p className="text-sm text-slate-500 italic">Yakında eklenecek...</p>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
