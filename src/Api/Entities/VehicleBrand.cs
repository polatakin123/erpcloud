using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Vehicle brand (e.g., Toyota, Mercedes-Benz, BMW)
/// </summary>
public class VehicleBrand : TenantEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Brand code (unique per tenant)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Brand name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Models under this brand
    /// </summary>
    public ICollection<VehicleModel> Models { get; set; } = new List<VehicleModel>();
}
