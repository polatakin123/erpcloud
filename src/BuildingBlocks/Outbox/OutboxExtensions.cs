using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpCloud.BuildingBlocks.Outbox;

public static class OutboxExtensions
{
    /// <summary>
    /// Adds outbox services to the service collection
    /// </summary>
    public static IServiceCollection AddOutbox<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IOutboxWriter>(sp => 
        {
            var dbContext = sp.GetRequiredService<TDbContext>();
            return new OutboxWriter(dbContext);
        });
        return services;
    }

    /// <summary>
    /// Configures outbox message entity in DbContext
    /// </summary>
    public static void ConfigureOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .IsRequired();

            entity.Property(e => e.OccurredAt)
                .HasColumnName("occurred_at")
                .IsRequired();

            entity.Property(e => e.Type)
                .HasColumnName("type")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .IsRequired();

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<int>()
                .IsRequired();

            entity.Property(e => e.Attempts)
                .HasColumnName("attempts")
                .IsRequired();

            entity.Property(e => e.LastError)
                .HasColumnName("last_error")
                .HasMaxLength(2000);

            entity.Property(e => e.SentAt)
                .HasColumnName("sent_at");

            // Indexes for efficient querying
            entity.HasIndex(e => new { e.TenantId, e.Status, e.OccurredAt })
                .HasDatabaseName("ix_outbox_messages_tenant_status_occurred");

            entity.HasIndex(e => new { e.Status, e.Attempts })
                .HasDatabaseName("ix_outbox_messages_status_attempts");
        });
    }
}
