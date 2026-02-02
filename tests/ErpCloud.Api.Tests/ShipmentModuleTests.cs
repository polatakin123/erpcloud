using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ErpCloud.Api.Tests;

public class ShipmentModuleTests : IDisposable
{
    private readonly TestDbFactory _dbFactory;
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IStockService _stockService;
    private readonly ISalesOrderService _salesOrderService;
    private readonly IShipmentService _shipmentService;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _branchId;
    private readonly Guid _warehouseId;
    private readonly Guid _partyId;
    private readonly Guid _variantId;

    public ShipmentModuleTests()
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(_tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(_userId);
        mockTenantContext.Setup(x => x.IsBypassEnabled).Returns(true);  // Bypass query filter for tests
        _tenantContext = mockTenantContext.Object;

        // Use InMemory for proper relational behavior
        _dbFactory = new TestDbFactory(_tenantContext);
        _context = _dbFactory.CreateContext(_tenantContext);

        // Seed test data FIRST
        _branchId = SeedBranch();
        _warehouseId = SeedWarehouse();
        _partyId = SeedParty();
        _variantId = SeedVariant();

        // DO NOT clear change tracker - causes issues with seeded data

        // Create test services AFTER seeding (they use same context)
        _stockService = new StockService(_context, _tenantContext);
        _salesOrderService = new SalesOrderService(_context, _tenantContext, _stockService);
        _shipmentService = new ShipmentService(_context, _tenantContext, _stockService);
    }

    private Guid SeedBranch()
    {
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = "ORG001",
            Name = "Test Organization",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = org.Id,
            Code = "BR001",
            Name = "Main Branch",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };

        _context.Organizations.Add(org);
        _context.Branches.Add(branch);
        _context.SaveChanges();

        return branch.Id;
    }

    private Guid SeedWarehouse()
    {
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            BranchId = _branchId,
            Code = "WH001",
            Name = "Main Warehouse",
            Type = "MAIN",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };

        _context.Warehouses.Add(warehouse);
        _context.SaveChanges();

        return warehouse.Id;
    }

    private Guid SeedParty()
    {
        var party = new Party
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = "CUST001",
            Name = "Test Customer",
            Type = "CUSTOMER",
            TaxNumber = "1234567890",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };

        _context.Parties.Add(party);
        _context.SaveChanges();

        return party.Id;
    }

    private Guid SeedVariant()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = "PROD001",
            Name = "Test Product",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = product.Id,
            Sku = "SKU001",
            Name = "Test Variant",
            Unit = "PCS",
            VatRate = 20m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.SaveChanges();

        return variant.Id;
    }

    private async Task<Guid> CreateConfirmedSalesOrderAsync(decimal qty = 100m)
    {
        // Receive stock first
        await _stockService.ReceiveStockAsync(new ReceiveStockDto(
            _warehouseId,  // WarehouseId first
            _variantId,    // VariantId second
            qty,
            100m,
            "PO",
            Guid.NewGuid(),
            "Initial stock"
        ));

        // Create and confirm order
        var orderDto = new CreateSalesOrderDto(
            "ORD-" + Guid.NewGuid().ToString("N").Substring(0, 6),
            _partyId,
            _branchId,
            _warehouseId,
            null,
            DateTime.Today,
            null,
            new List<CreateSalesOrderLineDto>
            {
                new(_variantId, qty, 150m, 20m, null)
            }
        );

        var order = await _salesOrderService.CreateDraftAsync(orderDto);
        await _salesOrderService.ConfirmAsync(order.Id);

        return order.Id;
    }

    [Fact]
    public async Task Test01_CreateShipmentDraft_Success()
    {
        // Arrange
        var orderId = await CreateConfirmedSalesOrderAsync();
        var orderLine = await _context.SalesOrderLines.FirstAsync(l => l.SalesOrderId == orderId);

        var dto = new CreateShipmentDto(
            "SHIP-001",
            orderId,
            _branchId,
            _warehouseId,
            DateTime.Today,
            "Test shipment",
            new List<CreateShipmentLineDto>
            {
                new(orderLine.Id, _variantId, 50m, null)
            }
        );

        // Act
        var result = await _shipmentService.CreateDraftAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SHIP-001", result.ShipmentNo);
        Assert.Equal("DRAFT", result.Status);
        Assert.Single(result.Lines);
        Assert.Equal(50m, result.Lines[0].Qty);
    }

    [Fact]
    public async Task Test02_Ship_StatusChangesToShipped()
    {
        // Arrange
        var orderId = await CreateConfirmedSalesOrderAsync();
        var orderLine = await _context.SalesOrderLines.FirstAsync(l => l.SalesOrderId == orderId);

        var shipment = await _shipmentService.CreateDraftAsync(new CreateShipmentDto(
            "SHIP-002",
            orderId,
            _branchId,
            _warehouseId,
            DateTime.Today,
            null,
            new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, 50m, null) }
        ));

        // Act
        var result = await _shipmentService.ShipAsync(shipment.Id);

        // Assert
        Assert.Equal("SHIPPED", result.Status);
    }

    [Fact]
    public async Task Test03_Ship_ReservedQtyDecreases()
    {
        // Arrange
        var orderId = await CreateConfirmedSalesOrderAsync(100m);
        var orderLine = await _context.SalesOrderLines.FirstAsync(l => l.SalesOrderId == orderId);
        var reservedBefore = orderLine.ReservedQty;

        var shipment = await _shipmentService.CreateDraftAsync(new CreateShipmentDto(
            "SHIP-003",
            orderId,
            _branchId,
            _warehouseId,
            DateTime.Today,
            null,
            new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, 50m, null) }
        ));

        // Act
        await _shipmentService.ShipAsync(shipment.Id);

        // Assert
        var orderLineAfter = await _context.SalesOrderLines.FirstAsync(l => l.Id == orderLine.Id);
        Assert.Equal(reservedBefore - 50m, orderLineAfter.ReservedQty);
    }

    [Fact]
    public async Task Test04_Ship_OnHandDecreases()
    {
        // Arrange
        var orderId = await CreateConfirmedSalesOrderAsync(100m);
        var orderLine = await _context.SalesOrderLines.FirstAsync(l => l.SalesOrderId == orderId);
        var balanceBefore = await _stockService.GetBalanceAsync(_warehouseId, _variantId);

        var shipment = await _shipmentService.CreateDraftAsync(new CreateShipmentDto(
            "SHIP-004",
            orderId,
            _branchId,
            _warehouseId,
            DateTime.Today,
            null,
            new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, 50m, null) }
        ));

        // Act
        await _shipmentService.ShipAsync(shipment.Id);

        // Assert
        var balanceAfter = await _stockService.GetBalanceAsync(_warehouseId, _variantId);
        Assert.NotNull(balanceBefore);
        Assert.NotNull(balanceAfter);
        Assert.Equal(balanceBefore.OnHand - 50m, balanceAfter.OnHand);
    }

    [Fact]
    public async Task Test05_Ship_ShippedQtyIncreases()
    {
        // Arrange
        var orderId = await CreateConfirmedSalesOrderAsync(100m);
        var orderLine = await _context.SalesOrderLines.FirstAsync(l => l.SalesOrderId == orderId);

        var shipment = await _shipmentService.CreateDraftAsync(new CreateShipmentDto(
            "SHIP-005",
            orderId,
            _branchId,
            _warehouseId,
            DateTime.Today,
            null,
            new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, 50m, null) }
        ));

        // Act
        await _shipmentService.ShipAsync(shipment.Id);

        // Assert
        var orderLineAfter = await _context.SalesOrderLines.FirstAsync(l => l.Id == orderLine.Id);
        Assert.Equal(50m, orderLineAfter.ShippedQty);
    }

    [Fact]
    public async Task Test06_PartialShipment_Works()
    {
        // Arrange
        var orderId = await CreateConfirmedSalesOrderAsync(100m);
        var orderLine = await _context.SalesOrderLines.FirstAsync(l => l.SalesOrderId == orderId);

        // First shipment: 30
        var shipment1 = await _shipmentService.CreateDraftAsync(new CreateShipmentDto(
            "SHIP-006A",
            orderId,
            _branchId,
            _warehouseId,
            DateTime.Today,
            null,
            new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, 30m, null) }
        ));
        await _shipmentService.ShipAsync(shipment1.Id);

        // Second shipment: 40
        var shipment2 = await _shipmentService.CreateDraftAsync(new CreateShipmentDto(
            "SHIP-006B",
            orderId,
            _branchId,
            _warehouseId,
            DateTime.Today,
            null,
            new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, 40m, null) }
        ));

        // Act
        await _shipmentService.ShipAsync(shipment2.Id);

        // Assert
        var orderLineAfter = await _context.SalesOrderLines.FirstAsync(l => l.Id == orderLine.Id);
        Assert.Equal(70m, orderLineAfter.ShippedQty);
        Assert.Equal(30m, orderLineAfter.ReservedQty); // 100 - 70
    }

    [Fact]
    public async Task Test07_OverShip_ThrowsError()
    {
        // Arrange
        var orderId = await CreateConfirmedSalesOrderAsync(100m);
        var orderLine = await _context.SalesOrderLines.FirstAsync(l => l.SalesOrderId == orderId);

        var shipment = await _shipmentService.CreateDraftAsync(new CreateShipmentDto(
            "SHIP-007",
            orderId,
            _branchId,
            _warehouseId,
            DateTime.Today,
            null,
            new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, 150m, null) } // More than reserved
        ));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _shipmentService.ShipAsync(shipment.Id));
        Assert.Contains("Insufficient reserved quantity", ex.Message);
    }

    [Fact]
    public async Task Test08_Ship_Idempotent()
    {
        // Arrange
        var orderId = await CreateConfirmedSalesOrderAsync(100m);
        var orderLine = await _context.SalesOrderLines.FirstAsync(l => l.SalesOrderId == orderId);

        var shipment = await _shipmentService.CreateDraftAsync(new CreateShipmentDto(
            "SHIP-008",
            orderId,
            _branchId,
            _warehouseId,
            DateTime.Today,
            null,
            new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, 50m, null) }
        ));

        await _shipmentService.ShipAsync(shipment.Id);
        var balanceAfterFirst = await _stockService.GetBalanceAsync(_warehouseId, _variantId);

        // Act - ship again
        await _shipmentService.ShipAsync(shipment.Id);

        // Assert - stock should not change
        var balanceAfterSecond = await _stockService.GetBalanceAsync(_warehouseId, _variantId);
        Assert.NotNull(balanceAfterFirst);
        Assert.NotNull(balanceAfterSecond);
        Assert.Equal(balanceAfterFirst.OnHand, balanceAfterSecond.OnHand);
        Assert.Equal(balanceAfterFirst.Reserved, balanceAfterSecond.Reserved);
    }

    [Fact]
    public async Task Test09_Cancel_OnlyDraft()
    {
        // Arrange
        var orderId = await CreateConfirmedSalesOrderAsync();
        var orderLine = await _context.SalesOrderLines.FirstAsync(l => l.SalesOrderId == orderId);

        var shipment = await _shipmentService.CreateDraftAsync(new CreateShipmentDto(
            "SHIP-009",
            orderId,
            _branchId,
            _warehouseId,
            DateTime.Today,
            null,
            new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, 50m, null) }
        ));

        // Act
        var result = await _shipmentService.CancelAsync(shipment.Id);

        // Assert
        Assert.Equal("CANCELLED", result.Status);
    }

    [Fact]
    public async Task Test10_ShippedCannotBeCancelled()
    {
        // Arrange
        var orderId = await CreateConfirmedSalesOrderAsync();
        var orderLine = await _context.SalesOrderLines.FirstAsync(l => l.SalesOrderId == orderId);

        var shipment = await _shipmentService.CreateDraftAsync(new CreateShipmentDto(
            "SHIP-010",
            orderId,
            _branchId,
            _warehouseId,
            DateTime.Today,
            null,
            new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, 50m, null) }
        ));

        await _shipmentService.ShipAsync(shipment.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _shipmentService.CancelAsync(shipment.Id));
        Assert.Contains("SHIPPED shipments cannot be cancelled", ex.Message);
    }

    [Fact]
    public async Task Test11_FullyShippedOrder_BecomesCompleted()
    {
        // Arrange
        var orderId = await CreateConfirmedSalesOrderAsync(100m);
        var orderLine = await _context.SalesOrderLines.FirstAsync(l => l.SalesOrderId == orderId);

        var shipment = await _shipmentService.CreateDraftAsync(new CreateShipmentDto(
            "SHIP-011",
            orderId,
            _branchId,
            _warehouseId,
            DateTime.Today,
            null,
            new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, 100m, null) } // Full qty
        ));

        // Act
        await _shipmentService.ShipAsync(shipment.Id);

        // Assert
        var order = await _context.SalesOrders.FirstAsync(o => o.Id == orderId);
        Assert.Equal("COMPLETED", order.Status);
    }

    [Fact]
    public async Task Test12_TenantIsolation_CannotAccessOtherTenantShipments()
    {
        // Arrange - create shipment for this tenant
        var orderId = await CreateConfirmedSalesOrderAsync();
        var orderLine = await _context.SalesOrderLines.FirstAsync(l => l.SalesOrderId == orderId);

        await _shipmentService.CreateDraftAsync(new CreateShipmentDto(
            "SHIP-012",
            orderId,
            _branchId,
            _warehouseId,
            DateTime.Today,
            null,
            new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, 50m, null) }
        ));

        // Create a different tenant context
        var otherTenantId = Guid.NewGuid();
        var mockOtherTenant = new Mock<ITenantContext>();
        mockOtherTenant.Setup(x => x.TenantId).Returns(otherTenantId);
        mockOtherTenant.Setup(x => x.UserId).Returns(_userId);
        mockOtherTenant.Setup(x => x.IsBypassEnabled).Returns(false);  // Normal query filter (tenant isolation)

        var otherContext = _dbFactory.CreateContext(mockOtherTenant.Object);
        var otherStockService = new StockService(otherContext, mockOtherTenant.Object);
        var otherShipmentService = new ShipmentService(otherContext, mockOtherTenant.Object, otherStockService);

        // Act
        var (items, total) = await otherShipmentService.SearchAsync(1, 20, null, null, null, null);

        // Assert
        Assert.Empty(items);
        Assert.Equal(0, total);
    }

    [Fact]
    public async Task Test13_UniqueShipmentNo_WithinTenant()
    {
        // Arrange
        var orderId = await CreateConfirmedSalesOrderAsync();
        var orderLine = await _context.SalesOrderLines.FirstAsync(l => l.SalesOrderId == orderId);

        await _shipmentService.CreateDraftAsync(new CreateShipmentDto(
            "SHIP-013",
            orderId,
            _branchId,
            _warehouseId,
            DateTime.Today,
            null,
            new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, 50m, null) }
        ));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _shipmentService.CreateDraftAsync(new CreateShipmentDto(
                "SHIP-013", // Duplicate
                orderId,
                _branchId,
                _warehouseId,
                DateTime.Today,
                null,
                new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, 30m, null) }
            ))
        );
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task Test14_LedgerEntries_HaveCorrectReference()
    {
        // Arrange
        var orderId = await CreateConfirmedSalesOrderAsync(100m);
        var orderLine = await _context.SalesOrderLines.FirstAsync(l => l.SalesOrderId == orderId);

        var shipment = await _shipmentService.CreateDraftAsync(new CreateShipmentDto(
            "SHIP-014",
            orderId,
            _branchId,
            _warehouseId,
            DateTime.Today,
            null,
            new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, 50m, null) }
        ));

        // Act
        await _shipmentService.ShipAsync(shipment.Id);

        // Assert
        var ledgerEntries = await _context.StockLedgerEntries
            .Where(e => e.ReferenceType == "Shipment" && e.ReferenceId == shipment.Id)
            .ToListAsync();

        Assert.Equal(2, ledgerEntries.Count); // One for release reservation, one for issue stock
        Assert.All(ledgerEntries, e => Assert.Equal("Shipment", e.ReferenceType));
        Assert.All(ledgerEntries, e => Assert.Equal(shipment.Id, e.ReferenceId));
    }

    public void Dispose()
    {
        _context?.Dispose();
        _dbFactory?.Dispose();
    }
}
