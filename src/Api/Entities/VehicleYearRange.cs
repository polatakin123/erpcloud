using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Vehicle year range (e.g., 2015-2020)
/// </summary>
public class VehicleYearRange : TenantEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Model (FK to VehicleModel)
    /// </summary>
    public Guid ModelId { get; set; }
    public VehicleModel Model { get; set; } = null!;

    /// <summary>
    /// Starting year (inclusive)
    /// </summary>
    public int YearFrom { get; set; }

    /// <summary>
    /// Ending year (inclusive)
    /// </summary>
    public int YearTo { get; set; }

    /// <summary>
    /// Engines for this year range
    /// </summary>
    public ICollection<VehicleEngine> Engines { get; set; } = new List<VehicleEngine>();
}
