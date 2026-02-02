import { useState } from 'react';
import { StandardListPage, DateRange } from '@/components/shared/StandardListPage';
import { useQuery } from '@tanstack/react-query';
import { ApiClient } from '@/lib/api-client';
import { CSVExporter } from '@/lib/csv-exporter';
import { Button } from '@/components/ui/button';
import { Download } from 'lucide-react';

interface StockMovement {
  id: string;
  movementDate: Date;
  productName: string;
  variantSku: string;
  warehouseName: string;
  movementType: string;
  quantity: number;
  unit: string;
  referenceType?: string;
  referenceNo?: string;
  note?: string;
}

interface PagedStockMovements {
  items: StockMovement[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export function StockLedgerPage() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [dateRange, setDateRange] = useState<DateRange>({});
  const [movementTypeFilter, setMovementTypeFilter] = useState('');

  const { data, isLoading } = useQuery<PagedStockMovements>({
    queryKey: ['stock-ledger', page, pageSize, search, dateRange, movementTypeFilter],
    queryFn: () => {
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: pageSize.toString(),
      });
      if (search) params.append('q', search);
      if (movementTypeFilter) params.append('movementType', movementTypeFilter);
      if (dateRange.from) params.append('from', dateRange.from.toISOString().split('T')[0]);
      if (dateRange.to) params.append('to', dateRange.to.toISOString().split('T')[0]);
      
      return ApiClient.get(`/api/stock/movements?${params.toString()}`);
    },
  });

  const handleExportCSV = () => {
    if (!data?.items.length) return;
    
    const csvData = data.items.map((item) => ({
      Date: new Date(item.movementDate).toLocaleDateString(),
      Product: item.productName,
      SKU: item.variantSku,
      Warehouse: item.warehouseName,
      'Movement Type': item.movementType,
      Quantity: item.quantity,
      Unit: item.unit,
      'Reference Type': item.referenceType || '',
      'Reference No': item.referenceNo || '',
      Note: item.note || '',
    }));

    const date = new Date().toISOString().split('T')[0];
    CSVExporter.export(csvData, `stock-movements_${date}.csv`);
  };

  const movementTypeOptions = [
    { value: 'RECEIPT', label: 'Receipt' },
    { value: 'SHIPMENT', label: 'Shipment' },
    { value: 'ADJUSTMENT', label: 'Adjustment' },
    { value: 'TRANSFER', label: 'Transfer' },
  ];

  return (
    <StandardListPage
      title="Stock Ledger"
      searchValue={search}
      onSearchChange={setSearch}
      searchPlaceholder="Search by product, SKU, or warehouse..."
      statusValue={movementTypeFilter}
      onStatusChange={setMovementTypeFilter}
      statusOptions={movementTypeOptions}
      dateRange={dateRange}
      onDateRangeChange={setDateRange}
      page={page}
      totalPages={data?.totalPages || 1}
      totalCount={data?.totalCount || 0}
      pageSize={pageSize}
      onPageChange={setPage}
      onPageSizeChange={setPageSize}
      primaryAction={{
        label: 'Export CSV',
        onClick: handleExportCSV,
        icon: <Download className="h-4 w-4 mr-2" />,
      }}
      isLoading={isLoading}
      isEmpty={!data || data.items.length === 0}
      emptyMessage="No stock movements found for the selected filters"
    >
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Date
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Product
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                SKU
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Warehouse
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Movement
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Quantity
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Reference
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Note
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {data?.items.map((movement) => (
              <tr key={movement.id} className="hover:bg-gray-50">
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {new Date(movement.movementDate).toLocaleDateString()}
                </td>
                <td className="px-6 py-4 text-sm text-gray-900">
                  {movement.productName}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-600">
                  {movement.variantSku}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {movement.warehouseName}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">
                  <span
                    className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                      movement.movementType === 'RECEIPT'
                        ? 'bg-green-100 text-green-800'
                        : movement.movementType === 'SHIPMENT'
                        ? 'bg-blue-100 text-blue-800'
                        : 'bg-gray-100 text-gray-800'
                    }`}
                  >
                    {movement.movementType}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-right font-semibold">
                  {movement.quantity > 0 ? '+' : ''}
                  {movement.quantity} {movement.unit}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                  {movement.referenceType && movement.referenceNo
                    ? `${movement.referenceType} #${movement.referenceNo}`
                    : '-'}
                </td>
                <td className="px-6 py-4 text-sm text-gray-500 max-w-xs truncate">
                  {movement.note || '-'}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </StandardListPage>
  );
}
