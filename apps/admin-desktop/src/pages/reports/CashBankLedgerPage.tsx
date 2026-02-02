import { useState } from 'react';
import { StandardListPage, DateRange } from '@/components/shared/StandardListPage';
import { useQuery } from '@tanstack/react-query';
import { ApiClient } from '@/lib/api-client';
import { CSVExporter } from '@/lib/csv-exporter';
import { Download } from 'lucide-react';

interface CashBankLedgerEntry {
  id: string;
  entryDate: Date;
  accountName: string;
  accountType: 'CASHBOX' | 'BANK';
  transactionType: string;
  debit: number;
  credit: number;
  balance: number;
  currency: string;
  referenceType?: string;
  referenceNo?: string;
  note?: string;
}

interface PagedCashBankLedger {
  items: CashBankLedgerEntry[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export function CashBankLedgerPage() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [dateRange, setDateRange] = useState<DateRange>({});
  const [accountTypeFilter, setAccountTypeFilter] = useState('');

  const { data, isLoading } = useQuery<PagedCashBankLedger>({
    queryKey: ['cash-bank-ledger', page, pageSize, search, dateRange, accountTypeFilter],
    queryFn: () => {
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: pageSize.toString(),
      });
      if (search) params.append('q', search);
      if (accountTypeFilter) params.append('accountType', accountTypeFilter);
      if (dateRange.from) params.append('from', dateRange.from.toISOString().split('T')[0]);
      if (dateRange.to) params.append('to', dateRange.to.toISOString().split('T')[0]);
      
      return ApiClient.get(`/api/cash-bank-ledger?${params.toString()}`);
    },
  });

  const handleExportCSV = () => {
    if (!data?.items.length) return;
    
    const csvData = data.items.map((item) => ({
      Date: new Date(item.entryDate).toLocaleDateString(),
      Account: item.accountName,
      'Account Type': item.accountType,
      'Transaction Type': item.transactionType,
      Debit: item.debit,
      Credit: item.credit,
      Balance: item.balance,
      Currency: item.currency,
      'Reference Type': item.referenceType || '',
      'Reference No': item.referenceNo || '',
      Note: item.note || '',
    }));

    const date = new Date().toISOString().split('T')[0];
    CSVExporter.export(csvData, `cash-bank-ledger_${date}.csv`);
  };

  const accountTypeOptions = [
    { value: 'CASHBOX', label: 'Cashbox' },
    { value: 'BANK', label: 'Bank Account' },
  ];

  return (
    <StandardListPage
      title="Cash & Bank Ledger"
      searchValue={search}
      onSearchChange={setSearch}
      searchPlaceholder="Search by account name or reference..."
      statusValue={accountTypeFilter}
      onStatusChange={setAccountTypeFilter}
      statusOptions={accountTypeOptions}
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
      emptyMessage="No cash/bank ledger entries found for the selected filters"
    >
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Date
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Account
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Type
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Transaction
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Debit
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Credit
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Balance
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
            {data?.items.map((entry) => (
              <tr key={entry.id} className="hover:bg-gray-50">
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {new Date(entry.entryDate).toLocaleDateString()}
                </td>
                <td className="px-6 py-4 text-sm text-gray-900">
                  {entry.accountName}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">
                  <span
                    className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                      entry.accountType === 'CASHBOX'
                        ? 'bg-green-100 text-green-800'
                        : 'bg-blue-100 text-blue-800'
                    }`}
                  >
                    {entry.accountType}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {entry.transactionType}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-red-600 font-semibold">
                  {entry.debit > 0 ? `${entry.debit.toFixed(2)} ${entry.currency}` : '-'}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-right text-green-600 font-semibold">
                  {entry.credit > 0 ? `${entry.credit.toFixed(2)} ${entry.currency}` : '-'}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-right font-bold">
                  <span className={entry.balance >= 0 ? 'text-green-700' : 'text-red-700'}>
                    {entry.balance.toFixed(2)} {entry.currency}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                  {entry.referenceType && entry.referenceNo
                    ? `${entry.referenceType} #${entry.referenceNo}`
                    : '-'}
                </td>
                <td className="px-6 py-4 text-sm text-gray-500 max-w-xs truncate">
                  {entry.note || '-'}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </StandardListPage>
  );
}
