import { useState } from 'react';
import { Car, ChevronDown, ChevronUp, X } from 'lucide-react';
import { useVehicleBrands, useVehicleModels, useVehicleYearRanges, useVehicleEngines } from '../hooks/useVehicles';
import { useVehicleContext } from '../hooks/useVehicleContext';

/**
 * MiniVehicleSelector - Compact collapsible vehicle selector for Tezgah (Quick Sale)
 * 
 * Reuses the same vehicle-context-store as FastSearchPage for persistence
 * Optimized for space-constrained layouts
 */
export function MiniVehicleSelector() {
  const [isExpanded, setIsExpanded] = useState(false);
  
  const {
    selectedBrandId,
    selectedModelId,
    selectedYearId,
    selectedEngineId,
    setSelectedBrand,
    setSelectedModel,
    setSelectedYear,
    setSelectedEngine,
    clearVehicleSelection,
  } = useVehicleContext();

  const { data: brands, isLoading: brandsLoading } = useVehicleBrands();
  const { data: models, isLoading: modelsLoading } = useVehicleModels(selectedBrandId || undefined);
  const { data: years, isLoading: yearsLoading } = useVehicleYearRanges(selectedModelId || undefined);
  const { data: engines, isLoading: enginesLoading } = useVehicleEngines(selectedYearId || undefined);

  const hasSelection = selectedBrandId || selectedModelId || selectedYearId || selectedEngineId;

  const getSelectedVehicleSummary = () => {
    if (!hasSelection) return 'Araç seçilmedi';
    
    const parts = [];
    if (selectedBrandId) parts.push(brands?.find(b => b.id === selectedBrandId)?.name);
    if (selectedModelId) parts.push(models?.find(m => m.id === selectedModelId)?.name);
    if (selectedYearId) parts.push(years?.find(y => y.id === selectedYearId)?.displayName);
    if (selectedEngineId) parts.push(engines?.find(e => e.id === selectedEngineId)?.displayName);
    
    return parts.filter(Boolean).join(' • ') || 'Araç seçilmedi';
  };

  const handleBrandChange = (brandId: string) => {
    setSelectedBrand(brandId);
    setSelectedModel(null);
    setSelectedYear(null);
    setSelectedEngine(null);
  };

  const handleModelChange = (modelId: string) => {
    setSelectedModel(modelId);
    setSelectedYear(null);
    setSelectedEngine(null);
  };

  const handleYearChange = (yearId: string) => {
    setSelectedYear(yearId);
    setSelectedEngine(null);
  };

  const handleEngineChange = (engineId: string) => {
    setSelectedEngine(engineId);
  };

  const handleClear = () => {
    clearVehicleSelection();
  };

  return (
    <div className="border border-purple-300 rounded-lg bg-gradient-to-r from-purple-50 to-blue-50">
      {/* Header - Always Visible */}
      <div 
        className="flex items-center justify-between p-3 cursor-pointer hover:bg-purple-100/50 rounded-t-lg transition-colors"
        onClick={() => setIsExpanded(!isExpanded)}
      >
        <div className="flex items-center gap-2">
          <Car className="h-4 w-4 text-purple-600" />
          <span className="text-sm font-medium text-slate-700">
            {hasSelection ? '🚗 ' : ''}
            {getSelectedVehicleSummary()}
          </span>
        </div>
        <div className="flex items-center gap-2">
          {hasSelection && (
            <button
              onClick={(e) => {
                e.stopPropagation();
                handleClear();
              }}
              className="p-1 hover:bg-red-100 rounded transition-colors"
              title="Temizle"
            >
              <X className="h-3 w-3 text-red-600" />
            </button>
          )}
          {isExpanded ? (
            <ChevronUp className="h-4 w-4 text-slate-400" />
          ) : (
            <ChevronDown className="h-4 w-4 text-slate-400" />
          )}
        </div>
      </div>

      {/* Expanded Dropdown Section */}
      {isExpanded && (
        <div className="p-3 pt-0 space-y-2 border-t border-purple-200">
          {/* Marka */}
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">
              Marka
            </label>
            <select
              value={selectedBrandId || ''}
              onChange={(e) => handleBrandChange(e.target.value)}
              disabled={brandsLoading}
              className="w-full px-2 py-1.5 text-sm border border-slate-300 rounded focus:ring-2 focus:ring-purple-500 focus:border-transparent disabled:bg-gray-100 disabled:cursor-not-allowed"
            >
              <option value="">Seçiniz...</option>
              {brands?.map((brand) => (
                <option key={brand.id} value={brand.id}>
                  {brand.name}
                </option>
              ))}
            </select>
          </div>

          {/* Model */}
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">
              Model
            </label>
            <select
              value={selectedModelId || ''}
              onChange={(e) => handleModelChange(e.target.value)}
              disabled={!selectedBrandId || modelsLoading}
              className="w-full px-2 py-1.5 text-sm border border-slate-300 rounded focus:ring-2 focus:ring-purple-500 focus:border-transparent disabled:bg-gray-100 disabled:cursor-not-allowed"
            >
              <option value="">Seçiniz...</option>
              {models?.map((model) => (
                <option key={model.id} value={model.id}>
                  {model.name}
                </option>
              ))}
            </select>
          </div>

          {/* Yıl */}
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">
              Yıl
            </label>
            <select
              value={selectedYearId || ''}
              onChange={(e) => handleYearChange(e.target.value)}
              disabled={!selectedModelId || yearsLoading}
              className="w-full px-2 py-1.5 text-sm border border-slate-300 rounded focus:ring-2 focus:ring-purple-500 focus:border-transparent disabled:bg-gray-100 disabled:cursor-not-allowed"
            >
              <option value="">Seçiniz...</option>
              {years?.map((year) => (
                <option key={year.id} value={year.id}>
                  {year.displayName}
                </option>
              ))}
            </select>
          </div>

          {/* Motor */}
          <div>
            <label className="block text-xs font-medium text-slate-600 mb-1">
              Motor
            </label>
            <select
              value={selectedEngineId || ''}
              onChange={(e) => handleEngineChange(e.target.value)}
              disabled={!selectedYearId || enginesLoading}
              className="w-full px-2 py-1.5 text-sm border border-slate-300 rounded focus:ring-2 focus:ring-purple-500 focus:border-transparent disabled:bg-gray-100 disabled:cursor-not-allowed"
            >
              <option value="">Seçiniz...</option>
              {engines?.map((engine) => (
                <option key={engine.id} value={engine.id}>
                  {engine.displayName}
                </option>
              ))}
            </select>
          </div>

          {selectedEngineId && (
            <div className="mt-2 p-2 bg-white rounded border border-purple-200 text-xs text-slate-600">
              ✓ Araç seçildi - Uyumlu parçalar filtreleniyor
            </div>
          )}
        </div>
      )}
    </div>
  );
}
