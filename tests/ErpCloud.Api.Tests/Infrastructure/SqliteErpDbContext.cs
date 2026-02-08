using ErpCloud.Api.Data;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Tests.Infrastructure;

/// <summary>
/// Test-specific ErpDbContext for SQLite in-memory testing.
/// Uses SQLite's native LIKE operator which is case-insensitive by default.
/// </summary>
public class SqliteErpDbContext : ErpDbContext
{
    public SqliteErpDbContext(DbContextOptions<ErpDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }
}
