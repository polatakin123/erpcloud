using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class Organization : TenantEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }

    // Navigation
    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}
