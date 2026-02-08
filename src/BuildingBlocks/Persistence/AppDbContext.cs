using ErpCloud.BuildingBlocks.Auditing;
using ErpCloud.BuildingBlocks.Outbox;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Reflection;

namespace ErpCloud.BuildingBlocks.Persistence;

/// <summary>
/// Base DbContext with tenant isolation and audit support.
/// </summary>
public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        // Replace the model cache key to include the TenantContext instance hash
        // This ensures each DbContext with a different TenantContext gets its own compiled model
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, TenantAwareModelCacheKeyFactory>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure outbox messages
        ConfigureOutboxMessages(modelBuilder);

        // Configure processed messages
        ConfigureProcessedMessages(modelBuilder);

        // Configure audit logs
        ConfigureAuditLogs(modelBuilder);

        // Configure all TenantEntity derived types
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(TenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                // TenantId is required
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(TenantEntity.TenantId))
                    .IsRequired();

                // Global query filter for tenant isolation
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var tenantProperty = System.Linq.Expressions.Expression.Property(parameter, nameof(TenantEntity.TenantId));
                
                // Create filter: e => IsBypassEnabled || e.TenantId == TenantId
                var contextTenantId = System.Linq.Expressions.Expression.Property(
                    System.Linq.Expressions.Expression.Constant(_tenantContext),
                    nameof(ITenantContext.TenantId));
                
                var contextIsBypass = System.Linq.Expressions.Expression.Property(
                    System.Linq.Expressions.Expression.Constant(_tenantContext),
                    nameof(ITenantContext.IsBypassEnabled));
                
                // Bypass check: tenantContext.IsBypassEnabled
                var bypassCheck = contextIsBypass;
                
                // Tenant check: e.TenantId == tenantContext.TenantId
                var tenantCheck = System.Linq.Expressions.Expression.Equal(tenantProperty, contextTenantId);
                
                // Combined: bypass || tenantMatch
                var filterExpression = System.Linq.Expressions.Expression.OrElse(bypassCheck, tenantCheck);
                var lambda = System.Linq.Expressions.Expression.Lambda(filterExpression, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);

                // Indexes for performance
                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex(nameof(TenantEntity.TenantId));

                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex(nameof(TenantEntity.TenantId), nameof(TenantEntity.CreatedAt));
            }
        }
    }

    public override int SaveChanges()
    {
        ApplyTenantAndAudit();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantAndAudit();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTenantAndAudit()
    {
        var entries = ChangeTracker.Entries<TenantEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // TenantId guaranteed by middleware (non-nullable)
                entry.Entity.TenantId = _tenantContext.TenantId;

                // Set audit fields
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedBy = _tenantContext.UserId ?? Guid.Empty;
                
                // TODO: When implementing update audit:
                // if (entry.State == EntityState.Modified)
                // {
                //     entry.Entity.UpdatedAt = DateTime.UtcNow;
                //     entry.Entity.UpdatedBy = _tenantContext.UserId ?? Guid.Empty;
                // }
            }
        }

        // Create audit logs for all modified entities
        var auditEntries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || 
                       e.State == EntityState.Modified || 
                       e.State == EntityState.Deleted)
            .ToList();

        var auditLogs = AuditHelper.CreateAuditLogs(
            auditEntries,
            _tenantContext.TenantId,
            _tenantContext.UserId ?? Guid.Empty);

        if (auditLogs.Any())
        {
            Set<AuditLog>().AddRange(auditLogs);
        }
    }

    private void ConfigureOutboxMessages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
            entity.Property(e => e.OccurredAt).HasColumnName("occurred_at").IsRequired();
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(500).IsRequired();
            entity.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<int>().IsRequired();
            entity.Property(e => e.Attempts).HasColumnName("attempts").IsRequired();
            entity.Property(e => e.LastError).HasColumnName("last_error").HasMaxLength(2000);
            entity.Property(e => e.SentAt).HasColumnName("sent_at");
            entity.Property(e => e.NextAttemptAt).HasColumnName("next_attempt_at");

            entity.HasIndex(e => new { e.TenantId, e.Status, e.OccurredAt })
                .HasDatabaseName("ix_outbox_messages_tenant_status_occurred");
            entity.HasIndex(e => new { e.Status, e.OccurredAt })
                .HasDatabaseName("ix_outbox_messages_status_occurred");
        });
    }

    private void ConfigureProcessedMessages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedMessage>(entity =>
        {
            entity.ToTable("processed_messages");
            
            // Composite key: TenantId + MessageId
            entity.HasKey(e => new { e.TenantId, e.MessageId });
            
            entity.Property(e => e.MessageId)
                .HasColumnName("id")
                .IsRequired();
            
            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .IsRequired();
            
            entity.Property(e => e.ProcessedAt)
                .HasColumnName("processed_at")
                .IsRequired();
            
            // Unique index to prevent duplicate processing
            entity.HasIndex(e => new { e.TenantId, e.MessageId })
                .HasDatabaseName("ix_processed_messages_tenant_message")
                .IsUnique();
        });
    }

    private void ConfigureAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.EntityName).HasColumnName("entity_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.EntityId).HasColumnName("entity_id").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Action).HasColumnName("action").HasConversion<int>().IsRequired();
            entity.Property(e => e.OccurredAt).HasColumnName("occurred_at").IsRequired();
            entity.Property(e => e.DiffJson).HasColumnName("diff_json").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);

            // Indexes for efficient querying
            entity.HasIndex(e => new { e.TenantId, e.OccurredAt })
                .HasDatabaseName("ix_audit_logs_tenant_occurred")
                .IsDescending(false, true); // DESC on OccurredAt for recent-first queries
            entity.HasIndex(e => new { e.TenantId, e.EntityName, e.EntityId })
                .HasDatabaseName("ix_audit_logs_tenant_entity");
            entity.HasIndex(e => new { e.TenantId, e.UserId, e.OccurredAt })
                .HasDatabaseName("ix_audit_logs_tenant_user_occurred")
                .IsDescending(false, false, true);
        });
    }
}

/// <summary>
/// Custom model cache key factory that includes the DbContext instance hash.
/// This prevents EF Core from reusing cached models across different DbContext instances,
/// which would cause query filter tenant context capture issues.
/// </summary>
internal class TenantAwareModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        // Include the context instance hash code in the cache key
        // This ensures each DbContext instance gets its own compiled model
        return (context.GetType(), designTime, context.GetHashCode());
    }
}
