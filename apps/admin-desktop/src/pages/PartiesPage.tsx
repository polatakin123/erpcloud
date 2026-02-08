import { useState } from 'react';
import { useParties, useCreateParty } from '@/hooks/useParties';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import type { CreatePartyDto } from '@/types/party';

export function PartiesPage() {
  const [search, setSearch] = useState('');
  const [showCreateForm, setShowCreateForm] = useState(false);
  const { data, isLoading, error } = useParties(search);

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Cariler</h1>
        <Button onClick={() => setShowCreateForm(!showCreateForm)}>
          {showCreateForm ? 'Vazgeç' : 'Yeni Cari'}
        </Button>
      </div>

      {showCreateForm && (
        <CreatePartyForm onSuccess={() => setShowCreateForm(false)} />
      )}

      <div className="mb-4">
        <Input
          placeholder="Cari ara..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="max-w-md"
        />
      </div>

      {isLoading && <div>Yükleniy or...</div>}
      
      {error && (
        <div className="text-destructive">
          Cariler yüklenirken hata: {error instanceof Error ? error.message : 'Bilinmeyen hata'}
        </div>
      )}

      {data && (
        <div className="space-y-4">
          <div className="text-sm text-muted-foreground">
            {data.totalCount} cari bulundu
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {data.items.map((party) => (
              <Card key={party.id}>
                <CardHeader>
                  <CardTitle className="text-lg">{party.name}</CardTitle>
                  <div className="text-sm text-muted-foreground">{party.code}</div>
                </CardHeader>
                <CardContent>
                  <div className="space-y-1 text-sm">
                    <div>
                      <span className="font-medium">Tip:</span> {party.type === 'CUSTOMER' ? 'Müşteri' : party.type === 'SUPPLIER' ? 'Tedarikçi' : party.type}
                    </div>
                    {party.email && (
                      <div>
                        <span className="font-medium">E-posta:</span> {party.email}
                      </div>
                    )}
                    {party.phone && (
                      <div>
                        <span className="font-medium">Telefon:</span> {party.phone}
                      </div>
                    )}
                    <div>
                      <span className="font-medium">Durum:</span>{' '}
                      <span
                        className={
                          party.isActive ? 'text-green-600' : 'text-red-600'
                        }
                      >
                        {party.isActive ? 'Aktif' : 'Pasif'}
                      </span>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

function CreatePartyForm({ onSuccess }: { onSuccess: () => void }) {
  const [formData, setFormData] = useState<CreatePartyDto>({
    code: '',
    name: '',
    type: 'CUSTOMER',
    isActive: true,
  });
  
  const createMutation = useCreateParty();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      await createMutation.mutateAsync(formData);
      onSuccess();
    } catch (error) {
      console.error('Failed to create party:', error);
    }
  };

  return (
    <Card className="mb-6">
      <CardHeader>
        <CardTitle>Yeni Cari Oluştur</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-2">
                Kod *
              </label>
              <Input
                required
                value={formData.code}
                onChange={(e) =>
                  setFormData({ ...formData, code: e.target.value })
                }
                placeholder="MÜŞ001"
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">
                İsim *
              </label>
              <Input
                required
                value={formData.name}
                onChange={(e) =>
                  setFormData({ ...formData, name: e.target.value })
                }
                placeholder="Cari İsmi"
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">
                Tip *
              </label>
              <select
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                value={formData.type}
                onChange={(e) =>
                  setFormData({ ...formData, type: e.target.value })
                }
              >
                <option value="CUSTOMER">Müşteri</option>
                <option value="SUPPLIER">Tedarikçi</option>
                <option value="BOTH">Her İkisi</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">
                E-posta
              </label>
              <Input
                type="email"
                value={formData.email || ''}
                onChange={(e) =>
                  setFormData({ ...formData, email: e.target.value })
                }
                placeholder="ornek@sirket.com"
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">
                Telefon
              </label>
              <Input
                value={formData.phone || ''}
                onChange={(e) =>
                  setFormData({ ...formData, phone: e.target.value })
                }
                placeholder="+90 555 123 4567"
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">
                Vergi Numarası
              </label>
              <Input
                value={formData.taxNumber || ''}
                onChange={(e) =>
                  setFormData({ ...formData, taxNumber: e.target.value })
                }
                placeholder="1234567890"
              />
            </div>
          </div>

          {createMutation.error && (
            <div className="text-destructive text-sm">
              {createMutation.error instanceof Error
                ? createMutation.error.message
                : 'Cari oluşturulamadı'}
            </div>
          )}

          <Button type="submit" disabled={createMutation.isPending}>
            {createMutation.isPending ? 'Oluşturuluyor...' : 'Cari Oluştur'}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
