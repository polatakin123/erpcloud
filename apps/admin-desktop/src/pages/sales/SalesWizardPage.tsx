import { useState, useEffect } from 'react';
import { useNavigate, Link, useLocation } from 'react-router-dom';
import { useAppContext } from '../../hooks/useAppContext';
import { AlertCircle, CheckCircle2, ChevronRight } from 'lucide-react';
import { Button } from '../../components/ui/button';
import { Card } from '../../components/ui/card';

// Import hooks
import { useParties } from '../../hooks/useParties';
import { useProductVariants } from '../../hooks/useProductVariants';
import {
  useCreateSalesOrder,
  useConfirmSalesOrder,
  useCreateShipment,
  useShipShipment,
  useCreateInvoiceFromShipment,
  useIssueInvoice,
} from '../../hooks/useSales';
import { useCashboxes, useCreatePayment } from '../../hooks/useCashBank';

interface WizardState {
  partyId: string | null;
  lines: Array<{ variantId: string; quantity: number; price: number; }>;
  orderId: string | null;
  orderNo: string | null;
  shipmentId: string | null;
  shipmentNo: string | null;
  invoiceId: string | null;
  invoiceNo: string | null;
  paymentId: string | null;
  totalAmount: number;
  currency: string;
}

export function SalesWizardPage() {
  const { activeBranchId, activeWarehouseId } = useAppContext();
  const navigate = useNavigate();
  const location = useLocation();
  const [currentStep, setCurrentStep] = useState(1);
  const [wizardData, setWizardData] = useState<WizardState>({
    partyId: null,
    lines: [],
    orderId: null,
    orderNo: null,
    shipmentId: null,
    shipmentNo: null,
    invoiceId: null,
    invoiceNo: null,
    paymentId: null,
    totalAmount: 0,
    currency: 'TRY',
  });

  // Check for pre-selected variant from Fast Search
  const preselectedVariantId = location.state?.selectedVariantId;

  // If variant pre-selected, skip to step 2 with that variant
  useEffect(() => {
    if (preselectedVariantId && wizardData.lines.length === 0) {
      setCurrentStep(2);
    }
  }, [preselectedVariantId]);

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
    { id: 1, name: 'Select Customer', icon: '👤' },
    { id: 2, name: 'Select Products', icon: '📦' },
    { id: 3, name: 'Create Order', icon: '📝' },
    { id: 4, name: 'Confirm Order', icon: '✅' },
    { id: 5, name: 'Create Shipment', icon: '🚚' },
    { id: 6, name: 'Ship Shipment', icon: '📤' },
    { id: 7, name: 'Create Invoice', icon: '🧾' },
    { id: 8, name: 'Issue Invoice', icon: '✔️' },
    { id: 9, name: 'Create Payment', icon: '💰' },
    { id: 10, name: 'Verification', icon: '🎯' },
  ];

  return (
    <div className="p-6 max-w-6xl mx-auto">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold">Sales Wizard</h1>
        <p className="text-sm text-gray-600 mt-1">
          Complete sales flow from customer selection to payment
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
        {currentStep === 1 && <Step1SelectCustomer wizardData={wizardData} setWizardData={setWizardData} setCurrentStep={setCurrentStep} />}
        {currentStep === 2 && <Step2SelectProducts wizardData={wizardData} setWizardData={setWizardData} setCurrentStep={setCurrentStep} preselectedVariantId={preselectedVariantId} />}
        {currentStep === 3 && <Step3CreateOrder wizardData={wizardData} setWizardData={setWizardData} setCurrentStep={setCurrentStep} />}
        {currentStep === 4 && <Step4ConfirmOrder wizardData={wizardData} setWizardData={setWizardData} setCurrentStep={setCurrentStep} />}
        {currentStep === 5 && <Step5CreateShipment wizardData={wizardData} setWizardData={setWizardData} setCurrentStep={setCurrentStep} />}
        {currentStep === 6 && <Step6ShipShipment wizardData={wizardData} setWizardData={setWizardData} setCurrentStep={setCurrentStep} />}
        {currentStep === 7 && <Step7CreateInvoice wizardData={wizardData} setWizardData={setWizardData} setCurrentStep={setCurrentStep} />}
        {currentStep === 8 && <Step8IssueInvoice wizardData={wizardData} setWizardData={setWizardData} setCurrentStep={setCurrentStep} />}
        {currentStep === 9 && <Step9CreatePayment wizardData={wizardData} setWizardData={setWizardData} setCurrentStep={setCurrentStep} />}
        {currentStep === 10 && <Step10Verification wizardData={wizardData} />}
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
          onClick={() => navigate('/sales-orders')}
        >
          Exit Wizard
        </Button>
      </div>
    </div>
  );
}

// Step Components
function Step1SelectCustomer({ wizardData, setWizardData, setCurrentStep }: any) {
  const { data: partiesResult, isLoading } = useParties(undefined, 1, 100);
  const [selectedId, setSelectedId] = useState<string | null>(wizardData.partyId);

  const customers = partiesResult?.items?.filter((p: any) => p.isCustomer) || [];

  const handleNext = () => {
    if (!selectedId) return;
    setWizardData({ ...wizardData, partyId: selectedId });
    setCurrentStep(2);
  };

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Select Customer</h2>
      {isLoading ? (
        <p>Loading customers...</p>
      ) : (
        <div className="space-y-2 max-h-96 overflow-y-auto">
          {customers.map((customer: any) => (
            <div
              key={customer.id}
              className={`
                p-3 border rounded cursor-pointer hover:bg-gray-50
                ${selectedId === customer.id ? 'border-blue-600 bg-blue-50' : ''}
              `}
              onClick={() => setSelectedId(customer.id)}
            >
              <div className="font-medium">{customer.name}</div>
              <div className="text-sm text-gray-600">Code: {customer.code} | Currency: {customer.currency}</div>
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

function Step2SelectProducts({ wizardData, setWizardData, setCurrentStep, preselectedVariantId }: any) {
  const { data: variantsResult, isLoading } = useProductVariants(undefined, 1, 100);
  const [selectedLines, setSelectedLines] = useState<any[]>(wizardData.lines);

  const variants = variantsResult?.items || [];

  // Auto-select pre-selected variant from Fast Search
  useEffect(() => {
    if (preselectedVariantId && !isLoading && variants.length > 0) {
      const variant = variants.find((v: any) => v.id === preselectedVariantId);
      if (variant && !selectedLines.find(l => l.variantId === variant.id)) {
        setSelectedLines([{ variantId: variant.id, quantity: 1, price: variant.price }]);
      }
    }
  }, [preselectedVariantId, isLoading, variants]);

  const handleToggleVariant = (variant: any) => {
    const exists = selectedLines.find(l => l.variantId === variant.id);
    if (exists) {
      setSelectedLines(selectedLines.filter(l => l.variantId !== variant.id));
    } else {
      setSelectedLines([...selectedLines, { variantId: variant.id, quantity: 1, price: variant.price }]);
    }
  };

  const handleQuantityChange = (variantId: string, quantity: number) => {
    setSelectedLines(selectedLines.map(l => 
      l.variantId === variantId ? { ...l, quantity } : l
    ));
  };

  const handleNext = () => {
    if (selectedLines.length === 0) return;
    const total = selectedLines.reduce((sum, l) => sum + (l.quantity * l.price), 0);
    setWizardData({ ...wizardData, lines: selectedLines, totalAmount: total });
    setCurrentStep(3);
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold">Select Products</h2>
        {preselectedVariantId && (
          <div className="text-sm text-green-600 font-medium">
            ⚡ Product pre-selected from Fast Search
          </div>
        )}
      </div>
      {isLoading ? (
        <p>Loading products...</p>
      ) : (
        <div className="space-y-2 max-h-96 overflow-y-auto">
          {variants.map((variant: any) => {
            const selected = selectedLines.find(l => l.variantId === variant.id);
            const isPreselected = variant.id === preselectedVariantId;
            return (
              <div
                key={variant.id}
                className={`p-3 border rounded ${selected ? 'border-blue-600 bg-blue-50' : ''} ${isPreselected ? 'ring-2 ring-green-500' : ''}`}
              >
                <div className="flex items-center justify-between">
                  <div className="flex-1">
                    <div className="font-medium">
                      {variant.productName} - {variant.variantName}
                      {isPreselected && <span className="ml-2 text-xs text-green-600 font-semibold">⚡ FROM FAST SEARCH</span>}
                    </div>
                    <div className="text-sm text-gray-600">SKU: {variant.sku} | Price: {variant.price} {variant.currency}</div>
                  </div>
                  {selected ? (
                    <div className="flex items-center gap-2">
                      <input
                        type="number"
                        min="1"
                        value={selected.quantity}
                        onChange={(e) => handleQuantityChange(variant.id, parseInt(e.target.value) || 1)}
                        className="w-20 px-2 py-1 border rounded"
                      />
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

function Step3CreateOrder({ wizardData, setWizardData, setCurrentStep }: any) {
  const { activeBranchId, activeWarehouseId } = useAppContext();
  const createOrderMutation = useCreateSalesOrder();

  const handleCreate = async () => {
    const orderData = {
      branchId: activeBranchId!,
      warehouseId: activeWarehouseId!,
      partyId: wizardData.partyId,
      issueDate: new Date().toISOString().split('T')[0],
      currency: wizardData.currency,
      lines: wizardData.lines.map((l: any) => ({
        variantId: l.variantId,
        quantity: l.quantity,
        price: l.price,
      })),
    };

    const result = await createOrderMutation.mutateAsync(orderData);
    setWizardData({ 
      ...wizardData, 
      orderId: result.id, 
      orderNo: result.orderNo 
    });
    setCurrentStep(4);
  };

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Create Sales Order</h2>
      <div className="space-y-2 mb-4">
        <p><strong>Customer ID:</strong> {wizardData.partyId}</p>
        <p><strong>Lines:</strong> {wizardData.lines.length}</p>
        <p><strong>Total:</strong> {wizardData.totalAmount.toFixed(2)} {wizardData.currency}</p>
      </div>
      <div className="flex justify-end gap-2">
        <Button onClick={handleCreate} disabled={createOrderMutation.isPending}>
          {createOrderMutation.isPending ? 'Creating...' : 'Create Order'}
        </Button>
      </div>
      {wizardData.orderId && (
        <div className="mt-4 p-3 bg-green-50 border border-green-200 rounded">
          <CheckCircle2 className="h-5 w-5 text-green-600 inline mr-2" />
          Order created: <Link to={`/sales-orders/${wizardData.orderId}`} className="text-blue-600 underline">{wizardData.orderNo}</Link>
        </div>
      )}
    </div>
  );
}

function Step4ConfirmOrder({ wizardData, setCurrentStep }: any) {
  const confirmMutation = useConfirmSalesOrder();

  const handleConfirm = async () => {
    await confirmMutation.mutateAsync(wizardData.orderId);
    setCurrentStep(5);
  };

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Confirm Sales Order</h2>
      <p className="mb-4">Order <strong>{wizardData.orderNo}</strong> is ready to be confirmed.</p>
      <div className="flex justify-end gap-2">
        <Button onClick={handleConfirm} disabled={confirmMutation.isPending}>
          {confirmMutation.isPending ? 'Confirming...' : 'Confirm Order'}
        </Button>
      </div>
    </div>
  );
}

function Step5CreateShipment({ wizardData, setWizardData, setCurrentStep }: any) {
  const createShipmentMutation = useCreateShipment();

  const handleCreate = async () => {
    const shipmentData = {
      orderId: wizardData.orderId,
      lines: wizardData.lines.map((l: any) => ({
        variantId: l.variantId,
        quantity: l.quantity,
      })),
    };

    const result = await createShipmentMutation.mutateAsync(shipmentData);
    setWizardData({ 
      ...wizardData, 
      shipmentId: result.id, 
      shipmentNo: result.shipmentNo 
    });
    setCurrentStep(6);
  };

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Create Shipment</h2>
      <p className="mb-4">Create shipment for order <strong>{wizardData.orderNo}</strong></p>
      <div className="flex justify-end gap-2">
        <Button onClick={handleCreate} disabled={createShipmentMutation.isPending}>
          {createShipmentMutation.isPending ? 'Creating...' : 'Create Shipment'}
        </Button>
      </div>
      {wizardData.shipmentId && (
        <div className="mt-4 p-3 bg-green-50 border border-green-200 rounded">
          <CheckCircle2 className="h-5 w-5 text-green-600 inline mr-2" />
          Shipment created: <Link to={`/shipments/${wizardData.shipmentId}`} className="text-blue-600 underline">{wizardData.shipmentNo}</Link>
        </div>
      )}
    </div>
  );
}

function Step6ShipShipment({ wizardData, setCurrentStep }: any) {
  const shipMutation = useShipShipment();

  const handleShip = async () => {
    await shipMutation.mutateAsync(wizardData.shipmentId);
    setCurrentStep(7);
  };

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Ship Shipment</h2>
      <p className="mb-4">Mark shipment <strong>{wizardData.shipmentNo}</strong> as shipped.</p>
      <div className="flex justify-end gap-2">
        <Button onClick={handleShip} disabled={shipMutation.isPending}>
          {shipMutation.isPending ? 'Shipping...' : 'Ship Now'}
        </Button>
      </div>
    </div>
  );
}

function Step7CreateInvoice({ wizardData, setWizardData, setCurrentStep }: any) {
  const createInvoiceMutation = useCreateInvoiceFromShipment();

  const handleCreate = async () => {
    const result = await createInvoiceMutation.mutateAsync(wizardData.shipmentId);
    setWizardData({ 
      ...wizardData, 
      invoiceId: result.id, 
      invoiceNo: result.invoiceNo 
    });
    setCurrentStep(8);
  };

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Create Invoice</h2>
      <p className="mb-4">Create invoice from shipment <strong>{wizardData.shipmentNo}</strong></p>
      <div className="flex justify-end gap-2">
        <Button onClick={handleCreate} disabled={createInvoiceMutation.isPending}>
          {createInvoiceMutation.isPending ? 'Creating...' : 'Create Invoice'}
        </Button>
      </div>
      {wizardData.invoiceId && (
        <div className="mt-4 p-3 bg-green-50 border border-green-200 rounded">
          <CheckCircle2 className="h-5 w-5 text-green-600 inline mr-2" />
          Invoice created: <Link to={`/invoices/${wizardData.invoiceId}`} className="text-blue-600 underline">{wizardData.invoiceNo}</Link>
        </div>
      )}
    </div>
  );
}

function Step8IssueInvoice({ wizardData, setCurrentStep }: any) {
  const issueMutation = useIssueInvoice();

  const handleIssue = async () => {
    await issueMutation.mutateAsync(wizardData.invoiceId);
    setCurrentStep(9);
  };

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Issue Invoice</h2>
      <p className="mb-4">Issue invoice <strong>{wizardData.invoiceNo}</strong></p>
      <div className="flex justify-end gap-2">
        <Button onClick={handleIssue} disabled={issueMutation.isPending}>
          {issueMutation.isPending ? 'Issuing...' : 'Issue Invoice'}
        </Button>
      </div>
    </div>
  );
}

function Step9CreatePayment({ wizardData, setWizardData, setCurrentStep }: any) {
  const { data: cashboxes } = useCashboxes();
  const createPaymentMutation = useCreatePayment();
  const [selectedCashboxId, setSelectedCashboxId] = useState<string | null>(null);

  const handleCreate = async () => {
    if (!selectedCashboxId) return;

    const paymentData = {
      partyId: wizardData.partyId,
      amount: wizardData.totalAmount,
      currency: wizardData.currency,
      paymentDate: new Date().toISOString().split('T')[0],
      cashboxId: selectedCashboxId,
    };

    const result = await createPaymentMutation.mutateAsync(paymentData);
    setWizardData({ ...wizardData, paymentId: result.id });
    setCurrentStep(10);
  };

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Create Payment</h2>
      <p className="mb-4">Amount: <strong>{wizardData.totalAmount.toFixed(2)} {wizardData.currency}</strong></p>
      
      <div className="space-y-2 mb-4">
        <label className="block text-sm font-medium">Select Cashbox:</label>
        {cashboxes?.map((cb: any) => (
          <div
            key={cb.id}
            className={`p-3 border rounded cursor-pointer hover:bg-gray-50 ${selectedCashboxId === cb.id ? 'border-blue-600 bg-blue-50' : ''}`}
            onClick={() => setSelectedCashboxId(cb.id)}
          >
            {cb.name} ({cb.currency})
          </div>
        ))}
      </div>

      <div className="flex justify-end gap-2">
        <Button onClick={handleCreate} disabled={!selectedCashboxId || createPaymentMutation.isPending}>
          {createPaymentMutation.isPending ? 'Creating...' : 'Create Payment'}
        </Button>
      </div>
    </div>
  );
}

function Step10Verification({ wizardData }: any) {
  return (
    <div>
      <h2 className="text-lg font-semibold mb-4 text-green-600">✅ Sales Flow Complete!</h2>
      <div className="space-y-2">
        <p>✅ Customer selected</p>
        <p>✅ {wizardData.lines.length} product(s) added</p>
        <p>✅ Sales Order created: <Link to={`/sales-orders/${wizardData.orderId}`} className="text-blue-600 underline">{wizardData.orderNo}</Link></p>
        <p>✅ Order confirmed</p>
        <p>✅ Shipment created: <Link to={`/shipments/${wizardData.shipmentId}`} className="text-blue-600 underline">{wizardData.shipmentNo}</Link></p>
        <p>✅ Shipment shipped</p>
        <p>✅ Invoice created: <Link to={`/invoices/${wizardData.invoiceId}`} className="text-blue-600 underline">{wizardData.invoiceNo}</Link></p>
        <p>✅ Invoice issued</p>
        <p>✅ Payment recorded</p>
      </div>
      <div className="mt-6 p-4 bg-blue-50 border border-blue-200 rounded">
        <h3 className="font-semibold mb-2">Expected Results:</h3>
        <ul className="text-sm space-y-1">
          <li>• Stock decreased by shipped quantities</li>
          <li>• Party ledger shows receivable of {wizardData.totalAmount.toFixed(2)} {wizardData.currency}</li>
          <li>• Cashbox balance increased by {wizardData.totalAmount.toFixed(2)} {wizardData.currency}</li>
        </ul>
      </div>
    </div>
  );
}
