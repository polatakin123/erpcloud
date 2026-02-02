import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

export function DashboardPage() {
  return (
    <div className="p-6">
      <h1 className="text-3xl font-bold mb-6">Dashboard</h1>
      
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Welcome</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              ERP Cloud Admin Desktop - Internal management console for testing and managing ERP modules.
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Quick Actions</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2 text-sm">
              <div>→ Create Party</div>
              <div>→ Create Product</div>
              <div>→ New Sales Order</div>
              <div>→ Record Payment</div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Modules</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-1 text-sm text-muted-foreground">
              <div>Catalog • Parties • Sales</div>
              <div>Purchase • Stock • Accounting</div>
              <div>Cash & Bank</div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
