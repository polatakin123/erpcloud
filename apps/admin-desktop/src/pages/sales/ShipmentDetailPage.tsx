import { useParams, Link, useNavigate } from 'react-router-dom';
import { useShipment, useShipShipment, useCreateInvoiceFromShipment } from '../../hooks/useSales';
import { Card } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { AlertCircle, FileText, Truck } from 'lucide-react';

export function ShipmentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: shipment, isLoading } = useShipment(id || null);
  const shipMutation = useShipShipment();
  const createInvoiceMutation = useCreateInvoiceFromShipment();

  const handleShip = async () => {
    if (!shipment) return;
    await shipMutation.mutateAsync(shipment.id);
  };

  const handleCreateInvoice = async () => {
    if (!shipment) return;
    const invoice = await createInvoiceMutation.mutateAsync(shipment.id);
    navigate(`/invoices/${invoice.id}`);
  };

  if (isLoading) {
    return <div className="p-6">Loading...</div>;
  }

  if (!shipment) {
    return (
      <div className="p-6">
        <Card className="p-6">
          <AlertCircle className="h-6 w-6 text-red-600 mb-2" />
          <h2 className="text-lg font-semibold">Shipment Not Found</h2>
        </Card>
      </div>
    );
  }

  const canShip = shipment.status === 'DRAFT';
  const canInvoice = shipment.status === 'SHIPPED';

  return (
    <div className="p-6 max-w-6xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Shipment {shipment.shipmentNo}</h1>
          <p className="text-sm text-gray-600">{new Date(shipment.shipmentDate).toLocaleDateString('tr-TR')}</p>
        </div>
        <StatusBadge status={shipment.status} />
      </div>

      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Shipment Information</h2>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="text-sm text-gray-600">Shipment No</label>
            <p className="font-medium">{shipment.shipmentNo}</p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Related Order</label>
            <p className="font-medium">
              <Link to={`/sales-orders/${shipment.salesOrderId}`} className="text-blue-600 hover:underline">
                {shipment.salesOrderId.substring(0, 8)}...
              </Link>
            </p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Status</label>
            <p className="font-medium">{shipment.status}</p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Date</label>
            <p className="font-medium">{new Date(shipment.shipmentDate).toLocaleDateString('tr-TR')}</p>
          </div>
        </div>
      </Card>

      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Shipment Lines</h2>
        <table className="w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="text-left p-2 text-sm">Variant ID</th>
              <th className="text-right p-2 text-sm">Qty</th>
              <th className="text-left p-2 text-sm">Note</th>
            </tr>
          </thead>
          <tbody>
            {shipment.lines.map((line) => (
              <tr key={line.id} className="border-t">
                <td className="p-2 text-sm font-mono text-xs">{line.variantId.substring(0, 8)}...</td>
                <td className="p-2 text-sm text-right">{line.qty}</td>
                <td className="p-2 text-sm text-gray-600">{line.note || '-'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>

      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Actions</h2>
        <div className="flex gap-3">
          {canShip && (
            <Button onClick={handleShip} disabled={shipMutation.isPending}>
              <Truck className="h-4 w-4 mr-2" />
              {shipMutation.isPending ? 'Shipping...' : 'Ship Now'}
            </Button>
          )}
          {canInvoice && (
            <Button onClick={handleCreateInvoice} disabled={createInvoiceMutation.isPending}>
              <FileText className="h-4 w-4 mr-2" />
              {createInvoiceMutation.isPending ? 'Creating...' : 'Create Invoice'}
            </Button>
          )}
          <Link to="/invoices">
            <Button variant="ghost">View Related Invoices</Button>
          </Link>
        </div>
      </Card>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  const colorMap: Record<string, string> = {
    DRAFT: 'bg-gray-100 text-gray-800',
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
