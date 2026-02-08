// Vehicle selection context store (similar to context-store.ts)
const isTauri = typeof window !== 'undefined' && '__TAURI__' in window;

let store: any = null;
if (isTauri) {
  // Tauri Store initialization will happen async
  import('@tauri-apps/plugin-store').then(({ Store }) => {
    (Store as any).load('vehicle-context.json').then((s: any) => {
      store = s;
    });
  });
}

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

export interface VehicleContext {
  selectedBrandId: string | null;
  selectedModelId: string | null;
  selectedYearId: string | null;
  selectedEngineId: string | null;
}

export class VehicleContextStore {
  static async getContext(): Promise<VehicleContext> {
    if (isTauri && store) {
      const selectedBrandId = (await store.get('selectedBrandId')) || null;
      const selectedModelId = (await store.get('selectedModelId')) || null;
      const selectedYearId = (await store.get('selectedYearId')) || null;
      const selectedEngineId = (await store.get('selectedEngineId')) || null;
      return { selectedBrandId, selectedModelId, selectedYearId, selectedEngineId };
    } else {
      const selectedBrandId = BrowserStorage.get('selectedBrandId') || null;
      const selectedModelId = BrowserStorage.get('selectedModelId') || null;
      const selectedYearId = BrowserStorage.get('selectedYearId') || null;
      const selectedEngineId = BrowserStorage.get('selectedEngineId') || null;
      return { selectedBrandId, selectedModelId, selectedYearId, selectedEngineId };
    }
  }

  static async setSelectedBrand(brandId: string | null): Promise<void> {
    if (isTauri && store) {
      if (brandId) {
        await store.set('selectedBrandId', brandId);
      } else {
        await store.delete('selectedBrandId');
      }
      // Clear downstream selections
      await store.delete('selectedModelId');
      await store.delete('selectedYearId');
      await store.delete('selectedEngineId');
      await store.save();
    } else {
      if (brandId) {
        BrowserStorage.set('selectedBrandId', brandId);
      } else {
        BrowserStorage.delete('selectedBrandId');
      }
      // Clear downstream selections
      BrowserStorage.delete('selectedModelId');
      BrowserStorage.delete('selectedYearId');
      BrowserStorage.delete('selectedEngineId');
    }
  }

  static async setSelectedModel(modelId: string | null): Promise<void> {
    if (isTauri && store) {
      if (modelId) {
        await store.set('selectedModelId', modelId);
      } else {
        await store.delete('selectedModelId');
      }
      // Clear downstream selections
      await store.delete('selectedYearId');
      await store.delete('selectedEngineId');
      await store.save();
    } else {
      if (modelId) {
        BrowserStorage.set('selectedModelId', modelId);
      } else {
        BrowserStorage.delete('selectedModelId');
      }
      // Clear downstream selections
      BrowserStorage.delete('selectedYearId');
      BrowserStorage.delete('selectedEngineId');
    }
  }

  static async setSelectedYear(yearId: string | null): Promise<void> {
    if (isTauri && store) {
      if (yearId) {
        await store.set('selectedYearId', yearId);
      } else {
        await store.delete('selectedYearId');
      }
      // Clear downstream selection
      await store.delete('selectedEngineId');
      await store.save();
    } else {
      if (yearId) {
        BrowserStorage.set('selectedYearId', yearId);
      } else {
        BrowserStorage.delete('selectedYearId');
      }
      // Clear downstream selection
      BrowserStorage.delete('selectedEngineId');
    }
  }

  static async setSelectedEngine(engineId: string | null): Promise<void> {
    if (isTauri && store) {
      if (engineId) {
        await store.set('selectedEngineId', engineId);
      } else {
        await store.delete('selectedEngineId');
      }
      await store.save();
    } else {
      if (engineId) {
        BrowserStorage.set('selectedEngineId', engineId);
      } else {
        BrowserStorage.delete('selectedEngineId');
      }
    }
  }

  static async clearContext(): Promise<void> {
    if (isTauri && store) {
      await store.delete('selectedBrandId');
      await store.delete('selectedModelId');
      await store.delete('selectedYearId');
      await store.delete('selectedEngineId');
      await store.save();
    } else {
      BrowserStorage.delete('selectedBrandId');
      BrowserStorage.delete('selectedModelId');
      BrowserStorage.delete('selectedYearId');
      BrowserStorage.delete('selectedEngineId');
    }
  }
}
