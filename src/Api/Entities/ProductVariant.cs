using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class ProductVariant : TenantEntity
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty; // EA, KG, etc.
    public decimal VatRate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Product Product { get; set; } = null!;
    public ICollection<PriceListItem> PriceListItems { get; set; } = new List<PriceListItem>();
    public ICollection<PartReference> PartReferences { get; set; } = new List<PartReference>();
}
