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

public class SalesOrderModuleTests
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

    private static async Task<(Guid partyId, Guid branchId, Guid warehouseId, Guid variantId, Guid priceListId)> 
        SetupTestDataAsync(ErpDbContext context, Guid tenantId)
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

        // Create party (customer)
        var party = new Party
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = "CUST001",
            Name = "Test Customer",
            Type = "CUSTOMER",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        context.Parties.Add(party);

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
            VatRate = 18.0m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        context.ProductVariants.Add(variant);

        // Create price list
        var priceList = new PriceList
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = "DEFAULT",
            Name = "Default Prices",
            Currency = "USD",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        context.PriceLists.Add(priceList);

        // Create price item
        var priceItem = new PriceListItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PriceListId = priceList.Id,
            VariantId = variant.Id,
            UnitPrice = 100.00m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        context.PriceListItems.Add(priceItem);

        // Initial stock
        var stockBalance = new StockBalance
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WarehouseId = warehouse.Id,
            VariantId = variant.Id,
            OnHand = 100.0m,
            Reserved = 0.0m,
            UpdatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        context.StockBalances.Add(stockBalance);

        await context.SaveChangesAsync();

        return (party.Id, branch.Id, warehouse.Id, variant.Id, priceList.Id);
    }

    [Fact]
    public async Task CreateDraft_Success()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var context = CreateTestDbContext(tenantId);
        var (partyId, branchId, warehouseId, variantId, priceListId) = await SetupTestDataAsync(context, tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var stockService = new StockService(context, mockTenantContext.Object);
        var salesOrderService = new SalesOrderService(context, mockTenantContext.Object, stockService);

        var dto = new CreateSalesOrderDto(
            OrderNo: "SO-001",
            PartyId: partyId,
            BranchId: branchId,
            WarehouseId: warehouseId,
            PriceListId: priceListId,
            OrderDate: DateTime.UtcNow,
            Note: "Test order",
            Lines: new List<CreateSalesOrderLineDto>
            {
                new(VariantId: variantId, Qty: 10.0m, UnitPrice: 100.00m, VatRate: 18.0m, Note: "Line 1")
            }
        );

        // Act
        var result = await salesOrderService.CreateDraftAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SO-001", result.OrderNo);
        Assert.Equal("DRAFT", result.Status);
        Assert.Single(result.Lines);
        Assert.Equal(10.0m, result.Lines[0].Qty);
        Assert.Equal(0.0m, result.Lines[0].ReservedQty);
    }

    [Fact]
    public async Task Confirm_ChangesStatusToConfirmed()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var context = CreateTestDbContext(tenantId);
        var (partyId, branchId, warehouseId, variantId, priceListId) = await SetupTestDataAsync(context, tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var stockService = new StockService(context, mockTenantContext.Object);
        var salesOrderService = new SalesOrderService(context, mockTenantContext.Object, stockService);

        var createDto = new CreateSalesOrderDto(
            OrderNo: "SO-002",
            PartyId: partyId,
            BranchId: branchId,
            WarehouseId: warehouseId,
            PriceListId: priceListId,
            OrderDate: DateTime.UtcNow,
            Note: null,
            Lines: new List<CreateSalesOrderLineDto>
            {
                new(VariantId: variantId, Qty: 5.0m, UnitPrice: 100.00m, VatRate: 18.0m, Note: null)
            }
        );

        var order = await salesOrderService.CreateDraftAsync(createDto);

        // Act
        var result = await salesOrderService.ConfirmAsync(order.Id);

        // Assert
        Assert.Equal("CONFIRMED", result.Status);
    }

    [Fact]
    public async Task Confirm_ReservesStock()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var context = CreateTestDbContext(tenantId);
        var (partyId, branchId, warehouseId, variantId, priceListId) = await SetupTestDataAsync(context, tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var stockService = new StockService(context, mockTenantContext.Object);
        var salesOrderService = new SalesOrderService(context, mockTenantContext.Object, stockService);

        var createDto = new CreateSalesOrderDto(
            OrderNo: "SO-003",
            PartyId: partyId,
            BranchId: branchId,
            WarehouseId: warehouseId,
            PriceListId: null,
            OrderDate: DateTime.UtcNow,
            Note: null,
            Lines: new List<CreateSalesOrderLineDto>
            {
                new(VariantId: variantId, Qty: 8.0m, UnitPrice: null, VatRate: null, Note: null)
            }
        );

        var order = await salesOrderService.CreateDraftAsync(createDto);

        // Act
        await salesOrderService.ConfirmAsync(order.Id);

        // Assert
        var balance = await stockService.GetBalanceAsync(warehouseId, variantId);
        Assert.NotNull(balance);
        Assert.Equal(8.0m, balance.Reserved);
        Assert.Equal(92.0m, balance.Available);
    }

    [Fact]
    public async Task Confirm_Idempotent_NoDoubleReservation()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var context = CreateTestDbContext(tenantId);
        var (partyId, branchId, warehouseId, variantId, priceListId) = await SetupTestDataAsync(context, tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var stockService = new StockService(context, mockTenantContext.Object);
        var salesOrderService = new SalesOrderService(context, mockTenantContext.Object, stockService);

        var createDto = new CreateSalesOrderDto(
            OrderNo: "SO-004",
            PartyId: partyId,
            BranchId: branchId,
            WarehouseId: warehouseId,
            PriceListId: priceListId,
            OrderDate: DateTime.UtcNow,
            Note: null,
            Lines: new List<CreateSalesOrderLineDto>
            {
                new(VariantId: variantId, Qty: 6.0m, UnitPrice: 100.00m, VatRate: 18.0m, Note: null)
            }
        );

        var order = await salesOrderService.CreateDraftAsync(createDto);

        // Act: Confirm twice
        await salesOrderService.ConfirmAsync(order.Id);
        await salesOrderService.ConfirmAsync(order.Id); // Second call should be no-op

        // Assert
        var balance = await stockService.GetBalanceAsync(warehouseId, variantId);
        Assert.NotNull(balance);
        Assert.Equal(6.0m, balance.Reserved); // Should still be 6, not 12
    }

    [Fact]
    public async Task Cancel_ChangesStatusToCancelled()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var context = CreateTestDbContext(tenantId);
        var (partyId, branchId, warehouseId, variantId, priceListId) = await SetupTestDataAsync(context, tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var stockService = new StockService(context, mockTenantContext.Object);
        var salesOrderService = new SalesOrderService(context, mockTenantContext.Object, stockService);

        var createDto = new CreateSalesOrderDto(
            OrderNo: "SO-005",
            PartyId: partyId,
            BranchId: branchId,
            WarehouseId: warehouseId,
            PriceListId: priceListId,
            OrderDate: DateTime.UtcNow,
            Note: null,
            Lines: new List<CreateSalesOrderLineDto>
            {
                new(VariantId: variantId, Qty: 3.0m, UnitPrice: 100.00m, VatRate: 18.0m, Note: null)
            }
        );

        var order = await salesOrderService.CreateDraftAsync(createDto);
        await salesOrderService.ConfirmAsync(order.Id);

        // Act
        var result = await salesOrderService.CancelAsync(order.Id);

        // Assert
        Assert.Equal("CANCELLED", result.Status);
    }

    [Fact]
    public async Task Cancel_ReleasesReservation()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var context = CreateTestDbContext(tenantId);
        var (partyId, branchId, warehouseId, variantId, priceListId) = await SetupTestDataAsync(context, tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var stockService = new StockService(context, mockTenantContext.Object);
        var salesOrderService = new SalesOrderService(context, mockTenantContext.Object, stockService);

        var createDto = new CreateSalesOrderDto(
            OrderNo: "SO-006",
            PartyId: partyId,
            BranchId: branchId,
            WarehouseId: warehouseId,
            PriceListId: priceListId,
            OrderDate: DateTime.UtcNow,
            Note: null,
            Lines: new List<CreateSalesOrderLineDto>
            {
                new(VariantId: variantId, Qty: 7.0m, UnitPrice: 100.00m, VatRate: 18.0m, Note: null)
            }
        );

        var order = await salesOrderService.CreateDraftAsync(createDto);
        await salesOrderService.ConfirmAsync(order.Id);

        // Act
        await salesOrderService.CancelAsync(order.Id);

        // Assert
        var balance = await stockService.GetBalanceAsync(warehouseId, variantId);
        Assert.NotNull(balance);
        Assert.Equal(0.0m, balance.Reserved);
        Assert.Equal(100.0m, balance.Available);
    }

    [Fact]
    public async Task CancelAfterCancel_NoError()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var context = CreateTestDbContext(tenantId);
        var (partyId, branchId, warehouseId, variantId, priceListId) = await SetupTestDataAsync(context, tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var stockService = new StockService(context, mockTenantContext.Object);
        var salesOrderService = new SalesOrderService(context, mockTenantContext.Object, stockService);

        var createDto = new CreateSalesOrderDto(
            OrderNo: "SO-007",
            PartyId: partyId,
            BranchId: branchId,
            WarehouseId: warehouseId,
            PriceListId: priceListId,
            OrderDate: DateTime.UtcNow,
            Note: null,
            Lines: new List<CreateSalesOrderLineDto>
            {
                new(VariantId: variantId, Qty: 2.0m, UnitPrice: 100.00m, VatRate: 18.0m, Note: null)
            }
        );

        var order = await salesOrderService.CreateDraftAsync(createDto);

        // Act: Cancel twice
        await salesOrderService.CancelAsync(order.Id);
        var result = await salesOrderService.CancelAsync(order.Id); // Should be idempotent

        // Assert
        Assert.Equal("CANCELLED", result.Status);
    }

    [Fact]
    public async Task InsufficientStock_ConfirmFails_StatusRemainsDraft()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var context = CreateTestDbContext(tenantId);
        var (partyId, branchId, warehouseId, variantId, priceListId) = await SetupTestDataAsync(context, tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var stockService = new StockService(context, mockTenantContext.Object);
        var salesOrderService = new SalesOrderService(context, mockTenantContext.Object, stockService);

        // Try to order more than available (100)
        var createDto = new CreateSalesOrderDto(
            OrderNo: "SO-008",
            PartyId: partyId,
            BranchId: branchId,
            WarehouseId: warehouseId,
            PriceListId: priceListId,
            OrderDate: DateTime.UtcNow,
            Note: null,
            Lines: new List<CreateSalesOrderLineDto>
            {
                new(VariantId: variantId, Qty: 150.0m, UnitPrice: 100.00m, VatRate: 18.0m, Note: null)
            }
        );

        var order = await salesOrderService.CreateDraftAsync(createDto);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await salesOrderService.ConfirmAsync(order.Id);
        });

        // Verify status is still DRAFT
        var reloadedOrder = await salesOrderService.GetByIdAsync(order.Id);
        Assert.NotNull(reloadedOrder);
        Assert.Equal("DRAFT", reloadedOrder.Status);
    }

    [Fact]
    public async Task TenantIsolation_DifferentTenantsCannotSeeEachOther()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();

        await using var context1 = CreateTestDbContext(tenant1);
        await using var context2 = CreateTestDbContext(tenant2);

        var (partyId1, branchId1, warehouseId1, variantId1, priceListId1) = await SetupTestDataAsync(context1, tenant1);
        var (partyId2, branchId2, warehouseId2, variantId2, priceListId2) = await SetupTestDataAsync(context2, tenant2);

        var mockTenant1 = new Mock<ITenantContext>();
        mockTenant1.Setup(x => x.TenantId).Returns(tenant1);
        mockTenant1.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var mockTenant2 = new Mock<ITenantContext>();
        mockTenant2.Setup(x => x.TenantId).Returns(tenant2);
        mockTenant2.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var stockService1 = new StockService(context1, mockTenant1.Object);
        var stockService2 = new StockService(context2, mockTenant2.Object);

        var service1 = new SalesOrderService(context1, mockTenant1.Object, stockService1);
        var service2 = new SalesOrderService(context2, mockTenant2.Object, stockService2);

        // Act: Create order in tenant1
        var order1 = await service1.CreateDraftAsync(new CreateSalesOrderDto(
            OrderNo: "SO-T1",
            PartyId: partyId1,
            BranchId: branchId1,
            WarehouseId: warehouseId1,
            PriceListId: priceListId1,
            OrderDate: DateTime.UtcNow,
            Note: null,
            Lines: new List<CreateSalesOrderLineDto>
            {
                new(VariantId: variantId1, Qty: 5.0m, UnitPrice: 100.00m, VatRate: 18.0m, Note: null)
            }
        ));

        // Assert: Tenant2 cannot see tenant1's order
        var result = await service2.GetByIdAsync(order1.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task UniqueOrderNo_EnforcedPerTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var context = CreateTestDbContext(tenantId);
        var (partyId, branchId, warehouseId, variantId, priceListId) = await SetupTestDataAsync(context, tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var stockService = new StockService(context, mockTenantContext.Object);
        var salesOrderService = new SalesOrderService(context, mockTenantContext.Object, stockService);

        var dto1 = new CreateSalesOrderDto(
            OrderNo: "SO-UNIQUE",
            PartyId: partyId,
            BranchId: branchId,
            WarehouseId: warehouseId,
            PriceListId: priceListId,
            OrderDate: DateTime.UtcNow,
            Note: null,
            Lines: new List<CreateSalesOrderLineDto>
            {
                new(VariantId: variantId, Qty: 1.0m, UnitPrice: 100.00m, VatRate: 18.0m, Note: null)
            }
        );

        await salesOrderService.CreateDraftAsync(dto1);

        // Act & Assert: Try to create with same OrderNo
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await salesOrderService.CreateDraftAsync(dto1);
        });
    }

    [Fact]
    public async Task UniqueVariant_EnforcedPerOrder()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var context = CreateTestDbContext(tenantId);
        var (partyId, branchId, warehouseId, variantId, priceListId) = await SetupTestDataAsync(context, tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var stockService = new StockService(context, mockTenantContext.Object);
        var salesOrderService = new SalesOrderService(context, mockTenantContext.Object, stockService);

        var dto = new CreateSalesOrderDto(
            OrderNo: "SO-DUP",
            PartyId: partyId,
            BranchId: branchId,
            WarehouseId: warehouseId,
            PriceListId: priceListId,
            OrderDate: DateTime.UtcNow,
            Note: null,
            Lines: new List<CreateSalesOrderLineDto>
            {
                new(VariantId: variantId, Qty: 1.0m, UnitPrice: 100.00m, VatRate: 18.0m, Note: null),
                new(VariantId: variantId, Qty: 2.0m, UnitPrice: 100.00m, VatRate: 18.0m, Note: null) // Duplicate
            }
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await salesOrderService.CreateDraftAsync(dto);
        });
    }

    [Fact]
    public async Task UpdateDraft_OnlyDraftCanBeUpdated()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var context = CreateTestDbContext(tenantId);
        var (partyId, branchId, warehouseId, variantId, priceListId) = await SetupTestDataAsync(context, tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var stockService = new StockService(context, mockTenantContext.Object);
        var salesOrderService = new SalesOrderService(context, mockTenantContext.Object, stockService);

        var createDto = new CreateSalesOrderDto(
            OrderNo: "SO-UPDATE",
            PartyId: partyId,
            BranchId: branchId,
            WarehouseId: warehouseId,
            PriceListId: priceListId,
            OrderDate: DateTime.UtcNow,
            Note: null,
            Lines: new List<CreateSalesOrderLineDto>
            {
                new(VariantId: variantId, Qty: 5.0m, UnitPrice: 100.00m, VatRate: 18.0m, Note: null)
            }
        );

        var order = await salesOrderService.CreateDraftAsync(createDto);
        await salesOrderService.ConfirmAsync(order.Id);

        var updateDto = new UpdateSalesOrderDto(
            OrderNo: "SO-UPDATE",
            PartyId: partyId,
            BranchId: branchId,
            WarehouseId: warehouseId,
            PriceListId: priceListId,
            OrderDate: DateTime.UtcNow,
            Note: "Updated",
            Lines: new List<UpdateSalesOrderLineDto>
            {
                new(VariantId: variantId, Qty: 10.0m, UnitPrice: 100.00m, VatRate: 18.0m, Note: null)
            }
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await salesOrderService.UpdateDraftAsync(order.Id, updateDto);
        });

        Assert.Contains("Only DRAFT orders can be updated", ex.Message);
    }

    [Fact]
    public async Task Search_FiltersByStatus()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var context = CreateTestDbContext(tenantId);
        var (partyId, branchId, warehouseId, variantId, priceListId) = await SetupTestDataAsync(context, tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var stockService = new StockService(context, mockTenantContext.Object);
        var salesOrderService = new SalesOrderService(context, mockTenantContext.Object, stockService);

        // Create DRAFT and CONFIRMED orders
        var draft = await salesOrderService.CreateDraftAsync(new CreateSalesOrderDto(
            OrderNo: "SO-DRAFT",
            PartyId: partyId,
            BranchId: branchId,
            WarehouseId: warehouseId,
            PriceListId: priceListId,
            OrderDate: DateTime.UtcNow,
            Note: null,
            Lines: new List<CreateSalesOrderLineDto>
            {
                new(VariantId: variantId, Qty: 1.0m, UnitPrice: 100.00m, VatRate: 18.0m, Note: null)
            }
        ));

        var confirmed = await salesOrderService.CreateDraftAsync(new CreateSalesOrderDto(
            OrderNo: "SO-CONFIRMED",
            PartyId: partyId,
            BranchId: branchId,
            WarehouseId: warehouseId,
            PriceListId: priceListId,
            OrderDate: DateTime.UtcNow,
            Note: null,
            Lines: new List<CreateSalesOrderLineDto>
            {
                new(VariantId: variantId, Qty: 1.0m, UnitPrice: 100.00m, VatRate: 18.0m, Note: null)
            }
        ));
        await salesOrderService.ConfirmAsync(confirmed.Id);

        // Act
        var searchDraft = await salesOrderService.SearchAsync(new SalesOrderSearchDto(Status: "DRAFT"));
        var searchConfirmed = await salesOrderService.SearchAsync(new SalesOrderSearchDto(Status: "CONFIRMED"));

        // Assert
        Assert.Single(searchDraft.Items);
        Assert.Equal("DRAFT", searchDraft.Items[0].Status);

        Assert.Single(searchConfirmed.Items);
        Assert.Equal("CONFIRMED", searchConfirmed.Items[0].Status);
    }

    [Fact]
    public async Task PricingIntegration_FetchesFromPriceList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var context = CreateTestDbContext(tenantId);
        var (partyId, branchId, warehouseId, variantId, priceListId) = await SetupTestDataAsync(context, tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var stockService = new StockService(context, mockTenantContext.Object);
        var salesOrderService = new SalesOrderService(context, mockTenantContext.Object, stockService);

        // Create order without providing UnitPrice (should fetch from price list)
        var dto = new CreateSalesOrderDto(
            OrderNo: "SO-PRICING",
            PartyId: partyId,
            BranchId: branchId,
            WarehouseId: warehouseId,
            PriceListId: null, // Use default price list
            OrderDate: DateTime.UtcNow,
            Note: null,
            Lines: new List<CreateSalesOrderLineDto>
            {
                new(VariantId: variantId, Qty: 1.0m, UnitPrice: null, VatRate: null, Note: null)
            }
        );

        // Act
        var result = await salesOrderService.CreateDraftAsync(dto);

        // Assert
        Assert.Equal(100.00m, result.Lines[0].UnitPrice); // From price list
        Assert.Equal(18.0m, result.Lines[0].VatRate); // From variant
    }
}
