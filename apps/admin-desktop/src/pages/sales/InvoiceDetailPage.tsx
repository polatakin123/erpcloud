import { useParams, Link, useNavigate } from 'react-router-dom';
import { useInvoice, useIssueInvoice } from '../../hooks/useSales';
import { Card } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { AlertCircle, CheckCircle2 } from 'lucide-react';

export function InvoiceDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: invoice, isLoading } = useInvoice(id || null);
  const issueMutation = useIssueInvoice();

  const handleIssue = async () => {
    if (!invoice) return;
    await issueMutation.mutateAsync(invoice.id);
  };

  if (isLoading) {
    return <div className="p-6">Loading...</div>;
  }

  if (!invoice) {
    return (
      <div className="p-6">
        <Card className="p-6">
          <AlertCircle className="h-6 w-6 text-red-600 mb-2" />
          <h2 className="text-lg font-semibold">Invoice Not Found</h2>
        </Card>
      </div>
    );
  }

  const canIssue = invoice.status === 'DRAFT';

  return (
    <div className="p-6 max-w-6xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Invoice {invoice.invoiceNo}</h1>
          <p className="text-sm text-gray-600">{invoice.partyName} | {invoice.invoiceDate}</p>
        </div>
        <StatusBadge status={invoice.status} />
      </div>

      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Invoice Information</h2>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="text-sm text-gray-600">Invoice No</label>
            <p className="font-medium">{invoice.invoiceNo}</p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Status</label>
            <p className="font-medium">{invoice.status}</p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Customer</label>
            <p className="font-medium">
              <Link to={`/parties/${invoice.partyId}`} className="text-blue-600 hover:underline">
                {invoice.partyName}
              </Link>
            </p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Invoice Date</label>
            <p className="font-medium">{invoice.invoiceDate}</p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Source</label>
            <p className="font-medium">{invoice.sourceType}</p>
          </div>
          <div>
            <label className="text-sm text-gray-600">Currency</label>
            <p className="font-medium">{invoice.currency}</p>
          </div>
          <div className="col-span-2">
            <label className="text-sm text-gray-600">Total Amount</label>
            <p className="font-medium text-2xl">
              {invoice.totalAmount.toFixed(2)} {invoice.currency}
            </p>
          </div>
        </div>
      </Card>

      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Invoice Lines</h2>
        <table className="w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="text-left p-2 text-sm">Product</th>
              <th className="text-right p-2 text-sm">Qty</th>
              <th className="text-right p-2 text-sm">Price</th>
              <th className="text-right p-2 text-sm">Total</th>
            </tr>
          </thead>
          <tbody>
            {invoice.lines.map((line, idx) => (
              <tr key={idx} className="border-t">
                <td className="p-2 text-sm">{line.productName}</td>
                <td className="p-2 text-sm text-right">{line.quantity}</td>
                <td className="p-2 text-sm text-right">{line.price.toFixed(2)}</td>
                <td className="p-2 text-sm text-right font-medium">
                  {(line.quantity * line.price).toFixed(2)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>

      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Party Ledger Impact</h2>
        <p className="text-sm text-gray-600 mb-2">
          When issued, this invoice will create a receivable of {invoice.totalAmount.toFixed(2)} {invoice.currency}.
        </p>
        <Link to={`/party-ledger?partyId=${invoice.partyId}`} className="text-blue-600 hover:underline text-sm">
          View Party Ledger →
        </Link>
      </Card>

      <Card className="p-6">
        <h2 className="text-lg font-semibold mb-4">Actions</h2>
        <div className="flex gap-3">
          {canIssue && (
            <Button onClick={handleIssue} disabled={issueMutation.isPending}>
              <CheckCircle2 className="h-4 w-4 mr-2" />
              {issueMutation.isPending ? 'Issuing...' : 'Issue Invoice'}
            </Button>
          )}
        </div>
      </Card>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  const colorMap: Record<string, string> = {
    DRAFT: 'bg-gray-100 text-gray-800',
    ISSUED: 'bg-green-100 text-green-800',
    CANCELLED: 'bg-red-100 text-red-800',
  };

  return (
    <span className={`px-3 py-1 rounded-full text-sm font-medium ${colorMap[status] || 'bg-gray-100'}`}>
      {status}
    </span>
  );
}
