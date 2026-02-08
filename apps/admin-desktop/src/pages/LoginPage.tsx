import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { SettingsService } from '@/lib/settings';
import { ApiClient } from '@/lib/api-client';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

export function LoginPage({ onLoginSuccess }: { onLoginSuccess?: () => void }) {
  const [token, setToken] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  // Auto-load demo token in development
  useEffect(() => {
    if (import.meta.env.DEV) {
      const demoToken = import.meta.env.VITE_DEMO_TOKEN;
      if (demoToken) {
        setToken(demoToken);
      }
    }
  }, []);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    
    if (!token.trim()) {
      setError('Lütfen bir token girin');
      return;
    }

    setLoading(true);
    try {
      // Save token
      await SettingsService.setAuthToken(token);
      ApiClient.setToken(token);

      // Notify parent component
      onLoginSuccess?.();

      // Navigate to dashboard
      navigate('/');
    } catch (err) {
      setError('Token kaydedilemedi');
      console.error('Giriş hatası:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>ERP Cloud Yönetim</CardTitle>
          <p className="text-sm text-muted-foreground">
            Devam etmek için JWT token'ınızı yapıştırın
          </p>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleLogin} className="space-y-4">
            <div>
              <label htmlFor="token" className="block text-sm font-medium mb-2">
                JWT Token
              </label>
              <Input
                id="token"
                type="text"
                placeholder="eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9..."
                value={token}
                onChange={(e) => setToken(e.target.value)}
                className="font-mono text-xs"
              />
            </div>
            
            {error && (
              <div className="text-sm text-destructive">{error}</div>
            )}
            
            <Button type="submit" className="w-full" disabled={loading}>
              {loading ? 'Kaydediliyor...' : 'Giriş Yap'}
            </Button>
            
            {import.meta.env.DEV && (
              <div className="text-xs text-muted-foreground text-center">
                Development mode: Token otomatik yüklendi
              </div>
            )}
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
