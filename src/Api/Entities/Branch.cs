using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class Branch : TenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Address { get; set; }

    // Navigation
    public Organization Organization { get; set; } = null!;
    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
