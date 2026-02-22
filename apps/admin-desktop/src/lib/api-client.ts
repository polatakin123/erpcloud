import { SettingsService } from './settings';

export class ApiError extends Error {
  constructor(
    public status: number,
    public statusText: string,
    public data?: any
  ) {
    super(`API Error: ${status} ${statusText}`);
    this.name = 'ApiError';
  }
}

export interface ApiRequestOptions extends RequestInit {
  skipAuth?: boolean;
}

export class ApiClient {
  private static baseUrl: string = '';
  private static token: string | null = null;

  static async initialize() {
    const settings = await SettingsService.getSettings();
    this.baseUrl = settings.apiBaseUrl;
    const token = localStorage.getItem('accessToken') || settings.authToken;
    if (token) {
      this.token = token;
    }
  }

  static setToken(token: string | null) {
    this.token = token;
  }

  static setBaseUrl(url: string) {
    this.baseUrl = url;
  }

  static async request<T>(
    path: string,
    options: ApiRequestOptions = {}
  ): Promise<T> {
    const { skipAuth = false, ...fetchOptions } = options;

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(fetchOptions.headers as Record<string, string> || {}),
    };

    if (!skipAuth && this.token) {
      headers['Authorization'] = `Bearer ${this.token}`;
    }

    const url = `${this.baseUrl}${path}`;

    try {
      const response = await fetch(url, {
        ...fetchOptions,
        headers,
      });

      if (!response.ok) {
        if (response.status === 401) {
          localStorage.removeItem('accessToken');
          this.token = null;
          window.location.href = '/login';
          throw new ApiError(response.status, 'Unauthorized', 'Oturum süresi doldu');
        }

        let errorData;
        const contentType = response.headers.get('content-type');
        
        if (contentType && contentType.includes('application/json')) {
          try {
            errorData = await response.json();
          } catch {
            errorData = response.statusText;
          }
        } else {
          try {
            errorData = await response.text();
          } catch {
            errorData = response.statusText;
          }
        }

        throw new ApiError(response.status, response.statusText, errorData);
      }

      // Handle 204 No Content
      if (response.status === 204) {
        return undefined as T;
      }

      return await response.json();
    } catch (error) {
      if (error instanceof ApiError) {
        throw error;
      }
      throw new Error(`Network error: ${error instanceof Error ? error.message : 'Unknown'}`);
    }
  }

  // Convenience methods
  static async get<T>(path: string, options?: ApiRequestOptions): Promise<T> {
    return this.request<T>(path, { ...options, method: 'GET' });
  }

  static async post<T>(
    path: string,
    data?: any,
    options?: ApiRequestOptions
  ): Promise<T> {
    return this.request<T>(path, {
      ...options,
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  static async put<T>(
    path: string,
    data?: any,
    options?: ApiRequestOptions
  ): Promise<T> {
    return this.request<T>(path, {
      ...options,
      method: 'PUT',
      body: data ? JSON.stringify(data) : undefined,
    });
  }

  static async delete<T>(path: string, options?: ApiRequestOptions): Promise<T> {
    return this.request<T>(path, { ...options, method: 'DELETE' });
  }
}

// Export a default instance for convenience
export const apiClient = ApiClient;
