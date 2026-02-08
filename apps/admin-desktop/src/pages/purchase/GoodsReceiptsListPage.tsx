import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useGoodsReceipts } from '../../hooks/usePurchase';
import { Card } from '../../components/ui/card';
import { Button } from '../../components/ui/button';
import { Input } from '../../components/ui/input';
import { Search } from 'lucide-react';

export function GoodsReceiptsListPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  
  const { data, isLoading } = useGoodsReceipts(page, 50);

  const statuses = ['DRAFT', 'RECEIVED', 'CANCELLED'];

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold">Goods Receipts</h1>
      </div>

      <Card className="p-4 mb-6">
        <div className="flex gap-4 items-center">
          <div className="flex-1">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
              <Input
                type="text"
                placeholder="Search receipts..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pl-10"
              />
            </div>
          </div>
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="px-3 py-2 border rounded-md text-sm"
          >
            <option value="">All Statuses</option>
            {statuses.map(status => (
              <option key={status} value={status}>{status}</option>
            ))}
          </select>
        </div>
      </Card>

      <Card>
        {isLoading ? (
          <div className="p-12 text-center">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900"></div>
          </div>
        ) : data?.items && data.items.length > 0 ? (
          <>
            <table className="w-full">
              <thead className="bg-gray-50 border-b">
                <tr>
                  <th className="text-left p-3 text-sm font-medium">GRN No</th>
                  <th className="text-left p-3 text-sm font-medium">PO No</th>
                  <th className="text-left p-3 text-sm font-medium">Date</th>
                  <th className="text-left p-3 text-sm font-medium">Status</th>
                  <th className="text-right p-3 text-sm font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((receipt) => (
                  <tr key={receipt.id} className="border-b hover:bg-gray-50">
                    <td className="p-3 text-sm font-medium">
                      <Link to={`/goods-receipts/${receipt.id}`} className="text-blue-600 hover:underline">
                        {receipt.grnNo}
                      </Link>
                    </td>
                    <td className="p-3 text-sm">
                      <Link to={`/purchase-orders/${receipt.purchaseOrderId}`} className="text-blue-600 hover:underline">
                        {receipt.poNo}
                      </Link>
                    </td>
                    <td className="p-3 text-sm">{receipt.receiptDate}</td>
                    <td className="p-3 text-sm">
                      <StatusBadge status={receipt.status} />
                    </td>
                    <td className="p-3 text-sm text-right">
                      <Link to={`/goods-receipts/${receipt.id}`}>
                        <Button size="sm" variant="ghost">View</Button>
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div className="p-4 border-t flex justify-between">
              <div className="text-sm text-gray-600">
                Page {data.page} of {data.totalPages}
              </div>
              <div className="flex gap-2">
                <Button size="sm" variant="outline" onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}>
                  Previous
                </Button>
                <Button size="sm" variant="outline" onClick={() => setPage(p => p + 1)} disabled={page >= data.totalPages}>
                  Next
                </Button>
              </div>
            </div>
          </>
        ) : (
          <div className="p-12 text-center">
            <p className="text-gray-600">No goods receipts found</p>
          </div>
        )}
      </Card>
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
    <span className={`px-2 py-1 rounded text-xs font-medium ${colorMap[status]}`}>
      {status}
    </span>
  );
}
