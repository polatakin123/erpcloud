/**
 * Optimized fast variant search hook with caching, debouncing, and performance tracking
 * Used by both FastSalesPage (tezgâh) and FastSearchPage
 */

import { useQuery } from '@tanstack/react-query';
import { useState, useEffect, useRef } from 'react';
import { apiClient } from '../lib/api-client';
import type { VariantSearchResponse } from './usePartReferences';
import { searchCache } from '../lib/search-cache';
import { perfMonitor } from '../lib/search-perf';
import { sortSearchResults } from '../lib/search-sort';

export interface UseFastVariantSearchParams {
  query: string;
  warehouseId?: string;
  engineId?: string;
  includeEquivalents?: boolean;
  includeUndefinedFitment?: boolean;
  page?: number;
  pageSize?: number;
  debounceMs?: number; // Custom debounce delay
  minChars?: number; // Minimum characters before searching
}

export interface UseFastVariantSearchResult {
  data?: VariantSearchResponse;
  isLoading: boolean;
  isFetching: boolean;
  error: Error | null;
  durationMs?: number;
}

/**
 * Check if query should bypass minimum character requirement
 * (e.g., barcodes, OEM codes are typically 6+ chars)
 */
function shouldBypassMinChars(query: string): boolean {
  const trimmed = query.trim();
  
  // Bypass if query is numeric and >= 6 chars (likely barcode/OEM)
  if (/^\d+$/.test(trimmed) && trimmed.length >= 6) {
    return true;
  }
  
  return false;
}

/**
 * Optimized variant search hook with intelligent caching and debouncing
 */
export function useFastVariantSearch(
  params: UseFastVariantSearchParams
): UseFastVariantSearchResult {
  const {
    query,
    warehouseId,
    engineId,
    includeEquivalents = true,
    includeUndefinedFitment = false,
    page = 1,
    pageSize = 20,
    debounceMs = 250,
    minChars = 2,
  } = params;

  const [debouncedQuery, setDebouncedQuery] = useState(query);
  const [searchDuration, setSearchDuration] = useState<number>();
  const startTimeRef = useRef<number>(0);

  // Debounce query input
  useEffect(() => {
    const trimmed = query.trim();
    
    // Don't debounce empty queries
    if (!trimmed) {
      setDebouncedQuery('');
      return;
    }

    const handler = setTimeout(() => {
      setDebouncedQuery(trimmed);
    }, debounceMs);

    return () => clearTimeout(handler);
  }, [query, debounceMs]);

  // Check if query meets minimum requirements
  const shouldSearch =
    !!debouncedQuery &&
    (debouncedQuery.length >= minChars || shouldBypassMinChars(debouncedQuery));

  // React Query with cache integration
  const result = useQuery({
    queryKey: [
      'variant-search-fast',
      {
        query: debouncedQuery,
        warehouseId,
        engineId,
        includeEquivalents,
        includeUndefinedFitment: engineId ? includeUndefinedFitment : undefined,
        page,
        pageSize,
      },
    ],
    queryFn: async () => {
      startTimeRef.current = performance.now();

      // Check cache first
      const cached = searchCache.get<VariantSearchResponse>({
        query: debouncedQuery,
        warehouseId,
        engineId,
        includeEquivalents,
        includeUndefinedFitment: engineId ? includeUndefinedFitment : undefined,
        page,
        pageSize,
      });

      if (cached) {
        const duration = performance.now() - startTimeRef.current;
        setSearchDuration(duration);

        // Record cache hit
        perfMonitor.record({
          query: debouncedQuery,
          timestamp: Date.now(),
          durationMs: duration,
          resultCount: cached.results.length,
          cacheHit: true,
          warehouseId,
          engineId,
        });

        return cached;
      }

      // Build API request
      const searchParams = new URLSearchParams();
      searchParams.append('q', debouncedQuery);
      if (warehouseId) searchParams.append('warehouseId', warehouseId);
      if (engineId) searchParams.append('engineId', engineId);
      searchParams.append('includeEquivalents', String(includeEquivalents));
      if (engineId) {
        searchParams.append('includeUndefinedFitment', String(includeUndefinedFitment));
      }
      searchParams.append('page', String(page));
      searchParams.append('pageSize', String(pageSize));

      const response = await apiClient.get<VariantSearchResponse>(
        `/api/search/variants?${searchParams.toString()}`
      );

      const duration = performance.now() - startTimeRef.current;
      setSearchDuration(duration);

      // Sort results
      const sortedResponse = {
        ...response,
        results: sortSearchResults(response.results),
      };

      // Cache response
      searchCache.set(
        {
          query: debouncedQuery,
          warehouseId,
          engineId,
          includeEquivalents,
          includeUndefinedFitment: engineId ? includeUndefinedFitment : undefined,
          page,
          pageSize,
        },
        sortedResponse
      );

      // Record metric
      perfMonitor.record({
        query: debouncedQuery,
        timestamp: Date.now(),
        durationMs: duration,
        resultCount: sortedResponse.results.length,
        cacheHit: false,
        warehouseId,
        engineId,
      });

      return sortedResponse;
    },
    enabled: shouldSearch,
    staleTime: 30000, // 30 seconds
    gcTime: 600000, // 10 minutes
    retry: 1,
    refetchOnWindowFocus: false,
  });

  return {
    data: result.data,
    isLoading: result.isLoading,
    isFetching: result.isFetching,
    error: result.error as Error | null,
    durationMs: searchDuration,
  };
}
