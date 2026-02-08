import { Car, X } from 'lucide-react';
import { useVehicleBrands, useVehicleModels, useVehicleYearRanges, useVehicleEngines } from '../hooks/useVehicles';
import { useVehicleContext } from '../hooks/useVehicleContext';

/**
 * VehicleFilterBar - Reusable vehicle selection component
 * 
 * Cascading dropdowns: Marka -> Model -> Yıl -> Motor
 * Persists selection using VehicleContext (similar to Branch/Warehouse pattern)
 */
export function VehicleFilterBar() {
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
    <div className="bg-gradient-to-r from-purple-50 to-blue-50 rounded-lg p-4 border border-purple-200">
      <div className="flex items-center gap-2 mb-3">
        <Car className="h-5 w-5 text-purple-600" />
        <h3 className="text-sm font-semibold text-gray-900">Araç Filtresi</h3>
        {selectedEngineId && (
          <button
            onClick={handleClear}
            className="ml-auto inline-flex items-center gap-1 px-2 py-1 text-xs font-medium text-gray-700 bg-white border border-gray-300 rounded hover:bg-gray-50"
          >
            <X className="h-3 w-3" />
            Temizle
          </button>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
        {/* Marka */}
        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">
            Marka
          </label>
          <select
            value={selectedBrandId || ''}
            onChange={(e) => handleBrandChange(e.target.value)}
            disabled={brandsLoading}
            className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent disabled:bg-gray-100 disabled:cursor-not-allowed"
          >
            <option value="">Seçiniz...</option>
            {brands?.map((brand) => (
              <option key={brand.id} value={brand.id}>
                {brand.name}
              </option>
            ))}
          </select>
          {brandsLoading && (
            <p className="text-xs text-gray-500 mt-1">Yükleniyor...</p>
          )}
        </div>

        {/* Model */}
        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">
            Model
          </label>
          <select
            value={selectedModelId || ''}
            onChange={(e) => handleModelChange(e.target.value)}
            disabled={!selectedBrandId || modelsLoading}
            className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent disabled:bg-gray-100 disabled:cursor-not-allowed"
          >
            <option value="">Seçiniz...</option>
            {models?.map((model) => (
              <option key={model.id} value={model.id}>
                {model.name}
              </option>
            ))}
          </select>
          {modelsLoading && (
            <p className="text-xs text-gray-500 mt-1">Yükleniyor...</p>
          )}
        </div>

        {/* Yıl */}
        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">
            Yıl
          </label>
          <select
            value={selectedYearId || ''}
            onChange={(e) => handleYearChange(e.target.value)}
            disabled={!selectedModelId || yearsLoading}
            className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent disabled:bg-gray-100 disabled:cursor-not-allowed"
          >
            <option value="">Seçiniz...</option>
            {years?.map((year) => (
              <option key={year.id} value={year.id}>
                {year.displayName}
              </option>
            ))}
          </select>
          {yearsLoading && (
            <p className="text-xs text-gray-500 mt-1">Yükleniyor...</p>
          )}
        </div>

        {/* Motor */}
        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">
            Motor
          </label>
          <select
            value={selectedEngineId || ''}
            onChange={(e) => handleEngineChange(e.target.value)}
            disabled={!selectedYearId || enginesLoading}
            className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent disabled:bg-gray-100 disabled:cursor-not-allowed"
          >
            <option value="">Seçiniz...</option>
            {engines?.map((engine) => (
              <option key={engine.id} value={engine.id}>
                {engine.displayName}
              </option>
            ))}
          </select>
          {enginesLoading && (
            <p className="text-xs text-gray-500 mt-1">Yükleniyor...</p>
          )}
        </div>
      </div>

      {selectedEngineId && engines && (
        <div className="mt-3 p-2 bg-white rounded border border-purple-200">
          <p className="text-xs text-gray-600">
            <span className="font-medium">Seçili Araç:</span>{' '}
            {brands?.find(b => b.id === selectedBrandId)?.name} -{' '}
            {models?.find(m => m.id === selectedModelId)?.name} -{' '}
            {years?.find(y => y.id === selectedYearId)?.displayName} -{' '}
            {engines?.find(e => e.id === selectedEngineId)?.displayName}
          </p>
        </div>
      )}
    </div>
  );
}
