import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSalesOrders } from '../../hooks/useSales';
import { StandardListPage, DateRange } from '@/components/shared/StandardListPage';
import { Plus } from 'lucide-react';

export function SalesOrdersListPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [statusFilter, setStatusFilter] = useState('');
  const [dateRange, setDateRange] = useState<DateRange>({});
  
  const { data, isLoading } = useSalesOrders(page, pageSize);

  const statusOptions = [
    { value: 'DRAFT', label: 'Draft' },
    { value: 'CONFIRMED', label: 'Confirmed' },
    { value: 'SHIPPED', label: 'Shipped' },
    { value: 'INVOICED', label: 'Invoiced' },
    { value: 'CANCELLED', label: 'Cancelled' },
  ];

  return (
    <StandardListPage
      title="Sales Orders"
      searchValue={search}
      onSearchChange={setSearch}
      searchPlaceholder="Search orders by number or customer..."
      statusValue={statusFilter}
      onStatusChange={setStatusFilter}
      statusOptions={statusOptions}
      dateRange={dateRange}
      onDateRangeChange={setDateRange}
      page={page}
      totalPages={data?.totalPages || 1}
      totalCount={data?.totalCount || 0}
      pageSize={pageSize}
      onPageChange={setPage}
      onPageSizeChange={setPageSize}
      primaryAction={{
        label: 'New Order',
        onClick: () => navigate('/sales/wizard'),
        icon: <Plus className="h-4 w-4 mr-2" />,
      }}
      isLoading={isLoading}
      isEmpty={!data || data.items.length === 0}
      emptyMessage="No sales orders found"
      emptyAction={{
        label: 'Create First Order',
        onClick: () => navigate('/sales/wizard'),
      }}
    >
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Order No
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Customer
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Date
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Status
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Total
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {data?.items.map((order) => (
              <tr
                key={order.id}
                className="hover:bg-gray-50 cursor-pointer"
                onClick={() => navigate(`/sales-orders/${order.id}`)}
              >
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-blue-600">
                  {order.orderNo}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {order.partyName}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {new Date(order.orderDate).toLocaleDateString('tr-TR')}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span
                    className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                      order.status === 'DRAFT'
                        ? 'bg-gray-100 text-gray-800'
                        : order.status === 'CONFIRMED'
                        ? 'bg-blue-100 text-blue-800'
                        : order.status === 'SHIPPED'
                        ? 'bg-green-100 text-green-800'
                        : order.status === 'INVOICED'
                        ? 'bg-purple-100 text-purple-800'
                        : 'bg-red-100 text-red-800'
                    }`}
                  >
                    {order.status}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-gray-900">
                  {order.totalAmount.toFixed(2)} {order.currency}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      navigate(`/sales-orders/${order.id}`);
                    }}
                    className="text-blue-600 hover:text-blue-900"
                  >
                    View
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </StandardListPage>
  );
}
