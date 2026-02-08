/**
 * Search performance telemetry module
 * Tracks search metrics for debugging and optimization
 */

export interface SearchPerfMetric {
  query: string;
  timestamp: number;
  durationMs: number;
  resultCount: number;
  cacheHit: boolean;
  warehouseId?: string;
  engineId?: string;
}

class SearchPerfMonitor {
  private metrics: SearchPerfMetric[] = [];
  private readonly MAX_METRICS = 50;
  private enabled: boolean = false;

  constructor() {
    // Check if debug mode is enabled
    this.enabled = this.isDebugEnabled();
  }

  private isDebugEnabled(): boolean {
    try {
      return localStorage.getItem('erp_perf_debug') === 'true';
    } catch {
      return false;
    }
  }

  /**
   * Enable performance debugging
   */
  enable(): void {
    this.enabled = true;
    try {
      localStorage.setItem('erp_perf_debug', 'true');
    } catch (error) {
      console.warn('[PERF] Failed to persist debug setting:', error);
    }
    console.log('[PERF] Performance debugging enabled');
  }

  /**
   * Disable performance debugging
   */
  disable(): void {
    this.enabled = false;
    try {
      localStorage.removeItem('erp_perf_debug');
    } catch (error) {
      console.warn('[PERF] Failed to remove debug setting:', error);
    }
    console.log('[PERF] Performance debugging disabled');
  }

  /**
   * Check if debugging is enabled
   */
  isEnabled(): boolean {
    return this.enabled;
  }

  /**
   * Record a search metric
   */
  record(metric: SearchPerfMetric): void {
    if (!this.enabled) return;

    this.metrics.push(metric);

    // Keep only last N metrics
    if (this.metrics.length > this.MAX_METRICS) {
      this.metrics.shift();
    }

    // Log to console
    const cacheStatus = metric.cacheHit ? 'CACHE' : 'API';
    console.log(
      `[SEARCH PERF] q="${metric.query}" ms=${metric.durationMs} count=${metric.resultCount} source=${cacheStatus}${
        metric.engineId ? ` engine=${metric.engineId.substring(0, 8)}` : ''
      }`
    );
  }

  /**
   * Get all recorded metrics
   */
  getMetrics(): SearchPerfMetric[] {
    return [...this.metrics];
  }

  /**
   * Get performance statistics
   */
  getStats(): {
    total: number;
    avgDuration: number;
    p50Duration: number;
    p95Duration: number;
    p99Duration: number;
    cacheHitRate: number;
  } {
    if (this.metrics.length === 0) {
      return {
        total: 0,
        avgDuration: 0,
        p50Duration: 0,
        p95Duration: 0,
        p99Duration: 0,
        cacheHitRate: 0,
      };
    }

    const durations = this.metrics.map(m => m.durationMs).sort((a, b) => a - b);
    const cacheHits = this.metrics.filter(m => m.cacheHit).length;

    const p50Index = Math.floor(durations.length * 0.5);
    const p95Index = Math.floor(durations.length * 0.95);
    const p99Index = Math.floor(durations.length * 0.99);

    return {
      total: this.metrics.length,
      avgDuration: durations.reduce((a, b) => a + b, 0) / durations.length,
      p50Duration: durations[p50Index] || 0,
      p95Duration: durations[p95Index] || 0,
      p99Duration: durations[p99Index] || 0,
      cacheHitRate: (cacheHits / this.metrics.length) * 100,
    };
  }

  /**
   * Clear all metrics
   */
  clear(): void {
    this.metrics = [];
    console.log('[PERF] Metrics cleared');
  }

  /**
   * Print performance report to console
   */
  printReport(): void {
    if (!this.enabled) {
      console.log('[PERF] Performance debugging is disabled');
      return;
    }

    const stats = this.getStats();
    
    console.log('\n=== SEARCH PERFORMANCE REPORT ===');
    console.log(`Total Searches: ${stats.total}`);
    console.log(`Average Duration: ${stats.avgDuration.toFixed(2)}ms`);
    console.log(`P50 Duration: ${stats.p50Duration.toFixed(2)}ms`);
    console.log(`P95 Duration: ${stats.p95Duration.toFixed(2)}ms`);
    console.log(`P99 Duration: ${stats.p99Duration.toFixed(2)}ms`);
    console.log(`Cache Hit Rate: ${stats.cacheHitRate.toFixed(1)}%`);
    console.log('================================\n');

    // Performance warnings
    if (stats.p95Duration > 2000) {
      console.warn('[PERF] ⚠️ P95 duration exceeds 2s target!');
    }
    if (stats.cacheHitRate < 20) {
      console.warn('[PERF] ⚠️ Low cache hit rate detected');
    }
  }
}

// Singleton instance
export const perfMonitor = new SearchPerfMonitor();

// Dev-only global access
if (import.meta.env.DEV) {
  (window as any).__searchPerf = {
    enable: () => perfMonitor.enable(),
    disable: () => perfMonitor.disable(),
    stats: () => perfMonitor.getStats(),
    report: () => perfMonitor.printReport(),
    clear: () => perfMonitor.clear(),
    metrics: () => perfMonitor.getMetrics(),
  };
  
  console.log('[PERF] Performance tools available: window.__searchPerf');
}
