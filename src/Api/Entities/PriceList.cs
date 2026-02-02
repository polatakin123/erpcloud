using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class PriceList : TenantEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty; // TRY, USD, EUR
    public bool IsDefault { get; set; }

    // Navigation
    public ICollection<PriceListItem> Items { get; set; } = new List<PriceListItem>();
}
