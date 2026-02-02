using ErpCloud.BuildingBlocks.Persistence;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ErpCloud.BuildingBlocks.Outbox.Tests;

public class IdempotencyTests
{
    private class TestDbContext : AppDbContext
    {
        public TestDbContext(DbContextOptions options, ITenantContext tenantContext)
            : base(options, tenantContext)
        {
        }

        public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    }

    [Fact]
    public async Task ProcessedMessage_ShouldPreventDuplicateWithSameMessageId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        // Act - First insert
        using (var dbContext = new TestDbContext(options, mockTenantContext.Object))
        {
            var firstMessage = new ProcessedMessage
            {
                MessageId = messageId,
                TenantId = tenantId,
                ProcessedAt = DateTime.UtcNow
            };
            dbContext.ProcessedMessages.Add(firstMessage);
            await dbContext.SaveChangesAsync();
        }

        // Act - Check if duplicate exists (simulates idempotency check in consumer)
        using (var dbContext = new TestDbContext(options, mockTenantContext.Object))
        {
            var alreadyProcessed = await dbContext.ProcessedMessages
                .AnyAsync(p => p.TenantId == tenantId && p.MessageId == messageId);

            // Assert - Should detect that message was already processed
            Assert.True(alreadyProcessed, "Message should be marked as already processed");
        }
    }

    [Fact]
    public async Task ProcessedMessage_ShouldAllowSameMessageIdForDifferentTenants()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        var sharedMessageId = Guid.NewGuid();
        var mockTenantContext = new Mock<ITenantContext>();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var dbContext = new TestDbContext(options, mockTenantContext.Object);

        // Act - Insert same messageId for two different tenants
        var message1 = new ProcessedMessage
        {
            MessageId = sharedMessageId,
            TenantId = tenant1Id,
            ProcessedAt = DateTime.UtcNow
        };
        dbContext.ProcessedMessages.Add(message1);
        await dbContext.SaveChangesAsync();

        var message2 = new ProcessedMessage
        {
            MessageId = sharedMessageId,
            TenantId = tenant2Id,
            ProcessedAt = DateTime.UtcNow
        };
        dbContext.ProcessedMessages.Add(message2);
        await dbContext.SaveChangesAsync();

        // Assert - Both messages should exist (different tenant isolation)
        var allMessages = await dbContext.ProcessedMessages.ToListAsync();
        Assert.Equal(2, allMessages.Count);
        Assert.Contains(allMessages, m => m.TenantId == tenant1Id && m.MessageId == sharedMessageId);
        Assert.Contains(allMessages, m => m.TenantId == tenant2Id && m.MessageId == sharedMessageId);
    }
}
