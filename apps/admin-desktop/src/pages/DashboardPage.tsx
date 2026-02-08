import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

export function DashboardPage() {
  return (
    <div className="p-6">
      <h1 className="text-3xl font-bold mb-6">Kontrol Paneli</h1>
      
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Hoş Geldiniz</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              ERP Cloud Yönetim Konsolu - Yedek parça ve ERP modüllerini yönetmek için iç yönetim paneli.
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Hızlı İşlemler</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2 text-sm">
              <div>→ Cari Ekle</div>
              <div>→ Ürün Ekle</div>
              <div>→ Satış Siparişi</div>
              <div>→ Tahsilat / Ödeme</div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Modüller</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-1 text-sm text-muted-foreground">
              <div>Katalog • Cariler • Satış</div>
              <div>Satın Alma • Stok • Muhasebe</div>
              <div>Kasa & Banka</div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
