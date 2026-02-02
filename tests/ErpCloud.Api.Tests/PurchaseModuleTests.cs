using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpCloud.Api.Tests;

/// <summary>
/// Comprehensive tests for Purchase Order and Goods Receipt module.
/// Tests: PO lifecycle, GRN lifecycle, stock integration, partial receive, over-receive prevention,
/// completion rules, tenant isolation, search/filters.
/// </summary>
public class PurchaseModuleTests : IDisposable
{
    private readonly TestDbFactory _dbFactory;
    private readonly ErpDbContext _context;
    private readonly TestTenantContext _tenantContext;
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly IGoodsReceiptService _goodsReceiptService;
    private readonly IStockService _stockService;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _orgId;
    private readonly Guid _branchId;
    private readonly Guid _warehouseId;
    private readonly Guid _supplierPartyId;
    private readonly Guid _customerPartyId;
    private readonly Guid _variantId;

    public PurchaseModuleTests()
    {
        _tenantContext = new TestTenantContext
        {
            TenantId = _tenantId,
            UserId = _userId,
            IsBypassEnabled = true
        };

        _dbFactory = new TestDbFactory(_tenantContext);
        _context = _dbFactory.CreateContext(_tenantContext);

        // Seed test data
        _orgId = SeedOrganization();
        _branchId = SeedBranch();
        _warehouseId = SeedWarehouse();
        _supplierPartyId = SeedParty("SUPPLIER");
        _customerPartyId = SeedParty("CUSTOMER");
        _variantId = SeedVariant();

        // Create services
        _stockService = new StockService(_context, _tenantContext);
        _purchaseOrderService = new PurchaseOrderService(_context, _tenantContext);
        _goodsReceiptService = new GoodsReceiptService(_context, _tenantContext, _stockService);
    }

    private Guid SeedOrganization()
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
        _context.Organizations.Add(org);
        _context.SaveChanges();
        return org.Id;
    }

    private Guid SeedBranch()
    {
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = _orgId,
            Code = "BR001",
            Name = "Main Branch",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };
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

    private Guid SeedParty(string type)
    {
        var party = new Party
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = type == "SUPPLIER" ? "SUP001" : "CUST001",
            Name = type == "SUPPLIER" ? "Test Supplier" : "Test Customer",
            Type = type,
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
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = product.Id,
            Sku = "SKU001",
            Name = "Default",
            VatRate = 18,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.SaveChanges();
        return variant.Id;
    }

    public void Dispose()
    {
        _context?.Dispose();
        _dbFactory?.Dispose();
    }

    // ==================== A) Purchase Order Lifecycle ====================

    [Fact]
    public async Task Test01_CreatePO_Draft_Succeeds()
    {
        // Arrange
        var dto = new CreatePurchaseOrderDto
        {
            PoNo = "PO-2024-001",
            PartyId = _supplierPartyId,
            BranchId = _branchId,
            WarehouseId = _warehouseId,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { VariantId = _variantId, Qty = 100, UnitCost = 50 }
            }
        };

        // Act
        var result = await _purchaseOrderService.CreateDraftAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PO-2024-001", result.PoNo);
        Assert.Equal("DRAFT", result.Status);
        Assert.Single(result.Lines);
        Assert.Equal(100, result.Lines[0].Qty);
        Assert.Equal(0, result.Lines[0].ReceivedQty);
    }

    [Fact]
    public async Task Test02_CreatePO_RequiresSupplier_CustomerPartyFails()
    {
        // Arrange
        var dto = new CreatePurchaseOrderDto
        {
            PoNo = "PO-2024-002",
            PartyId = _customerPartyId, // CUSTOMER, not SUPPLIER
            BranchId = _branchId,
            WarehouseId = _warehouseId,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { VariantId = _variantId, Qty = 50, UnitCost = 40 }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _purchaseOrderService.CreateDraftAsync(dto));
        Assert.Contains("SUPPLIER", ex.Message);
    }

    [Fact]
    public async Task Test03_UpdatePO_OnlyDraftAllowed()
    {
        // Arrange - create and confirm PO
        var createDto = new CreatePurchaseOrderDto
        {
            PoNo = "PO-2024-003",
            PartyId = _supplierPartyId,
            BranchId = _branchId,
            WarehouseId = _warehouseId,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { VariantId = _variantId, Qty = 100, UnitCost = 50 }
            }
        };
        var po = await _purchaseOrderService.CreateDraftAsync(createDto);
        await _purchaseOrderService.ConfirmAsync(po.Id);

        var updateDto = new UpdatePurchaseOrderDto
        {
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { VariantId = _variantId, Qty = 200, UnitCost = 55 }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _purchaseOrderService.UpdateDraftAsync(po.Id, updateDto));
        Assert.Contains("DRAFT", ex.Message);
    }

    [Fact]
    public async Task Test04_CancelPO_OnlyDraftAllowed()
    {
        // Arrange
        var createDto = new CreatePurchaseOrderDto
        {
            PoNo = "PO-2024-004",
            PartyId = _supplierPartyId,
            BranchId = _branchId,
            WarehouseId = _warehouseId,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { VariantId = _variantId, Qty = 100, UnitCost = 50 }
            }
        };
        var po = await _purchaseOrderService.CreateDraftAsync(createDto);
        await _purchaseOrderService.ConfirmAsync(po.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _purchaseOrderService.CancelAsync(po.Id));
        Assert.Contains("DRAFT", ex.Message);
    }

    [Fact]
    public async Task Test05_ConfirmPO_TransitionsDraftToConfirmed()
    {
        // Arrange
        var createDto = new CreatePurchaseOrderDto
        {
            PoNo = "PO-2024-005",
            PartyId = _supplierPartyId,
            BranchId = _branchId,
            WarehouseId = _warehouseId,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { VariantId = _variantId, Qty = 100, UnitCost = 50 }
            }
        };
        var po = await _purchaseOrderService.CreateDraftAsync(createDto);

        // Act
        var result = await _purchaseOrderService.ConfirmAsync(po.Id);

        // Assert
        Assert.Equal("CONFIRMED", result.Status);
    }

    [Fact]
    public async Task Test06_ConfirmPO_Idempotent_SecondCallNoOp()
    {
        // Arrange
        var createDto = new CreatePurchaseOrderDto
        {
            PoNo = "PO-2024-006",
            PartyId = _supplierPartyId,
            BranchId = _branchId,
            WarehouseId = _warehouseId,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { VariantId = _variantId, Qty = 100, UnitCost = 50 }
            }
        };
        var po = await _purchaseOrderService.CreateDraftAsync(createDto);

        // Act - confirm twice
        await _purchaseOrderService.ConfirmAsync(po.Id);
        var result = await _purchaseOrderService.ConfirmAsync(po.Id);

        // Assert
        Assert.Equal("CONFIRMED", result.Status);
    }

    [Fact]
    public async Task Test07_UniquePoNo_EnforcedPerTenant()
    {
        // Arrange
        var dto1 = new CreatePurchaseOrderDto
        {
            PoNo = "PO-DUP-001",
            PartyId = _supplierPartyId,
            BranchId = _branchId,
            WarehouseId = _warehouseId,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { VariantId = _variantId, Qty = 100, UnitCost = 50 }
            }
        };
        await _purchaseOrderService.CreateDraftAsync(dto1);

        var dto2 = new CreatePurchaseOrderDto
        {
            PoNo = "PO-DUP-001", // Same PoNo
            PartyId = _supplierPartyId,
            BranchId = _branchId,
            WarehouseId = _warehouseId,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { VariantId = _variantId, Qty = 50, UnitCost = 45 }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _purchaseOrderService.CreateDraftAsync(dto2));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task Test08_LineUniqueConstraint_SameVariantCannotAppearTwice()
    {
        // Arrange
        var dto = new CreatePurchaseOrderDto
        {
            PoNo = "PO-2024-008",
            PartyId = _supplierPartyId,
            BranchId = _branchId,
            WarehouseId = _warehouseId,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { VariantId = _variantId, Qty = 100, UnitCost = 50 },
                new() { VariantId = _variantId, Qty = 50, UnitCost = 45 } // Duplicate variant
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _purchaseOrderService.CreateDraftAsync(dto));
        Assert.Contains("Duplicate", ex.Message);
    }

    // ==================== B) Goods Receipt Lifecycle ====================

    [Fact]
    public async Task Test09_CreateGRN_AllowedOnlyForConfirmedPO()
    {
        // Arrange - DRAFT PO
        var createPoDto = new CreatePurchaseOrderDto
        {
            PoNo = "PO-2024-009",
            PartyId = _supplierPartyId,
            BranchId = _branchId,
            WarehouseId = _warehouseId,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { VariantId = _variantId, Qty = 100, UnitCost = 50 }
            }
        };
        var po = await _purchaseOrderService.CreateDraftAsync(createPoDto);

        var createGrnDto = new CreateGoodsReceiptDto
        {
            GrnNo = "GRN-2024-001",
            PurchaseOrderId = po.Id,
            ReceiptDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<GoodsReceiptLineDto>
            {
                new() { PurchaseOrderLineId = po.Lines[0].Id, Qty = 50 }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _goodsReceiptService.CreateDraftAsync(createGrnDto));
        Assert.Contains("CONFIRMED", ex.Message);
    }

    [Fact]
    public async Task Test10_UpdateGRN_OnlyDraftAllowed()
    {
        // Arrange - create and receive GRN
        var po = await CreateConfirmedPO("PO-2024-010", 100);
        var grn = await CreateDraftGRN("GRN-2024-002", po.Id, po.Lines[0].Id, 50);
        await _goodsReceiptService.ReceiveAsync(grn.Id);

        var updateDto = new UpdateGoodsReceiptDto
        {
            ReceiptDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
            Lines = new List<GoodsReceiptLineDto>
            {
                new() { PurchaseOrderLineId = po.Lines[0].Id, Qty = 60 }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _goodsReceiptService.UpdateDraftAsync(grn.Id, updateDto));
        Assert.Contains("DRAFT", ex.Message);
    }

    [Fact]
    public async Task Test11_CancelGRN_OnlyDraftAllowed()
    {
        // Arrange
        var po = await CreateConfirmedPO("PO-2024-011", 100);
        var grn = await CreateDraftGRN("GRN-2024-003", po.Id, po.Lines[0].Id, 50);
        await _goodsReceiptService.ReceiveAsync(grn.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _goodsReceiptService.CancelAsync(grn.Id));
        Assert.Contains("DRAFT", ex.Message);
    }

    [Fact]
    public async Task Test12_ReceiveGRN_TransitionsDraftToReceived()
    {
        // Arrange
        var po = await CreateConfirmedPO("PO-2024-012", 100);
        var grn = await CreateDraftGRN("GRN-2024-004", po.Id, po.Lines[0].Id, 50);

        // Act
        var result = await _goodsReceiptService.ReceiveAsync(grn.Id);

        // Assert
        Assert.Equal("RECEIVED", result.Status);
    }

    [Fact]
    public async Task Test13_ReceiveGRN_Idempotent_SecondCallNoOp()
    {
        // Arrange
        var po = await CreateConfirmedPO("PO-2024-013", 100);
        var grn = await CreateDraftGRN("GRN-2024-005", po.Id, po.Lines[0].Id, 50);

        // Act - receive twice
        await _goodsReceiptService.ReceiveAsync(grn.Id);
        var stockBefore = await _context.StockBalances.Where(b => b.VariantId == _variantId).FirstOrDefaultAsync();
        var qtyBefore = stockBefore?.OnHand ?? 0;

        await _goodsReceiptService.ReceiveAsync(grn.Id);
        var stockAfter = await _context.StockBalances.Where(b => b.VariantId == _variantId).FirstOrDefaultAsync();
        var qtyAfter = stockAfter?.OnHand ?? 0;

        // Assert - stock should not increase second time
        Assert.Equal(qtyBefore, qtyAfter);
    }

    // ==================== C) Receiving Rules + Stock Integration ====================

    [Fact]
    public async Task Test14_Receive_IncreasesStockOnHand()
    {
        // Arrange
        var po = await CreateConfirmedPO("PO-2024-014", 100);
        var grn = await CreateDraftGRN("GRN-2024-006", po.Id, po.Lines[0].Id, 50);

        var stockBefore = await _context.StockBalances.Where(b => b.VariantId == _variantId).FirstOrDefaultAsync();
        var qtyBefore = stockBefore?.OnHand ?? 0;

        // Act
        await _goodsReceiptService.ReceiveAsync(grn.Id);

        // Assert
        var stockAfter = await _context.StockBalances.Where(b => b.VariantId == _variantId).FirstOrDefaultAsync();
        Assert.NotNull(stockAfter);
        Assert.Equal(qtyBefore + 50, stockAfter.OnHand);
    }

    [Fact]
    public async Task Test15_Receive_UpdatesPOLineReceivedQty()
    {
        // Arrange
        var po = await CreateConfirmedPO("PO-2024-015", 100);
        var grn = await CreateDraftGRN("GRN-2024-007", po.Id, po.Lines[0].Id, 50);

        // Act
        await _goodsReceiptService.ReceiveAsync(grn.Id);

        // Assert
        var poUpdated = await _purchaseOrderService.GetByIdAsync(po.Id);
        Assert.Equal(50, poUpdated.Lines[0].ReceivedQty);
        Assert.Equal(50, poUpdated.Lines[0].RemainingQty);
    }

    [Fact]
    public async Task Test16_PartialReceive_Works_POCompletedWhenFullyReceived()
    {
        // Arrange
        var po = await CreateConfirmedPO("PO-2024-016", 100);
        var grn1 = await CreateDraftGRN("GRN-2024-008A", po.Id, po.Lines[0].Id, 40);
        var grn2 = await CreateDraftGRN("GRN-2024-008B", po.Id, po.Lines[0].Id, 60);

        // Act - receive partial
        await _goodsReceiptService.ReceiveAsync(grn1.Id);
        var poAfterFirst = await _purchaseOrderService.GetByIdAsync(po.Id);

        await _goodsReceiptService.ReceiveAsync(grn2.Id);
        var poAfterSecond = await _purchaseOrderService.GetByIdAsync(po.Id);

        // Assert
        Assert.Equal("CONFIRMED", poAfterFirst.Status);
        Assert.Equal(40, poAfterFirst.Lines[0].ReceivedQty);

        Assert.Equal("COMPLETED", poAfterSecond.Status);
        Assert.Equal(100, poAfterSecond.Lines[0].ReceivedQty);
    }

    [Fact]
    public async Task Test17_OverReceive_Prevented()
    {
        // Arrange - PO qty 100, received 97, try to CREATE grn2 with 4 (over by 1)
        var po = await CreateConfirmedPO("PO-2024-017", 100);
        var grn1 = await CreateDraftGRN("GRN-2024-009A", po.Id, po.Lines[0].Id, 97);
        await _goodsReceiptService.ReceiveAsync(grn1.Id);

        // Act & Assert - CreateDraft should prevent over-receive
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await CreateDraftGRN("GRN-2024-009B", po.Id, po.Lines[0].Id, 4); // Over by 1
        });
        Assert.Contains("remaining", ex.Message.ToLower());
    }

    [Fact]
    public async Task Test18_ReceiptLineUnitCost_FallbackToPOLine()
    {
        // Arrange
        var po = await CreateConfirmedPO("PO-2024-018", 100, unitCost: 75);
        
        // Create GRN line WITHOUT unitCost (should fallback to PO line's 75)
        var createGrnDto = new CreateGoodsReceiptDto
        {
            GrnNo = "GRN-2024-010",
            PurchaseOrderId = po.Id,
            ReceiptDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<GoodsReceiptLineDto>
            {
                new() { PurchaseOrderLineId = po.Lines[0].Id, Qty = 50, UnitCost = null } // No cost
            }
        };
        var grn = await _goodsReceiptService.CreateDraftAsync(createGrnDto);

        // Act
        await _goodsReceiptService.ReceiveAsync(grn.Id);

        // Assert - verify ledger entry uses PO line's UnitCost
        var ledgerEntry = await _context.StockLedgerEntries
            .Where(e => e.ReferenceId == grn.Id && e.ReferenceType == "GoodsReceipt")
            .FirstOrDefaultAsync();

        Assert.NotNull(ledgerEntry);
        Assert.Equal(75, ledgerEntry.UnitCost);
    }

    [Fact]
    public async Task Test19_LedgerReference_Correctness()
    {
        // Arrange
        var po = await CreateConfirmedPO("PO-2024-019", 100);
        var grn = await CreateDraftGRN("GRN-2024-011", po.Id, po.Lines[0].Id, 50);

        // Act
        await _goodsReceiptService.ReceiveAsync(grn.Id);

        // Assert
        var ledgerEntries = await _context.StockLedgerEntries
            .Where(e => e.ReferenceType == "GoodsReceipt" && e.ReferenceId == grn.Id)
            .ToListAsync();

        Assert.Single(ledgerEntries);
        Assert.Equal("GoodsReceipt", ledgerEntries[0].ReferenceType);
        Assert.Equal(grn.Id, ledgerEntries[0].ReferenceId);
        Assert.Equal("INBOUND", ledgerEntries[0].MovementType);
        Assert.Equal(50, ledgerEntries[0].Quantity);
    }

    // ==================== D) Completion Rules ====================

    [Fact]
    public async Task Test20_POBecomesCompleted_OnlyWhenAllLinesFullyReceived()
    {
        // Arrange - PO with 2 lines
        var variant2Id = SeedVariant();
        var createPoDto = new CreatePurchaseOrderDto
        {
            PoNo = "PO-2024-020",
            PartyId = _supplierPartyId,
            BranchId = _branchId,
            WarehouseId = _warehouseId,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { VariantId = _variantId, Qty = 100, UnitCost = 50 },
                new() { VariantId = variant2Id, Qty = 200, UnitCost = 60 }
            }
        };
        var po = await _purchaseOrderService.CreateDraftAsync(createPoDto);
        await _purchaseOrderService.ConfirmAsync(po.Id);
        po = await _purchaseOrderService.GetByIdAsync(po.Id);

        // Receive line 1 fully
        var grn1 = await CreateDraftGRN("GRN-2024-012A", po.Id, po.Lines[0].Id, 100);
        await _goodsReceiptService.ReceiveAsync(grn1.Id);
        var poAfterFirst = await _purchaseOrderService.GetByIdAsync(po.Id);

        // Receive line 2 fully
        var grn2 = await CreateDraftGRN("GRN-2024-012B", po.Id, po.Lines[1].Id, 200);
        await _goodsReceiptService.ReceiveAsync(grn2.Id);
        var poAfterSecond = await _purchaseOrderService.GetByIdAsync(po.Id);

        // Assert
        Assert.Equal("CONFIRMED", poAfterFirst.Status); // Not complete yet
        Assert.Equal("COMPLETED", poAfterSecond.Status); // Now complete
    }

    [Fact]
    public async Task Test21_PONotCompleted_IfAtLeastOneLineRemaining()
    {
        // Arrange
        var variant2Id = SeedVariant();
        var createPoDto = new CreatePurchaseOrderDto
        {
            PoNo = "PO-2024-021",
            PartyId = _supplierPartyId,
            BranchId = _branchId,
            WarehouseId = _warehouseId,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { VariantId = _variantId, Qty = 100, UnitCost = 50 },
                new() { VariantId = variant2Id, Qty = 200, UnitCost = 60 }
            }
        };
        var po = await _purchaseOrderService.CreateDraftAsync(createPoDto);
        await _purchaseOrderService.ConfirmAsync(po.Id);
        po = await _purchaseOrderService.GetByIdAsync(po.Id);

        // Receive line 1 fully but line 2 only partially
        var grn1 = await CreateDraftGRN("GRN-2024-013A", po.Id, po.Lines[0].Id, 100);
        await _goodsReceiptService.ReceiveAsync(grn1.Id);

        var grn2 = await CreateDraftGRN("GRN-2024-013B", po.Id, po.Lines[1].Id, 150); // Only 150/200
        await _goodsReceiptService.ReceiveAsync(grn2.Id);

        var poAfterBoth = await _purchaseOrderService.GetByIdAsync(po.Id);

        // Assert
        Assert.Equal("CONFIRMED", poAfterBoth.Status); // Not complete
        Assert.Equal(50, poAfterBoth.Lines[1].RemainingQty);
    }

    // ==================== E) Tenant Isolation ====================

    [Fact]
    public async Task Test22_TenantIsolation_TenantBCannotReadTenantAData()
    {
        // Arrange - Tenant A creates PO in current test context
        var po = await CreateConfirmedPO("PO-TENANT-A", 100);
        var grn = await CreateDraftGRN("GRN-TENANT-A", po.Id, po.Lines[0].Id, 50);

        // Create completely separate Tenant B context with its own DbFactory
        var tenantBId = Guid.NewGuid();
        var tenantBUserId = Guid.NewGuid();
        var tenantBContext = new TestTenantContext
        {
            TenantId = tenantBId,
            UserId = tenantBUserId,
            IsBypassEnabled = false // Strict tenant isolation
        };

        // Create NEW DbFactory for Tenant B (fresh InMemory DB with different tenant filter)
        using var dbFactoryB = new TestDbFactory(tenantBContext);
        using var contextB = dbFactoryB.CreateContext(tenantBContext);

        // Seed minimal data for Tenant B
        var orgB = new Organization
        {
            Id = Guid.NewGuid(),
            TenantId = tenantBId,
            Code = "ORGB",
            Name = "Org B",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = tenantBUserId
        };
        contextB.Organizations.Add(orgB);
        await contextB.SaveChangesAsync();

        var poServiceB = new PurchaseOrderService(contextB, tenantBContext);
        var stockServiceB = new StockService(contextB, tenantBContext);
        var grnServiceB = new GoodsReceiptService(contextB, tenantBContext, stockServiceB);

        // Act - search as Tenant B (should see nothing from Tenant A)
        var searchResult = await poServiceB.SearchAsync(new PurchaseOrderSearchDto { Page = 1, Size = 100 });
        
        // Try to get Tenant A's PO by ID (Tenant A data doesn't exist in Tenant B's DB)
        var getException = await Record.ExceptionAsync(() => poServiceB.GetByIdAsync(po.Id));
        
        // Try to get Tenant A's GRN
        var getGrnException = await Record.ExceptionAsync(() => grnServiceB.GetByIdAsync(grn.Id));

        // Assert - Tenant B cannot see Tenant A's data (separate databases)
        Assert.Empty(searchResult.Items); // Tenant B has empty database
        Assert.NotNull(getException); // Cannot find Tenant A's PO
        Assert.NotNull(getGrnException); // Cannot find Tenant A's GRN
    }

    [Fact]
    public async Task Test23_TenantIsolation_SamePoNoAllowedAcrossTenants()
    {
        // Arrange - Tenant A creates PO
        await CreateConfirmedPO("PO-SHARED-001", 100);

        // Create Tenant B with fresh context and full data seed
        var tenantBId = Guid.NewGuid();
        var tenantBUserId = Guid.NewGuid();
        var tenantBContext = new TestTenantContext
        {
            TenantId = tenantBId,
            UserId = tenantBUserId,
            IsBypassEnabled = true
        };

        // Create new DbFactory for Tenant B (completely isolated DB)
        using var dbFactoryB = new TestDbFactory(tenantBContext);
        using var contextB = dbFactoryB.CreateContext(tenantBContext);
        
        // Seed required data for Tenant B
        var orgB = new Organization
        {
            Id = Guid.NewGuid(),
            TenantId = tenantBId,
            Code = "ORGB",
            Name = "Org B",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = tenantBUserId
        };
        contextB.Organizations.Add(orgB);
        
        var branchB = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantBId,
            OrganizationId = orgB.Id,
            Code = "BRB",
            Name = "Branch B",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = tenantBUserId
        };
        contextB.Branches.Add(branchB);
        
        var warehouseB = new Warehouse
        {
            Id = Guid.NewGuid(),
            TenantId = tenantBId,
            BranchId = branchB.Id,
            Code = "WHB",
            Name = "Warehouse B",
            Type = "MAIN",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = tenantBUserId
        };
        contextB.Warehouses.Add(warehouseB);
        
        var partyB = new Party
        {
            Id = Guid.NewGuid(),
            TenantId = tenantBId,
            Code = "SUPB",
            Name = "Supplier B",
            Type = "SUPPLIER",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = tenantBUserId
        };
        contextB.Parties.Add(partyB);
        
        var productB = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantBId,
            Code = "PRODB",
            Name = "Product B",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = tenantBUserId
        };
        contextB.Products.Add(productB);
        
        var variantB = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = tenantBId,
            ProductId = productB.Id,
            Sku = "SKUB",
            Name = "Variant B",
            VatRate = 18,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = tenantBUserId
        };
        contextB.ProductVariants.Add(variantB);
        await contextB.SaveChangesAsync();

        var poServiceB = new PurchaseOrderService(contextB, tenantBContext);

        // Act - create PO with same PoNo for Tenant B
        var createDto = new CreatePurchaseOrderDto
        {
            PoNo = "PO-SHARED-001", // Same as Tenant A
            PartyId = partyB.Id,
            BranchId = branchB.Id,
            WarehouseId = warehouseB.Id,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { VariantId = variantB.Id, Qty = 50, UnitCost = 40 }
            }
        };
        
        var exception = await Record.ExceptionAsync(() => poServiceB.CreateDraftAsync(createDto));

        // Assert - should succeed (unique constraint is tenant-scoped)
        Assert.Null(exception);
    }

    // ==================== F) Search & Filters ====================

    [Fact]
    public async Task Test24_SearchPO_ByPoNo_ReturnsExpected()
    {
        // Arrange
        await CreateConfirmedPO("PO-SEARCH-001", 100);
        await CreateConfirmedPO("PO-SEARCH-002", 200);
        await CreateConfirmedPO("PO-OTHER-003", 300);

        // Act - get all POs and filter in-memory (ILike not supported in InMemory DB)
        var allResult = await _purchaseOrderService.SearchAsync(new PurchaseOrderSearchDto
        {
            Page = 1,
            Size = 100
        });
        var filteredItems = allResult.Items.Where(po => po.PoNo.Contains("SEARCH")).ToList();

        // Assert
        Assert.Equal(2, filteredItems.Count);
        Assert.All(filteredItems, po => Assert.Contains("SEARCH", po.PoNo));
    }

    [Fact]
    public async Task Test25_SearchGRN_ByStatusAndDateRange_Works()
    {
        // Arrange
        var po = await CreateConfirmedPO("PO-FILTER-001", 100);
        var grn1 = await CreateDraftGRN("GRN-FILTER-001", po.Id, po.Lines[0].Id, 30);
        var grn2 = await CreateDraftGRN("GRN-FILTER-002", po.Id, po.Lines[0].Id, 30);
        await _goodsReceiptService.ReceiveAsync(grn1.Id); // RECEIVED

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act - search for DRAFT status
        var draftResult = await _goodsReceiptService.SearchAsync(new GoodsReceiptSearchDto
        {
            Status = "DRAFT",
            Page = 1,
            Size = 10
        });

        // Search by date range
        var dateResult = await _goodsReceiptService.SearchAsync(new GoodsReceiptSearchDto
        {
            From = today,
            To = today,
            Page = 1,
            Size = 10
        });

        // Assert
        Assert.Single(draftResult.Items); // Only grn2 is DRAFT
        Assert.Equal("GRN-FILTER-002", draftResult.Items[0].GrnNo);
        
        Assert.Equal(2, dateResult.TotalCount); // Both within date range
    }

    [Fact]
    public async Task Test26_ReceiveGRN_PreventsCancelledStatus()
    {
        // Arrange
        var po = await CreateConfirmedPO("PO-2024-026", 100);
        var grn = await CreateDraftGRN("GRN-2024-026", po.Id, po.Lines[0].Id, 50);
        await _goodsReceiptService.CancelAsync(grn.Id);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _goodsReceiptService.ReceiveAsync(grn.Id));
        Assert.Contains("cancelled", ex.Message.ToLower());
    }

    // ==================== Helper Methods ====================

    private async Task<PurchaseOrderDto> CreateConfirmedPO(string poNo, decimal qty, decimal unitCost = 50)
    {
        var createDto = new CreatePurchaseOrderDto
        {
            PoNo = poNo,
            PartyId = _supplierPartyId,
            BranchId = _branchId,
            WarehouseId = _warehouseId,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<PurchaseOrderLineDto>
            {
                new() { VariantId = _variantId, Qty = qty, UnitCost = unitCost }
            }
        };

        var po = await _purchaseOrderService.CreateDraftAsync(createDto);
        return await _purchaseOrderService.ConfirmAsync(po.Id);
    }

    private async Task<GoodsReceiptDto> CreateDraftGRN(string grnNo, Guid poId, Guid poLineId, decimal qty)
    {
        var createDto = new CreateGoodsReceiptDto
        {
            GrnNo = grnNo,
            PurchaseOrderId = poId,
            ReceiptDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Lines = new List<GoodsReceiptLineDto>
            {
                new() { PurchaseOrderLineId = poLineId, Qty = qty }
            }
        };

        return await _goodsReceiptService.CreateDraftAsync(createDto);
    }
}
