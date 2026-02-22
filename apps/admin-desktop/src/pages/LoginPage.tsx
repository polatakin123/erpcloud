import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ApiClient } from '@/lib/api-client';
import { useAuthStore } from '@/lib/auth-store';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

interface LoginResponse {
  token: string;
  expiresAt: string;
  username: string;
  role: string;
}

export function LoginPage({ onLoginSuccess }: { onLoginSuccess?: () => void }) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const setAuth = useAuthStore((state) => state.setAuth);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    
    if (!username.trim() || !password.trim()) {
      setError('Kullanıcı adı ve şifre gerekli');
      return;
    }

    setLoading(true);
    try {
      const response = await ApiClient.post<LoginResponse>('/api/auth/login', {
        username: username.trim(),
        password: password.trim()
      }, { skipAuth: true });
      
      localStorage.setItem('accessToken', response.token);
      ApiClient.setToken(response.token);
      setAuth(response.token);

      onLoginSuccess?.();
      
      // Permission based redirect - wait for setAuth to complete
      setTimeout(() => {
        const { hasPerm } = useAuthStore.getState();
        if (hasPerm('ADMIN.SETTINGS')) {
          navigate('/dashboard');
        } else {
          navigate('/tezgah');
        }
      }, 0);
    } catch (err: any) {
      console.error('Login failed:', err);
      if (err.status === 401) {
        setError('Kullanıcı adı veya şifre hatalı');
      } else {
        setError(err.message || 'Giriş yapılırken bir hata oluştu');
      }
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
            Lütfen giriş yapın
          </p>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleLogin} className="space-y-4">
            <div>
              <label htmlFor="username" className="block text-sm font-medium mb-2">
                Kullanıcı Adı
              </label>
              <Input
                id="username"
                type="text"
                placeholder="admin veya kasiyer"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                disabled={loading}
                autoComplete="username"
              />
            </div>

            <div>
              <label htmlFor="password" className="block text-sm font-medium mb-2">
                Şifre
              </label>
              <Input
                id="password"
                type="password"
                placeholder="Şifrenizi girin"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                disabled={loading}
                autoComplete="current-password"
              />
            </div>
            
            {error && (
              <div className="text-sm text-destructive">{error}</div>
            )}
            
            <Button type="submit" className="w-full" disabled={loading}>
              {loading ? 'Giriş yapılıyor...' : 'Giriş Yap'}
            </Button>
            
            <div className="text-xs text-muted-foreground space-y-1">
              <div>Admin: admin / Admin123!</div>
              <div>Kasiyer: kasiyer / Kasiyer123!</div>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
