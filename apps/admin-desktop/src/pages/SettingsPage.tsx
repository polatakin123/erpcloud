import React, { useState } from 'react';
import { SettingsService } from '@/lib/settings';
import { ApiClient } from '@/lib/api-client';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

export function SettingsPage() {
  const [apiBaseUrl, setApiBaseUrl] = useState('');
  const [loading, setLoading] = useState(false);
  const [saved, setSaved] = useState(false);

  React.useEffect(() => {
    SettingsService.getSettings().then((settings) => {
      setApiBaseUrl(settings.apiBaseUrl);
    });
  }, []);

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setSaved(false);

    try {
      await SettingsService.setApiBaseUrl(apiBaseUrl);
      ApiClient.setBaseUrl(apiBaseUrl);
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    } catch (error) {
      console.error('Failed to save settings:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-6">
      <h1 className="text-3xl font-bold mb-6">Settings</h1>
      
      <Card className="max-w-2xl">
        <CardHeader>
          <CardTitle>API Configuration</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSave} className="space-y-4">
            <div>
              <label htmlFor="apiBaseUrl" className="block text-sm font-medium mb-2">
                API Base URL
              </label>
              <Input
                id="apiBaseUrl"
                type="url"
                placeholder="http://localhost:5000"
                value={apiBaseUrl}
                onChange={(e) => setApiBaseUrl(e.target.value)}
              />
              <p className="text-xs text-muted-foreground mt-1">
                The base URL of your ERP Cloud API server
              </p>
            </div>

            {saved && (
              <div className="text-sm text-green-600">
                Settings saved successfully!
              </div>
            )}

            <Button type="submit" disabled={loading}>
              {loading ? 'Saving...' : 'Save Settings'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
