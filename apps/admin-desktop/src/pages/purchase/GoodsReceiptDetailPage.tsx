import { useParams, Link, useNavigate } from 'react-router-dom';
import { useGoodsReceipt, useReceiveGoodsReceipt } from '../../hooks/usePurchase';
import { Card } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { AlertCircle, CheckCircle2 } from 'lucide-react';

export function GoodsReceiptDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: receipt, isLoading } = useGoodsReceipt(id || null);
  const receiveMutation = useReceiveGoodsReceipt();

  const handleReceive = async () => {
    if (!receipt) return;
    await receiveMutation.mutateAsync(receipt.id);
  };

  if (isLoading) {
    return <div className="p-6">Loading...</div>;
  }

  if (!receipt) {
    return (
      <div className="p-6">
        <Card className="p-6">
          <AlertCircle className="h-6 w-6 text-red-600 mb-2" />
          <h2 className="text-lg font-semibold">Goods Receipt Not Found</h2>
        </Card>
      </div>
    );
  }

  const canReceive = receipt.status === 'DRAFT';

  return (
    <div className="p-6 max-w-6xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Goods Receipt {receipt.receiptNo}</h1>
          <p className="text-sm text-gray-600">{receipt.receiptDate}</p>
        </div>
        <StatusBadge status={receipt.status} />
      </div>

      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Receipt Information</h2>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="text-sm text-gray-600">GRN No</label>
            <p className="font-medium">{receipt.receiptNo}</p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Related PO</label>
            <p className="font-medium">
              <Link to={`/purchase-orders/${receipt.purchaseOrderId}`} className="text-blue-600 hover:underline">
                {receipt.purchaseOrderNo}
              </Link>
            </p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Status</label>
            <p className="font-medium">{receipt.status}</p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Receipt Date</label>
            <p className="font-medium">{receipt.receiptDate}</p>
          </div>
        </div>
      </Card>

      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Receipt Lines</h2>
        <table className="w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="text-left p-2 text-sm">SKU</th>
              <th className="text-left p-2 text-sm">Product</th>
              <th className="text-right p-2 text-sm">Qty</th>
              <th className="text-right p-2 text-sm">Unit Cost</th>
              <th className="text-right p-2 text-sm">Total</th>
            </tr>
          </thead>
          <tbody>
            {receipt.lines.map((line, idx) => (
              <tr key={idx} className="border-t">
                <td className="p-2 text-sm">{line.sku}</td>
                <td className="p-2 text-sm">{line.productName}</td>
                <td className="p-2 text-sm text-right">{line.quantity}</td>
                <td className="p-2 text-sm text-right">{line.unitCost.toFixed(2)}</td>
                <td className="p-2 text-sm text-right font-medium">
                  {(line.quantity * line.unitCost).toFixed(2)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>

      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Actions</h2>
        <div className="flex gap-3">
          {canReceive && (
            <Button onClick={handleReceive} disabled={receiveMutation.isPending}>
              <CheckCircle2 className="h-4 w-4 mr-2" />
              {receiveMutation.isPending ? 'Receiving...' : 'Receive GRN'}
            </Button>
          )}
          <Link to={`/purchase-orders/${receipt.purchaseOrderId}`}>
            <Button variant="ghost">View Purchase Order</Button>
          </Link>
        </div>
      </Card>

      {receipt.status === 'RECEIVED' && (
        <Card className="p-4 bg-green-50 border-green-200">
          <p className="text-sm text-green-800">
            ✅ Stock balances have been updated with the received quantities.
          </p>
          <Link to="/stock-balance" className="text-green-600 hover:underline text-sm">
            View Stock Balance →
          </Link>
        </Card>
      )}
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  const colorMap: Record<string, string> = {
    DRAFT: 'bg-gray-100 text-gray-800',
    RECEIVED: 'bg-green-100 text-green-800',
    CANCELLED: 'bg-red-100 text-red-800',
  };

  return (
    <span className={`px-3 py-1 rounded-full text-sm font-medium ${colorMap[status] || 'bg-gray-100'}`}>
      {status}
    </span>
  );
}
