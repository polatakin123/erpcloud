/**
 * Unified search result sorting logic for dealer-focused UX
 * Priority: Compatible + In Stock + Direct > Compatible + Equivalent > Out of Stock > Undefined Fitment
 */

import type { VariantSearchResult } from '../hooks/usePartReferences';

/**
 * Calculate sort priority for a variant search result
 * Lower number = higher priority (appears first)
 */
function calculatePriority(result: VariantSearchResult): number {
  const isCompatible = result.isCompatible ?? true; // Default true if no fitment data
  const hasDefinedFitment = result.hasDefinedFitment ?? true;
  const inStock = (result.available ?? 0) > 0;
  const matchType = result.matchType;

  // Priority 1: Compatible + In Stock + DIRECT
  if (isCompatible && inStock && matchType === 'DIRECT') return 1;
  
  // Priority 2: Compatible + In Stock + EQUIVALENT
  if (isCompatible && inStock && matchType === 'EQUIVALENT') return 2;
  
  // Priority 3: Compatible + In Stock + BOTH
  if (isCompatible && inStock && matchType === 'BOTH') return 3;
  
  // Priority 4: Compatible + Out of Stock + DIRECT
  if (isCompatible && !inStock && matchType === 'DIRECT') return 4;
  
  // Priority 5: Compatible + Out of Stock + EQUIVALENT
  if (isCompatible && !inStock && matchType === 'EQUIVALENT') return 5;
  
  // Priority 6: Compatible + Out of Stock + BOTH
  if (isCompatible && !inStock && matchType === 'BOTH') return 6;
  
  // Priority 7: Undefined fitment + In Stock
  if (!hasDefinedFitment && inStock) return 7;
  
  // Priority 8: Undefined fitment + Out of Stock
  if (!hasDefinedFitment && !inStock) return 8;
  
  // Priority 9: Incompatible
  return 9;
}

/**
 * Sort variant search results with dealer-focused logic
 */
export function sortSearchResults(results: VariantSearchResult[]): VariantSearchResult[] {
  return [...results].sort((a, b) => {
    // First criterion: Priority
    const priorityA = calculatePriority(a);
    const priorityB = calculatePriority(b);
    
    if (priorityA !== priorityB) {
      return priorityA - priorityB;
    }
    
    // Second criterion: Stock quantity (descending)
    const stockA = a.available ?? 0;
    const stockB = b.available ?? 0;
    
    if (stockA !== stockB) {
      return stockB - stockA;
    }
    
    // Third criterion: Alphabetical by name
    const nameA = (a.name || a.variantName || '').toLowerCase();
    const nameB = (b.name || b.variantName || '').toLowerCase();
    
    return nameA.localeCompare(nameB, 'tr');
  });
}

/**
 * Group results by priority for display sections
 */
export interface SearchResultGroup {
  title: string;
  priority: number;
  results: VariantSearchResult[];
}

export function groupSearchResults(results: VariantSearchResult[]): SearchResultGroup[] {
  const groups = new Map<number, VariantSearchResult[]>();
  
  for (const result of results) {
    const priority = calculatePriority(result);
    const group = groups.get(priority) || [];
    group.push(result);
    groups.set(priority, group);
  }
  
  const groupDefinitions: Array<{ priority: number; title: string }> = [
    { priority: 1, title: 'Uyumlu - Stokta - Direkt Eşleşme' },
    { priority: 2, title: 'Uyumlu - Stokta - Muadil' },
    { priority: 3, title: 'Uyumlu - Stokta - Karışık' },
    { priority: 4, title: 'Uyumlu - Stok Yok - Direkt Eşleşme' },
    { priority: 5, title: 'Uyumlu - Stok Yok - Muadil' },
    { priority: 6, title: 'Uyumlu - Stok Yok - Karışık' },
    { priority: 7, title: 'Uyum Tanımsız - Stokta' },
    { priority: 8, title: 'Uyum Tanımsız - Stok Yok' },
    { priority: 9, title: 'Uyumsuz' },
  ];
  
  return groupDefinitions
    .filter(def => groups.has(def.priority))
    .map(def => ({
      title: def.title,
      priority: def.priority,
      results: sortSearchResults(groups.get(def.priority)!),
    }));
}
