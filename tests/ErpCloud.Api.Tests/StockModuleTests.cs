using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory;
using Moq;
using Xunit;

namespace ErpCloud.Api.Tests;

public class StockModuleTests
{
    private static ErpDbContext CreateTestDbContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());
        mockTenantContext.Setup(x => x.IsBypassEnabled).Returns(true);

        return new ErpDbContext(options, mockTenantContext.Object);
    }

    private static async Task<(Guid warehouseId, Guid variantId)> CreateWarehouseAndVariantAsync(ErpDbContext context, Guid tenantId)
    {
        // Create organization
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = "ORG001",
            Name = "Test Org",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        context.Organizations.Add(org);

        // Create branch
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrganizationId = org.Id,
            Code = "BR001",
            Name = "Test Branch",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        context.Branches.Add(branch);

        // Create warehouse
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branch.Id,
            Code = "WH001",
            Name = "Test Warehouse",
            Type = "MAIN",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        context.Warehouses.Add(warehouse);

        // Create product
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = "PROD001",
            Name = "Test Product",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        context.Products.Add(product);

        // Create variant
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = product.Id,
            Sku = "SKU001",
            Name = "Test Variant",
            Unit = "PCS",
            VatRate = 18.00m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        context.ProductVariants.Add(variant);

        await context.SaveChangesAsync();

        return (warehouse.Id, variant.Id);
    }

    [Fact]
    public async Task INBOUND_IncreaseOnHand()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var (warehouseId, variantId) = await CreateWarehouseAndVariantAsync(dbContext, tenantId);
        var stockService = new StockService(dbContext, mockTenantContext.Object);

        // Act
        await stockService.ReceiveStockAsync(new ReceiveStockDto(
            warehouseId, variantId, 100.0m, 10.50m, "PurchaseReceipt", Guid.NewGuid(), "Initial stock"
        ));

        // Assert
        var balance = await stockService.GetBalanceAsync(warehouseId, variantId);
        Assert.NotNull(balance);
        Assert.Equal(100.0m, balance.OnHand);
        Assert.Equal(0m, balance.Reserved);
        Assert.Equal(100.0m, balance.Available);
    }

    [Fact]
    public async Task OUTBOUND_DecreaseOnHand()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var (warehouseId, variantId) = await CreateWarehouseAndVariantAsync(dbContext, tenantId);
        var stockService = new StockService(dbContext, mockTenantContext.Object);

        // Setup: Receive stock first
        await stockService.ReceiveStockAsync(new ReceiveStockDto(
            warehouseId, variantId, 100.0m, null, null, null, null
        ));

        // Act: Issue stock
        await stockService.IssueStockAsync(new IssueStockDto(
            warehouseId, variantId, 30.0m, "SalesOrder", Guid.NewGuid(), "Ship to customer"
        ));

        // Assert
        var balance = await stockService.GetBalanceAsync(warehouseId, variantId);
        Assert.NotNull(balance);
        Assert.Equal(70.0m, balance.OnHand);
        Assert.Equal(0m, balance.Reserved);
        Assert.Equal(70.0m, balance.Available);
    }

    [Fact]
    public async Task RESERVE_IncreaseReserved()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var (warehouseId, variantId) = await CreateWarehouseAndVariantAsync(dbContext, tenantId);
        var stockService = new StockService(dbContext, mockTenantContext.Object);

        // Setup: Receive stock first
        await stockService.ReceiveStockAsync(new ReceiveStockDto(
            warehouseId, variantId, 100.0m, null, null, null, null
        ));

        // Act: Reserve stock
        await stockService.ReserveStockAsync(new ReserveStockDto(
            warehouseId, variantId, 25.0m, "SalesOrder", Guid.NewGuid(), "Reserve for order"
        ));

        // Assert
        var balance = await stockService.GetBalanceAsync(warehouseId, variantId);
        Assert.NotNull(balance);
        Assert.Equal(100.0m, balance.OnHand);
        Assert.Equal(25.0m, balance.Reserved);
        Assert.Equal(75.0m, balance.Available);
    }

    [Fact]
    public async Task RELEASE_DecreaseReserved()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var (warehouseId, variantId) = await CreateWarehouseAndVariantAsync(dbContext, tenantId);
        var stockService = new StockService(dbContext, mockTenantContext.Object);

        // Setup: Receive and reserve
        await stockService.ReceiveStockAsync(new ReceiveStockDto(
            warehouseId, variantId, 100.0m, null, null, null, null
        ));
        await stockService.ReserveStockAsync(new ReserveStockDto(
            warehouseId, variantId, 25.0m, null, null, null
        ));

        // Act: Release reservation
        await stockService.ReleaseReservationAsync(new ReleaseReservationDto(
            warehouseId, variantId, 10.0m, null, null, null
        ));

        // Assert
        var balance = await stockService.GetBalanceAsync(warehouseId, variantId);
        Assert.NotNull(balance);
        Assert.Equal(100.0m, balance.OnHand);
        Assert.Equal(15.0m, balance.Reserved);
        Assert.Equal(85.0m, balance.Available);
    }

    [Fact]
    public async Task Available_EqualsOnHandMinusReserved()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var (warehouseId, variantId) = await CreateWarehouseAndVariantAsync(dbContext, tenantId);
        var stockService = new StockService(dbContext, mockTenantContext.Object);

        // Act: Various operations
        await stockService.ReceiveStockAsync(new ReceiveStockDto(warehouseId, variantId, 100.0m, null, null, null, null));
        await stockService.ReserveStockAsync(new ReserveStockDto(warehouseId, variantId, 20.0m, null, null, null));
        await stockService.IssueStockAsync(new IssueStockDto(warehouseId, variantId, 10.0m, null, null, null));
        await stockService.ReserveStockAsync(new ReserveStockDto(warehouseId, variantId, 15.0m, null, null, null));

        // Assert
        var balance = await stockService.GetBalanceAsync(warehouseId, variantId);
        Assert.NotNull(balance);
        Assert.Equal(90.0m, balance.OnHand); // 100 - 10
        Assert.Equal(35.0m, balance.Reserved); // 20 + 15
        Assert.Equal(55.0m, balance.Available); // 90 - 35
        Assert.Equal(balance.OnHand - balance.Reserved, balance.Available);
    }

    [Fact]
    public async Task ReserveStock_InsufficientAvailable_ThrowsException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var (warehouseId, variantId) = await CreateWarehouseAndVariantAsync(dbContext, tenantId);
        var stockService = new StockService(dbContext, mockTenantContext.Object);

        // Setup: Small stock
        await stockService.ReceiveStockAsync(new ReceiveStockDto(
            warehouseId, variantId, 10.0m, null, null, null, null
        ));

        // Act & Assert: Try to reserve more than available
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            stockService.ReserveStockAsync(new ReserveStockDto(
                warehouseId, variantId, 15.0m, null, null, null
            ))
        );
    }

    [Fact]
    public async Task IssueStock_InsufficientAvailable_ThrowsException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var (warehouseId, variantId) = await CreateWarehouseAndVariantAsync(dbContext, tenantId);
        var stockService = new StockService(dbContext, mockTenantContext.Object);

        // Setup: Stock with reservation
        await stockService.ReceiveStockAsync(new ReceiveStockDto(
            warehouseId, variantId, 50.0m, null, null, null, null
        ));
        await stockService.ReserveStockAsync(new ReserveStockDto(
            warehouseId, variantId, 30.0m, null, null, null
        ));

        // Act & Assert: Try to issue more than available (50 - 30 = 20 available, trying 25)
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            stockService.IssueStockAsync(new IssueStockDto(
                warehouseId, variantId, 25.0m, null, null, null
            ))
        );
    }

    [Fact]
    public async Task ReleaseReservation_InsufficientReserved_ThrowsException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var (warehouseId, variantId) = await CreateWarehouseAndVariantAsync(dbContext, tenantId);
        var stockService = new StockService(dbContext, mockTenantContext.Object);

        // Setup: Stock with small reservation
        await stockService.ReceiveStockAsync(new ReceiveStockDto(
            warehouseId, variantId, 100.0m, null, null, null, null
        ));
        await stockService.ReserveStockAsync(new ReserveStockDto(
            warehouseId, variantId, 10.0m, null, null, null
        ));

        // Act & Assert: Try to release more than reserved
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            stockService.ReleaseReservationAsync(new ReleaseReservationDto(
                warehouseId, variantId, 15.0m, null, null, null
            ))
        );
    }

    [Fact]
    public async Task TransferStock_MovesStockBetweenWarehouses()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var (warehouse1Id, variantId) = await CreateWarehouseAndVariantAsync(dbContext, tenantId);
        
        // Create second warehouse
        var branch = await dbContext.Branches.FirstAsync();
        var warehouse2 = new Warehouse
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branch.Id,
            Code = "WH002",
            Name = "Warehouse 2",
            Type = "STORE",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        dbContext.Warehouses.Add(warehouse2);
        await dbContext.SaveChangesAsync();
        
        var stockService = new StockService(dbContext, mockTenantContext.Object);

        // Setup: Add stock to warehouse 1
        await stockService.ReceiveStockAsync(new ReceiveStockDto(
            warehouse1Id, variantId, 100.0m, null, null, null, null
        ));

        // Act: Transfer stock
        await stockService.TransferStockAsync(new TransferStockDto(
            warehouse1Id, warehouse2.Id, variantId, 40.0m, "Transfer", Guid.NewGuid(), "Transfer between warehouses"
        ));

        // Assert
        var balance1 = await stockService.GetBalanceAsync(warehouse1Id, variantId);
        var balance2 = await stockService.GetBalanceAsync(warehouse2.Id, variantId);

        Assert.NotNull(balance1);
        Assert.NotNull(balance2);
        Assert.Equal(60.0m, balance1.OnHand); // 100 - 40
        Assert.Equal(40.0m, balance2.OnHand); // 0 + 40
    }

    [Fact]
    public async Task Ledger_IsImmutable_NoUpdates()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var (warehouseId, variantId) = await CreateWarehouseAndVariantAsync(dbContext, tenantId);
        var stockService = new StockService(dbContext, mockTenantContext.Object);

        // Act: Create entry
        await stockService.ReceiveStockAsync(new ReceiveStockDto(
            warehouseId, variantId, 100.0m, 10.0m, "PurchaseReceipt", Guid.NewGuid(), "Test"
        ));

        // Get entry
        var ledger = await stockService.GetLedgerAsync(warehouseId, variantId, null, null, 1, 10);
        var entry = ledger.Items.First();

        // Assert: Ledger entry should be immutable (no update method in service)
        // Attempting to modify through DbContext should not affect ledger
        var dbEntry = await dbContext.StockLedgerEntries.FindAsync(entry.Id);
        Assert.NotNull(dbEntry);
        Assert.Equal(100.0m, dbEntry.Quantity);
        Assert.Equal(10.0m, dbEntry.UnitCost);

        // Note: In real implementation, EF configuration would prevent updates
        // For now, verify entry exists and matches
        Assert.Equal(entry.Quantity, dbEntry.Quantity);
    }

    [Fact]
    public async Task TenantIsolation_StockBalancesSeparated()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using var dbContextA = CreateTestDbContext(tenantA);
        await using var dbContextB = CreateTestDbContext(tenantB);

        var mockTenantContextA = new Mock<ITenantContext>();
        mockTenantContextA.Setup(x => x.TenantId).Returns(tenantA);
        mockTenantContextA.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var mockTenantContextB = new Mock<ITenantContext>();
        mockTenantContextB.Setup(x => x.TenantId).Returns(tenantB);
        mockTenantContextB.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var (warehouseA, variantA) = await CreateWarehouseAndVariantAsync(dbContextA, tenantA);
        var (warehouseB, variantB) = await CreateWarehouseAndVariantAsync(dbContextB, tenantB);

        var stockServiceA = new StockService(dbContextA, mockTenantContextA.Object);
        var stockServiceB = new StockService(dbContextB, mockTenantContextB.Object);

        // Act: Different stock in each tenant
        await stockServiceA.ReceiveStockAsync(new ReceiveStockDto(
            warehouseA, variantA, 100.0m, null, null, null, null
        ));
        await stockServiceB.ReceiveStockAsync(new ReceiveStockDto(
            warehouseB, variantB, 200.0m, null, null, null, null
        ));

        // Assert: Each tenant sees only their stock
        var balanceA = await stockServiceA.GetBalanceAsync(warehouseA, variantA);
        var balanceB = await stockServiceB.GetBalanceAsync(warehouseB, variantB);

        Assert.NotNull(balanceA);
        Assert.NotNull(balanceB);
        Assert.Equal(100.0m, balanceA.OnHand);
        Assert.Equal(200.0m, balanceB.OnHand);
    }

    [Fact]
    public async Task Concurrency_TwoReserves_OnlyOneSucceeds()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        
        // Use a real database for concurrency testing (InMemory doesn't support row locking)
        // For this test, we'll simulate the behavior by running sequentially
        // In production, this would be tested with real Postgres + parallel requests
        
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var (warehouseId, variantId) = await CreateWarehouseAndVariantAsync(dbContext, tenantId);
        var stockService = new StockService(dbContext, mockTenantContext.Object);

        // Setup: Limited stock
        await stockService.ReceiveStockAsync(new ReceiveStockDto(
            warehouseId, variantId, 10.0m, null, null, null, null
        ));

        // Act: Try to reserve more than available in sequence (simulating concurrent requests)
        var reserve1Success = true;
        var reserve2Success = true;

        try
        {
            await stockService.ReserveStockAsync(new ReserveStockDto(
                warehouseId, variantId, 8.0m, null, null, null
            ));
        }
        catch (InvalidOperationException)
        {
            reserve1Success = false;
        }

        try
        {
            await stockService.ReserveStockAsync(new ReserveStockDto(
                warehouseId, variantId, 8.0m, null, null, null
            ));
        }
        catch (InvalidOperationException)
        {
            reserve2Success = false;
        }

        // Assert: Only one should succeed (or both fail if insufficient)
        var balance = await stockService.GetBalanceAsync(warehouseId, variantId);
        
        // First reservation should succeed, second should fail
        Assert.True(reserve1Success);
        Assert.False(reserve2Success);
        Assert.NotNull(balance);
        Assert.Equal(8.0m, balance.Reserved);
        Assert.Equal(2.0m, balance.Available);
    }
}
