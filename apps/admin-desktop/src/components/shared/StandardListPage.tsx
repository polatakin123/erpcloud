import { ReactNode, useState, useEffect } from 'react';
import { Search, Filter, ChevronLeft, ChevronRight, Settings2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card } from '@/components/ui/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { Checkbox } from '@/components/ui/checkbox';

export interface DateRange {
  from?: Date;
  to?: Date;
}

export interface StandardListPageProps {
  title: string;
  children: ReactNode;
  
  // Search
  searchValue: string;
  onSearchChange: (value: string) => void;
  searchPlaceholder?: string;
  
  // Status filter (optional)
  statusValue?: string;
  onStatusChange?: (value: string) => void;
  statusOptions?: { value: string; label: string }[];
  
  // Date range filter
  dateRange?: DateRange;
  onDateRangeChange?: (range: DateRange) => void;
  
  // Quick date filters
  quickDateFilters?: boolean;
  
  // Pagination
  page: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (size: number) => void;
  
  // Actions
  primaryAction?: {
    label: string;
    onClick: () => void;
    icon?: ReactNode;
  };
  
  // Column visibility
  columns?: { id: string; label: string; visible: boolean }[];
  onColumnVisibilityChange?: (columns: { id: string; visible: boolean }[]) => void;
  
  // Loading and empty states
  isLoading: boolean;
  isEmpty: boolean;
  emptyMessage?: string;
  emptyAction?: {
    label: string;
    onClick: () => void;
  };
}

export function StandardListPage({
  title,
  children,
  searchValue,
  onSearchChange,
  searchPlaceholder = 'Search...',
  statusValue,
  onStatusChange,
  statusOptions,
  dateRange,
  onDateRangeChange,
  quickDateFilters = true,
  page,
  totalPages,
  totalCount,
  pageSize,
  onPageChange,
  onPageSizeChange,
  primaryAction,
  columns,
  onColumnVisibilityChange,
  isLoading,
  isEmpty,
  emptyMessage = 'No items found',
  emptyAction,
}: StandardListPageProps) {
  const [debouncedSearch, setDebouncedSearch] = useState(searchValue);

  // Debounced search (300ms)
  useEffect(() => {
    const timer = setTimeout(() => {
      onSearchChange(debouncedSearch);
    }, 300);
    return () => clearTimeout(timer);
  }, [debouncedSearch]);

  // Quick date filter presets
  const applyQuickDateFilter = (preset: 'today' | 'last7' | 'last30') => {
    const to = new Date();
    const from = new Date();
    
    switch (preset) {
      case 'today':
        from.setHours(0, 0, 0, 0);
        break;
      case 'last7':
        from.setDate(from.getDate() - 7);
        break;
      case 'last30':
        from.setDate(from.getDate() - 30);
        break;
    }
    
    onDateRangeChange?.({ from, to });
  };

  return (
    <div className="p-6">
      {/* Header */}
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">{title}</h1>
        {primaryAction && (
          <Button onClick={primaryAction.onClick}>
            {primaryAction.icon}
            {primaryAction.label}
          </Button>
        )}
      </div>

      {/* Filters Card */}
      <Card className="p-4 mb-6">
        <div className="flex flex-wrap gap-4 items-end">
          {/* Search */}
          <div className="flex-1 min-w-[200px]">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
              <Input
                placeholder={searchPlaceholder}
                value={debouncedSearch}
                onChange={(e) => setDebouncedSearch(e.target.value)}
                className="pl-10"
              />
            </div>
          </div>

          {/* Status Filter */}
          {statusOptions && onStatusChange && (
            <div className="w-[180px]">
              <Select value={statusValue || ''} onValueChange={onStatusChange}>
                <SelectTrigger>
                  <Filter className="h-4 w-4 mr-2" />
                  <SelectValue placeholder="All statuses" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">All statuses</SelectItem>
                  {statusOptions.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

          {/* Quick Date Filters */}
          {quickDateFilters && onDateRangeChange && (
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => applyQuickDateFilter('today')}
              >
                Today
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => applyQuickDateFilter('last7')}
              >
                Last 7 days
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => applyQuickDateFilter('last30')}
              >
                Last 30 days
              </Button>
            </div>
          )}

          {/* Date Range (custom) */}
          {onDateRangeChange && (
            <div className="flex gap-2">
              <Input
                type="date"
                value={dateRange?.from?.toISOString().split('T')[0] || ''}
                onChange={(e) =>
                  onDateRangeChange({
                    ...dateRange,
                    from: e.target.value ? new Date(e.target.value) : undefined,
                  })
                }
                className="w-[150px]"
                placeholder="From"
              />
              <Input
                type="date"
                value={dateRange?.to?.toISOString().split('T')[0] || ''}
                onChange={(e) =>
                  onDateRangeChange({
                    ...dateRange,
                    to: e.target.value ? new Date(e.target.value) : undefined,
                  })
                }
                className="w-[150px]"
                placeholder="To"
              />
            </div>
          )}

          {/* Column Visibility */}
          {columns && onColumnVisibilityChange && (
            <Popover>
              <PopoverTrigger asChild>
                <Button variant="outline" size="sm">
                  <Settings2 className="h-4 w-4 mr-2" />
                  Columns
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-[200px]">
                <div className="space-y-2">
                  <div className="font-semibold text-sm mb-2">Toggle Columns</div>
                  {columns.map((col) => (
                    <div key={col.id} className="flex items-center space-x-2">
                      <Checkbox
                        id={col.id}
                        checked={col.visible}
                        onCheckedChange={(checked) => {
                          const updated = columns.map((c) =>
                            c.id === col.id ? { ...c, visible: !!checked } : c
                          );
                          onColumnVisibilityChange(updated);
                        }}
                      />
                      <label
                        htmlFor={col.id}
                        className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
                      >
                        {col.label}
                      </label>
                    </div>
                  ))}
                </div>
              </PopoverContent>
            </Popover>
          )}
        </div>
      </Card>

      {/* Content */}
      {isLoading ? (
        <div className="flex flex-col items-center justify-center py-12">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-gray-900 mb-4"></div>
          <p className="text-gray-600">Loading {title.toLowerCase()}...</p>
        </div>
      ) : isEmpty ? (
        <Card className="p-12">
          <div className="text-center">
            <p className="text-gray-500 mb-4">{emptyMessage}</p>
            {emptyAction && (
              <Button onClick={emptyAction.onClick}>{emptyAction.label}</Button>
            )}
          </div>
        </Card>
      ) : (
        <Card>
          {children}
          
          {/* Pagination */}
          <div className="p-4 border-t flex justify-between items-center">
            <div className="text-sm text-gray-600">
              Page {page} of {totalPages} ({totalCount} total)
            </div>
            
            <div className="flex items-center gap-4">
              {onPageSizeChange && (
                <Select
                  value={pageSize.toString()}
                  onValueChange={(value) => onPageSizeChange(parseInt(value))}
                >
                  <SelectTrigger className="w-[100px]">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="10">10 / page</SelectItem>
                    <SelectItem value="25">25 / page</SelectItem>
                    <SelectItem value="50">50 / page</SelectItem>
                    <SelectItem value="100">100 / page</SelectItem>
                    <SelectItem value="200">200 / page</SelectItem>
                  </SelectContent>
                </Select>
              )}
              
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => onPageChange(Math.max(1, page - 1))}
                  disabled={page === 1}
                >
                  <ChevronLeft className="h-4 w-4" />
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => onPageChange(page + 1)}
                  disabled={page >= totalPages}
                >
                  Next
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </div>
        </Card>
      )}
    </div>
  );
}
