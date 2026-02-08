import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useProducts, useCreateProduct } from '@/hooks/useProducts';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { BrandSelect } from '@/components/brands/BrandSelect';

export function ProductsPage() {
  const [search, setSearch] = useState('');
  const { data, isLoading } = useProducts(search);
  const [showForm, setShowForm] = useState(false);
  const navigate = useNavigate();

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Ürünler (Yönetici)</h1>
        <Button onClick={() => setShowForm(!showForm)}>
          {showForm ? 'Vazgeç' : 'Yeni Ürün'}
        </Button>
      </div>

      {showForm && <CreateProductForm onSuccess={() => setShowForm(false)} />}

      <Input
        placeholder="Ürün ara..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        className="max-w-md mb-4"
      />

      {isLoading && <div>Yükleniy or...</div>}

      {data && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {data.items.map((product) => (
            <Card 
              key={product.id}
              className="cursor-pointer hover:border-blue-500 transition-colors"
              onClick={() => navigate(`/products/${product.id}`)}
            >
              <CardHeader>
                <CardTitle className="text-lg">{product.name}</CardTitle>
                <div className="text-sm text-muted-foreground">{product.code}</div>
              </CardHeader>
              <CardContent>
                {product.description && (
                  <p className="text-sm text-muted-foreground">{product.description}</p>
                )}
                <div className="mt-2 text-sm">
                  Durum: {product.isActive ? 'Aktif' : 'Pasif'}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}

function CreateProductForm({ onSuccess }: { onSuccess: () => void }) {
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [brandId, setBrandId] = useState<string | undefined>();
  const [brandError, setBrandError] = useState('');
  const createMutation = useCreateProduct();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    // Validate brand is selected
    if (!brandId) {
      setBrandError('Marka seçilmelidir.');
      return;
    }
    
    try {
      await createMutation.mutateAsync({ code, name, brandId });
      onSuccess();
    } catch (error) {
      console.error('Ürün oluşturulamadı:', error);
    }
  };

  return (
    <Card className="mb-6">
      <CardHeader>
        <CardTitle>Yeni Ürün Oluştur</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-2">Kod *</label>
            <Input
              required
              value={code}
              onChange={(e) => setCode(e.target.value)}
              placeholder="Örn: BLT001"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-2">İsim *</label>
            <Input
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Ürün Adı"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-2">Marka *</label>
            <BrandSelect
              value={brandId}
              onChange={(id) => {
                setBrandId(id);
                setBrandError('');
              }}
              error={brandError}
            />
          </div>
          {createMutation.error && (
            <div className="text-destructive text-sm">Ürün oluşturulamadı</div>
          )}
          <Button type="submit" disabled={createMutation.isPending}>
            {createMutation.isPending ? 'Oluşturuluyor...' : 'Ürün Oluştur'}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
