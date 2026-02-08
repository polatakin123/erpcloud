using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Stock card fitment - links a product variant (part) to a vehicle engine
/// </summary>
public class StockCardFitment : TenantEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Product variant (FK to ProductVariant - this is the "stock card")
    /// </summary>
    public Guid VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;

    /// <summary>
    /// Vehicle engine (FK to VehicleEngine)
    /// </summary>
    public Guid VehicleEngineId { get; set; }
    public VehicleEngine VehicleEngine { get; set; } = null!;

    /// <summary>
    /// Optional notes for this fitment
    /// </summary>
    public string? Notes { get; set; }
}
