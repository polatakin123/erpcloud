import { useParams, Link, useNavigate } from 'react-router-dom';
import { useSalesOrder, useConfirmSalesOrder } from '../../hooks/useSales';
import { Card } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { AlertCircle, Package, CheckCircle2 } from 'lucide-react';

export function SalesOrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: order, isLoading, error } = useSalesOrder(id || null);
  const confirmMutation = useConfirmSalesOrder();

  const handleConfirm = async () => {
    if (!order) return;
    await confirmMutation.mutateAsync(order.id);
  };

  const handleCreateShipment = () => {
    navigate('/sales/wizard', { state: { orderId: id, step: 5 } });
  };

  if (isLoading) {
    return (
      <div className="p-6">
        <div className="animate-pulse space-y-4">
          <div className="h-8 bg-gray-200 rounded w-1/4"></div>
          <div className="h-64 bg-gray-200 rounded"></div>
        </div>
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="p-6">
        <Card className="p-6">
          <div className="flex items-center gap-3 text-red-600">
            <AlertCircle className="h-6 w-6" />
            <div>
              <h2 className="text-lg font-semibold">Order Not Found</h2>
              <p className="text-sm text-gray-600 mt-1">
                The sales order you're looking for doesn't exist or you don't have permission to view it.
              </p>
            </div>
          </div>
          <div className="mt-4">
            <Button onClick={() => navigate('/sales-orders')}>Back to Orders</Button>
          </div>
        </Card>
      </div>
    );
  }

  const canConfirm = order.status === 'DRAFT';
  const canCreateShipment = order.status === 'CONFIRMED' || order.status === 'SHIPPED';

  return (
    <div className="p-6 max-w-6xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Sales Order {order.orderNo}</h1>
          <p className="text-sm text-gray-600">
            {order.partyName} | {new Date(order.orderDate).toLocaleDateString('tr-TR')}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <StatusBadge status={order.status} />
        </div>
      </div>

      {/* Order Info */}
      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Order Information</h2>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="text-sm text-gray-600">Order No</label>
            <p className="font-medium">{order.orderNo}</p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Status</label>
            <p className="font-medium">{order.status}</p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Customer</label>
            <p className="font-medium">
              <Link to={`/parties/${order.partyId}`} className="text-blue-600 hover:underline">
                {order.partyName}
              </Link>
            </p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Issue Date</label>
            <p className="font-medium">{new Date(order.orderDate).toLocaleDateString('tr-TR')}</p>
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
            <label className="text-sm text-gray-600">Price List</label>
            <p className="font-medium">{order.priceListCode || '-'}</p>
          </div>
        </div>
        {order.note && (
          <div className="mt-4">
            <label className="text-sm text-gray-600">Note</label>
            <p className="text-sm">{order.note}</p>
          </div>
        )}
      </Card>

      {/* Order Lines */}
      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Order Lines</h2>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="text-left p-2 text-sm font-medium">SKU</th>
                <th className="text-left p-2 text-sm font-medium">Product</th>
                <th className="text-right p-2 text-sm font-medium">Qty</th>
                <th className="text-right p-2 text-sm font-medium">Reserved</th>
                <th className="text-right p-2 text-sm font-medium">Shipped</th>
                <th className="text-right p-2 text-sm font-medium">Remaining</th>
                <th className="text-right p-2 text-sm font-medium">Price</th>
                <th className="text-right p-2 text-sm font-medium">Total</th>
              </tr>
            </thead>
            <tbody>
              {order.lines.map((line) => (
                <tr key={line.id} className="border-t">
                  <td className="p-2 text-sm">{line.sku}</td>
                  <td className="p-2 text-sm">{line.variantName}</td>
                  <td className="p-2 text-sm text-right">{line.qty}</td>
                  <td className="p-2 text-sm text-right">{line.reservedQty}</td>
                  <td className="p-2 text-sm text-right">-</td>
                  <td className="p-2 text-sm text-right font-medium">-</td>
                  <td className="p-2 text-sm text-right">{line.price.toFixed(2)}</td>
                  <td className="p-2 text-sm text-right font-medium">
                    {(line.quantity * line.price).toFixed(2)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Actions */}
      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Actions</h2>
        <div className="flex gap-3">
          {canConfirm && (
            <Button
              onClick={handleConfirm}
              disabled={confirmMutation.isPending}
            >
              <CheckCircle2 className="h-4 w-4 mr-2" />
              {confirmMutation.isPending ? 'Confirming...' : 'Confirm Order'}
            </Button>
          )}
          {canCreateShipment && (
            <Button
              onClick={handleCreateShipment}
              variant="outline"
            >
              <Package className="h-4 w-4 mr-2" />
              Create Shipment
            </Button>
          )}
          <Link to="/shipments">
            <Button variant="ghost">View Related Shipments</Button>
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
    SHIPPED: 'bg-green-100 text-green-800',
    INVOICED: 'bg-purple-100 text-purple-800',
    CANCELLED: 'bg-red-100 text-red-800',
  };

  return (
    <span className={`px-3 py-1 rounded-full text-sm font-medium ${colorMap[status] || 'bg-gray-100'}`}>
      {status}
    </span>
  );
}
