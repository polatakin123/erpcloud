using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ErpCloud.Api.Tests;

public class ShipmentInvoicingModuleTests : IDisposable
{
    private readonly TestDbFactory _dbFactory;
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly StockService _stockService;
    private readonly SalesOrderService _salesOrderService;
    private readonly ShipmentService _shipmentService;
    private readonly ShipmentInvoicingService _shipmentInvoicingService;
    private readonly InvoiceService _invoiceService;
    private readonly PartyLedgerService _ledgerService;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _branchId;
    private readonly Guid _warehouseId;
    private readonly Guid _partyId;
    private readonly Guid _variantId;

    public ShipmentInvoicingModuleTests()
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(_tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(_userId);
        mockTenantContext.Setup(x => x.IsBypassEnabled).Returns(true);
        _tenantContext = mockTenantContext.Object;

        _dbFactory = new TestDbFactory(_tenantContext);
        _context = _dbFactory.CreateContext(_tenantContext);

        _branchId = SeedBranch();
        _warehouseId = SeedWarehouse();
        _partyId = SeedParty();
        _variantId = SeedVariant();

        // DO NOT clear change tracker - causes issues with seeded data

        _stockService = new StockService(_context, _tenantContext);
        _salesOrderService = new SalesOrderService(_context, _tenantContext, _stockService);
        _shipmentService = new ShipmentService(_context, _tenantContext, _stockService);
        _shipmentInvoicingService = new ShipmentInvoicingService(_context, _tenantContext);
        _ledgerService = new PartyLedgerService(_context, _tenantContext);
        _invoiceService = new InvoiceService(_context, _tenantContext, _shipmentInvoicingService);
    }

    public void Dispose()
    {
        _context.Dispose();
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
            VatRate = 18.00m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        _context.SaveChanges();

        return variant.Id;
    }

    private async Task<Guid> CreateShippedShipmentAsync(decimal qty = 100m)
    {
        // Receive stock
        await _stockService.ReceiveStockAsync(new ReceiveStockDto(
            _warehouseId,
            _variantId,
            qty,
            100m,
            "PO",
            Guid.NewGuid(),
            "Initial stock"
        ));

        // Create and confirm order
        var orderDto = new CreateSalesOrderDto(
            "ORD-" + Guid.NewGuid().ToString("N")[..6],
            _partyId,
            _branchId,
            _warehouseId,
            null,
            DateTime.Today,
            null,
            new List<CreateSalesOrderLineDto>
            {
                new(_variantId, qty, 150m, 18m, null)
            }
        );

        var order = await _salesOrderService.CreateDraftAsync(orderDto);
        await _salesOrderService.ConfirmAsync(order.Id);

        // Create and ship shipment
        var orderLine = await _context.SalesOrderLines.FirstAsync(l => l.SalesOrderId == order.Id);

        var shipmentDto = new CreateShipmentDto(
            "SHIP-" + Guid.NewGuid().ToString("N")[..6],
            order.Id,
            _branchId,
            _warehouseId,
            DateTime.Today,
            null,
            new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, qty, null) }
        );

        var shipment = await _shipmentService.CreateDraftAsync(shipmentDto);
        await _shipmentService.ShipAsync(shipment.Id);

        return shipment.Id;
    }

    [Fact]
    public async Task Test01_CreateInvoiceFromShipment_Success()
    {
        // Arrange
        var shipmentId = await CreateShippedShipmentAsync();

        var request = new CreateInvoiceFromShipmentDto(
            "INV-001",
            DateTime.Today,
            DateTime.Today.AddDays(30),
            "Test invoice",
            null // Full invoice
        );

        // Act
        var invoice = await _shipmentInvoicingService.CreateDraftInvoiceFromShipmentAsync(shipmentId, request);

        // Assert
        Assert.NotNull(invoice);
        Assert.Equal("INV-001", invoice.InvoiceNo);
        Assert.Equal("SALES", invoice.Type);
        Assert.Equal("DRAFT", invoice.Status);
        Assert.Equal("SHIPMENT", invoice.SourceType);
        Assert.Equal(shipmentId, invoice.SourceId);
        Assert.Single(invoice.Lines);
        Assert.Equal(100m, invoice.Lines[0].Qty);
    }

    [Fact]
    public async Task Test02_InvoiceTotalsCalculatedCorrectly()
    {
        // Arrange
        var shipmentId = await CreateShippedShipmentAsync(100m);

        var request = new CreateInvoiceFromShipmentDto(
            "INV-002",
            DateTime.Today,
            null,
            null,
            null
        );

        // Act
        var invoice = await _shipmentInvoicingService.CreateDraftInvoiceFromShipmentAsync(shipmentId, request);

        // Assert
        var expectedSubtotal = 100m * 150m; // qty * unit price = 15000
        var expectedVat = expectedSubtotal * 0.18m; // 2700
        var expectedTotal = expectedSubtotal + expectedVat; // 17700

        Assert.Equal(expectedSubtotal, invoice.Subtotal);
        Assert.Equal(expectedVat, invoice.VatTotal);
        Assert.Equal(expectedTotal, invoice.GrandTotal);
    }

    [Fact]
    public async Task Test03_PreviewDoesNotCreateInvoice()
    {
        // Arrange
        var shipmentId = await CreateShippedShipmentAsync();

        var request = new CreateInvoiceFromShipmentDto(
            "INV-PREVIEW",
            DateTime.Today,
            null,
            null,
            null
        );

        // Act
        var preview = await _shipmentInvoicingService.PreviewInvoiceFromShipmentAsync(shipmentId, request);

        // Assert
        Assert.NotNull(preview);
        Assert.Equal(15000m, preview.Subtotal);
        
        // Verify no invoice was created
        var invoiceCount = await _context.Invoices.CountAsync(i => i.TenantId == _tenantId);
        Assert.Equal(0, invoiceCount);
    }

    [Fact]
    public async Task Test04_PartialInvoice_AllowsRemainingQty()
    {
        // Arrange
        var shipmentId = await CreateShippedShipmentAsync(100m);
        var shipment = await _context.Shipments.Include(s => s.Lines).FirstAsync(s => s.Id == shipmentId);
        var shipmentLineId = shipment.Lines.First().Id;

        var request = new CreateInvoiceFromShipmentDto(
            "INV-PARTIAL",
            DateTime.Today,
            null,
            null,
            new List<ShipmentInvoiceLineRequestDto>
            {
                new(shipmentLineId, 60m) // Partial qty
            }
        );

        // Act
        var invoice = await _shipmentInvoicingService.CreateDraftInvoiceFromShipmentAsync(shipmentId, request);

        // Assert
        Assert.Equal(60m, invoice.Lines[0].Qty);
        Assert.Equal(60m * 150m, invoice.Subtotal); // 9000
    }

    [Fact]
    public async Task Test05_PartialInvoice_OverQtyFails()
    {
        // Arrange
        var shipmentId = await CreateShippedShipmentAsync(100m);
        var shipment = await _context.Shipments.Include(s => s.Lines).FirstAsync(s => s.Id == shipmentId);
        var shipmentLineId = shipment.Lines.First().Id;

        var request = new CreateInvoiceFromShipmentDto(
            "INV-OVER",
            DateTime.Today,
            null,
            null,
            new List<ShipmentInvoiceLineRequestDto>
            {
                new(shipmentLineId, 150m) // Over shipment qty
            }
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentInvoicingService.CreateDraftInvoiceFromShipmentAsync(shipmentId, request));
        
        Assert.Contains("Cannot invoice 150", ex.Message);
    }

    [Fact]
    public async Task Test06_IssueInvoice_UpdatesShipmentLineInvoicedQty()
    {
        // Arrange
        var shipmentId = await CreateShippedShipmentAsync();

        var request = new CreateInvoiceFromShipmentDto(
            "INV-ISSUE",
            DateTime.Today,
            null,
            null,
            null
        );

        var invoice = await _shipmentInvoicingService.CreateDraftInvoiceFromShipmentAsync(shipmentId, request);

        // Act
        await _invoiceService.IssueAsync(invoice.Id);

        // Assert
        var shipment = await _context.Shipments.Include(s => s.Lines).FirstAsync(s => s.Id == shipmentId);
        Assert.Equal(100m, shipment.Lines.First().InvoicedQty);
    }

    [Fact]
    public async Task Test07_IssueIdempotent_DoesNotDoubleInvoiceQty()
    {
        // Arrange
        var shipmentId = await CreateShippedShipmentAsync();

        var request = new CreateInvoiceFromShipmentDto(
            "INV-IDEMP",
            DateTime.Today,
            null,
            null,
            null
        );

        var invoice = await _shipmentInvoicingService.CreateDraftInvoiceFromShipmentAsync(shipmentId, request);
        await _invoiceService.IssueAsync(invoice.Id);

        // Act - Issue again (idempotent)
        await _invoiceService.IssueAsync(invoice.Id);

        // Assert - InvoicedQty should still be 100, not 200
        var shipment = await _context.Shipments.Include(s => s.Lines).FirstAsync(s => s.Id == shipmentId);
        Assert.Equal(100m, shipment.Lines.First().InvoicedQty);
    }

    [Fact]
    public async Task Test08_SameShipmentLineCannotBeInvoicedTwice()
    {
        // Arrange
        var shipmentId = await CreateShippedShipmentAsync();
        var shipment = await _context.Shipments.Include(s => s.Lines).FirstAsync(s => s.Id == shipmentId);
        var shipmentLineId = shipment.Lines.First().Id;

        var request1 = new CreateInvoiceFromShipmentDto(
            "INV-FIRST",
            DateTime.Today,
            null,
            null,
            new List<ShipmentInvoiceLineRequestDto> { new(shipmentLineId, 60m) }
        );

        await _shipmentInvoicingService.CreateDraftInvoiceFromShipmentAsync(shipmentId, request1);

        // Try to invoice same line again (even with remaining qty)
        var request2 = new CreateInvoiceFromShipmentDto(
            "INV-SECOND",
            DateTime.Today,
            null,
            null,
            new List<ShipmentInvoiceLineRequestDto> { new(shipmentLineId, 40m) }
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentInvoicingService.CreateDraftInvoiceFromShipmentAsync(shipmentId, request2));

        Assert.Contains("already invoiced", ex.Message);
    }

    [Fact]
    public async Task Test09_NonShippedShipmentCannotBeInvoiced()
    {
        // Arrange - Create draft shipment but don't ship it
        var qty = 100m;
        await _stockService.ReceiveStockAsync(new ReceiveStockDto(
            _warehouseId, _variantId, qty, 100m, "PO", Guid.NewGuid(), null));

        var orderDto = new CreateSalesOrderDto(
            "ORD-DRAFT", _partyId, _branchId, _warehouseId, null, DateTime.Today, null,
            new List<CreateSalesOrderLineDto> { new(_variantId, qty, 150m, 18m, null) });

        var order = await _salesOrderService.CreateDraftAsync(orderDto);
        await _salesOrderService.ConfirmAsync(order.Id);

        var orderLine = await _context.SalesOrderLines.FirstAsync(l => l.SalesOrderId == order.Id);

        var shipmentDto = new CreateShipmentDto(
            "SHIP-DRAFT", order.Id, _branchId, _warehouseId, DateTime.Today, null,
            new List<CreateShipmentLineDto> { new(orderLine.Id, _variantId, qty, null) });

        var shipment = await _shipmentService.CreateDraftAsync(shipmentDto);
        // DO NOT ship

        var request = new CreateInvoiceFromShipmentDto(
            "INV-FAIL", DateTime.Today, null, null, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentInvoicingService.CreateDraftInvoiceFromShipmentAsync(shipment.Id, request));

        Assert.Contains("SHIPPED", ex.Message);
    }

    [Fact]
    public async Task Test10_ShipmentLineNotBelongingToShipmentFails()
    {
        // Arrange
        var shipmentId = await CreateShippedShipmentAsync();
        var wrongLineId = Guid.NewGuid();

        var request = new CreateInvoiceFromShipmentDto(
            "INV-WRONG",
            DateTime.Today,
            null,
            null,
            new List<ShipmentInvoiceLineRequestDto> { new(wrongLineId, 50m) }
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shipmentInvoicingService.CreateDraftInvoiceFromShipmentAsync(shipmentId, request));

        Assert.Contains("does not belong", ex.Message);
    }

    [Fact]
    public async Task Test11_TenantIsolation_CannotInvoiceOtherTenantShipment()
    {
        // Arrange
        var shipmentId = await CreateShippedShipmentAsync();

        // Different tenant
        var otherTenantId = Guid.NewGuid();
        var mockOtherTenant = new Mock<ITenantContext>();
        mockOtherTenant.Setup(x => x.TenantId).Returns(otherTenantId);
        mockOtherTenant.Setup(x => x.UserId).Returns(_userId);
        mockOtherTenant.Setup(x => x.IsBypassEnabled).Returns(false);

        var otherContext = _dbFactory.CreateContext(mockOtherTenant.Object);
        var otherInvoicingService = new ShipmentInvoicingService(otherContext, mockOtherTenant.Object);

        var request = new CreateInvoiceFromShipmentDto(
            "INV-OTHER", DateTime.Today, null, null, null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => otherInvoicingService.CreateDraftInvoiceFromShipmentAsync(shipmentId, request));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task Test12_GetShipmentInvoices_ReturnsCorrectList()
    {
        // Arrange
        var shipmentId = await CreateShippedShipmentAsync();

        var request = new CreateInvoiceFromShipmentDto(
            "INV-LIST", DateTime.Today, null, null, null);

        await _shipmentInvoicingService.CreateDraftInvoiceFromShipmentAsync(shipmentId, request);

        // Act
        var invoices = await _shipmentInvoicingService.GetShipmentInvoicesAsync(shipmentId);

        // Assert
        Assert.Single(invoices);
        Assert.Equal("INV-LIST", invoices[0].InvoiceNo);
        Assert.Equal("SHIPMENT", invoices[0].SourceType);
        Assert.Equal(shipmentId, invoices[0].SourceId);
    }

    [Fact]
    public async Task Test13_InvoiceResponseContainsSourceInfo()
    {
        // Arrange
        var shipmentId = await CreateShippedShipmentAsync();

        var request = new CreateInvoiceFromShipmentDto(
            "INV-SOURCE", DateTime.Today, null, null, null);

        // Act
        var invoice = await _shipmentInvoicingService.CreateDraftInvoiceFromShipmentAsync(shipmentId, request);

        // Assert
        Assert.Equal("SHIPMENT", invoice.SourceType);
        Assert.Equal(shipmentId, invoice.SourceId);
        Assert.All(invoice.Lines, line => Assert.NotNull(line.ShipmentLineId));
        Assert.All(invoice.Lines, line => Assert.NotNull(line.SalesOrderLineId));
    }

    [Fact]
    public async Task Test14_InvoicedQtyConstraint_ZeroToQty()
    {
        // Arrange
        var shipmentId = await CreateShippedShipmentAsync(100m);
        var shipment = await _context.Shipments.Include(s => s.Lines).FirstAsync(s => s.Id == shipmentId);

        // Act - Initially zero
        Assert.Equal(0m, shipment.Lines.First().InvoicedQty);

        // Create partial invoice
        var shipmentLineId = shipment.Lines.First().Id;
        var request = new CreateInvoiceFromShipmentDto(
            "INV-PARTIAL2", DateTime.Today, null, null,
            new List<ShipmentInvoiceLineRequestDto> { new(shipmentLineId, 60m) });

        var invoice = await _shipmentInvoicingService.CreateDraftInvoiceFromShipmentAsync(shipmentId, request);
        await _invoiceService.IssueAsync(invoice.Id);

        // Assert - After issue, invoiced qty updated
        var updatedShipment = await _context.Shipments.Include(s => s.Lines).FirstAsync(s => s.Id == shipmentId);
        Assert.Equal(60m, updatedShipment.Lines.First().InvoicedQty);
        Assert.True(updatedShipment.Lines.First().InvoicedQty <= updatedShipment.Lines.First().Qty);
    }

    [Fact]
    public async Task Test15_TransactionalIntegrity_LedgerAndInvoicing()
    {
        // Arrange
        var shipmentId = await CreateShippedShipmentAsync();

        var request = new CreateInvoiceFromShipmentDto(
            "INV-TRANS", DateTime.Today, null, null, null);

        var invoice = await _shipmentInvoicingService.CreateDraftInvoiceFromShipmentAsync(shipmentId, request);

        // Act - Issue creates both ledger entry and updates shipment
        await _invoiceService.IssueAsync(invoice.Id);

        // Assert - Both ledger entry and invoiced qty should be present
        var ledgerEntries = await _context.PartyLedgerEntries
            .Where(e => e.SourceType == "INVOICE" && e.SourceId == invoice.Id)
            .ToListAsync();

        var shipment = await _context.Shipments.Include(s => s.Lines).FirstAsync(s => s.Id == shipmentId);

        Assert.Single(ledgerEntries);
        Assert.Equal(17700m, ledgerEntries[0].AmountSigned); // 15000 + 2700 VAT
        Assert.Equal(100m, shipment.Lines.First().InvoicedQty);
    }

    [Fact]
    public async Task Test16_InvoiceLineSalesOrderLineIdSet()
    {
        // Arrange
        var shipmentId = await CreateShippedShipmentAsync();

        var request = new CreateInvoiceFromShipmentDto(
            "INV-ORDERREF", DateTime.Today, null, null, null);

        // Act
        var invoice = await _shipmentInvoicingService.CreateDraftInvoiceFromShipmentAsync(shipmentId, request);

        // Assert
        var invoiceFromDb = await _context.Invoices
            .Include(i => i.Lines)
            .FirstAsync(i => i.Id == invoice.Id);

        Assert.All(invoiceFromDb.Lines, line =>
        {
            Assert.NotNull(line.SalesOrderLineId);
            Assert.NotNull(line.ShipmentLineId);
        });
    }
}
