using ErpCloud.Api.Data;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Tests;

/// <summary>
/// Factory for creating isolated test database contexts.
/// Uses EF Core InMemory provider with disabled parallelization (see AssemblyInfo.cs).
/// Query filters are disabled for tests since Mock<ITenantContext> doesn't work well with EF query compilation.
/// </summary>
public sealed class TestDbFactory : IDisposable
{
    private readonly DbContextOptions<ErpDbContext> _options;
    private readonly string _databaseName;

    public TestDbFactory(ITenantContext tenantContext)
    {
        // Use unique database name for complete isolation between test classes
        _databaseName = Guid.NewGuid().ToString();

        // Configure DbContext to use InMemory database
        _options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        // Create the initial context to ensure database is created
        using var context = new ErpDbContext(_options, tenantContext);
        context.Database.EnsureCreated();
    }

    /// <summary>
    /// Creates a new DbContext instance using the same InMemory database.
    /// Each call returns a fresh context, but all contexts share the same in-memory database.
    /// </summary>
    public ErpDbContext CreateContext(ITenantContext tenantContext)
    {
        return new ErpDbContext(_options, tenantContext);
    }

    public void Dispose()
    {
        // InMemory database is automatically cleaned up
    }
}
