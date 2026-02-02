using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ErpCloud.Api.Data;

/// <summary>
/// Design-time factory for EF Core migrations
/// </summary>
public class ErpDbContextFactory : IDesignTimeDbContextFactory<ErpDbContext>
{
    public ErpDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ErpDbContext>();
        
        // Use connection string from environment or default
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=erpcloud;Username=postgres;Password=Po359695@";
        
        optionsBuilder.UseNpgsql(connectionString);

        // Create a dummy tenant context for design-time
        var tenantContext = new DesignTimeTenantContext();

        return new ErpDbContext(optionsBuilder.Options, tenantContext);
    }

    private class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty; // Design-time placeholder
        public Guid? UserId => null;
        public bool IsBypassEnabled => true; // Bypass tenant filter during migrations
    }
}
