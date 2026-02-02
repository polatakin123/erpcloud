import { useState } from 'react';
import { useSalesSummary, usePurchaseSummary } from '../../hooks/useReports';
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card';
import { Input } from '../../components/ui/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table';

export function SalesReportsPage() {
  const [activeTab, setActiveTab] = useState<'sales' | 'purchase'>('sales');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [groupBy, setGroupBy] = useState<'DAY' | 'MONTH'>('DAY');

  const { data: salesData, isLoading: salesLoading } = useSalesSummary(
    fromDate || undefined,
    toDate || undefined,
    groupBy
  );

  const { data: purchaseData, isLoading: purchaseLoading } = usePurchaseSummary(
    fromDate || undefined,
    toDate || undefined,
    groupBy
  );

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY',
    }).format(value);
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-3xl font-bold">Sales & Purchase Reports</h1>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 border-b">
        <button
          className={`px-4 py-2 font-medium ${
            activeTab === 'sales'
              ? 'border-b-2 border-blue-500 text-blue-600'
              : 'text-gray-600 hover:text-gray-900'
          }`}
          onClick={() => setActiveTab('sales')}
        >
          Sales Summary
        </button>
        <button
          className={`px-4 py-2 font-medium ${
            activeTab === 'purchase'
              ? 'border-b-2 border-blue-500 text-blue-600'
              : 'text-gray-600 hover:text-gray-900'
          }`}
          onClick={() => setActiveTab('purchase')}
        >
          Purchase Summary
        </button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>{activeTab === 'sales' ? 'Sales' : 'Purchase'} Summary</CardTitle>
          <div className="grid grid-cols-3 gap-4 mt-4">
            <Input
              type="date"
              placeholder="From Date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
            />
            <Input
              type="date"
              placeholder="To Date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
            />
            <select
              className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background"
              value={groupBy}
              onChange={(e) => setGroupBy(e.target.value as 'DAY' | 'MONTH')}
            >
              <option value="DAY">Group by Day</option>
              <option value="MONTH">Group by Month</option>
            </select>
          </div>
        </CardHeader>
        <CardContent>
          {(activeTab === 'sales' ? salesLoading : purchaseLoading) ? (
            <div className="text-center py-8">Loading...</div>
          ) : (
            <>
              {activeTab === 'sales' && salesData && salesData.length > 0 ? (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Period</TableHead>
                      <TableHead className="text-right">Invoice Count</TableHead>
                      <TableHead className="text-right">Total Net</TableHead>
                      <TableHead className="text-right">Total VAT</TableHead>
                      <TableHead className="text-right">Total Gross</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {salesData.map((item) => (
                      <TableRow key={item.period}>
                        <TableCell className="font-medium">{item.period}</TableCell>
                        <TableCell className="text-right">{item.invoiceCount}</TableCell>
                        <TableCell className="text-right">{formatCurrency(item.totalNet)}</TableCell>
                        <TableCell className="text-right">{formatCurrency(item.totalVat)}</TableCell>
                        <TableCell className="text-right font-bold">{formatCurrency(item.totalGross)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              ) : activeTab === 'purchase' && purchaseData && purchaseData.length > 0 ? (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Period</TableHead>
                      <TableHead className="text-right">Invoice Count</TableHead>
                      <TableHead className="text-right">Total Net</TableHead>
                      <TableHead className="text-right">Total VAT</TableHead>
                      <TableHead className="text-right">Total Gross</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {purchaseData.map((item) => (
                      <TableRow key={item.period}>
                        <TableCell className="font-medium">{item.period}</TableCell>
                        <TableCell className="text-right">{item.invoiceCount}</TableCell>
                        <TableCell className="text-right">{formatCurrency(item.totalNet)}</TableCell>
                        <TableCell className="text-right">{formatCurrency(item.totalVat)}</TableCell>
                        <TableCell className="text-right font-bold">{formatCurrency(item.totalGross)}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              ) : (
                <div className="text-center py-8 text-gray-500">
                  No data found for the selected period
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
