import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAppContext } from '../../hooks/useAppContext';
import { AlertCircle, CheckCircle2, ChevronRight } from 'lucide-react';
import { Button } from '../../components/ui/button';
import { Card } from '../../components/ui/card';

// Import hooks
import { useParties } from '../../hooks/useParties';
import { useProductVariants } from '../../hooks/useProductVariants';
import {
  useCreatePurchaseOrder,
  useConfirmPurchaseOrder,
  useCreateGoodsReceipt,
  useReceiveGoodsReceipt,
} from '../../hooks/usePurchase';
import { useStockBalances } from '../../hooks/useStock';

interface WizardState {
  partyId: string | null;
  lines: Array<{ variantId: string; quantity: number; unitCost?: number; }>;
  orderId: string | null;
  orderNo: string | null;
  receiptId: string | null;
  receiptNo: string | null;
  totalAmount: number;
  currency: string;
}

export function PurchaseWizardPage() {
  const { activeBranchId, activeWarehouseId } = useAppContext();
  const navigate = useNavigate();
  const [currentStep, setCurrentStep] = useState(1);
  const [wizardData, setWizardData] = useState<WizardState>({
    partyId: null,
    lines: [],
    orderId: null,
    orderNo: null,
    receiptId: null,
    receiptNo: null,
    totalAmount: 0,
    currency: 'TRY',
  });

  // Check if context is set
  if (!activeBranchId || !activeWarehouseId) {
    return (
      <div className="p-6">
        <Card className="p-6">
          <div className="flex items-center gap-3 text-orange-600">
            <AlertCircle className="h-6 w-6" />
            <div>
              <h2 className="text-lg font-semibold">Branch & Warehouse Required</h2>
              <p className="text-sm text-gray-600 mt-1">
                Please select a Branch and Warehouse from the context bar above before starting the wizard.
              </p>
            </div>
          </div>
        </Card>
      </div>
    );
  }

  const steps = [
    { id: 1, name: 'Select Supplier', icon: '🏭' },
    { id: 2, name: 'Select Products', icon: '📦' },
    { id: 3, name: 'Create PO', icon: '📝' },
    { id: 4, name: 'Confirm PO', icon: '✅' },
    { id: 5, name: 'Create GRN', icon: '📥' },
    { id: 6, name: 'Receive GRN', icon: '✔️' },
    { id: 7, name: 'Verification', icon: '🎯' },
  ];

  return (
    <div className="p-6 max-w-6xl mx-auto">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold">Purchase Wizard</h1>
        <p className="text-sm text-gray-600 mt-1">
          Complete purchase flow from supplier selection to goods receipt
        </p>
      </div>

      {/* Step Indicator */}
      <div className="mb-8">
        <div className="flex items-center justify-between">
          {steps.map((step, idx) => (
            <div key={step.id} className="flex items-center">
              <div className="flex flex-col items-center">
                <div
                  className={`
                    w-10 h-10 rounded-full flex items-center justify-center text-lg
                    ${currentStep === step.id ? 'bg-blue-600 text-white' : 
                      currentStep > step.id ? 'bg-green-600 text-white' : 
                      'bg-gray-200 text-gray-600'}
                  `}
                >
                  {currentStep > step.id ? <CheckCircle2 className="h-5 w-5" /> : step.icon}
                </div>
                <span className="text-xs mt-1 text-center max-w-[80px]">{step.name}</span>
              </div>
              {idx < steps.length - 1 && (
                <ChevronRight className="h-5 w-5 text-gray-400 mx-2" />
              )}
            </div>
          ))}
        </div>
      </div>

      {/* Step Content */}
      <Card className="p-6">
        {currentStep === 1 && <Step1SelectSupplier wizardData={wizardData} setWizardData={setWizardData} setCurrentStep={setCurrentStep} />}
        {currentStep === 2 && <Step2SelectProducts wizardData={wizardData} setWizardData={setWizardData} setCurrentStep={setCurrentStep} />}
        {currentStep === 3 && <Step3CreatePO wizardData={wizardData} setWizardData={setWizardData} setCurrentStep={setCurrentStep} />}
        {currentStep === 4 && <Step4ConfirmPO wizardData={wizardData} setCurrentStep={setCurrentStep} />}
        {currentStep === 5 && <Step5CreateGRN wizardData={wizardData} setWizardData={setWizardData} setCurrentStep={setCurrentStep} />}
        {currentStep === 6 && <Step6ReceiveGRN wizardData={wizardData} setCurrentStep={setCurrentStep} />}
        {currentStep === 7 && <Step7Verification wizardData={wizardData} activeWarehouseId={activeWarehouseId} />}
      </Card>

      {/* Navigation Buttons */}
      <div className="flex justify-between mt-6">
        <Button
          variant="outline"
          onClick={() => setCurrentStep(Math.max(1, currentStep - 1))}
          disabled={currentStep === 1}
        >
          Previous
        </Button>
        
        <Button
          variant="ghost"
          onClick={() => navigate('/purchase-orders')}
        >
          Exit Wizard
        </Button>
      </div>
    </div>
  );
}

// Step Components
function Step1SelectSupplier({ wizardData, setWizardData, setCurrentStep }: any) {
  const { data: partiesResult, isLoading } = useParties(undefined, 1, 100);
  const [selectedId, setSelectedId] = useState<string | null>(wizardData.partyId);

  const suppliers = partiesResult?.items?.filter((p: any) => p.isSupplier) || [];

  const handleNext = () => {
    if (!selectedId) return;
    setWizardData({ ...wizardData, partyId: selectedId });
    setCurrentStep(2);
  };

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Select Supplier</h2>
      {isLoading ? (
        <p>Loading suppliers...</p>
      ) : (
        <div className="space-y-2 max-h-96 overflow-y-auto">
          {suppliers.map((supplier: any) => (
            <div
              key={supplier.id}
              className={`
                p-3 border rounded cursor-pointer hover:bg-gray-50
                ${selectedId === supplier.id ? 'border-blue-600 bg-blue-50' : ''}
              `}
              onClick={() => setSelectedId(supplier.id)}
            >
              <div className="font-medium">{supplier.name}</div>
              <div className="text-sm text-gray-600">Code: {supplier.code} | Currency: {supplier.currency}</div>
            </div>
          ))}
        </div>
      )}
      <div className="flex justify-end mt-4">
        <Button onClick={handleNext} disabled={!selectedId}>
          Next
        </Button>
      </div>
    </div>
  );
}

function Step2SelectProducts({ wizardData, setWizardData, setCurrentStep }: any) {
  const { data: variantsResult, isLoading } = useProductVariants(undefined, 1, 100);
  const [selectedLines, setSelectedLines] = useState<any[]>(wizardData.lines);

  const variants = variantsResult?.items || [];

  const handleToggleVariant = (variant: any) => {
    const exists = selectedLines.find(l => l.variantId === variant.id);
    if (exists) {
      setSelectedLines(selectedLines.filter(l => l.variantId !== variant.id));
    } else {
      setSelectedLines([...selectedLines, { 
        variantId: variant.id, 
        quantity: 1, 
        unitCost: variant.price || 0 
      }]);
    }
  };

  const handleQuantityChange = (variantId: string, quantity: number) => {
    setSelectedLines(selectedLines.map(l => 
      l.variantId === variantId ? { ...l, quantity } : l
    ));
  };

  const handleCostChange = (variantId: string, unitCost: number) => {
    setSelectedLines(selectedLines.map(l => 
      l.variantId === variantId ? { ...l, unitCost } : l
    ));
  };

  const handleNext = () => {
    if (selectedLines.length === 0) return;
    const total = selectedLines.reduce((sum, l) => sum + (l.quantity * (l.unitCost || 0)), 0);
    setWizardData({ ...wizardData, lines: selectedLines, totalAmount: total });
    setCurrentStep(3);
  };

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Select Products</h2>
      {isLoading ? (
        <p>Loading products...</p>
      ) : (
        <div className="space-y-2 max-h-96 overflow-y-auto">
          {variants.map((variant: any) => {
            const selected = selectedLines.find(l => l.variantId === variant.id);
            return (
              <div
                key={variant.id}
                className={`p-3 border rounded ${selected ? 'border-blue-600 bg-blue-50' : ''}`}
              >
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <div className="font-medium">{variant.productName} - {variant.variantName}</div>
                    <div className="text-sm text-gray-600">SKU: {variant.sku} | Suggested: {variant.price} {variant.currency}</div>
                  </div>
                  {selected ? (
                    <div className="flex items-center gap-2">
                      <div className="flex flex-col gap-1">
                        <input
                          type="number"
                          min="1"
                          value={selected.quantity}
                          onChange={(e) => handleQuantityChange(variant.id, parseInt(e.target.value) || 1)}
                          className="w-20 px-2 py-1 border rounded text-sm"
                          placeholder="Qty"
                        />
                        <input
                          type="number"
                          min="0"
                          step="0.01"
                          value={selected.unitCost || 0}
                          onChange={(e) => handleCostChange(variant.id, parseFloat(e.target.value) || 0)}
                          className="w-20 px-2 py-1 border rounded text-sm"
                          placeholder="Cost"
                        />
                      </div>
                      <Button size="sm" variant="ghost" onClick={() => handleToggleVariant(variant)}>
                        Remove
                      </Button>
                    </div>
                  ) : (
                    <Button size="sm" onClick={() => handleToggleVariant(variant)}>
                      Add
                    </Button>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}
      <div className="flex justify-end mt-4">
        <Button onClick={handleNext} disabled={selectedLines.length === 0}>
          Next ({selectedLines.length} items)
        </Button>
      </div>
    </div>
  );
}

function Step3CreatePO({ wizardData, setWizardData, setCurrentStep }: any) {
  const { activeBranchId, activeWarehouseId } = useAppContext();
  const createPOMutation = useCreatePurchaseOrder();

  const handleCreate = async () => {
    const poData = {
      branchId: activeBranchId!,
      warehouseId: activeWarehouseId!,
      partyId: wizardData.partyId,
      issueDate: new Date().toISOString().split('T')[0],
      currency: wizardData.currency,
      lines: wizardData.lines.map((l: any) => ({
        variantId: l.variantId,
        quantity: l.quantity,
        unitCost: l.unitCost || 0,
      })),
    };

    const result = await createPOMutation.mutateAsync(poData);
    setWizardData({ 
      ...wizardData, 
      orderId: result.id, 
      orderNo: result.poNo 
    });
    setCurrentStep(4);
  };

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Create Purchase Order</h2>
      <div className="space-y-2 mb-4">
        <p><strong>Supplier ID:</strong> {wizardData.partyId}</p>
        <p><strong>Lines:</strong> {wizardData.lines.length}</p>
        <p><strong>Total:</strong> {wizardData.totalAmount.toFixed(2)} {wizardData.currency}</p>
      </div>
      <div className="flex justify-end gap-2">
        <Button onClick={handleCreate} disabled={createPOMutation.isPending}>
          {createPOMutation.isPending ? 'Creating...' : 'Create Purchase Order'}
        </Button>
      </div>
      {wizardData.orderId && (
        <div className="mt-4 p-3 bg-green-50 border border-green-200 rounded">
          <CheckCircle2 className="h-5 w-5 text-green-600 inline mr-2" />
          PO created: <Link to={`/purchase-orders/${wizardData.orderId}`} className="text-blue-600 underline">{wizardData.orderNo}</Link>
        </div>
      )}
    </div>
  );
}

function Step4ConfirmPO({ wizardData, setCurrentStep }: any) {
  const confirmMutation = useConfirmPurchaseOrder();

  const handleConfirm = async () => {
    await confirmMutation.mutateAsync(wizardData.orderId);
    setCurrentStep(5);
  };

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Confirm Purchase Order</h2>
      <p className="mb-4">PO <strong>{wizardData.orderNo}</strong> is ready to be confirmed.</p>
      <div className="flex justify-end gap-2">
        <Button onClick={handleConfirm} disabled={confirmMutation.isPending}>
          {confirmMutation.isPending ? 'Confirming...' : 'Confirm PO'}
        </Button>
      </div>
    </div>
  );
}

function Step5CreateGRN({ wizardData, setWizardData, setCurrentStep }: any) {
  const createGRNMutation = useCreateGoodsReceipt();

  const handleCreate = async () => {
    const grnData = {
      purchaseOrderId: wizardData.orderId,
      lines: wizardData.lines.map((l: any) => ({
        variantId: l.variantId,
        quantity: l.quantity,
      })),
    };

    const result = await createGRNMutation.mutateAsync(grnData);
    setWizardData({ 
      ...wizardData, 
      receiptId: result.id, 
      receiptNo: result.grnNo 
    });
    setCurrentStep(6);
  };

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Create Goods Receipt</h2>
      <p className="mb-4">Create GRN for PO <strong>{wizardData.orderNo}</strong></p>
      <div className="flex justify-end gap-2">
        <Button onClick={handleCreate} disabled={createGRNMutation.isPending}>
          {createGRNMutation.isPending ? 'Creating...' : 'Create GRN'}
        </Button>
      </div>
      {wizardData.receiptId && (
        <div className="mt-4 p-3 bg-green-50 border border-green-200 rounded">
          <CheckCircle2 className="h-5 w-5 text-green-600 inline mr-2" />
          GRN created: <Link to={`/goods-receipts/${wizardData.receiptId}`} className="text-blue-600 underline">{wizardData.receiptNo}</Link>
        </div>
      )}
    </div>
  );
}

function Step6ReceiveGRN({ wizardData, setCurrentStep }: any) {
  const receiveMutation = useReceiveGoodsReceipt();

  const handleReceive = async () => {
    await receiveMutation.mutateAsync(wizardData.receiptId);
    setCurrentStep(7);
  };

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Receive Goods Receipt</h2>
      <p className="mb-4">Mark GRN <strong>{wizardData.receiptNo}</strong> as received. This will update stock balances.</p>
      <div className="flex justify-end gap-2">
        <Button onClick={handleReceive} disabled={receiveMutation.isPending}>
          {receiveMutation.isPending ? 'Receiving...' : 'Receive Now'}
        </Button>
      </div>
    </div>
  );
}

function Step7Verification({ wizardData, activeWarehouseId }: any) {
  const { data: stockData } = useStockBalances({ warehouseId: activeWarehouseId });

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4 text-green-600">✅ Purchase Flow Complete!</h2>
      <div className="space-y-2">
        <p>✅ Supplier selected</p>
        <p>✅ {wizardData.lines.length} product(s) added</p>
        <p>✅ Purchase Order created: <Link to={`/purchase-orders/${wizardData.orderId}`} className="text-blue-600 underline">{wizardData.orderNo}</Link></p>
        <p>✅ PO confirmed</p>
        <p>✅ Goods Receipt created: <Link to={`/goods-receipts/${wizardData.receiptId}`} className="text-blue-600 underline">{wizardData.receiptNo}</Link></p>
        <p>✅ GRN received</p>
      </div>
      <div className="mt-6 p-4 bg-blue-50 border border-blue-200 rounded">
        <h3 className="font-semibold mb-2">Expected Results:</h3>
        <ul className="text-sm space-y-1">
          <li>• Stock increased by received quantities (Warehouse: {activeWarehouseId})</li>
          <li>• PO lines show ReceivedQty = Qty</li>
          <li>• Party ledger shows payable of {wizardData.totalAmount.toFixed(2)} {wizardData.currency}</li>
        </ul>
        {stockData && (
          <div className="mt-3">
            <Link to="/stock-balance" className="text-blue-600 underline text-sm">
              View Stock Balance →
            </Link>
          </div>
        )}
      </div>
    </div>
  );
}
