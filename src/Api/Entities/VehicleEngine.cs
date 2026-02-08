using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Vehicle engine (e.g., 1.6 Benzin, 2.0 Dizel)
/// </summary>
public class VehicleEngine : TenantEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Year range (FK to VehicleYearRange)
    /// </summary>
    public Guid YearRangeId { get; set; }
    public VehicleYearRange YearRange { get; set; } = null!;

    /// <summary>
    /// Engine code (e.g., "1.6", "2.0 TDI")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Fuel type (BENZIN, DIZEL, HYBRID, ELEKTRIK, LPG, CNG)
    /// </summary>
    public string FuelType { get; set; } = string.Empty;

    /// <summary>
    /// Stock card fitments using this engine
    /// </summary>
    public ICollection<StockCardFitment> Fitments { get; set; } = new List<StockCardFitment>();
}
