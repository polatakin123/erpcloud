import { useNavigate } from 'react-router-dom';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { CheckCircle2, XCircle, PlayCircle, FileText, Package, ShoppingCart } from 'lucide-react';

export function QAVerificationPage() {
  const navigate = useNavigate();

  return (
    <div className="p-6 max-w-6xl mx-auto">
      <h1 className="text-3xl font-bold mb-6">QA & Verification Panel</h1>

      {/* Quick Links */}
      <Card className="mb-6">
        <CardHeader>
          <CardTitle>Quick Actions</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <Button
              onClick={() => navigate('/sales/wizard')}
              className="h-20 flex flex-col items-center justify-center gap-2"
              variant="outline"
            >
              <ShoppingCart className="h-6 w-6" />
              <span>Sales Wizard</span>
            </Button>
            <Button
              onClick={() => navigate('/purchase/wizard')}
              className="h-20 flex flex-col items-center justify-center gap-2"
              variant="outline"
            >
              <Package className="h-6 w-6" />
              <span>Purchase Wizard</span>
            </Button>
            <Button
              onClick={() => navigate('/stock-ledger')}
              className="h-20 flex flex-col items-center justify-center gap-2"
              variant="outline"
            >
              <FileText className="h-6 w-6" />
              <span>Stock Ledger</span>
            </Button>
            <Button
              onClick={() => navigate('/party-ledger')}
              className="h-20 flex flex-col items-center justify-center gap-2"
              variant="outline"
            >
              <FileText className="h-6 w-6" />
              <span>Party Ledger</span>
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Happy Path Tests */}
      <Card className="mb-6">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <PlayCircle className="h-5 w-5" />
            Happy Path Testing Guide
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          {/* Sales Happy Path */}
          <div>
            <h3 className="font-semibold text-lg mb-3 text-blue-700">Sales Flow (Order → Shipment → Invoice → Payment)</h3>
            <ol className="list-decimal list-inside space-y-2 text-sm">
              <li>Navigate to <strong>Sales Wizard</strong></li>
              <li>Step 1: Select a customer</li>
              <li>Step 2-3: Add products (check stock reservation)</li>
              <li>Step 4: Create Sales Order → verify order created</li>
              <li>Step 5: Confirm Order → verify status = CONFIRMED</li>
              <li>Step 6: Create Shipment → verify shipment created</li>
              <li>Step 7: Ship Goods → <strong>verify stock decreased</strong></li>
              <li>Step 8: Create Invoice → verify invoice created</li>
              <li>Step 9: Issue Invoice → <strong>verify party ledger updated (receivable +)</strong></li>
              <li>Step 10: Record Payment → <strong>verify party balance decreased, cash/bank increased</strong></li>
              <li>Verification: Check Stock Ledger for negative movement</li>
              <li>Verification: Check Party Ledger for debit entry</li>
              <li>Verification: Check Cash/Bank Ledger for credit entry</li>
            </ol>
          </div>

          {/* Purchase Happy Path */}
          <div>
            <h3 className="font-semibold text-lg mb-3 text-green-700">Purchase Flow (PO → GRN → Payment)</h3>
            <ol className="list-decimal list-inside space-y-2 text-sm">
              <li>Navigate to <strong>Purchase Wizard</strong></li>
              <li>Step 1: Select a supplier</li>
              <li>Step 2: Add products with quantities + unit costs</li>
              <li>Step 3: Create PO → verify PO created (status = DRAFT)</li>
              <li>Step 4: Confirm PO → verify status = CONFIRMED</li>
              <li>Step 5: Create GRN → verify GRN created</li>
              <li>Step 6: Receive GRN → <strong>verify stock increased</strong></li>
              <li>Step 7: Verification → check stock balance link</li>
              <li>Verification: Check Stock Ledger for positive movement</li>
              <li>Verification: Navigate to PO detail → check progress bar (should be 100%)</li>
            </ol>
          </div>
        </CardContent>
      </Card>

      {/* Verification Checklist */}
      <Card className="mb-6">
        <CardHeader>
          <CardTitle>Verification Checklist</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div className="border-l-4 border-blue-500 pl-4 py-2">
              <h4 className="font-semibold mb-2">After Sales Flow:</h4>
              <ul className="space-y-1 text-sm">
                <li className="flex items-center gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-600" />
                  Stock balance decreased by shipped quantity
                </li>
                <li className="flex items-center gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-600" />
                  Party ledger shows receivable (debit entry)
                </li>
                <li className="flex items-center gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-600" />
                  Cash/Bank ledger shows payment received (credit entry)
                </li>
                <li className="flex items-center gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-600" />
                  Stock ledger has SHIPMENT movement record
                </li>
              </ul>
            </div>

            <div className="border-l-4 border-green-500 pl-4 py-2">
              <h4 className="font-semibold mb-2">After Purchase Flow:</h4>
              <ul className="space-y-1 text-sm">
                <li className="flex items-center gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-600" />
                  Stock balance increased by received quantity
                </li>
                <li className="flex items-center gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-600" />
                  PO detail page shows 100% progress bar
                </li>
                <li className="flex items-center gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-600" />
                  Stock ledger has RECEIPT movement record
                </li>
              </ul>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Known Limitations */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <XCircle className="h-5 w-5 text-orange-600" />
            Known Limitations (Not Implemented)
          </CardTitle>
        </CardHeader>
        <CardContent>
          <ul className="space-y-2 text-sm">
            <li className="flex items-start gap-2">
              <span className="text-orange-600 mt-1">•</span>
              <span><strong>Payment Aging/Matching:</strong> Payments not matched to specific invoices yet (manual reconciliation required)</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-orange-600 mt-1">•</span>
              <span><strong>Auto-Reversal:</strong> Cancelling a shipment does NOT auto-reverse stock (manual adjustment needed)</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-orange-600 mt-1">•</span>
              <span><strong>Return Flows:</strong> Sales returns and purchase returns not implemented (use manual stock adjustments)</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-orange-600 mt-1">•</span>
              <span><strong>Partial Receipts:</strong> Cannot edit GRN quantities before receiving (must match PO exactly or cancel and recreate)</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-orange-600 mt-1">•</span>
              <span><strong>Multi-Currency:</strong> All calculations assume single currency per transaction (no FX conversion)</span>
            </li>
            <li className="flex items-start gap-2">
              <span className="text-orange-600 mt-1">•</span>
              <span><strong>Stock Reservation Expiry:</strong> Reserved stock doesn't auto-release after timeout (manual cleanup needed)</span>
            </li>
          </ul>
        </CardContent>
      </Card>

      {/* Quick Export Links */}
      <div className="mt-6 flex gap-4">
        <Button variant="outline" onClick={() => navigate('/stock-ledger')}>
          View Stock Ledger & Export CSV
        </Button>
        <Button variant="outline" onClick={() => navigate('/party-ledger')}>
          View Party Ledger & Export CSV
        </Button>
        <Button variant="outline" onClick={() => navigate('/cash-bank-ledger')}>
          View Cash/Bank Ledger & Export CSV
        </Button>
      </div>
    </div>
  );
}
