using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Vehicle model (e.g., Corolla, C-Class, 3 Series)
/// </summary>
public class VehicleModel : TenantEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Brand (FK to VehicleBrand)
    /// </summary>
    public Guid BrandId { get; set; }
    public VehicleBrand Brand { get; set; } = null!;

    /// <summary>
    /// Model name (unique per brand per tenant)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Year ranges for this model
    /// </summary>
    public ICollection<VehicleYearRange> YearRanges { get; set; } = new List<VehicleYearRange>();
}
