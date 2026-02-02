import { useState } from 'react';
import { useProducts, useCreateProduct } from '@/hooks/useProducts';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

export function ProductsPage() {
  const [search, setSearch] = useState('');
  const { data, isLoading } = useProducts(search);
  const [showForm, setShowForm] = useState(false);

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Products</h1>
        <Button onClick={() => setShowForm(!showForm)}>
          {showForm ? 'Cancel' : 'New Product'}
        </Button>
      </div>

      {showForm && <CreateProductForm onSuccess={() => setShowForm(false)} />}

      <Input
        placeholder="Search products..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        className="max-w-md mb-4"
      />

      {isLoading && <div>Loading...</div>}

      {data && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {data.items.map((product) => (
            <Card key={product.id}>
              <CardHeader>
                <CardTitle className="text-lg">{product.name}</CardTitle>
                <div className="text-sm text-muted-foreground">{product.code}</div>
              </CardHeader>
              <CardContent>
                {product.description && (
                  <p className="text-sm text-muted-foreground">{product.description}</p>
                )}
                <div className="mt-2 text-sm">
                  Status: {product.isActive ? 'Active' : 'Inactive'}
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
  const createMutation = useCreateProduct();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await createMutation.mutateAsync({ code, name });
      onSuccess();
    } catch (error) {
      console.error('Failed to create product:', error);
    }
  };

  return (
    <Card className="mb-6">
      <CardHeader>
        <CardTitle>Create New Product</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-2">Code *</label>
            <Input
              required
              value={code}
              onChange={(e) => setCode(e.target.value)}
              placeholder="PROD001"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-2">Name *</label>
            <Input
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Product Name"
            />
          </div>
          {createMutation.error && (
            <div className="text-destructive text-sm">Failed to create product</div>
          )}
          <Button type="submit" disabled={createMutation.isPending}>
            {createMutation.isPending ? 'Creating...' : 'Create Product'}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
