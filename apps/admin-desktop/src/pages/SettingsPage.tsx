import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Settings, Server, Save, CheckCircle, AlertCircle, ArrowLeft } from 'lucide-react';
import { SettingsService } from '@/lib/settings';
import { ApiClient } from '@/lib/api-client';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

export function SettingsPage() {
  const navigate = useNavigate();
  const [apiBaseUrl, setApiBaseUrl] = useState('');
  const [loading, setLoading] = useState(false);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState('');
  const [isTestingConnection, setIsTestingConnection] = useState(false);
  const [connectionStatus, setConnectionStatus] = useState<'idle' | 'success' | 'error'>('idle');
  const [connectionMessage, setConnectionMessage] = useState('');

  React.useEffect(() => {
    SettingsService.getSettings().then((settings) => {
      setApiBaseUrl(settings.apiBaseUrl);
    });
  }, []);

  const testConnection = async () => {
    if (!apiBaseUrl.trim()) {
      setError('API adresi boş olamaz');
      return;
    }

    setIsTestingConnection(true);
    setConnectionStatus('idle');
    setError('');
    setConnectionMessage('');

    try {
      // Test connection to /health endpoint
      const response = await fetch(`${apiBaseUrl.trim()}/health`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        setConnectionStatus('success');
        setConnectionMessage('Sunucu erişilebilir durumda');
      } else {
        setConnectionStatus('error');
        setConnectionMessage(`Sunucu yanıt verdi ama hata döndü (${response.status})`);
      }
    } catch (err) {
      setConnectionStatus('error');
      setConnectionMessage('Sunucuya bağlanılamadı. Lütfen API adresini ve ağ bağlantısını kontrol edin.');
    } finally {
      setIsTestingConnection(false);
    }
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!apiBaseUrl.trim()) {
      setError('API adresi boş olamaz');
      return;
    }

    // Validate URL format
    try {
      new URL(apiBaseUrl.trim());
    } catch {
      setError('Geçersiz URL formatı. Örnek: http://localhost:5039 veya https://api.sirket.com');
      return;
    }

    setLoading(true);
    setSaved(false);
    setError('');

    try {
      await SettingsService.setApiBaseUrl(apiBaseUrl.trim());
      ApiClient.setBaseUrl(apiBaseUrl.trim());
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    } catch (err) {
      setError('Ayarlar kaydedilemedi. Lütfen tekrar deneyin.');
      console.error('Failed to save settings:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-4xl mx-auto p-6">
      {/* Header */}
      <div className="mb-6">
        <Button
          variant="ghost"
          onClick={() => navigate(-1)}
          className="mb-4"
        >
          <ArrowLeft className="h-4 w-4 mr-2" />
          Geri Dön
        </Button>
        <div className="flex items-center gap-3 mb-2">
          <Settings className="h-8 w-8 text-blue-600" />
          <h1 className="text-3xl font-bold text-gray-900">Uygulama Ayarları</h1>
        </div>
        <p className="text-gray-600">
          API sunucu adresi ve bağlantı ayarlarını yapılandırın
        </p>
      </div>
      
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Server className="h-5 w-5 text-blue-600" />
            <CardTitle>API Sunucu Ayarları</CardTitle>
          </div>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSave} className="space-y-4">
            <div>
              <label htmlFor="apiBaseUrl" className="block text-sm font-medium mb-2">
                API Sunucu Adresi
              </label>
              <Input
                id="apiBaseUrl"
                type="text"
                placeholder="Örn: http://localhost:5039 veya https://api.sirket.com"
                value={apiBaseUrl}
                onChange={(e) => {
                  setApiBaseUrl(e.target.value);
                  setConnectionStatus('idle');
                  setError('');
                  setSaved(false);
                }}
                className="font-mono"
              />
              <p className="text-xs text-gray-500 mt-1">
                Backend API sunucunuzun tam adresi (protokol dahil: http:// veya https://)
              </p>
            </div>

            {/* Test Connection */}
            <div>
              <Button
                type="button"
                onClick={testConnection}
                disabled={isTestingConnection || !apiBaseUrl.trim()}
                variant="outline"
                className="w-full"
              >
                {isTestingConnection ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600 mr-2"></div>
                    Bağlantı Test Ediliyor...
                  </>
                ) : (
                  <>
                    <Server className="h-4 w-4 mr-2" />
                    Bağlantıyı Test Et
                  </>
                )}
              </Button>
            </div>

            {/* Connection Status */}
            {connectionStatus === 'success' && (
              <div className="bg-green-50 border border-green-200 rounded-lg p-3 flex items-center gap-2">
                <CheckCircle className="h-5 w-5 text-green-600 flex-shrink-0" />
                <div>
                  <p className="text-sm font-medium text-green-800">Bağlantı Başarılı</p>
                  <p className="text-xs text-green-700">{connectionMessage}</p>
                </div>
              </div>
            )}

            {connectionStatus === 'error' && (
              <div className="bg-red-50 border border-red-200 rounded-lg p-3 flex items-center gap-2">
                <AlertCircle className="h-5 w-5 text-red-600 flex-shrink-0" />
                <div>
                  <p className="text-sm font-medium text-red-800">Bağlantı Hatası</p>
                  <p className="text-xs text-red-700">{connectionMessage}</p>
                </div>
              </div>
            )}

            {saved && (
              <div className="bg-green-50 border border-green-200 rounded-lg p-3 flex items-center gap-2">
                <CheckCircle className="h-5 w-5 text-green-600" />
                <p className="text-sm font-medium text-green-800">
                  Ayarlar başarıyla kaydedildi
                </p>
              </div>
            )}

            {error && (
              <div className="bg-red-50 border border-red-200 rounded-lg p-3 flex items-center gap-2">
                <AlertCircle className="h-5 w-5 text-red-600" />
                <p className="text-sm text-red-700">{error}</p>
              </div>
            )}

            <div className="pt-4 border-t">
              <Button type="submit" disabled={loading || !apiBaseUrl.trim()} className="w-full">
                {loading ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                    Kaydediliyor...
                  </>
                ) : (
                  <>
                    <Save className="h-4 w-4 mr-2" />
                    Kaydet
                  </>
                )}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      {/* Info Card */}
      <Card className="mt-6 bg-blue-50 border-blue-200">
        <CardContent className="pt-6">
          <div className="flex gap-3">
            <AlertCircle className="h-5 w-5 text-blue-600 flex-shrink-0 mt-0.5" />
            <div className="text-sm text-blue-800">
              <p className="font-medium mb-2">Önemli Bilgiler:</p>
              <ul className="list-disc list-inside space-y-1">
                <li>API sunucusu ayrı bir makinede çalışmalıdır</li>
                <li>Sunucu adresi ağdaki diğer bilgisayarlardan erişilebilir olmalıdır</li>
                <li>HTTPS kullanıyorsanız geçerli SSL sertifikası olmalıdır</li>
                <li>Değişiklikler hemen etkili olur, yeniden başlatma gerekmez</li>
              </ul>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Version Info */}
      <div className="mt-6 text-center text-sm text-gray-500">
        <p>ERP Cloud Masaüstü Uygulaması v1.0.0</p>
        <p className="text-xs mt-1">© 2026 ERP Cloud. Tüm hakları saklıdır.</p>
      </div>
    </div>
  );
}
