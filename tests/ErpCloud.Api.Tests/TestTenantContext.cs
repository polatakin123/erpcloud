using ErpCloud.BuildingBlocks.Tenant;

namespace ErpCloud.Api.Tests;

/// <summary>
/// Simple POCO implementation of ITenantContext for testing.
/// Avoids Mock<> issues with EF Core query compilation.
/// </summary>
public class TestTenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public bool IsBypassEnabled { get; set; } = true;
}
