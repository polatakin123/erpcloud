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
        <h1 className="text-3xl font-bold">Parties</h1>
        <Button onClick={() => setShowCreateForm(!showCreateForm)}>
          {showCreateForm ? 'Cancel' : 'New Party'}
        </Button>
      </div>

      {showCreateForm && (
        <CreatePartyForm onSuccess={() => setShowCreateForm(false)} />
      )}

      <div className="mb-4">
        <Input
          placeholder="Search parties..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="max-w-md"
        />
      </div>

      {isLoading && <div>Loading...</div>}
      
      {error && (
        <div className="text-destructive">
          Error loading parties: {error instanceof Error ? error.message : 'Unknown error'}
        </div>
      )}

      {data && (
        <div className="space-y-4">
          <div className="text-sm text-muted-foreground">
            Found {data.totalCount} parties
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
                      <span className="font-medium">Type:</span> {party.type}
                    </div>
                    {party.email && (
                      <div>
                        <span className="font-medium">Email:</span> {party.email}
                      </div>
                    )}
                    {party.phone && (
                      <div>
                        <span className="font-medium">Phone:</span> {party.phone}
                      </div>
                    )}
                    <div>
                      <span className="font-medium">Status:</span>{' '}
                      <span
                        className={
                          party.isActive ? 'text-green-600' : 'text-red-600'
                        }
                      >
                        {party.isActive ? 'Active' : 'Inactive'}
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
        <CardTitle>Create New Party</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-2">
                Code *
              </label>
              <Input
                required
                value={formData.code}
                onChange={(e) =>
                  setFormData({ ...formData, code: e.target.value })
                }
                placeholder="CUST001"
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">
                Name *
              </label>
              <Input
                required
                value={formData.name}
                onChange={(e) =>
                  setFormData({ ...formData, name: e.target.value })
                }
                placeholder="Customer Name"
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">
                Type *
              </label>
              <select
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                value={formData.type}
                onChange={(e) =>
                  setFormData({ ...formData, type: e.target.value })
                }
              >
                <option value="CUSTOMER">Customer</option>
                <option value="SUPPLIER">Supplier</option>
                <option value="BOTH">Both</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">
                Email
              </label>
              <Input
                type="email"
                value={formData.email || ''}
                onChange={(e) =>
                  setFormData({ ...formData, email: e.target.value })
                }
                placeholder="email@example.com"
              />
            </div>

            <div>
              <label className="block text-sm font-medium mb-2">
                Phone
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
                Tax Number
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
                : 'Failed to create party'}
            </div>
          )}

          <Button type="submit" disabled={createMutation.isPending}>
            {createMutation.isPending ? 'Creating...' : 'Create Party'}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
