using ErpCloud.BuildingBlocks.Auditing;
using ErpCloud.BuildingBlocks.Persistence;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;

namespace ErpCloud.BuildingBlocks.Auditing.Tests;

public class AuditTests
{
    private class TestEntity : TenantEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Password { get; set; } // Sensitive field
    }

    private class TestDbContext : AppDbContext
    {
        public TestDbContext(DbContextOptions options, ITenantContext tenantContext)
            : base(options, tenantContext)
        {
        }

        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    }

    [Fact]
    public async Task Update_ShouldLogOnlyModifiedFields()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(userId);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TestDbContext(options, mockTenantContext.Object);

        // Create initial entity
        var entity = new TestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Original Name",
            Price = 100m,
            Password = "secret123",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };
        dbContext.TestEntities.Add(entity);
        await dbContext.SaveChangesAsync();

        // Clear audit logs from creation
        var creationLogs = await dbContext.AuditLogs.ToListAsync();
        dbContext.AuditLogs.RemoveRange(creationLogs);
        await dbContext.SaveChangesAsync();

        // Act - Update only Name and Price (not Password)
        entity.Name = "Updated Name";
        entity.Price = 200m;
        await dbContext.SaveChangesAsync();

        // Assert
        var auditLogs = await dbContext.AuditLogs.ToListAsync();
        Assert.Single(auditLogs);

        var auditLog = auditLogs[0];
        Assert.Equal(AuditAction.Updated, auditLog.Action);
        Assert.Equal(nameof(TestEntity), auditLog.EntityName);

        // Parse diff JSON
        var diff = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog.DiffJson);
        Assert.NotNull(diff);
        Assert.True(diff.ContainsKey("before"));
        Assert.True(diff.ContainsKey("after"));

        var before = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(diff["before"].GetRawText());
        var after = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(diff["after"].GetRawText());

        // Should only contain modified fields
        Assert.Equal(2, before?.Count); // Name and Price
        Assert.Equal(2, after?.Count);
        
        Assert.NotNull(before);
        Assert.True(before.ContainsKey("Name"));
        Assert.True(before.ContainsKey("Price"));
        Assert.False(before.ContainsKey("Password")); // Not modified, should not be in diff

        Assert.Equal("Original Name", before["Name"].GetString());
        Assert.Equal(100m, before["Price"].GetDecimal());
        
        Assert.NotNull(after);
        Assert.Equal("Updated Name", after["Name"].GetString());
        Assert.Equal(200m, after["Price"].GetDecimal());
    }

    [Fact]
    public async Task Create_ShouldLogAllFields()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(userId);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TestDbContext(options, mockTenantContext.Object);

        // Act - Create new entity
        var entity = new TestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Test Item",
            Price = 150m,
            Password = "secret456",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };
        dbContext.TestEntities.Add(entity);
        await dbContext.SaveChangesAsync();

        // Assert
        var auditLogs = await dbContext.AuditLogs.ToListAsync();
        Assert.Single(auditLogs);

        var auditLog = auditLogs[0];
        Assert.Equal(AuditAction.Created, auditLog.Action);
        Assert.Equal(nameof(TestEntity), auditLog.EntityName);

        // Parse diff JSON
        var diff = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog.DiffJson);
        Assert.NotNull(diff);
        Assert.True(diff.ContainsKey("after"));

        var after = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(diff["after"].GetRawText());
        Assert.NotNull(after);

        // Should contain all scalar fields
        Assert.True(after.ContainsKey("Name"));
        Assert.True(after.ContainsKey("Price"));
        Assert.True(after.ContainsKey("Password")); // Sensitive field masking tested separately
        
        Assert.Equal("Test Item", after["Name"].GetString());
        Assert.Equal(150m, after["Price"].GetDecimal());
    }

    [Fact]
    public async Task SensitiveField_ShouldBeMasked()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(userId);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TestDbContext(options, mockTenantContext.Object);

        // Act - Create entity with sensitive field
        var entity = new TestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Test",
            Price = 100m,
            Password = "SuperSecretPassword123!",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };
        dbContext.TestEntities.Add(entity);
        await dbContext.SaveChangesAsync();

        // Assert
        var auditLog = await dbContext.AuditLogs.FirstAsync();
        var diff = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog.DiffJson);
        var after = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(diff!["after"].GetRawText());

        // Password should be masked
        Assert.Equal("***", after!["Password"].GetString());
    }
}
