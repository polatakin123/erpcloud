import { useState, useEffect, useRef, useCallback, useMemo } from "react";
import { useLocation } from "react-router-dom";
import { Search, CreditCard, FileText, X, Loader2, CheckCircle, Plus, Settings } from "lucide-react";
import CustomerPickerModal from "../components/CustomerPickerModal";
import { MiniVehicleSelector } from "../components/MiniVehicleSelector";
import { CartLineRow } from "../components/CartLineRow";
import { RecentItemsPanel } from "../components/RecentItemsPanel";
import { RecentSalesPanel, saveRecentSale } from "../components/RecentSalesPanel";
import { useCreateSalesOrder, useConfirmSalesOrder, useCreateShipment, useShipShipment, useCreateInvoiceFromShipment, useIssueInvoice } from "../hooks/useSales";
import { useCreatePayment, useCashboxes, useCreateCashbox } from "../hooks/usePayments";
import { useWarehouses } from "../hooks/useWarehouses";
import { useAutoAllocatePayment } from "../hooks/usePaymentAllocation";
import { useToast } from "../hooks/useToast";
import { useVehicleContext } from "../hooks/useVehicleContext";
import { usePricingCalculation } from "../hooks/usePricing";
import { useFastSaleKeyboard } from "../hooks/useFastSaleKeyboard";
import { useFastVariantSearch } from "../hooks/useFastVariantSearch";
import { ErrorMapper } from "../lib/error-mapper";
import { ApiClient } from "../lib/api-client";

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
  isCompatible?: boolean; // Fitment compatibility flag
  fitmentPriority?: number;
  
  // Brand information
  brand?: string;
  brandId?: string;
  brandCode?: string;
  brandLogoUrl?: string;
  isBrandActive?: boolean;
  
  // Pricing details
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

interface Party {
  id: string;
  name: string;
  code: string;
  balance: number;
  creditLimit: number;
}

type SaleType = "cash" | "credit";
type PaymentMethod = "cash" | "card" | "bank";

export default function FastSalesPage() {
  const location = useLocation();
  const [saleType, setSaleType] = useState<SaleType>("cash");
  const [salesLines, setSalesLines] = useState<SalesLine[]>([]);
  const [selectedParty, setSelectedParty] = useState<Party | null>(null);
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>("cash");
  const [showCustomerPicker, setShowCustomerPicker] = useState(false);
  const [showCashboxSettings, setShowCashboxSettings] = useState(false);
  const [newCashboxName, setNewCashboxName] = useState("");
  const [isProcessing, setIsProcessing] = useState(false);
  const [processingStep, setProcessingStep] = useState("");
  
  // Vehicle context for fitment filtering
  const { selectedEngineId } = useVehicleContext();
  
  // Backend hooks
  const createOrder = useCreateSalesOrder();
  const confirmOrder = useConfirmSalesOrder();
  const createShipment = useCreateShipment();
  const shipShipment = useShipShipment();
  const createInvoice = useCreateInvoiceFromShipment();
  const issueInvoice = useIssueInvoice();
  const createPayment = useCreatePayment();
  const autoAllocate = useAutoAllocatePayment();
  const { toast } = useToast();
  const { data: warehouses } = useWarehouses();
  const { data: cashboxesData, refetch: refetchCashboxes } = useCashboxes();
  const createCashbox = useCreateCashbox();
  const pricingCalculation = usePricingCalculation();
  
  // Search state
  const [searchQuery, setSearchQuery] = useState("");
  const [includeEquivalents] = useState(true); // Always include equivalents for fast sales
  const [includeUndefinedFitment] = useState(false); // Don't show undefined fitment by default
  
  // Use optimized fast search hook with debouncing and caching
  const { data: searchData, isLoading: isSearching, durationMs } = useFastVariantSearch({
    query: searchQuery,
    warehouseId: warehouses?.find((w: any) => w.isDefault)?.id || warehouses?.[0]?.id,
    engineId: selectedEngineId || undefined,
    includeEquivalents,
    includeUndefinedFitment: selectedEngineId ? includeUndefinedFitment : undefined,
    debounceMs: 150, // Tezgâh: 150ms debounce for snappy UX
    minChars: 2,
    page: 1,
    pageSize: 20,
  });
  
  const searchResults = searchData?.results || [];
  
  // Barcode state
  const [barcodeBuffer, setBarcodeBuffer] = useState("");
  const [lastBarcodeTime, setLastBarcodeTime] = useState(0);
  
  // Recent items tracking
  const [recentItems, setRecentItems] = useState<any[]>([]);
  
  // Selected line for keyboard navigation
  const [selectedLineIndex, setSelectedLineIndex] = useState<number>(-1);
  
  // Toggle recent panels
  const [showRecentPanel, setShowRecentPanel] = useState(false);
  
  // Refs for keyboard navigation
  const searchInputRef = useRef<HTMLInputElement>(null);
  const barcodeInputRef = useRef<HTMLInputElement>(null);
  const paymentMethodRef = useRef<HTMLSelectElement>(null);
  const discountInputRef = useRef<HTMLInputElement>(null);

  // Add item from FastSearchPage if passed via location state
  useEffect(() => {
    if (location.state?.addToCart) {
      const item = location.state.addToCart;
      addToCart(item);
      // Clear location state
      window.history.replaceState({}, document.title);
    }
  }, [location.state]);

  // Calculate totals
  const subtotal = useMemo(() => 
    salesLines.reduce((sum, line) => sum + line.totalPrice, 0),
    [salesLines]
  );
  const totalDiscount = useMemo(() => 
    salesLines.reduce((sum, line) => sum + (line.unitPrice * line.quantity * line.discount / 100), 0),
    [salesLines]
  );
  const grandTotal = subtotal;

  // Keyboard shortcuts
  const handleBarcodeFocus = useCallback(() => {
    barcodeInputRef.current?.focus();
    barcodeInputRef.current?.select();
  }, []);

  const handleSearchFocus = useCallback(() => {
    searchInputRef.current?.focus();
    searchInputRef.current?.select();
  }, []);

  const handleDiscountFocus = useCallback(() => {
    if (selectedLineIndex >= 0 && discountInputRef.current) {
      discountInputRef.current.focus();
      discountInputRef.current.select();
    }
  }, [selectedLineIndex]);

  const handlePaymentFocus = useCallback(() => {
    if (saleType === 'cash' && paymentMethodRef.current) {
      paymentMethodRef.current.focus();
    }
  }, [saleType]);

  const handleCustomerPickerForCredit = useCallback(() => {
    if (saleType === 'credit') {
      setShowCustomerPicker(true);
    }
  }, [saleType]);

  const handleToggleRecent = useCallback(() => {
    setShowRecentPanel(prev => !prev);
  }, []);

  const handleNextLine = useCallback(() => {
    if (salesLines.length > 0) {
      setSelectedLineIndex(prev => (prev + 1) % salesLines.length);
    }
  }, [salesLines.length]);

  const handlePrevLine = useCallback(() => {
    if (salesLines.length > 0) {
      setSelectedLineIndex(prev => prev <= 0 ? salesLines.length - 1 : prev - 1);
    }
  }, [salesLines.length]);

  const handleDeleteLine = useCallback(() => {
    if (selectedLineIndex >= 0 && selectedLineIndex < salesLines.length) {
      const line = salesLines[selectedLineIndex];
      removeLine(line.id);
      setSelectedLineIndex(Math.max(0, selectedLineIndex - 1));
    }
  }, [selectedLineIndex, salesLines]);

  const handleIncreaseQty = useCallback(() => {
    if (selectedLineIndex >= 0 && selectedLineIndex < salesLines.length) {
      const line = salesLines[selectedLineIndex];
      updateLine(line.id, "quantity", line.quantity + 1);
    }
  }, [selectedLineIndex, salesLines]);

  const handleDecreaseQty = useCallback(() => {
    if (selectedLineIndex >= 0 && selectedLineIndex < salesLines.length) {
      const line = salesLines[selectedLineIndex];
      updateLine(line.id, "quantity", Math.max(1, line.quantity - 1));
    }
  }, [selectedLineIndex, salesLines]);

  useFastSaleKeyboard({
    onBarcodeFocus: handleBarcodeFocus,
    onSearchFocus: handleSearchFocus,
    onCustomerPicker: () => setShowCustomerPicker(true),
    onDiscountFocus: handleDiscountFocus,
    onPaymentFocus: handlePaymentFocus,
    onCustomerPickerForCredit: handleCustomerPickerForCredit,
    onCompleteSale: () => handleSale(),
    onToggleRecent: handleToggleRecent,
    onCancel: () => handleCancel(),
    onNextLine: handleNextLine,
    onPrevLine: handlePrevLine,
    onDeleteLine: handleDeleteLine,
    onIncreaseQty: handleIncreaseQty,
    onDecreaseQty: handleDecreaseQty,
  }, !showCustomerPicker && !isProcessing);

  // Focus barcode on mount and after modals close
  useEffect(() => {
    if (!showCustomerPicker && !isProcessing) {
      handleBarcodeFocus();
    }
  }, [showCustomerPicker, isProcessing, handleBarcodeFocus]);

  // Add item from FastSearchPage if passed via location state
  useEffect(() => {
    if (location.state?.addToCart) {
      const item = location.state.addToCart;
      addToCart(item);
      // Clear location state
      window.history.replaceState({}, document.title);
    }
  }, [location.state]);

  // Handle barcode input with Enter key - auto-add if single result
  const handleBarcodeSubmit = async (barcode: string) => {
    if (!barcode.trim()) return;

    try {
      // Perform immediate search
      const params = new URLSearchParams();
      params.append('q', barcode.trim());
      params.append('includeEquivalents', 'false'); // Barcode should match exactly
      const defaultWarehouse = warehouses?.find((w: any) => w.isDefault);
      if (defaultWarehouse?.id) {
        params.append('warehouseId', defaultWarehouse.id);
      }
      
      const response = await ApiClient.get<{ results: any[] }>(
        `/api/search/variants?${params.toString()}`
      );

      if (response.results.length === 1) {
        // Single match - auto-add to cart
        const result = response.results[0];
        // Map API response to variant format expected by addToCart
        const variant = {
          variantId: result.variantId,
          sku: result.sku,
          barcode: result.barcode,
          name: result.name,
          variantName: result.name,
          brand: result.brand,
          brandId: result.brandId,
          brandCode: result.brandCode,
          brandLogoUrl: result.brandLogoUrl,
          isBrandActive: result.isBrandActive,
          stock: result.available || 0,
          available: result.available || 0,
          price: result.price || 0,
          oemRefs: result.oemRefs || [],
          isCompatible: result.isCompatible,
          fitmentPriority: result.fitmentPriority,
        };
        await addToCart(variant);
        toast({
          title: "Ürün Eklendi",
          description: `${result.name} sepete eklendi`,
        });
      } else if (response.results.length > 1) {
        // Multiple matches - show in search results for manual selection
        setSearchQuery(barcode.trim());
        toast({
          title: "Çoklu Sonuç",
          description: `${response.results.length} ürün bulundu. Lütfen seçim yapın.`,
          variant: "default",
        });
      } else {
        // No match
        toast({
          title: "Ürün Bulunamadı",
          description: `"${barcode}" için sonuç bulunamadı`,
          variant: "destructive",
        });
      }
    } catch (error) {
      const mapped = ErrorMapper.mapError(error);
      toast({
        variant: "destructive",
        title: "Arama Hatası",
        description: mapped.message,
      });
    }
  };

  // Calculate pricing for a line using the pricing service
  const calculateLinePricing = async (variantId: string, quantity: number): Promise<Partial<SalesLine>> => {
    if (!selectedParty && saleType === "credit") {
      return {}; // No pricing without customer for credit sales
    }

    const warehouse = warehouses?.find((w: any) => w.isDefault) || warehouses?.[0];
    
    try {
      const result = await pricingCalculation.mutateAsync({
        partyId: selectedParty?.id || "00000000-0000-0000-0000-000000000000",
        variantId,
        quantity,
        warehouseId: warehouse?.id,
        currency: "TRY",
      });

      return {
        listPrice: result.listPrice,
        discountPercent: result.discountPercent,
        discountAmount: result.discountAmount,
        netPrice: result.netPrice,
        unitPrice: result.netPrice, // Use net price as unit price
        discount: result.discountPercent || 0,
        totalPrice: result.lineTotal,
        unitCost: result.unitCost,
        profit: result.profit,
        profitPercent: result.profitPercent,
        appliedRuleDescription: result.ruleDescription,
        pricingWarning: result.hasWarning ? result.warningMessage : undefined,
      };
    } catch (error) {
      console.error("Pricing calculation error:", error);
      return {};
    }
  };

  const addToCart = async (variant: any) => {
    // Check fitment compatibility if vehicle is selected
    if (selectedEngineId && variant.fitmentPriority === 4) {
      toast({
        variant: "destructive",
        title: "⚠️ Uyumsuz Parça",
        description: "Bu parça seçili araçla uyumlu değil. Devam etmek için araç seçimini temizleyin.",
      });
      return;
    }

    const existing = salesLines.find(line => line.variantId === variant.variantId);
    
    // Check for barcode burst (same barcode within 2 seconds)
    const now = Date.now();
    const burstThreshold = 2000; // 2 seconds
    const isBarcodeInput = barcodeBuffer === variant.sku || barcodeBuffer === variant.barcode;
    const isBurst = isBarcodeInput && (now - lastBarcodeTime) < burstThreshold;
    
    if (existing) {
      // Increase quantity and recalculate pricing
      const newQuantity = existing.quantity + 1;
      const pricingData = await calculateLinePricing(variant.variantId, newQuantity);
      
      setSalesLines(salesLines.map(line =>
        line.variantId === variant.variantId
          ? { 
              ...line, 
              quantity: newQuantity,
              ...pricingData,
              // Fallback to manual calculation if pricing fails
              totalPrice: pricingData.totalPrice ?? ((newQuantity) * line.unitPrice * (1 - line.discount / 100))
            }
          : line
      ));
      
      if (isBurst) {
        // Show toast for qty increase
        toast({
          title: `${variant.name}`,
          description: `Miktar: ${newQuantity} ✓`,
        });
      }
    } else {
      // Add new line with pricing
      const pricingData = await calculateLinePricing(variant.variantId, 1);
      
      // Show warnings if any
      if (pricingData.pricingWarning) {
        toast({
          variant: "default",
          title: "💡 Fiyatlandırma Uyarısı",
          description: pricingData.pricingWarning,
        });
      }
      
      // Warn if low stock
      if (variant.stock < 5 && variant.stock > 0) {
        toast({
          variant: "default",
          title: "⚠️ Düşük Stok",
          description: `${variant.name} - Sadece ${variant.stock} adet kaldı`,
        });
      } else if (variant.stock <= 0) {
        toast({
          variant: "destructive",
          title: "❌ Stokta Yok",
          description: `${variant.name} stoklarda bulunamadı. Satış yapılabilir ancak sevk edilemeyebilir.`,
        });
      }
      
      const newLine: SalesLine = {
        id: Math.random().toString(36).substr(2, 9),
        variantId: variant.variantId,
        sku: variant.sku,
        name: variant.variantName || variant.name,
        quantity: 1,
        unitPrice: pricingData.unitPrice ?? variant.price ?? 0,
        discount: pricingData.discount ?? 0,
        totalPrice: pricingData.totalPrice ?? variant.price ?? 0,
        stock: variant.stock || variant.available || 0,
        isCompatible: selectedEngineId ? variant.isCompatible : undefined,
        fitmentPriority: variant.fitmentPriority,
        // Brand information
        brand: variant.brand,
        brandId: variant.brandId,
        brandCode: variant.brandCode,
        brandLogoUrl: variant.brandLogoUrl,
        isBrandActive: variant.isBrandActive,
        // Pricing details
        ...pricingData,
      };
      setSalesLines([...salesLines, newLine]);
      
      // Add to recent items (keep last 10 unique)
      setRecentItems(prev => {
        const filtered = prev.filter(item => item.variantId !== variant.variantId);
        return [{
          variantId: variant.variantId,
          sku: variant.sku,
          name: variant.variantName || variant.name,
          brand: variant.brand,
          stock: variant.stock || 0,
          lastAdded: Date.now(),
        }, ...filtered].slice(0, 10);
      });
    }

    // Track barcode timing for burst detection
    if (isBarcodeInput) {
      setLastBarcodeTime(now);
      setBarcodeBuffer(variant.sku || variant.barcode);
    }

    // Clear search
    setSearchQuery("");
    handleBarcodeFocus();
  };

  const updateLine = async (id: string, field: keyof SalesLine, value: any) => {
    const line = salesLines.find(l => l.id === id);
    if (!line) return;

    // If quantity changed, recalculate pricing
    if (field === "quantity") {
      const pricingData = await calculateLinePricing(line.variantId, value);
      
      setSalesLines(salesLines.map(l => {
        if (l.id !== id) return l;
        
        return {
          ...l,
          quantity: value,
          ...pricingData,
          // Fallback to manual calculation
          totalPrice: pricingData.totalPrice ?? (value * l.unitPrice * (1 - l.discount / 100))
        };
      }));
    } else {
      // For other fields, update normally
      setSalesLines(salesLines.map(l => {
        if (l.id !== id) return l;
        
        const updated = { ...l, [field]: value };
        
        // Recalculate total for price/discount changes
        if (field === "unitPrice" || field === "discount") {
          updated.totalPrice = updated.quantity * updated.unitPrice * (1 - updated.discount / 100);
        }
        
        return updated;
      }));
    }
  };

  const removeLine = (id: string) => {
    setSalesLines(salesLines.filter(line => line.id !== id));
  };

  const handleSale = async () => {
    if (salesLines.length === 0) {
      toast({
        variant: "destructive",
        title: "Satış Yapılamaz",
        description: "Sepete en az 1 ürün ekleyiniz.",
      });
      return;
    }

    if (saleType === "credit" && !selectedParty) {
      toast({
        variant: "destructive",
        title: "Cari Seçilmedi",
        description: "Veresiye satış için cari seçmelisiniz.",
      });
      setShowCustomerPicker(true);
      return;
    }

    // Get default warehouse
    const warehouse = warehouses?.find((w: any) => w.isDefault) || warehouses?.[0];
    if (!warehouse) {
      toast({
        variant: "destructive",
        title: "Depo Bulunamadı",
        description: "Lütfen ayarlardan depo tanımlayın.",
      });
      return;
    }

    setIsProcessing(true);

    try {
      // STEP 1: Create Sales Order
      setProcessingStep("Sipariş oluşturuluyor...");
      const orderData = {
        orderNo: `SO-${Date.now()}`,
        branchId: warehouse.branchId,
        warehouseId: warehouse.id,
        partyId: selectedParty?.id || "00000000-0000-0000-0000-000000000001", // Walk-in customer
        orderDate: new Date().toISOString(),
        priceListId: null,
        note: saleType === "cash" ? "Peşin satış - Tezgâh" : "Veresiye satış - Tezgâh",
        lines: salesLines.map(line => ({
          variantId: line.variantId,
          qty: line.quantity,
          unitPrice: line.unitPrice,
          vatRate: 20, // Default VAT
          note: null,
        })),
      };

      const order = await createOrder.mutateAsync(orderData);

      // STEP 2: Confirm Order
      setProcessingStep("Sipariş onaylanıyor...");
      const confirmedOrder = await confirmOrder.mutateAsync(order.id);

      // STEP 3: Create Shipment
      setProcessingStep("Sevkiyat oluşturuluyor...");
      const shipmentData = {
        shipmentNo: `SHP-${Date.now()}`,
        salesOrderId: confirmedOrder.id,
        branchId: warehouse.branchId,
        warehouseId: warehouse.id,
        shipmentDate: new Date().toISOString(),
        note: null,
        lines: confirmedOrder.lines.map((line: any) => ({
          salesOrderLineId: line.id,
          qty: line.qty,
        })),
      };
      const shipment = await createShipment.mutateAsync(shipmentData);

      // STEP 4: Ship it
      setProcessingStep("Sevkiyat tamamlanıyor...");
      const shippedShipment = await shipShipment.mutateAsync(shipment.id);

      if (saleType === "cash") {
        // CASH SALE: Continue to invoice and payment
        
        // STEP 5: Create Invoice from Shipment
        setProcessingStep("Fatura oluşturuluyor...");
        const invoice = await createInvoice.mutateAsync(shippedShipment.id);

        // STEP 6: Issue Invoice
        setProcessingStep("Fatura kesiliyor...");
        const issuedInvoice = await issueInvoice.mutateAsync(invoice.id);

        // STEP 7: Create Payment
        setProcessingStep("Tahsilat kaydediliyor...");
        const cashbox = cashboxesData?.items?.[0];
        if (!cashbox) {
          toast({
            variant: "destructive",
            title: "Kasa Bulunamadı",
            description: "Ödeme kaydedilemedi.",
          });
          throw new Error("No cashbox found");
        }

        const paymentData = {
          paymentNo: `PAY-${Date.now()}`,
          partyId: selectedParty?.id || "00000000-0000-0000-0000-000000000001",
          branchId: warehouse.branchId,
          date: new Date().toISOString(),
          direction: "IN",
          method: paymentMethod === "cash" ? "CASH" : paymentMethod === "card" ? "CARD" : "BANK",
          currency: "TRY",
          amount: grandTotal,
          sourceType: paymentMethod === "cash" || paymentMethod === "card" ? "CASHBOX" : "BANK_ACCOUNT",
          sourceId: cashbox.id,
          note: `Peşin satış tahsilatı - Fatura: ${issuedInvoice.invoiceNo}`,
        };

        const payment = await createPayment.mutateAsync(paymentData);

        // STEP 8: Auto-allocate payment to invoice
        setProcessingStep("Fatura ve tahsilat eşleştiriliyor...");
        try {
          await autoAllocate.mutateAsync({
            paymentId: payment.id,
            invoiceIds: [issuedInvoice.id],
          });
        } catch (allocError) {
          console.warn("Payment allocation error:", allocError);
          // Show visible warning to user but don't fail the sale
          toast({
            variant: "destructive",
            title: "⚠️ Tahsilat Eşleştirilemedi",
            description: "Tahsilat eşleştirilemedi. Fatura açık kalmış olabilir.",
          });
          // Don't fail the entire sale, continue
        }

        // Success - Save to recent sales
        saveRecentSale({
          invoiceNo: issuedInvoice.invoiceNo,
          date: new Date().toISOString(),
          customerName: selectedParty?.name || "Perakende",
          total: grandTotal,
          type: "cash",
          itemCount: salesLines.length,
        });

        // Success toast
        toast({
          title: "✅ Peşin Satış Tamamlandı!",
          description: (
            <div className="mt-2 space-y-1 text-sm">
              <div><strong>Fatura No:</strong> {issuedInvoice.invoiceNo}</div>
              <div><strong>Toplam:</strong> ₺{grandTotal.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</div>
              <div><strong>Ödeme:</strong> {paymentMethod === 'cash' ? '💵 Nakit' : paymentMethod === 'card' ? '💳 Kredi Kartı' : '🏦 Banka'}</div>
            </div>
          ),
        });
      } else {
        // CREDIT SALE: Stop at shipment (irsaliye)
        
        // Save to recent sales
        saveRecentSale({
          invoiceNo: shippedShipment.shipmentNo,
          date: new Date().toISOString(),
          customerName: selectedParty?.name || "",
          total: grandTotal,
          type: "credit",
          itemCount: salesLines.length,
        });

        toast({
          title: "✅ Veresiye Satış Tamamlandı!",
          description: (
            <div className="mt-2 space-y-1 text-sm">
              <div><strong>İrsaliye No:</strong> {shippedShipment.shipmentNo}</div>
              <div><strong>Cari:</strong> {selectedParty?.name}</div>
              <div><strong>Toplam:</strong> ₺{grandTotal.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}</div>
            </div>
          ),
        });
      }

      // Clear cart
      setSalesLines([]);
      setRecentItems([]);
      setSelectedParty(null);
      setSelectedLineIndex(-1);
      barcodeInputRef.current?.focus();

    } catch (error: any) {
      console.error("Sale processing error:", error);
      const mapped = ErrorMapper.mapError(error);
      toast({
        variant: "destructive",
        title: `❌ ${processingStep} Başarısız`,
        description: mapped.message,
      });
    } finally {
      setIsProcessing(false);
      setProcessingStep("");
    }
  };

  const handleCancel = () => {
    if (salesLines.length > 0) {
      const confirmCancel = window.confirm("Satışı iptal etmek istediğinize emin misiniz?");
      if (confirmCancel) {
        setSalesLines([]);
        setRecentItems([]);
        setSelectedParty(null);
        setSelectedLineIndex(-1);
        toast({
          title: "Satış İptal Edildi",
          description: "Sepet temizlendi.",
        });
        barcodeInputRef.current?.focus();
      }
    }
  };

  const handleCreateCashbox = async () => {
    if (!newCashboxName.trim()) {
      toast({
        variant: "destructive",
        title: "Hata",
        description: "Kasa adı boş olamaz",
      });
      return;
    }

    try {
      const code = newCashboxName.toUpperCase().replace(/[^A-Z0-9]/g, '_').substring(0, 32);
      await createCashbox.mutateAsync({
        code,
        name: newCashboxName.trim(),
        currency: 'TRY',
        isDefault: !cashboxesData?.items?.length, // First cashbox is default
      });

      toast({
        title: "✅ Kasa Oluşturuldu",
        description: `${newCashboxName} kasası başarıyla oluşturuldu.`,
      });

      setNewCashboxName("");
      setShowCashboxSettings(false);
      refetchCashboxes();
    } catch (error: any) {
      const mapped = ErrorMapper.mapError(error);
      toast({
        variant: "destructive",
        title: "❌ Kasa Oluşturulamadı",
        description: mapped.message,
      });
    }
  };

  return (
    <div className="h-screen flex flex-col bg-slate-50">
      {/* Header */}
      <div className="bg-gradient-to-r from-green-600 to-green-700 text-white px-6 py-4 shadow-lg">
        <h1 className="text-2xl font-bold">⚡ Hızlı Satış</h1>
      </div>

      <div className="flex-1 flex overflow-hidden">
        {/* LEFT: Search Panel */}
        <div className="w-1/3 border-r border-slate-200 bg-white flex flex-col">
          <div className="p-4 border-b border-slate-200 bg-slate-50">
            <h2 className="font-semibold text-slate-700 mb-3">Ürün Arama</h2>
            
            {/* Mini Vehicle Selector */}
            <div className="mb-3">
              <MiniVehicleSelector />
            </div>
            
            {/* Barcode Input */}
            <div className="mb-3">
              <label className="block text-sm text-slate-600 mb-1">
                <kbd className="bg-slate-200 px-2 py-1 rounded text-xs">F1</kbd> Barkod
              </label>
              <input
                ref={barcodeInputRef}
                type="text"
                placeholder="Barkod okutun veya girin..."
                className="w-full px-4 py-3 border border-slate-300 rounded-lg font-mono text-lg focus:ring-2 focus:ring-green-500 focus:border-green-500"
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    const barcode = e.currentTarget.value.trim();
                    e.currentTarget.value = "";
                    handleBarcodeSubmit(barcode);
                  }
                }}
              />
            </div>

            {/* OEM / Name Search */}
            <div className="mb-3">
              <label className="block text-sm text-slate-600 mb-1">
                <kbd className="bg-slate-200 px-2 py-1 rounded text-xs">F2</kbd> OEM / Ürün Adı
              </label>
              <div className="flex gap-2 relative">
                <input
                  ref={searchInputRef}
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder="OEM kodu veya ürün adı..."
                  className="flex-1 px-4 py-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
                {isSearching && (
                  <div className="absolute right-3 top-1/2 -translate-y-1/2">
                    <div className="animate-spin w-5 h-5 border-2 border-blue-500 border-t-transparent rounded-full"></div>
                  </div>
                )}
              </div>
              {durationMs !== undefined && durationMs > 0 && (
                <div className="text-xs text-slate-400 mt-1">
                  Arama süresi: {durationMs.toFixed(0)}ms
                </div>
              )}
            </div>
          </div>

          {/* Search Results */}
          <div className="flex-1 overflow-y-auto p-4">
            {searchResults.length === 0 && !isSearching && (
              <div className="text-center text-slate-400 mt-8">
                <Search className="w-12 h-12 mx-auto mb-2 opacity-50" />
                <p>Aramaya başlayın</p>
              </div>
            )}

            {isSearching && (
              <div className="text-center text-slate-400 mt-8">
                <div className="animate-spin w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full mx-auto"></div>
                <p className="mt-2">Aranıyor...</p>
              </div>
            )}

            <div className="space-y-2">
              {searchResults.map((result) => {
                // Map API response to variant format
                const variant = {
                  variantId: result.variantId,
                  sku: result.sku,
                  barcode: result.barcode,
                  name: result.name,
                  variantName: result.name,
                  brand: result.brand,
                  brandId: result.brandId,
                  brandCode: result.brandCode,
                  brandLogoUrl: result.brandLogoUrl,
                  isBrandActive: result.isBrandActive,
                  stock: result.available || 0,
                  available: result.available || 0,
                  price: result.price || 0,
                  oemRefs: result.oemRefs || [],
                  isCompatible: result.isCompatible,
                  fitmentPriority: result.fitmentPriority,
                  matchType: result.matchType,
                };
                
                return (
                    <div
                      key={result.variantId}
                      className="border border-slate-200 rounded-lg p-3 hover:bg-blue-50 hover:border-blue-300 cursor-pointer transition-colors"
                      onDoubleClick={() => addToCart(variant)}
                    >
                      <div className="flex justify-between items-start mb-1">
                        <div className="font-medium text-slate-800">{result.name}</div>
                        <div className="text-xs bg-slate-100 px-2 py-1 rounded">
                          Stok: {result.available || 0}
                        </div>
                      </div>
                      <div className="text-sm text-slate-600">{result.sku}</div>
                      {result.oemRefs && result.oemRefs.length > 0 && (
                        <div className="text-xs text-blue-600 mt-1">
                          OEM: {result.oemRefs.join(", ")}
                        </div>
                      )}
                      <div className="flex gap-1 mt-2">
                        {result.matchType === "EQUIVALENT" && (
                          <span className="text-xs bg-orange-100 text-orange-800 px-2 py-0.5 rounded">
                            ⚡ Muadil
                          </span>
                        )}
                        {selectedEngineId && result.isCompatible && (
                          <span className="text-xs bg-green-100 text-green-800 px-2 py-0.5 rounded flex items-center gap-1">
                            <CheckCircle className="h-3 w-3" />
                            Araç uyumlu
                          </span>
                        )}
                      </div>
                    </div>
                );
              })}
            </div>
          </div>
        </div>

        {/* CENTER: Sales Lines */}
        <div className="flex-1 flex flex-col bg-white">
          <div className="p-4 border-b border-slate-200 bg-slate-50">
            <h2 className="font-semibold text-slate-700">Satış Kalemleri</h2>
          </div>

          <div className="flex-1 overflow-y-auto">
            {salesLines.length === 0 ? (
              <div className="text-center text-slate-400 mt-16">
                <Plus className="w-16 h-16 mx-auto mb-3 opacity-50" />
                <p className="text-lg">Satış kalemi ekleyiniz</p>
                <p className="text-sm">Barkod okutun veya OEM arayın</p>
              </div>
            ) : (
              <table className="w-full">
                <thead className="bg-slate-100 sticky top-0">
                  <tr className="text-left text-sm text-slate-600">
                    <th className="px-4 py-3">SKU / Ürün</th>
                    <th className="px-4 py-3 w-24">Miktar</th>
                    <th className="px-4 py-3 w-32">Birim Fiyat</th>
                    <th className="px-4 py-3 w-24">İskonto %</th>
                    <th className="px-4 py-3 w-32">Toplam</th>
                    <th className="px-4 py-3 w-16"></th>
                  </tr>
                </thead>
                <tbody>
                  {salesLines.map((line, index) => (
                    <CartLineRow
                      key={line.id}
                      line={line}
                      isSelected={index === selectedLineIndex}
                      onSelect={() => setSelectedLineIndex(index)}
                      onUpdateQuantity={(qty) => updateLine(line.id, "quantity", qty)}
                      onUpdatePrice={(price) => updateLine(line.id, "unitPrice", price)}
                      onUpdateDiscount={(disc) => updateLine(line.id, "discount", disc)}
                      onRemove={() => removeLine(line.id)}
                      onIncrement={() => updateLine(line.id, "quantity", line.quantity + 1)}
                      onDecrement={() => updateLine(line.id, "quantity", Math.max(1, line.quantity - 1))}
                    />
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {/* Totals */}
          {salesLines.length > 0 && (
            <div className="border-t-2 border-slate-300 bg-slate-50 p-4">
              <div className="flex justify-end space-y-2">
                <div className="w-64">
                  <div className="flex justify-between text-slate-600 mb-1">
                    <span>Ara Toplam:</span>
                    <span>₺{subtotal.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-slate-600 mb-1">
                    <span>Toplam İskonto:</span>
                    <span className="text-red-600">-₺{totalDiscount.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between text-2xl font-bold text-slate-800 pt-2 border-t border-slate-300">
                    <span>TOPLAM:</span>
                    <span>₺{grandTotal.toFixed(2)}</span>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* RIGHT: Payment Panel */}
        <div className="w-80 border-l border-slate-200 bg-white flex flex-col">
          <div className="p-4 border-b border-slate-200 bg-slate-50">
            <h2 className="font-semibold text-slate-700 mb-3">Ödeme Türü</h2>
            
            {/* Sale Type Toggle */}
            <div className="grid grid-cols-2 gap-2 mb-4">
              <button
                onClick={() => setSaleType("cash")}
                className={`px-4 py-3 rounded-lg font-medium transition-colors ${
                  saleType === "cash"
                    ? "bg-green-600 text-white"
                    : "bg-slate-100 text-slate-600 hover:bg-slate-200"
                }`}
              >
                Peşin Satış
              </button>
              <button
                onClick={() => setSaleType("credit")}
                className={`px-4 py-3 rounded-lg font-medium transition-colors ${
                  saleType === "credit"
                    ? "bg-purple-600 text-white"
                    : "bg-slate-100 text-slate-600 hover:bg-slate-200"
                }`}
              >
                Veresiye
              </button>
            </div>
          </div>

          {/* Payment Method (Cash Sale) */}
          {saleType === "cash" && (
            <>
              {/* Cashbox Management */}
              <div className="p-4 border-b border-slate-200">
                <div className="flex items-center justify-between mb-2">
                  <label className="block text-sm font-medium text-slate-700">
                    Kasa
                  </label>
                  <button
                    onClick={() => setShowCashboxSettings(!showCashboxSettings)}
                    className="text-slate-600 hover:text-slate-800"
                    title="Kasa Ayarları"
                  >
                    <Settings className="w-4 h-4" />
                  </button>
                </div>

                {showCashboxSettings && (
                  <div className="mb-3 p-3 bg-slate-50 rounded-lg border border-slate-200">
                    <h3 className="text-sm font-medium text-slate-700 mb-2">Yeni Kasa Oluştur</h3>
                    <div className="space-y-2">
                      <input
                        type="text"
                        placeholder="Kasa Adı (örn: Ana Kasa)"
                        value={newCashboxName}
                        onChange={(e) => setNewCashboxName(e.target.value)}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter') {
                            handleCreateCashbox();
                          }
                        }}
                        className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:ring-2 focus:ring-green-500"
                      />
                      <div className="flex gap-2">
                        <button
                          onClick={handleCreateCashbox}
                          disabled={createCashbox.isPending || !newCashboxName.trim()}
                          className="flex-1 px-3 py-2 bg-green-600 text-white text-sm rounded hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                          {createCashbox.isPending ? 'Oluşturuluyor...' : 'Oluştur'}
                        </button>
                        <button
                          onClick={() => {
                            setShowCashboxSettings(false);
                            setNewCashboxName("");
                          }}
                          className="px-3 py-2 bg-slate-200 text-slate-700 text-sm rounded hover:bg-slate-300"
                        >
                          İptal
                        </button>
                      </div>
                    </div>
                  </div>
                )}

                {cashboxesData?.items && cashboxesData.items.length > 0 ? (
                  <div className="space-y-1">
                    {cashboxesData.items.map((cashbox) => (
                      <div
                        key={cashbox.id}
                        className="flex items-center justify-between px-3 py-2 bg-green-50 border border-green-200 rounded text-sm"
                      >
                        <div>
                          <div className="font-medium text-green-900">{cashbox.name}</div>
                          <div className="text-xs text-green-600">{cashbox.code}</div>
                        </div>
                        {cashbox.isDefault && (
                          <span className="text-xs bg-green-600 text-white px-2 py-1 rounded">
                            Varsayılan
                          </span>
                        )}
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="text-sm text-amber-600 bg-amber-50 border border-amber-200 rounded p-3">
                    ⚠️ Ödeme alabilmek için kasa oluşturmalısınız.
                  </div>
                )}
              </div>

              <div className="p-4 border-b border-slate-200">
                <label className="block text-sm font-medium text-slate-700 mb-2">
                  Ödeme Şekli
                </label>
              <div className="space-y-2">
                <button
                  onClick={() => setPaymentMethod("cash")}
                  className={`w-full px-4 py-2 rounded-lg text-left transition-colors ${
                    paymentMethod === "cash"
                      ? "bg-green-100 border-2 border-green-600 text-green-800"
                      : "bg-slate-50 border border-slate-300 text-slate-600 hover:bg-slate-100"
                  }`}
                >
                  💵 Nakit
                </button>
                <button
                  onClick={() => setPaymentMethod("card")}
                  className={`w-full px-4 py-2 rounded-lg text-left transition-colors ${
                    paymentMethod === "card"
                      ? "bg-green-100 border-2 border-green-600 text-green-800"
                      : "bg-slate-50 border border-slate-300 text-slate-600 hover:bg-slate-100"
                  }`}
                >
                  💳 Kredi Kartı
                </button>
                <button
                  onClick={() => setPaymentMethod("bank")}
                  className={`w-full px-4 py-2 rounded-lg text-left transition-colors ${
                    paymentMethod === "bank"
                      ? "bg-green-100 border-2 border-green-600 text-green-800"
                      : "bg-slate-50 border border-slate-300 text-slate-600 hover:bg-slate-100"
                  }`}
                >
                  🏦 Banka Transferi
                </button>
              </div>
            </div>
            </>
          )}

          {/* Party Selection (Credit Sale) */}
          {saleType === "credit" && (
            <div className="p-4 border-b border-slate-200">
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Cari Seçimi
              </label>
              {!selectedParty ? (
                <button
                  onClick={() => setShowCustomerPicker(true)}
                  className="w-full px-4 py-3 bg-purple-600 text-white rounded-lg hover:bg-purple-700 font-semibold transition-all"
                >
                  Cari Seç (F3)
                </button>
              ) : (
                <div className="bg-purple-50 border border-purple-200 rounded-lg p-3">
                  <div className="flex justify-between items-start mb-2">
                    <div>
                      <div className="font-medium text-purple-900">{selectedParty.name}</div>
                      <div className="text-sm text-purple-600">{selectedParty.code}</div>
                    </div>
                    <button
                      onClick={() => setSelectedParty(null)}
                      className="text-purple-600 hover:text-purple-800"
                    >
                      <X className="w-4 h-4" />
                    </button>
                  </div>
                  <div className="text-sm">
                    <div className="flex justify-between mb-1">
                      <span className="text-slate-600">Bakiye:</span>
                      <span className={selectedParty.balance > 0 ? "text-red-600 font-medium" : "text-green-600"}>
                        ₺{Math.abs(selectedParty.balance).toFixed(2)}
                        {selectedParty.balance > 0 ? " (Borç)" : " (Alacak)"}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-600">Limit:</span>
                      <span className="text-slate-800">₺{selectedParty.creditLimit.toFixed(2)}</span>
                    </div>
                  </div>
                  {(selectedParty.balance + grandTotal) > selectedParty.creditLimit && (
                    <div className="mt-2 text-xs bg-orange-100 border border-orange-300 text-orange-800 px-2 py-1 rounded">
                      ⚠️ Limit aşılacak!
                    </div>
                  )}
                </div>
              )}
            </div>
          )}

          {/* Action Buttons */}
          <div className="flex-1"></div>
          <div className="p-4 space-y-3 border-t border-slate-200">
            <button
              onClick={handleSale}
              disabled={salesLines.length === 0 || isProcessing}
              className="w-full px-6 py-4 bg-gradient-to-r from-green-600 to-green-700 text-white rounded-xl font-bold text-lg hover:from-green-700 hover:to-green-800 disabled:opacity-50 disabled:cursor-not-allowed shadow-lg hover:shadow-xl transition-all"
            >
              {isProcessing ? (
                <div className="flex items-center justify-center gap-2">
                  <Loader2 className="w-6 h-6 animate-spin" />
                  <span>{processingStep}</span>
                </div>
              ) : (
                <>
                  <div className="flex items-center justify-center gap-2">
                    {saleType === "cash" ? <CreditCard className="w-6 h-6" /> : <FileText className="w-6 h-6" />}
                    <span>{saleType === "cash" ? "Fatura Kes" : "İrsaliye Kes"}</span>
                  </div>
                  <div className="text-sm opacity-90 mt-1">
                    <kbd className="bg-white/20 px-2 py-0.5 rounded">F9</kbd>
                  </div>
                </>
              )}
            </button>

            <button
              onClick={handleCancel}
              className="w-full px-6 py-3 bg-slate-200 text-slate-700 rounded-lg font-medium hover:bg-slate-300 transition-colors"
            >
              <div className="flex items-center justify-center gap-2">
                <X className="w-5 h-5" />
                <span>İptal</span>
              </div>
              <div className="text-xs opacity-70 mt-1">
                <kbd className="bg-slate-300 px-2 py-0.5 rounded">ESC</kbd>
              </div>
            </button>
          </div>

          {/* Keyboard Shortcuts */}
          <div className="p-4 bg-slate-800 text-white text-xs">
            <div className="space-y-1">
              <div><kbd className="bg-slate-700 px-1.5 py-0.5 rounded">F1</kbd> Barkod</div>
              <div><kbd className="bg-slate-700 px-1.5 py-0.5 rounded">F2</kbd> Arama</div>
              <div><kbd className="bg-slate-700 px-1.5 py-0.5 rounded">F3</kbd> Cari Seç</div>
              <div><kbd className="bg-slate-700 px-1.5 py-0.5 rounded">F4</kbd> İskonto</div>
              <div><kbd className="bg-slate-700 px-1.5 py-0.5 rounded">F6</kbd> Ödeme</div>
              <div><kbd className="bg-slate-700 px-1.5 py-0.5 rounded">F9</kbd> Satış</div>
              <div><kbd className="bg-slate-700 px-1.5 py-0.5 rounded">F10</kbd> Geçmiş</div>
              <div><kbd className="bg-slate-700 px-1.5 py-0.5 rounded">↑↓</kbd> Satır Seç</div>
              <div><kbd className="bg-slate-700 px-1.5 py-0.5 rounded">+/-</kbd> Miktar</div>
              <div><kbd className="bg-slate-700 px-1.5 py-0.5 rounded">Del</kbd> Sil</div>
              <div><kbd className="bg-slate-700 px-1.5 py-0.5 rounded">ESC</kbd> İptal</div>
            </div>
          </div>
        </div>

        {/* RECENT PANELS (F10 toggle) */}
        {showRecentPanel && (
          <div className="w-72 border-l border-slate-200 bg-slate-50 flex flex-col overflow-hidden">
            <div className="p-3 bg-slate-700 text-white flex items-center justify-between">
              <h3 className="font-semibold">📊 Son İşlemler</h3>
              <button 
                onClick={handleToggleRecent}
                className="hover:bg-slate-600 p-1 rounded"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
            
            {/* Recent Items */}
            <div className="border-b border-slate-300">
              <RecentItemsPanel
                items={recentItems}
                onAddToCart={(item) => addToCart(item)}
              />
            </div>
            
            {/* Recent Sales */}
            <div className="flex-1 overflow-hidden">
              <RecentSalesPanel />
            </div>
          </div>
        )}
      </div>

      {/* Customer Picker Modal */}
      <CustomerPickerModal
        isOpen={showCustomerPicker}
        onClose={() => setShowCustomerPicker(false)}
        onSelect={(party) => setSelectedParty(party)}
      />
    </div>
  );
}
