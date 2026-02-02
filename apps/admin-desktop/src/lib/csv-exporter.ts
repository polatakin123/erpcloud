/**
 * CSV Export Utilities
 */

export interface CSVColumn<T> {
  key: keyof T | string;
  label: string;
  format?: (value: any, row: T) => string;
}

export class CSVExporter {
  /**
   * Export data to CSV and trigger download
   */
  static export<T>(
    data: T[],
    columns: CSVColumn<T>[],
    filename: string
  ): void {
    const csv = this.generateCSV(data, columns);
    this.download(csv, filename);
  }

  /**
   * Generate CSV string from data
   */
  static generateCSV<T>(data: T[], columns: CSVColumn<T>[]): string {
    // Header row
    const header = columns.map(col => this.escapeCSV(col.label)).join(',');

    // Data rows
    const rows = data.map(row => {
      return columns.map(col => {
        const value = this.getCellValue(row, col);
        return this.escapeCSV(value);
      }).join(',');
    });

    return [header, ...rows].join('\n');
  }

  /**
   * Get cell value with optional formatting
   */
  private static getCellValue<T>(row: T, column: CSVColumn<T>): string {
    const rawValue = typeof column.key === 'string' 
      ? (row as any)[column.key] 
      : row[column.key as keyof T];

    if (column.format) {
      return column.format(rawValue, row);
    }

    // Handle null/undefined
    if (rawValue === null || rawValue === undefined) {
      return '';
    }

    // Handle Date objects
    if (rawValue instanceof Date) {
      return rawValue.toISOString();
    }

    // Handle objects
    if (typeof rawValue === 'object') {
      return JSON.stringify(rawValue);
    }

    return String(rawValue);
  }

  /**
   * Escape CSV value (handle quotes and commas)
   */
  private static escapeCSV(value: string): string {
    if (value === null || value === undefined) {
      return '';
    }

    const stringValue = String(value);

    // If contains comma, quote, or newline, wrap in quotes and escape quotes
    if (stringValue.includes(',') || stringValue.includes('"') || stringValue.includes('\n')) {
      return `"${stringValue.replace(/"/g, '""')}"`;
    }

    return stringValue;
  }

  /**
   * Trigger browser download
   */
  private static download(csv: string, filename: string): void {
    // Add BOM for Excel UTF-8 support
    const BOM = '\uFEFF';
    const csvWithBOM = BOM + csv;

    const blob = new Blob([csvWithBOM], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);

    link.setAttribute('href', url);
    link.setAttribute('download', filename);
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    // Clean up
    URL.revokeObjectURL(url);
  }

  /**
   * Format date for CSV
   */
  static formatDate(date: string | Date | null | undefined): string {
    if (!date) return '';
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toISOString().split('T')[0]; // YYYY-MM-DD
  }

  /**
   * Format datetime for CSV
   */
  static formatDateTime(date: string | Date | null | undefined): string {
    if (!date) return '';
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toISOString().replace('T', ' ').split('.')[0]; // YYYY-MM-DD HH:MM:SS
  }

  /**
   * Format currency for CSV
   */
  static formatCurrency(amount: number | null | undefined, currency?: string): string {
    if (amount === null || amount === undefined) return '';
    const formatted = amount.toFixed(2);
    return currency ? `${formatted} ${currency}` : formatted;
  }

  /**
   * Format number with decimals
   */
  static formatNumber(value: number | null | undefined, decimals: number = 2): string {
    if (value === null || value === undefined) return '';
    return value.toFixed(decimals);
  }
}
