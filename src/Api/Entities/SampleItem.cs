using ErpCloud.BuildingBlocks.Persistence;
using System.ComponentModel.DataAnnotations;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Sample entity for demonstrating multi-tenant functionality and audit logging.
/// </summary>
public class SampleItem : TenantEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    
    public decimal Price { get; set; }
    
    /// <summary>
    /// Sensitive field - will be masked in audit logs
    /// </summary>
    public string? Password { get; set; }
}
