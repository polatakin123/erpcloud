import { useState } from 'react';
import { useCashBankBalances } from '../../hooks/useReports';
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card';
import { Input } from '../../components/ui/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table';

export function CashBankReportsPage() {
  const [atDate, setAtDate] = useState('');

  const { data: balances, isLoading } = useCashBankBalances(atDate || undefined);

  const formatCurrency = (value: number, currency: string) => {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: currency,
    }).format(value);
  };

  const cashboxes = balances?.filter((b) => b.sourceType === 'CASHBOX') || [];
  const bankAccounts = balances?.filter((b) => b.sourceType === 'BANK') || [];
  const totalCash = cashboxes.reduce((sum, b) => sum + b.balance, 0);
  const totalBank = bankAccounts.reduce((sum, b) => sum + b.balance, 0);

  return (
    <div className="p-6 space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-3xl font-bold">Cash & Bank Reports</h1>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Cash & Bank Account Balances</CardTitle>
          <div className="grid grid-cols-3 gap-4 mt-4">
            <Input
              type="date"
              placeholder="As of Date"
              value={atDate}
              onChange={(e) => setAtDate(e.target.value)}
            />
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="text-center py-8">Loading...</div>
          ) : balances && balances.length > 0 ? (
            <div className="space-y-6">
              {/* Cashboxes */}
              {cashboxes.length > 0 && (
                <div>
                  <h3 className="text-lg font-semibold mb-3">Cashboxes</h3>
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Code</TableHead>
                        <TableHead>Name</TableHead>
                        <TableHead>Currency</TableHead>
                        <TableHead className="text-right">Balance</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {cashboxes.map((item) => (
                        <TableRow key={item.sourceId}>
                          <TableCell className="font-medium">{item.code}</TableCell>
                          <TableCell>{item.name}</TableCell>
                          <TableCell>{item.currency}</TableCell>
                          <TableCell className="text-right font-bold">
                            {formatCurrency(item.balance, item.currency)}
                          </TableCell>
                        </TableRow>
                      ))}
                      <TableRow>
                        <TableCell colSpan={3} className="font-bold">Total Cashboxes</TableCell>
                        <TableCell className="text-right font-bold text-blue-600">
                          {formatCurrency(totalCash, 'TRY')}
                        </TableCell>
                      </TableRow>
                    </TableBody>
                  </Table>
                </div>
              )}

              {/* Bank Accounts */}
              {bankAccounts.length > 0 && (
                <div>
                  <h3 className="text-lg font-semibold mb-3">Bank Accounts</h3>
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Code</TableHead>
                        <TableHead>Name</TableHead>
                        <TableHead>Currency</TableHead>
                        <TableHead className="text-right">Balance</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {bankAccounts.map((item) => (
                        <TableRow key={item.sourceId}>
                          <TableCell className="font-medium">{item.code}</TableCell>
                          <TableCell>{item.name}</TableCell>
                          <TableCell>{item.currency}</TableCell>
                          <TableCell className="text-right font-bold">
                            {formatCurrency(item.balance, item.currency)}
                          </TableCell>
                        </TableRow>
                      ))}
                      <TableRow>
                        <TableCell colSpan={3} className="font-bold">Total Bank Accounts</TableCell>
                        <TableCell className="text-right font-bold text-blue-600">
                          {formatCurrency(totalBank, 'TRY')}
                        </TableCell>
                      </TableRow>
                    </TableBody>
                  </Table>
                </div>
              )}

              {/* Grand Total */}
              <div className="border-t pt-4">
                <div className="flex justify-between items-center text-lg font-bold">
                  <span>Grand Total (Cash + Bank)</span>
                  <span className="text-green-600">{formatCurrency(totalCash + totalBank, 'TRY')}</span>
                </div>
              </div>
            </div>
          ) : (
            <div className="text-center py-8 text-gray-500">No cash or bank accounts found</div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
