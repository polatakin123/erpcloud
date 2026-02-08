/**
 * Client-side search cache layer with TTL
 * Reduces redundant API calls for repeated searches
 */

interface CacheEntry<T> {
  data: T;
  timestamp: number;
  ttl: number;
}

class SearchCache {
  private cache: Map<string, CacheEntry<any>>;
  private readonly DEFAULT_TTL = 30000; // 30 seconds
  private readonly MAX_SIZE = 100;

  constructor() {
    this.cache = new Map();
  }

  /**
   * Generate normalized cache key from search parameters
   */
  private generateKey(params: {
    query: string;
    warehouseId?: string;
    engineId?: string;
    includeEquivalents?: boolean;
    includeUndefinedFitment?: boolean;
    page?: number;
    pageSize?: number;
  }): string {
    const normalized = {
      q: params.query.trim().toUpperCase(),
      w: params.warehouseId || '',
      e: params.engineId || '',
      eq: params.includeEquivalents ?? true,
      uf: params.includeUndefinedFitment ?? false,
      p: params.page ?? 1,
      ps: params.pageSize ?? 20,
    };
    return JSON.stringify(normalized);
  }

  /**
   * Get cached data if not expired
   */
  get<T>(params: {
    query: string;
    warehouseId?: string;
    engineId?: string;
    includeEquivalents?: boolean;
    includeUndefinedFitment?: boolean;
    page?: number;
    pageSize?: number;
  }): T | null {
    const key = this.generateKey(params);
    const entry = this.cache.get(key);

    if (!entry) return null;

    const now = Date.now();
    const isExpired = now - entry.timestamp > entry.ttl;

    if (isExpired) {
      this.cache.delete(key);
      return null;
    }

    return entry.data;
  }

  /**
   * Set cache entry with optional TTL
   */
  set<T>(
    params: {
      query: string;
      warehouseId?: string;
      engineId?: string;
      includeEquivalents?: boolean;
      includeUndefinedFitment?: boolean;
      page?: number;
      pageSize?: number;
    },
    data: T,
    ttl?: number
  ): void {
    const key = this.generateKey(params);

    // Enforce max size (LRU-like: remove oldest)
    if (this.cache.size >= this.MAX_SIZE) {
      const firstKey = this.cache.keys().next().value;
      if (firstKey) this.cache.delete(firstKey);
    }

    this.cache.set(key, {
      data,
      timestamp: Date.now(),
      ttl: ttl ?? this.DEFAULT_TTL,
    });
  }

  /**
   * Clear all cache entries
   */
  clear(): void {
    this.cache.clear();
  }

  /**
   * Get cache statistics
   */
  getStats(): { size: number; maxSize: number } {
    return {
      size: this.cache.size,
      maxSize: this.MAX_SIZE,
    };
  }
}

// Singleton instance
export const searchCache = new SearchCache();

/**
 * Last searches localStorage cache
 */
const LAST_SEARCHES_KEY = 'erp_last_searches';
const MAX_LAST_SEARCHES = 10;

export interface LastSearch {
  query: string;
  type: 'OEM' | 'BARCODE' | 'SKU' | 'NAME';
  timestamp: number;
}

export function saveLastSearch(query: string, type: LastSearch['type']): void {
  try {
    const searches = getLastSearches();
    
    // Remove duplicates
    const filtered = searches.filter(s => s.query.toUpperCase() !== query.toUpperCase());
    
    // Add new search at the beginning
    filtered.unshift({
      query: query.trim(),
      type,
      timestamp: Date.now(),
    });

    // Keep only last N searches
    const trimmed = filtered.slice(0, MAX_LAST_SEARCHES);

    localStorage.setItem(LAST_SEARCHES_KEY, JSON.stringify(trimmed));
  } catch (error) {
    console.error('[SEARCH CACHE] Failed to save last search:', error);
  }
}

export function getLastSearches(): LastSearch[] {
  try {
    const data = localStorage.getItem(LAST_SEARCHES_KEY);
    if (!data) return [];
    
    const searches = JSON.parse(data) as LastSearch[];
    
    // Filter out old searches (older than 7 days)
    const weekAgo = Date.now() - 7 * 24 * 60 * 60 * 1000;
    return searches.filter(s => s.timestamp > weekAgo);
  } catch (error) {
    console.error('[SEARCH CACHE] Failed to load last searches:', error);
    return [];
  }
}

export function clearLastSearches(): void {
  try {
    localStorage.removeItem(LAST_SEARCHES_KEY);
  } catch (error) {
    console.error('[SEARCH CACHE] Failed to clear last searches:', error);
  }
}
