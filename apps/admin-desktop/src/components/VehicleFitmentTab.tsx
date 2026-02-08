import { useState } from 'react';
import { Car, Plus, Trash2, AlertCircle } from 'lucide-react';
import {
  useVehicleBrands,
  useVehicleModels,
  useVehicleYearRanges,
  useVehicleEngines,
  useStockCardFitments,
  useCreateStockCardFitment,
  useDeleteStockCardFitment,
} from '@/hooks/useVehicles';
import { useToast } from '@/hooks/useToast';

interface VehicleFitmentTabProps {
  variantId: string;
}

export default function VehicleFitmentTab({ variantId }: VehicleFitmentTabProps) {
  const { toast } = useToast();
  
  const [selectedBrandId, setSelectedBrandId] = useState<string>('');
  const [selectedModelId, setSelectedModelId] = useState<string>('');
  const [selectedYearRangeId, setSelectedYearRangeId] = useState<string>('');
  const [selectedEngineId, setSelectedEngineId] = useState<string>('');
  const [notes, setNotes] = useState<string>('');

  // Queries
  const { data: brands = [], isLoading: loadingBrands } = useVehicleBrands();
  const { data: models = [], isLoading: loadingModels } = useVehicleModels(selectedBrandId || undefined);
  const { data: yearRanges = [], isLoading: loadingYears } = useVehicleYearRanges(selectedModelId || undefined);
  const { data: engines = [], isLoading: loadingEngines } = useVehicleEngines(selectedYearRangeId || undefined);
  const { data: fitments = [], isLoading: loadingFitments } = useStockCardFitments(variantId);

  // Mutations
  const createFitment = useCreateStockCardFitment(variantId);
  const deleteFitment = useDeleteStockCardFitment(variantId);

  const handleBrandChange = (brandId: string) => {
    setSelectedBrandId(brandId);
    setSelectedModelId('');
    setSelectedYearRangeId('');
    setSelectedEngineId('');
  };

  const handleModelChange = (modelId: string) => {
    setSelectedModelId(modelId);
    setSelectedYearRangeId('');
    setSelectedEngineId('');
  };

  const handleYearRangeChange = (yearRangeId: string) => {
    setSelectedYearRangeId(yearRangeId);
    setSelectedEngineId('');
  };

  const handleAddFitment = async () => {
    if (!selectedEngineId) {
      toast({ title: 'Lütfen motor seçin', variant: 'destructive' });
      return;
    }

    try {
      await createFitment.mutateAsync({
        vehicleEngineId: selectedEngineId,
        notes: notes.trim() || undefined,
      });
      
      // Reset form
      setSelectedBrandId('');
      setSelectedModelId('');
      setSelectedYearRangeId('');
      setSelectedEngineId('');
      setNotes('');
      
      toast({ title: 'Uyumluluk başarıyla eklendi' });
    } catch (error: any) {
      const errorMessage = error?.response?.data?.message || 'Uyumluluk eklenirken hata oluştu';
      toast({ title: errorMessage, variant: 'destructive' });
    }
  };

  const handleDeleteFitment = async (fitmentId: string) => {
    if (!confirm('Bu uyumluluğu silmek istediğinize emin misiniz?')) {
      return;
    }

    try {
      await deleteFitment.mutateAsync(fitmentId);
      toast({ title: 'Uyumluluk başarıyla silindi' });
    } catch (error: any) {
      const errorMessage = error?.response?.data?.message || 'Uyumluluk silinirken hata oluştu';
      toast({ title: errorMessage, variant: 'destructive' });
    }
  };

  const canAddFitment = selectedBrandId && selectedModelId && selectedYearRangeId && selectedEngineId;

  return (
    <div className="space-y-6">
      {/* Add Fitment Form */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
          <Plus className="h-5 w-5 text-blue-600" />
          Yeni Uyumluluk Ekle
        </h3>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
          {/* Brand Selector */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Marka
            </label>
            <select
              value={selectedBrandId}
              onChange={(e) => handleBrandChange(e.target.value)}
              disabled={loadingBrands}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">Marka Seçin</option>
              {brands.map((brand) => (
                <option key={brand.id} value={brand.id}>
                  {brand.name} ({brand.code})
                </option>
              ))}
            </select>
          </div>

          {/* Model Selector */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Model
            </label>
            <select
              value={selectedModelId}
              onChange={(e) => handleModelChange(e.target.value)}
              disabled={!selectedBrandId || loadingModels}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
            >
              <option value="">Model Seçin</option>
              {models.map((model) => (
                <option key={model.id} value={model.id}>
                  {model.name}
                </option>
              ))}
            </select>
          </div>

          {/* Year Range Selector */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Yıl Aralığı
            </label>
            <select
              value={selectedYearRangeId}
              onChange={(e) => handleYearRangeChange(e.target.value)}
              disabled={!selectedModelId || loadingYears}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
            >
              <option value="">Yıl Aralığı Seçin</option>
              {yearRanges.map((year) => (
                <option key={year.id} value={year.id}>
                  {year.displayName}
                </option>
              ))}
            </select>
          </div>

          {/* Engine Selector */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Motor
            </label>
            <select
              value={selectedEngineId}
              onChange={(e) => setSelectedEngineId(e.target.value)}
              disabled={!selectedYearRangeId || loadingEngines}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:bg-gray-100"
            >
              <option value="">Motor Seçin</option>
              {engines.map((engine) => (
                <option key={engine.id} value={engine.id}>
                  {engine.displayName}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Notes */}
        <div className="mb-4">
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Notlar (Opsiyonel)
          </label>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Uyumluluk hakkında notlar..."
            rows={3}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        {/* Add Button */}
        <button
          onClick={handleAddFitment}
          disabled={!canAddFitment || createFitment.isPending}
          className="w-full md:w-auto px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed font-medium"
        >
          {createFitment.isPending ? 'Ekleniyor...' : 'Uyumluluk Ekle'}
        </button>
      </div>

      {/* Existing Fitments List */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
          <Car className="h-5 w-5 text-green-600" />
          Uyumlu Araçlar ({fitments.length})
        </h3>

        {loadingFitments ? (
          <div className="text-center py-8">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
            <p className="text-sm text-gray-500 mt-2">Yükleniyor...</p>
          </div>
        ) : fitments.length === 0 ? (
          <div className="text-center py-8">
            <AlertCircle className="h-12 w-12 text-gray-400 mx-auto mb-2" />
            <p className="text-sm text-gray-500">Henüz uyumlu araç eklenmemiş</p>
          </div>
        ) : (
          <div className="space-y-3">
            {fitments.map((fitment) => (
              <div
                key={fitment.id}
                className="flex items-start justify-between p-4 bg-gray-50 border border-gray-200 rounded-lg hover:bg-gray-100 transition-colors"
              >
                <div className="flex-1">
                  <div className="font-semibold text-gray-900 mb-1">
                    {fitment.fullDisplay}
                  </div>
                  {fitment.notes && (
                    <div className="text-sm text-gray-600 mt-1">
                      <span className="font-medium">Not:</span> {fitment.notes}
                    </div>
                  )}
                  <div className="text-xs text-gray-500 mt-2">
                    {fitment.brandName} • {fitment.modelName} • {fitment.yearRange}
                  </div>
                </div>
                <button
                  onClick={() => handleDeleteFitment(fitment.id)}
                  disabled={deleteFitment.isPending}
                  className="ml-4 p-2 text-red-600 hover:bg-red-50 rounded-md transition-colors disabled:opacity-50"
                  title="Sil"
                >
                  <Trash2 className="h-5 w-5" />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
