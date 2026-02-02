// Check if running in Tauri
const isTauri = typeof window !== 'undefined' && '__TAURI__' in window;

let store: any = null;
if (isTauri) {
  const { Store } = await import('@tauri-apps/plugin-store');
  store = new Store('settings.json');
}

export interface AppSettings {
  apiBaseUrl: string;
  authToken: string | null;
}

const DEFAULT_SETTINGS: AppSettings = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000',
  authToken: null,
};

// Browser fallback using localStorage
class BrowserStorage {
  static get(key: string): string | null {
    return localStorage.getItem(key);
  }

  static set(key: string, value: string): void {
    localStorage.setItem(key, value);
  }

  static delete(key: string): void {
    localStorage.removeItem(key);
  }
}

export class SettingsService {
  static async getSettings(): Promise<AppSettings> {
    if (isTauri && store) {
      const apiBaseUrl = await store.get<string>('apiBaseUrl') || DEFAULT_SETTINGS.apiBaseUrl;
      const authToken = await store.get<string>('authToken') || null;
      return { apiBaseUrl, authToken };
    } else {
      // Browser fallback
      const apiBaseUrl = BrowserStorage.get('apiBaseUrl') || DEFAULT_SETTINGS.apiBaseUrl;
      const authToken = BrowserStorage.get('authToken') || null;
      return { apiBaseUrl, authToken };
    }
  }

  static async setApiBaseUrl(url: string): Promise<void> {
    if (isTauri && store) {
      await store.set('apiBaseUrl', url);
      await store.save();
    } else {
      BrowserStorage.set('apiBaseUrl', url);
    }
  }

  static async setAuthToken(token: string | null): Promise<void> {
    if (isTauri && store) {
      await store.set('authToken', token);
      await store.save();
    } else {
      if (token) {
        BrowserStorage.set('authToken', token);
      } else {
        BrowserStorage.delete('authToken');
      }
    }
  }

  static async clearAuthToken(): Promise<void> {
    if (isTauri && store) {
      await store.delete('authToken');
      await store.save();
    } else {
      BrowserStorage.delete('authToken');
    }
  }
}
