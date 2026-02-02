using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

public class EDocumentStatusHistory : TenantEntity
{
    public Guid Id { get; set; }
    public Guid EDocumentId { get; set; }
    public string Status { get; set; } = null!;
    public string? Message { get; set; }
    public DateTime OccurredAt { get; set; }

    // Navigation
    public EDocument EDocument { get; set; } = null!;
}
