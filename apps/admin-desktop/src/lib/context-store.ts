// Global context store for Branch/Warehouse selection
const isTauri = typeof window !== 'undefined' && '__TAURI__' in window;

let store: any = null;
if (isTauri) {
  // Tauri Store initialization will happen async
  import('@tauri-apps/plugin-store').then(({ Store }) => {
    (Store as any).load('context.json').then((s: any) => {
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

export interface AppContext {
  activeOrgId: string | null;
  activeBranchId: string | null;
  activeWarehouseId: string | null;
}

export class ContextStore {
  static async getContext(): Promise<AppContext> {
    if (isTauri && store) {
      const activeOrgId = (await store.get('activeOrgId')) || null;
      const activeBranchId = (await store.get('activeBranchId')) || null;
      const activeWarehouseId = (await store.get('activeWarehouseId')) || null;
      return { activeOrgId, activeBranchId, activeWarehouseId };
    } else {
      const activeOrgId = BrowserStorage.get('activeOrgId') || null;
      const activeBranchId = BrowserStorage.get('activeBranchId') || null;
      const activeWarehouseId = BrowserStorage.get('activeWarehouseId') || null;
      return { activeOrgId, activeBranchId, activeWarehouseId };
    }
  }

  static async setActiveOrg(orgId: string | null): Promise<void> {
    if (isTauri && store) {
      if (orgId) {
        await store.set('activeOrgId', orgId);
      } else {
        await store.delete('activeOrgId');
      }
      await store.save();
    } else {
      if (orgId) {
        BrowserStorage.set('activeOrgId', orgId);
      } else {
        BrowserStorage.delete('activeOrgId');
      }
    }
  }

  static async setActiveBranch(branchId: string | null): Promise<void> {
    if (isTauri && store) {
      if (branchId) {
        await store.set('activeBranchId', branchId);
      } else {
        await store.delete('activeBranchId');
      }
      await store.save();
    } else {
      if (branchId) {
        BrowserStorage.set('activeBranchId', branchId);
      } else {
        BrowserStorage.delete('activeBranchId');
      }
    }
  }

  static async setActiveWarehouse(warehouseId: string | null): Promise<void> {
    if (isTauri && store) {
      if (warehouseId) {
        await store.set('activeWarehouseId', warehouseId);
      } else {
        await store.delete('activeWarehouseId');
      }
      await store.save();
    } else {
      if (warehouseId) {
        BrowserStorage.set('activeWarehouseId', warehouseId);
      } else {
        BrowserStorage.delete('activeWarehouseId');
      }
    }
  }

  static async clearContext(): Promise<void> {
    if (isTauri && store) {
      await store.delete('activeOrgId');
      await store.delete('activeBranchId');
      await store.delete('activeWarehouseId');
      await store.save();
    } else {
      BrowserStorage.delete('activeOrgId');
      BrowserStorage.delete('activeBranchId');
      BrowserStorage.delete('activeWarehouseId');
    }
  }
}
