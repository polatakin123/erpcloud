using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ErpCloud.BuildingBlocks.Auditing;

/// <summary>
/// Audit helper for generating audit logs from EF Core change tracking
/// </summary>
public static class AuditHelper
{
    private static readonly HashSet<string> SensitiveFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "Secret", "ApiKey", "Token", "PrivateKey", "ConnectionString"
    };

    /// <summary>
    /// Creates audit logs for modified entries
    /// </summary>
    public static List<AuditLog> CreateAuditLogs(
        IEnumerable<EntityEntry> entries,
        Guid tenantId,
        Guid userId)
    {
        var auditLogs = new List<AuditLog>();

        foreach (var entry in entries)
        {
            // Skip audit logs themselves and entities without keys
            if (entry.Entity is AuditLog || !entry.Metadata.FindPrimaryKey()?.Properties.Any() == true)
                continue;

            var entityName = entry.Metadata.ClrType.Name;
            var entityId = GetEntityId(entry);
            
            if (string.IsNullOrEmpty(entityId))
                continue;

            AuditLog? auditLog = entry.State switch
            {
                EntityState.Added => CreateAddedAudit(entry, tenantId, userId, entityName, entityId),
                EntityState.Modified => CreateModifiedAudit(entry, tenantId, userId, entityName, entityId),
                EntityState.Deleted => CreateDeletedAudit(entry, tenantId, userId, entityName, entityId),
                _ => null
            };

            if (auditLog != null)
            {
                auditLogs.Add(auditLog);
            }
        }

        return auditLogs;
    }

    private static AuditLog CreateAddedAudit(
        EntityEntry entry,
        Guid tenantId,
        Guid userId,
        string entityName,
        string entityId)
    {
        var currentValues = GetPropertyValues(entry.CurrentValues);

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            EntityName = entityName,
            EntityId = entityId,
            Action = AuditAction.Created,
            OccurredAt = DateTime.UtcNow,
            DiffJson = JsonSerializer.Serialize(new { after = currentValues })
        };
    }

    private static AuditLog? CreateModifiedAudit(
        EntityEntry entry,
        Guid tenantId,
        Guid userId,
        string entityName,
        string entityId)
    {
        var modifiedProperties = entry.Properties
            .Where(p => p.IsModified && !p.Metadata.IsPrimaryKey())
            .ToList();

        if (!modifiedProperties.Any())
            return null;

        var beforeValues = new Dictionary<string, object?>();
        var afterValues = new Dictionary<string, object?>();

        foreach (var property in modifiedProperties)
        {
            var propertyName = property.Metadata.Name;
            beforeValues[propertyName] = MaskSensitiveValue(propertyName, property.OriginalValue);
            afterValues[propertyName] = MaskSensitiveValue(propertyName, property.CurrentValue);
        }

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            EntityName = entityName,
            EntityId = entityId,
            Action = AuditAction.Updated,
            OccurredAt = DateTime.UtcNow,
            DiffJson = JsonSerializer.Serialize(new { before = beforeValues, after = afterValues })
        };
    }

    private static AuditLog CreateDeletedAudit(
        EntityEntry entry,
        Guid tenantId,
        Guid userId,
        string entityName,
        string entityId)
    {
        var originalValues = GetPropertyValues(entry.OriginalValues);

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            EntityName = entityName,
            EntityId = entityId,
            Action = AuditAction.Deleted,
            OccurredAt = DateTime.UtcNow,
            DiffJson = JsonSerializer.Serialize(new { before = originalValues })
        };
    }

    private static string GetEntityId(EntityEntry entry)
    {
        var keyValues = entry.Metadata.FindPrimaryKey()?.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "")
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();

        return keyValues?.Any() == true ? string.Join("-", keyValues) : string.Empty;
    }

    private static Dictionary<string, object?> GetPropertyValues(PropertyValues propertyValues)
    {
        var result = new Dictionary<string, object?>();

        foreach (var property in propertyValues.Properties)
        {
            // Skip primary keys and navigation properties
            if (property.IsPrimaryKey() || property.IsForeignKey())
                continue;

            var propertyName = property.Name;
            var value = propertyValues[property];
            result[propertyName] = MaskSensitiveValue(propertyName, value);
        }

        return result;
    }

    private static object? MaskSensitiveValue(string propertyName, object? value)
    {
        if (value == null)
            return null;

        // Mask sensitive fields
        if (SensitiveFieldNames.Contains(propertyName))
            return "***";

        return value;
    }
}
