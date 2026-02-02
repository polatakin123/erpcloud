import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { StandardListPage, DateRange } from '@/components/shared/StandardListPage';
import { useQuery } from '@tanstack/react-query';
import { ApiClient } from '@/lib/api-client';
import type { Payment } from '@/types/payment';

interface PagedPayments {
  items: Payment[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export function PaymentsListPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [dateRange, setDateRange] = useState<DateRange>({});
  const [statusFilter, setStatusFilter] = useState('');

  const { data, isLoading } = useQuery<PagedPayments>({
    queryKey: ['payments', page, pageSize, search, dateRange, statusFilter],
    queryFn: () => {
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: pageSize.toString(),
      });
      if (search) params.append('q', search);
      if (statusFilter) params.append('status', statusFilter);
      if (dateRange.from) params.append('from', dateRange.from.toISOString().split('T')[0]);
      if (dateRange.to) params.append('to', dateRange.to.toISOString().split('T')[0]);
      
      return ApiClient.get(`/api/payments?${params.toString()}`);
    },
  });

  const statusOptions = [
    { value: 'DRAFT', label: 'Draft' },
    { value: 'CONFIRMED', label: 'Confirmed' },
    { value: 'CANCELLED', label: 'Cancelled' },
  ];

  return (
    <StandardListPage
      title="Payments"
      searchValue={search}
      onSearchChange={setSearch}
      searchPlaceholder="Search payments..."
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
      isLoading={isLoading}
      isEmpty={!data || data.items.length === 0}
      emptyMessage="No payments found"
    >
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Payment No
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Date
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Party
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Type
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Method
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Amount
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Status
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {data?.items.map((payment) => (
              <tr
                key={payment.id}
                className="hover:bg-gray-50 cursor-pointer"
                onClick={() => navigate(`/payments/${payment.id}`)}
              >
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-blue-600">
                  {payment.paymentNo || payment.id}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {new Date(payment.paymentDate).toLocaleDateString()}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {payment.partyName || '-'}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {payment.paymentType}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {payment.paymentMethod}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {payment.amount.toFixed(2)} {payment.currency}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span
                    className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                      payment.status === 'CONFIRMED'
                        ? 'bg-green-100 text-green-800'
                        : payment.status === 'CANCELLED'
                        ? 'bg-red-100 text-red-800'
                        : 'bg-gray-100 text-gray-800'
                    }`}
                  >
                    {payment.status}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      navigate(`/payments/${payment.id}`);
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
