import { create } from 'zustand';
import { jwtDecode } from 'jwt-decode';
import { ApiClient } from './api-client';

interface JwtPayload {
  user_id: string;
  tenant_id: string;
  email: string;
  realm_access?: {
    roles: string[];
  };
  role?: string;
  permissions?: string[];
}

interface AuthState {
  token: string | null;
  role: string | null;
  userId: string | null;
  tenantId: string | null;
  permissions: string[];
  hydrated: boolean;
  setAuth: (token: string) => void;
  clearAuth: () => void;
  isAdmin: () => boolean;
  isDealer: () => boolean;
  hasPerm: (perm: string) => boolean;
  hasAnyPerm: (perms: string[]) => boolean;
}

function parsePermissions(permsField: any): string[] {
  if (!permsField) return [];
  if (Array.isArray(permsField)) return permsField;
  if (typeof permsField === 'string') {
    try {
      const parsed = JSON.parse(permsField);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }
  return [];
}

export const useAuthStore = create<AuthState>((set, get) => ({
  token: null,
  role: null,
  userId: null,
  tenantId: null,
  permissions: [],
  hydrated: false,

  setAuth: (token: string) => {
    try {
      const decoded = jwtDecode<JwtPayload>(token);
      const role = decoded.realm_access?.roles?.[0] || decoded.role || null;
      const permissions = parsePermissions(decoded.permissions);
      
      set({
        token,
        role,
        userId: decoded.user_id,
        tenantId: decoded.tenant_id,
        permissions,
        hydrated: true,
      });
      
      // Update ApiClient token
      ApiClient.setToken(token);
    } catch (error) {
      console.error('Failed to decode token:', error);
      set({ token, role: null, userId: null, tenantId: null, permissions: [], hydrated: true });
      ApiClient.setToken(token);
    }
  },

  clearAuth: () => {
    set({ token: null, role: null, userId: null, tenantId: null, permissions: [], hydrated: true });
    localStorage.removeItem('accessToken');
    ApiClient.setToken(null);
  },

  isAdmin: () => get().role === 'Admin',
  isDealer: () => get().role === 'Dealer',
  
  hasPerm: (perm: string) => {
    const perms = get().permissions;
    return perms.includes(perm) || perms.some(p => p.endsWith('.*'));
  },
  
  hasAnyPerm: (perms: string[]) => perms.some(p => get().hasPerm(p)),
}));
