using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class PriceListItem : TenantEntity
{
    public Guid Id { get; set; }
    public Guid PriceListId { get; set; }
    public Guid VariantId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? MinQty { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    // Navigation
    public PriceList PriceList { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
}
