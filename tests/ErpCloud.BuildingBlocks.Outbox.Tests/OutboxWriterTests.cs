using ErpCloud.BuildingBlocks.Outbox;
using ErpCloud.BuildingBlocks.Persistence;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ErpCloud.BuildingBlocks.Outbox.Tests;

public class OutboxWriterTests
{
    private class TestDbContext : AppDbContext
    {
        public TestDbContext(DbContextOptions options, ITenantContext tenantContext)
            : base(options, tenantContext)
        {
        }

        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    }

    [Fact]
    public async Task AddEventAsync_ShouldSerializeAndInsertToOutbox()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TestDbContext(options, mockTenantContext.Object);
        var outboxWriter = new OutboxWriter(dbContext);

        var testEvent = new TestEvent
        {
            OrderNo = "ORD-12345",
            Amount = 1250.75m
        };

        // Act
        await outboxWriter.AddEventAsync(tenantId, testEvent);
        await dbContext.SaveChangesAsync();

        // Assert
        var messages = await dbContext.OutboxMessages.ToListAsync();
        Assert.Single(messages);
        
        var message = messages[0];
        Assert.Equal(tenantId, message.TenantId);
        Assert.Equal("TestEvent", message.Type);
        Assert.Contains("ORD-12345", message.Payload);
        Assert.Contains("1250.75", message.Payload);
        Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.SentAt);
    }

    [Fact]
    public async Task AddEventAsync_ShouldSetCorrectTimestamp()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TestDbContext(options, mockTenantContext.Object);
        var outboxWriter = new OutboxWriter(dbContext);

        var testEvent = new TestEvent { OrderNo = "ORD-001", Amount = 100m };
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        await outboxWriter.AddEventAsync(tenantId, testEvent);
        await dbContext.SaveChangesAsync();

        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert
        var message = await dbContext.OutboxMessages.SingleAsync();
        Assert.True(message.OccurredAt >= before && message.OccurredAt <= after);
    }

    private class TestEvent
    {
        public string OrderNo { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
