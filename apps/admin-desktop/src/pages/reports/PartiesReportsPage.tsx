import { useState } from 'react';
import { usePartyBalances, usePartyAging } from '../../hooks/useReports';
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card';
import { Input } from '../../components/ui/input';
import { Button } from '../../components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table';

export function PartiesReportsPage() {
  const [activeTab, setActiveTab] = useState<'balances' | 'aging'>('balances');
  
  // Common filters
  const [searchQuery, setSearchQuery] = useState('');
  const [partyType, setPartyType] = useState('');
  const [atDate, setAtDate] = useState('');
  const [page, setPage] = useState(1);

  const { data: balances, isLoading: balancesLoading } = usePartyBalances(
    searchQuery || undefined,
    partyType || undefined,
    page,
    50,
    atDate || undefined
  );

  const { data: aging, isLoading: agingLoading } = usePartyAging(
    searchQuery || undefined,
    partyType || undefined,
    page,
    50,
    atDate || undefined
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
        <h1 className="text-3xl font-bold">Party Reports</h1>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 border-b">
        <button
          className={`px-4 py-2 font-medium ${
            activeTab === 'balances'
              ? 'border-b-2 border-blue-500 text-blue-600'
              : 'text-gray-600 hover:text-gray-900'
          }`}
          onClick={() => setActiveTab('balances')}
        >
          Party Balances
        </button>
        <button
          className={`px-4 py-2 font-medium ${
            activeTab === 'aging'
              ? 'border-b-2 border-blue-500 text-blue-600'
              : 'text-gray-600 hover:text-gray-900'
          }`}
          onClick={() => setActiveTab('aging')}
        >
          Party Aging
        </button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>
            {activeTab === 'balances' ? 'Party Balance List' : 'Party Aging Report'}
          </CardTitle>
          <div className="grid grid-cols-3 gap-4 mt-4">
            <Input
              placeholder="Search Code or Name"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
            <select
              className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background"
              value={partyType}
              onChange={(e) => setPartyType(e.target.value)}
            >
              <option value="">All Types</option>
              <option value="CUSTOMER">Customer</option>
              <option value="SUPPLIER">Supplier</option>
            </select>
            <Input
              type="date"
              placeholder="As of Date"
              value={atDate}
              onChange={(e) => setAtDate(e.target.value)}
            />
          </div>
          {activeTab === 'aging' && (
            <div className="mt-2 text-sm text-amber-600">
              Note: This report shows gross exposure from SALES ISSUED invoices only.
              Payment matching is not implemented yet.
            </div>
          )}
        </CardHeader>
        <CardContent>
          {activeTab === 'balances' && (
            <>
              {balancesLoading ? (
                <div className="text-center py-8">Loading...</div>
              ) : balances && balances.items.length > 0 ? (
                <>
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Code</TableHead>
                        <TableHead>Name</TableHead>
                        <TableHead>Type</TableHead>
                        <TableHead className="text-right">Balance</TableHead>
                        <TableHead>Currency</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {balances.items.map((item) => (
                        <TableRow key={item.partyId}>
                          <TableCell className="font-medium">{item.code}</TableCell>
                          <TableCell>{item.name}</TableCell>
                          <TableCell>{item.type}</TableCell>
                          <TableCell className="text-right font-bold">
                            {formatCurrency(item.balance)}
                          </TableCell>
                          <TableCell>{item.currency}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                  <div className="flex justify-between items-center mt-4">
                    <div className="text-sm text-gray-600">
                      Showing {balances.items.length} of {balances.total} results
                    </div>
                    <div className="flex gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={page === 1}
                        onClick={() => setPage(page - 1)}
                      >
                        Previous
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={page * 50 >= balances.total}
                        onClick={() => setPage(page + 1)}
                      >
                        Next
                      </Button>
                    </div>
                  </div>
                </>
              ) : (
                <div className="text-center py-8 text-gray-500">No results found</div>
              )}
            </>
          )}

          {activeTab === 'aging' && (
            <>
              {agingLoading ? (
                <div className="text-center py-8">Loading...</div>
              ) : aging && aging.items.length > 0 ? (
                <>
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Code</TableHead>
                        <TableHead>Name</TableHead>
                        <TableHead className="text-right">0-30 Days</TableHead>
                        <TableHead className="text-right">31-60 Days</TableHead>
                        <TableHead className="text-right">61-90 Days</TableHead>
                        <TableHead className="text-right">90+ Days</TableHead>
                        <TableHead className="text-right">Total</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {aging.items.map((item) => (
                        <TableRow key={item.partyId}>
                          <TableCell className="font-medium">{item.code}</TableCell>
                          <TableCell>{item.name}</TableCell>
                          <TableCell className="text-right">{formatCurrency(item.bucket0_30)}</TableCell>
                          <TableCell className="text-right">{formatCurrency(item.bucket31_60)}</TableCell>
                          <TableCell className="text-right">{formatCurrency(item.bucket61_90)}</TableCell>
                          <TableCell className="text-right">{formatCurrency(item.bucket90Plus)}</TableCell>
                          <TableCell className="text-right font-bold">{formatCurrency(item.total)}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                  <div className="flex justify-between items-center mt-4">
                    <div className="text-sm text-gray-600">
                      Showing {aging.items.length} of {aging.total} results
                    </div>
                    <div className="flex gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={page === 1}
                        onClick={() => setPage(page - 1)}
                      >
                        Previous
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={page * 50 >= aging.total}
                        onClick={() => setPage(page + 1)}
                      >
                        Next
                      </Button>
                    </div>
                  </div>
                </>
              ) : (
                <div className="text-center py-8 text-gray-500">No results found</div>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
