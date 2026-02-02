import { useParams, Link, useNavigate } from 'react-router-dom';
import { usePurchaseOrder, useConfirmPurchaseOrder } from '../../hooks/usePurchase';
import { Card } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { AlertCircle, Package, CheckCircle2 } from 'lucide-react';

export function PurchaseOrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: order, isLoading } = usePurchaseOrder(id || null);
  const confirmMutation = useConfirmPurchaseOrder();

  const handleConfirm = async () => {
    if (!order) return;
    await confirmMutation.mutateAsync(order.id);
  };

  const handleCreateGRN = () => {
    navigate('/purchase/wizard', { state: { orderId: id, step: 5 } });
  };

  if (isLoading) {
    return <div className="p-6">Loading...</div>;
  }

  if (!order) {
    return (
      <div className="p-6">
        <Card className="p-6">
          <AlertCircle className="h-6 w-6 text-red-600 mb-2" />
          <h2 className="text-lg font-semibold">Purchase Order Not Found</h2>
        </Card>
      </div>
    );
  }

  const canConfirm = order.status === 'DRAFT';
  const canCreateGRN = order.status === 'CONFIRMED' || order.status === 'RECEIVED';

  // Calculate completion percentage
  const totalQty = order.lines.reduce((sum, l) => sum + l.quantity, 0);
  const receivedQty = order.lines.reduce((sum, l) => sum + (l.receivedQty || 0), 0);
  const completionPct = totalQty > 0 ? (receivedQty / totalQty) * 100 : 0;

  return (
    <div className="p-6 max-w-6xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Purchase Order {order.orderNo}</h1>
          <p className="text-sm text-gray-600">{order.partyName} | {order.issueDate}</p>
        </div>
        <StatusBadge status={order.status} />
      </div>

      {/* Progress Bar */}
      <Card className="p-4">
        <div className="flex items-center justify-between mb-2">
          <span className="text-sm font-medium">Order Completion</span>
          <span className="text-sm text-gray-600">{completionPct.toFixed(0)}%</span>
        </div>
        <div className="w-full bg-gray-200 rounded-full h-2">
          <div
            className="bg-green-600 h-2 rounded-full transition-all"
            style={{ width: `${completionPct}%` }}
          />
        </div>
      </Card>

      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Order Information</h2>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="text-sm text-gray-600">PO No</label>
            <p className="font-medium">{order.orderNo}</p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Status</label>
            <p className="font-medium">{order.status}</p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Supplier</label>
            <p className="font-medium">
              <Link to={`/parties/${order.partyId}`} className="text-blue-600 hover:underline">
                {order.partyName}
              </Link>
            </p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Issue Date</label>
            <p className="font-medium">{order.issueDate}</p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Branch</label>
            <p className="font-medium">{order.branchName}</p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Warehouse</label>
            <p className="font-medium">{order.warehouseName}</p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Currency</label>
            <p className="font-medium">{order.currency}</p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Total Amount</label>
            <p className="font-medium text-lg">
              {order.totalAmount.toFixed(2)} {order.currency}
            </p>
          </div>
        </div>
      </Card>

      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Order Lines</h2>
        <table className="w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="text-left p-2 text-sm">SKU</th>
              <th className="text-left p-2 text-sm">Product</th>
              <th className="text-right p-2 text-sm">Qty</th>
              <th className="text-right p-2 text-sm">Received</th>
              <th className="text-right p-2 text-sm">Remaining</th>
              <th className="text-right p-2 text-sm">Unit Cost</th>
              <th className="text-right p-2 text-sm">Total</th>
            </tr>
          </thead>
          <tbody>
            {order.lines.map((line, idx) => {
              const remaining = line.quantity - (line.receivedQty || 0);
              return (
                <tr key={idx} className="border-t">
                  <td className="p-2 text-sm">{line.sku}</td>
                  <td className="p-2 text-sm">{line.productName}</td>
                  <td className="p-2 text-sm text-right">{line.quantity}</td>
                  <td className="p-2 text-sm text-right">{line.receivedQty || 0}</td>
                  <td className="p-2 text-sm text-right font-medium">
                    {remaining}
                  </td>
                  <td className="p-2 text-sm text-right">{line.unitCost.toFixed(2)}</td>
                  <td className="p-2 text-sm text-right font-medium">
                    {(line.quantity * line.unitCost).toFixed(2)}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </Card>

      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Actions</h2>
        <div className="flex gap-3">
          {canConfirm && (
            <Button onClick={handleConfirm} disabled={confirmMutation.isPending}>
              <CheckCircle2 className="h-4 w-4 mr-2" />
              {confirmMutation.isPending ? 'Confirming...' : 'Confirm PO'}
            </Button>
          )}
          {canCreateGRN && (
            <Button onClick={handleCreateGRN} variant="outline">
              <Package className="h-4 w-4 mr-2" />
              Create Goods Receipt
            </Button>
          )}
          <Link to="/goods-receipts">
            <Button variant="ghost">View Related GRNs</Button>
          </Link>
        </div>
      </Card>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  const colorMap: Record<string, string> = {
    DRAFT: 'bg-gray-100 text-gray-800',
    CONFIRMED: 'bg-blue-100 text-blue-800',
    RECEIVED: 'bg-green-100 text-green-800',
    CANCELLED: 'bg-red-100 text-red-800',
  };

  return (
    <span className={`px-3 py-1 rounded-full text-sm font-medium ${colorMap[status] || 'bg-gray-100'}`}>
      {status}
    </span>
  );
}
