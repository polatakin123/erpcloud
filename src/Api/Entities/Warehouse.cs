using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class Warehouse : TenantEntity
{
    public Guid Id { get; set; }
    public Guid BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // MAIN, STORE, VIRTUAL
    public bool IsDefault { get; set; }

    // Navigation
    public Branch Branch { get; set; } = null!;
}
