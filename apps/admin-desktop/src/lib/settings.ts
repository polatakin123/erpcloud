// Check if running in Tauri
const isTauri = typeof window !== 'undefined' && '__TAURI__' in window;

let store: any = null;
let storePromise: Promise<any> | null = null;

async function getStore() {
  if (store) return store;
  if (storePromise) return storePromise;
  
  if (isTauri) {
    storePromise = import('@tauri-apps/plugin-store').then(async ({ Store }) => {
      store = await Store.load('settings.json');
      return store;
    });
    return storePromise;
  }
  return null;
}

export interface AppSettings {
  apiBaseUrl: string;
  authToken: string | null;
}

const DEFAULT_SETTINGS: AppSettings = {
  apiBaseUrl: (import.meta.env?.VITE_API_BASE_URL as string | undefined) || 'http://localhost:5000',
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
    const tauriStore = await getStore();
    if (isTauri && tauriStore) {
      const apiBaseUrl = (await tauriStore.get('apiBaseUrl')) as string | null || DEFAULT_SETTINGS.apiBaseUrl;
      const authToken = (await tauriStore.get('authToken')) as string | null || null;
      return { apiBaseUrl, authToken };
    } else {
      // Browser fallback
      const apiBaseUrl = BrowserStorage.get('apiBaseUrl') || DEFAULT_SETTINGS.apiBaseUrl;
      const authToken = BrowserStorage.get('authToken') || null;
      return { apiBaseUrl, authToken };
    }
  }

  static async setApiBaseUrl(url: string): Promise<void> {
    const tauriStore = await getStore();
    if (isTauri && tauriStore) {
      await tauriStore.set('apiBaseUrl', url);
      await tauriStore.save();
    } else {
      BrowserStorage.set('apiBaseUrl', url);
    }
  }

  static async setAuthToken(token: string | null): Promise<void> {
    const tauriStore = await getStore();
    if (isTauri && tauriStore) {
      await tauriStore.set('authToken', token);
      await tauriStore.save();
    } else {
      if (token) {
        BrowserStorage.set('authToken', token);
      } else {
        BrowserStorage.delete('authToken');
      }
    }
  }

  static async clearAuthToken(): Promise<void> {
    const tauriStore = await getStore();
    if (isTauri && tauriStore) {
      await tauriStore.delete('authToken');
      await tauriStore.save();
    } else {
      BrowserStorage.delete('authToken');
    }
  }
}
