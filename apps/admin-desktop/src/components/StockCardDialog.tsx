import { Dialog, DialogContent, DialogHeader, DialogTitle } from "./ui/dialog";
import { Package, TrendingUp, Calendar, DollarSign, Save } from "lucide-react";
import { useState, useEffect } from "react";

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
  const [editedName, setEditedName] = useState(variant.name);
  const [editedBarcode, setEditedBarcode] = useState(variant.barcode || "");
  const [editedPrice, setEditedPrice] = useState(variant.price?.toString() || "0");
  const [editedStock, setEditedStock] = useState(variant.stock?.toString() || "0");

  // Variant değiştiğinde state'i güncelle
  useEffect(() => {
    setEditedName(variant.name);
    setEditedBarcode(variant.barcode || "");
    setEditedPrice(variant.price?.toString() || "0");
    setEditedStock(variant.stock?.toString() || "0");
  }, [variant]);

  const handleSave = () => {
    console.log("Saving changes:", {
      name: editedName,
      barcode: editedBarcode,
      price: parseFloat(editedPrice),
      stock: parseInt(editedStock),
    });
    alert("Değişiklikler kaydedildi! (API entegrasyonu yapılacak)");
    onOpenChange(false);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-5xl max-h-[95vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Package className="h-5 w-5" />
            Stok Kartı - Detaylı Görünüm
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-6">
          {/* Basic Info */}
          <div className="border border-slate-200 rounded-lg p-6 bg-slate-50">
            <h3 className="font-semibold text-slate-700 mb-4 text-lg">Ürün Bilgileri</h3>
            <div className="grid grid-cols-2 gap-6">
              <div>
                <label className="block text-sm font-medium text-slate-600 mb-2">SKU (Değiştirilemez)</label>
                <div className="font-mono font-medium text-lg bg-white px-4 py-3 rounded-lg border border-slate-300">
                  {variant.sku}
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-600 mb-2">Barkod</label>
                <input
                  type="text"
                  value={editedBarcode}
                  onChange={(e) => setEditedBarcode(e.target.value)}
                  className="w-full font-mono px-4 py-3 rounded-lg border border-slate-300 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  placeholder="Barkod giriniz"
                />
              </div>
              <div className="col-span-2">
                <label className="block text-sm font-medium text-slate-600 mb-2">Ürün Adı</label>
                <input
                  type="text"
                  value={editedName}
                  onChange={(e) => setEditedName(e.target.value)}
                  className="w-full px-4 py-3 rounded-lg border border-slate-300 focus:ring-2 focus:ring-blue-500 focus:border-transparent text-lg"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-600 mb-2">Marka</label>
                <div className="font-medium text-lg bg-white px-4 py-3 rounded-lg border border-slate-300">
                  {variant.brand || "-"}
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-600 mb-2">Marka Kodu</label>
                <div className="font-medium text-lg bg-white px-4 py-3 rounded-lg border border-slate-300">
                  {variant.brandCode || "-"}
                </div>
              </div>
            </div>
          </div>

          {/* Stock Info */}
          <div className="border border-slate-200 rounded-lg p-6">
            <h3 className="font-semibold text-slate-700 mb-4 text-lg flex items-center gap-2">
              <TrendingUp className="h-5 w-5" />
              Stok Durumu
            </h3>
            <div className="grid grid-cols-2 gap-6">
              <div>
                <label className="block text-sm font-medium text-slate-600 mb-2">Toplam Stok</label>
                <input
                  type="number"
                  value={editedStock}
                  onChange={(e) => setEditedStock(e.target.value)}
                  className="w-full px-4 py-3 rounded-lg border border-slate-300 focus:ring-2 focus:ring-blue-500 focus:border-transparent text-2xl font-bold"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-600 mb-2">Kullanılabilir</label>
                <div className="text-3xl font-bold text-green-600 bg-green-50 px-4 py-3 rounded-lg border border-green-200">
                  {variant.available ?? 0}
                </div>
              </div>
            </div>
          </div>

          {/* Pricing Info */}
          <div className="border border-slate-200 rounded-lg p-6">
            <h3 className="font-semibold text-slate-700 mb-4 text-lg flex items-center gap-2">
              <DollarSign className="h-5 w-5" />
              Fiyat Bilgisi
            </h3>
            <div className="grid grid-cols-2 gap-6">
              <div>
                <label className="block text-sm font-medium text-slate-600 mb-2">Satış Fiyatı (₺)</label>
                <input
                  type="number"
                  step="0.01"
                  value={editedPrice}
                  onChange={(e) => setEditedPrice(e.target.value)}
                  className="w-full px-4 py-3 rounded-lg border border-slate-300 focus:ring-2 focus:ring-blue-500 focus:border-transparent text-2xl font-bold"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-600 mb-2">KDV Dahil</label>
                <div className="text-2xl font-bold text-blue-600 bg-blue-50 px-4 py-3 rounded-lg border border-blue-200">
                  ₺{(parseFloat(editedPrice) * 1.20).toFixed(2)}
                </div>
              </div>
            </div>
          </div>

          {/* OEM References */}
          {variant.oemRefs && variant.oemRefs.length > 0 && (
            <div className="border border-slate-200 rounded-lg p-6">
              <h3 className="font-semibold text-slate-700 mb-4 text-lg">OEM Referansları</h3>
              <div className="flex flex-wrap gap-2">
                {variant.oemRefs.map((oem, index) => (
                  <span
                    key={index}
                    className="bg-blue-100 text-blue-800 text-sm px-4 py-2 rounded-full font-mono"
                  >
                    {oem}
                  </span>
                ))}
              </div>
            </div>
          )}

          {/* Action Buttons */}
          <div className="flex justify-end gap-3 pt-4 border-t border-slate-200">
            <button
              onClick={() => onOpenChange(false)}
              className="px-6 py-3 text-slate-700 border border-slate-300 rounded-lg hover:bg-slate-50 font-medium transition-colors"
            >
              İptal
            </button>
            <button
              onClick={handleSave}
              className="px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 font-medium transition-colors flex items-center gap-2"
            >
              <Save className="h-4 w-4" />
              Kaydet
            </button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
