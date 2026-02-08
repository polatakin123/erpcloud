import { useState } from 'react';
import { StandardListPage, DateRange } from '@/components/shared/StandardListPage';
import { useQuery } from '@tanstack/react-query';
import { ApiClient } from '@/lib/api-client';
import { CSVExporter } from '@/lib/csv-exporter';
import { Download } from 'lucide-react';

interface PartyLedgerEntry {
  id: string;
  entryDate: Date;
  partyName: string;
  transactionType: string;
  debit: number;
  credit: number;
  balance: number;
  currency: string;
  referenceType?: string;
  referenceNo?: string;
  note?: string;
}

interface PagedPartyLedger {
  items: PartyLedgerEntry[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export function PartyLedgerPage() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [dateRange, setDateRange] = useState<DateRange>({});
  const [partyFilter, _setPartyFilter] = useState('');

  const { data, isLoading } = useQuery<PagedPartyLedger>({
    queryKey: ['party-ledger', page, pageSize, search, dateRange, partyFilter],
    queryFn: () => {
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: pageSize.toString(),
      });
      if (search) params.append('q', search);
      if (partyFilter) params.append('partyId', partyFilter);
      if (dateRange.from) params.append('from', dateRange.from.toISOString().split('T')[0]);
      if (dateRange.to) params.append('to', dateRange.to.toISOString().split('T')[0]);
      
      return ApiClient.get(`/api/party-ledger?${params.toString()}`);
    },
  });

  const handleExportCSV = () => {
    if (!data?.items.length) return;
    
    const csvData = data.items;
    const columns = [
      { key: 'entryDate', label: 'Date', format: (val: string) => new Date(val).toLocaleDateString() },
      { key: 'partyName', label: 'Party' },
      { key: 'transactionType', label: 'Transaction Type' },
      { key: 'debit', label: 'Debit' },
      { key: 'credit', label: 'Credit' },
      { key: 'balance', label: 'Balance' },
      { key: 'currency', label: 'Currency' },
      { key: 'referenceType', label: 'Reference Type' },
      { key: 'referenceNo', label: 'Reference No' },
      { key: 'note', label: 'Note' },
    ];

    const date = new Date().toISOString().split('T')[0];
    CSVExporter.export(csvData, columns, `party-ledger_${date}.csv`);
  };

  return (
    <StandardListPage
      title="Party Ledger"
      searchValue={search}
      onSearchChange={setSearch}
      searchPlaceholder="Search by party name or reference..."
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
      emptyMessage="No party ledger entries found for the selected filters"
    >
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Date
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Party
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
                  {entry.partyName}
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
