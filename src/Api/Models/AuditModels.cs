namespace ErpCloud.Api.Models;

/// <summary>
/// Audit log DTO for API responses
/// </summary>
public class AuditLogDto
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string DiffJson { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Paginated response for audit logs
/// </summary>
public class PaginatedAuditResponse
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int Total { get; set; }
    public List<AuditLogDto> Items { get; set; } = new();
}
