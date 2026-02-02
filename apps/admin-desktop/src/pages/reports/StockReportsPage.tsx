import { useState } from 'react';
import { useStockBalances, useStockMovements } from '../../hooks/useReports';
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card';
import { Input } from '../../components/ui/input';
import { Button } from '../../components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table';

export function StockReportsPage() {
  const [activeTab, setActiveTab] = useState<'balances' | 'movements'>('balances');
  
  // Balance filters
  const [warehouseId, setWarehouseId] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [balancePage, setBalancePage] = useState(1);
  
  // Movement filters
  const [movWarehouseId, setMovWarehouseId] = useState('');
  const [variantId, setVariantId] = useState('');
  const [movementType, setMovementType] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [movPage, setMovPage] = useState(1);

  const { data: balances, isLoading: balancesLoading } = useStockBalances(
    warehouseId,
    searchQuery,
    balancePage,
    50
  );

  const { data: movements, isLoading: movementsLoading } = useStockMovements(
    movWarehouseId || undefined,
    variantId || undefined,
    movementType || undefined,
    fromDate || undefined,
    toDate || undefined,
    movPage,
    50
  );

  return (
    <div className="p-6 space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-3xl font-bold">Stock Reports</h1>
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
          Stock Balances
        </button>
        <button
          className={`px-4 py-2 font-medium ${
            activeTab === 'movements'
              ? 'border-b-2 border-blue-500 text-blue-600'
              : 'text-gray-600 hover:text-gray-900'
          }`}
          onClick={() => setActiveTab('movements')}
        >
          Stock Movements
        </button>
      </div>

      {/* Stock Balances */}
      {activeTab === 'balances' && (
        <Card>
          <CardHeader>
            <CardTitle>Stock Balance Summary</CardTitle>
            <div className="grid grid-cols-3 gap-4 mt-4">
              <Input
                placeholder="Warehouse ID (required)"
                value={warehouseId}
                onChange={(e) => setWarehouseId(e.target.value)}
              />
              <Input
                placeholder="Search SKU or Name"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </div>
          </CardHeader>
          <CardContent>
            {balancesLoading ? (
              <div className="text-center py-8">Loading...</div>
            ) : balances && balances.items.length > 0 ? (
              <>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>SKU</TableHead>
                      <TableHead>Variant Name</TableHead>
                      <TableHead>Unit</TableHead>
                      <TableHead className="text-right">On Hand</TableHead>
                      <TableHead className="text-right">Reserved</TableHead>
                      <TableHead className="text-right">Available</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {balances.items.map((item) => (
                      <TableRow key={item.variantId}>
                        <TableCell>{item.sku}</TableCell>
                        <TableCell>{item.variantName}</TableCell>
                        <TableCell>{item.unit}</TableCell>
                        <TableCell className="text-right">{item.onHand.toFixed(2)}</TableCell>
                        <TableCell className="text-right">{item.reserved.toFixed(2)}</TableCell>
                        <TableCell className="text-right">{item.available.toFixed(2)}</TableCell>
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
                      disabled={balancePage === 1}
                      onClick={() => setBalancePage(balancePage - 1)}
                    >
                      Previous
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={balancePage * 50 >= balances.total}
                      onClick={() => setBalancePage(balancePage + 1)}
                    >
                      Next
                    </Button>
                  </div>
                </div>
              </>
            ) : (
              <div className="text-center py-8 text-gray-500">
                {warehouseId ? 'No results found' : 'Enter Warehouse ID to view balances'}
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Stock Movements */}
      {activeTab === 'movements' && (
        <Card>
          <CardHeader>
            <CardTitle>Stock Movements (Ledger)</CardTitle>
            <div className="grid grid-cols-3 gap-4 mt-4">
              <Input
                placeholder="Warehouse ID"
                value={movWarehouseId}
                onChange={(e) => setMovWarehouseId(e.target.value)}
              />
              <Input
                placeholder="Variant ID"
                value={variantId}
                onChange={(e) => setVariantId(e.target.value)}
              />
              <Input
                placeholder="Movement Type"
                value={movementType}
                onChange={(e) => setMovementType(e.target.value)}
              />
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
            </div>
          </CardHeader>
          <CardContent>
            {movementsLoading ? (
              <div className="text-center py-8">Loading...</div>
            ) : movements && movements.items.length > 0 ? (
              <>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Date/Time</TableHead>
                      <TableHead>Type</TableHead>
                      <TableHead className="text-right">Quantity</TableHead>
                      <TableHead>Reference Type</TableHead>
                      <TableHead>Reference ID</TableHead>
                      <TableHead>Note</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {movements.items.map((item, idx) => (
                      <TableRow key={idx}>
                        <TableCell>{new Date(item.occurredAt).toLocaleString()}</TableCell>
                        <TableCell>{item.movementType}</TableCell>
                        <TableCell className="text-right">{item.quantity.toFixed(2)}</TableCell>
                        <TableCell>{item.referenceType}</TableCell>
                        <TableCell className="text-xs">{item.referenceId}</TableCell>
                        <TableCell>{item.note || '-'}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
                <div className="flex justify-between items-center mt-4">
                  <div className="text-sm text-gray-600">
                    Showing {movements.items.length} of {movements.total} results
                  </div>
                  <div className="flex gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={movPage === 1}
                      onClick={() => setMovPage(movPage - 1)}
                    >
                      Previous
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={movPage * 50 >= movements.total}
                      onClick={() => setMovPage(movPage + 1)}
                    >
                      Next
                    </Button>
                  </div>
                </div>
              </>
            ) : (
              <div className="text-center py-8 text-gray-500">
                No movements found
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
