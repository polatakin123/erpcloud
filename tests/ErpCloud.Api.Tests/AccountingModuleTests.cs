using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ErpCloud.Api.Tests;

public class AccountingModuleTests : IDisposable
{
    private readonly TestDbFactory _dbFactory;
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly InvoiceService _invoiceService;
    private readonly PaymentService _paymentService;
    private readonly PartyLedgerService _ledgerService;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _branchId;
    private readonly Guid _partyId;
    private readonly Guid _variantId;

    public AccountingModuleTests()
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(_tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(_userId);
        mockTenantContext.Setup(x => x.IsBypassEnabled).Returns(true);  // Bypass query filter for tests
        _tenantContext = mockTenantContext.Object;

        // Use SQLite in-memory for proper relational behavior
        _dbFactory = new TestDbFactory(_tenantContext);
        _context = _dbFactory.CreateContext(_tenantContext);

        // Seed test data FIRST (synchronous for constructor)
        _branchId = SeedBranch();
        _partyId = SeedParty();
        _variantId = SeedVariant();

        // Clear change tracker to ensure fresh queries
        _context.ChangeTracker.Clear();

        // Create test services AFTER seeding (they use same context)
        _invoiceService = new InvoiceService(_context, _tenantContext);
        _paymentService = new PaymentService(_context, _tenantContext);
        _ledgerService = new PartyLedgerService(_context, _tenantContext);
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

    [Fact]
    public async Task Test01_CreateSalesInvoiceDraft_Success()
    {
        // Arrange
        var dto = new CreateInvoiceDto(
            InvoiceNo: "INV-001",
            Type: "SALES",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: "Test sales invoice",
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: _variantId,
                    Description: "Product Line",
                    Qty: 10m,
                    UnitPrice: 100m,
                    LineTotal: null,
                    VatRate: 20m
                )
            }
        );

        // Act
        var result = await _invoiceService.CreateDraftAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("INV-001", result.InvoiceNo);
        Assert.Equal("SALES", result.Type);
        Assert.Equal("DRAFT", result.Status);
        Assert.Equal(1000m, result.Subtotal); // 10 * 100
        Assert.Equal(200m, result.VatTotal); // 1000 * 20%
        Assert.Equal(1200m, result.GrandTotal); // 1000 + 200
        Assert.Single(result.Lines);
    }

    [Fact]
    public async Task Test02_CreatePurchaseInvoiceDraft_Success()
    {
        // Arrange
        var dto = new CreateInvoiceDto(
            InvoiceNo: "PINV-001",
            Type: "PURCHASE",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: "Test purchase invoice",
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: null,
                    Description: "Service Line",
                    Qty: null,
                    UnitPrice: null,
                    LineTotal: 500m,
                    VatRate: 20m
                )
            }
        );

        // Act
        var result = await _invoiceService.CreateDraftAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PINV-001", result.InvoiceNo);
        Assert.Equal("PURCHASE", result.Type);
        Assert.Equal("DRAFT", result.Status);
        Assert.Equal(500m, result.Subtotal);
        Assert.Equal(100m, result.VatTotal); // 500 * 20%
        Assert.Equal(600m, result.GrandTotal);
    }

    [Fact]
    public async Task Test03_UpdateInvoiceDraft_RecalculatesTotals()
    {
        // Arrange - create invoice
        var createDto = new CreateInvoiceDto(
            InvoiceNo: "INV-002",
            Type: "SALES",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: null,
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: _variantId,
                    Description: "Line 1",
                    Qty: 5m,
                    UnitPrice: 100m,
                    LineTotal: null,
                    VatRate: 20m
                )
            }
        );
        var invoice = await _invoiceService.CreateDraftAsync(createDto);

        // Act - update with new lines
        var updateDto = new UpdateInvoiceDto(
            InvoiceNo: "INV-002",
            Type: "SALES",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: "Updated",
            Lines: new List<UpdateInvoiceLineDto>
            {
                new(_variantId, "Line 1 Updated", 10m, 100m, 20m, null),
                new(null, "Line 2", null, null, 10m, 300m)
            }
        );
        var result = await _invoiceService.UpdateDraftAsync(invoice.Id, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Note);
        Assert.Equal(2, result.Lines.Count);
        Assert.Equal(1300m, result.Subtotal); // 1000 + 300
        Assert.Equal(230m, result.VatTotal); // 200 + 30
        Assert.Equal(1530m, result.GrandTotal);
    }

    [Fact]
    public async Task Test04_IssueSalesInvoice_CreatesPositiveLedgerEntry()
    {
        // Arrange - create draft
        var createDto = new CreateInvoiceDto(
            InvoiceNo: "INV-003",
            Type: "SALES",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: null,
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: _variantId,
                    Description: "Product",
                    Qty: 10m,
                    UnitPrice: 100m,
                    LineTotal: null,
                    VatRate: 20m
                )
            }
        );
        var invoice = await _invoiceService.CreateDraftAsync(createDto);

        // Act - issue invoice
        var result = await _invoiceService.IssueAsync(invoice.Id);

        // Assert
        Assert.Equal("ISSUED", result.Status);

        // Verify ledger entry
        var ledgerEntries = await _context.Set<PartyLedgerEntry>()
            .Where(e => e.TenantId == _tenantId && e.SourceType == "INVOICE" && e.SourceId == invoice.Id)
            .ToListAsync();

        Assert.Single(ledgerEntries);
        var entry = ledgerEntries[0];
        Assert.Equal(_partyId, entry.PartyId);
        Assert.Equal(1200m, entry.AmountSigned); // Positive: customer owes us
    }

    [Fact]
    public async Task Test05_IssuePurchaseInvoice_CreatesNegativeLedgerEntry()
    {
        // Arrange - create draft
        var createDto = new CreateInvoiceDto(
            InvoiceNo: "PINV-002",
            Type: "PURCHASE",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: null,
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: null,
                    Description: "Service Line",
                    Qty: null,
                    UnitPrice: null,
                    LineTotal: 500m,
                    VatRate: 20m
                )
            }
        );
        var invoice = await _invoiceService.CreateDraftAsync(createDto);

        // Act - issue invoice
        var result = await _invoiceService.IssueAsync(invoice.Id);

        // Assert
        Assert.Equal("ISSUED", result.Status);

        // Verify ledger entry
        var ledgerEntries = await _context.Set<PartyLedgerEntry>()
            .Where(e => e.TenantId == _tenantId && e.SourceType == "INVOICE" && e.SourceId == invoice.Id)
            .ToListAsync();

        Assert.Single(ledgerEntries);
        var entry = ledgerEntries[0];
        Assert.Equal(_partyId, entry.PartyId);
        Assert.Equal(-600m, entry.AmountSigned); // Negative: we owe supplier
    }

    [Fact]
    public async Task Test06_IssueInvoice_Idempotent()
    {
        // Arrange - create and issue
        var createDto = new CreateInvoiceDto(
            InvoiceNo: "INV-004",
            Type: "SALES",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: null,
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: _variantId,
                    Description: "Product",
                    Qty: 5m,
                    UnitPrice: 100m,
                    LineTotal: null,
                    VatRate: 20m
                )
            }
        );
        var invoice = await _invoiceService.CreateDraftAsync(createDto);
        await _invoiceService.IssueAsync(invoice.Id);

        // Act - issue again
        var result = await _invoiceService.IssueAsync(invoice.Id);

        // Assert - no duplicate ledger entry
        var ledgerEntries = await _context.Set<PartyLedgerEntry>()
            .Where(e => e.TenantId == _tenantId && e.SourceType == "INVOICE" && e.SourceId == invoice.Id)
            .ToListAsync();

        Assert.Single(ledgerEntries); // Still only one entry
        Assert.Equal("ISSUED", result.Status);
    }

    [Fact]
    public async Task Test07_CancelInvoice_CreatesReverseLedgerEntry()
    {
        // Arrange - create and issue
        var createDto = new CreateInvoiceDto(
            InvoiceNo: "INV-005",
            Type: "SALES",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: null,
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: _variantId,
                    Description: "Product",
                    Qty: 10m,
                    UnitPrice: 100m,
                    LineTotal: null,
                    VatRate: 20m
                )
            }
        );
        var invoice = await _invoiceService.CreateDraftAsync(createDto);
        await _invoiceService.IssueAsync(invoice.Id);

        // Act - cancel invoice
        var result = await _invoiceService.CancelAsync(invoice.Id);

        // Assert
        Assert.Equal("CANCELLED", result.Status);

        // Verify ledger entries
        var ledgerEntries = await _context.Set<PartyLedgerEntry>()
            .Where(e => e.TenantId == _tenantId && e.SourceId == invoice.Id)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        Assert.Equal(2, ledgerEntries.Count);
        Assert.Equal("INVOICE", ledgerEntries[0].SourceType);
        Assert.Equal(1200m, ledgerEntries[0].AmountSigned); // Original +1200

        Assert.Equal("INVOICE_CANCEL", ledgerEntries[1].SourceType);
        Assert.Equal(-1200m, ledgerEntries[1].AmountSigned); // Reverse -1200
    }

    [Fact]
    public async Task Test08_CancelInvoice_Idempotent()
    {
        // Arrange - create, issue, cancel
        var createDto = new CreateInvoiceDto(
            InvoiceNo: "INV-006",
            Type: "SALES",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: null,
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: _variantId,
                    Description: "Product",
                    Qty: 5m,
                    UnitPrice: 100m,
                    LineTotal: null,
                    VatRate: 20m
                )
            }
        );
        var invoice = await _invoiceService.CreateDraftAsync(createDto);
        await _invoiceService.IssueAsync(invoice.Id);
        await _invoiceService.CancelAsync(invoice.Id);

        // Act - cancel again
        var result = await _invoiceService.CancelAsync(invoice.Id);

        // Assert - no duplicate cancel entry
        var ledgerEntries = await _context.Set<PartyLedgerEntry>()
            .Where(e => e.TenantId == _tenantId && e.SourceId == invoice.Id)
            .ToListAsync();

        Assert.Equal(2, ledgerEntries.Count); // Still only 2 entries
        Assert.Equal("CANCELLED", result.Status);
    }

    [Fact]
    public async Task Test09_CreatePaymentIN_CreatesNegativeLedgerEntry()
    {
        // Arrange
        var dto = new CreatePaymentDto(
            PaymentNo: "PAY-001",
            PartyId: _partyId,
            BranchId: _branchId,
            Date: DateTime.Today,
            Direction: "IN",
            Method: "CASH",
            Currency: "TRY",
            Amount: 500m,
            Note: "Cash received",
            SourceType: null,
            SourceId: null
        );

        // Act
        var result = await _paymentService.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PAY-001", result.PaymentNo);
        Assert.Equal("IN", result.Direction);
        Assert.Equal(500m, result.Amount);

        // Verify ledger entry
        var ledgerEntries = await _context.Set<PartyLedgerEntry>()
            .Where(e => e.TenantId == _tenantId && e.SourceType == "PAYMENT" && e.SourceId == result.Id)
            .ToListAsync();

        Assert.Single(ledgerEntries);
        Assert.Equal(-500m, ledgerEntries[0].AmountSigned); // Negative: reduces receivable
    }

    [Fact]
    public async Task Test10_CreatePaymentOUT_CreatesPositiveLedgerEntry()
    {
        // Arrange
        var dto = new CreatePaymentDto(
            PaymentNo: "PAY-002",
            PartyId: _partyId,
            BranchId: _branchId,
            Date: DateTime.Today,
            Direction: "OUT",
            Method: "BANK",
            Currency: "TRY",
            Amount: 300m,
            Note: "Payment made",
            SourceType: null,
            SourceId: null
        );

        // Act
        var result = await _paymentService.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PAY-002", result.PaymentNo);
        Assert.Equal("OUT", result.Direction);
        Assert.Equal(300m, result.Amount);

        // Verify ledger entry
        var ledgerEntries = await _context.Set<PartyLedgerEntry>()
            .Where(e => e.TenantId == _tenantId && e.SourceType == "PAYMENT" && e.SourceId == result.Id)
            .ToListAsync();

        Assert.Single(ledgerEntries);
        Assert.Equal(300m, ledgerEntries[0].AmountSigned); // Positive: reduces payable
    }

    [Fact]
    public async Task Test11_CalculatePartyBalance_FromMultipleTransactions()
    {
        // Arrange - create multiple transactions
        // 1. Sales invoice: +1200
        var invoice1 = await _invoiceService.CreateDraftAsync(new CreateInvoiceDto(
            InvoiceNo: "INV-101",
            Type: "SALES",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: null,
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: _variantId,
                    Description: "Product",
                    Qty: 10m,
                    UnitPrice: 100m,
                    LineTotal: null,
                    VatRate: 20m
                )
            }
        ));
        await _invoiceService.IssueAsync(invoice1.Id);

        // 2. Payment IN: -500
        await _paymentService.CreateAsync(new CreatePaymentDto(
            "PAY-101", _partyId, _branchId, DateTime.Today, "IN", "CASH", "TRY", 500m, null, null, null
        ));

        // 3. Purchase invoice: -600
        var invoice2 = await _invoiceService.CreateDraftAsync(new CreateInvoiceDto(
            InvoiceNo: "PINV-101",
            Type: "PURCHASE",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: null,
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: null,
                    Description: "Service",
                    Qty: null,
                    UnitPrice: null,
                    LineTotal: 500m,
                    VatRate: 20m
                )
            }
        ));
        await _invoiceService.IssueAsync(invoice2.Id);

        // 4. Payment OUT: +300
        await _paymentService.CreateAsync(new CreatePaymentDto(
            "PAY-102", _partyId, _branchId, DateTime.Today, "OUT", "BANK", "TRY", 300m, null, null, null
        ));

        // Act
        var balance = await _ledgerService.GetBalanceAsync(_partyId);

        // Assert
        // Balance = +1200 - 500 - 600 + 300 = +400 (customer owes us 400)
        Assert.Equal(400m, balance.Balance);
    }

    [Fact]
    public async Task Test12_GetPartyLedger_ReturnsAllEntries()
    {
        // Arrange - create transactions
        var invoice = await _invoiceService.CreateDraftAsync(new CreateInvoiceDto(
            InvoiceNo: "INV-201",
            Type: "SALES",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: null,
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: _variantId,
                    Description: "Product",
                    Qty: 5m,
                    UnitPrice: 100m,
                    LineTotal: null,
                    VatRate: 20m
                )
            }
        ));
        await _invoiceService.IssueAsync(invoice.Id);

        await _paymentService.CreateAsync(new CreatePaymentDto(
            "PAY-201", _partyId, _branchId, DateTime.Today, "IN", "CASH", "TRY", 300m, null, null, null
        ));

        // Act
        var ledger = await _ledgerService.GetLedgerAsync(_partyId, new PartyLedgerSearchDto(
            From: null, To: null, Page: 1, Size: 50
        ));

        // Assert
        Assert.Equal(2, ledger.TotalCount);
        Assert.Equal(2, ledger.Items.Count);
    }

    [Fact]
    public async Task Test13_VatCalculation_RoundedCorrectly()
    {
        // Arrange - create invoice with tricky VAT calculation
        var dto = new CreateInvoiceDto(
            InvoiceNo: "INV-VAT",
            Type: "SALES",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: null,
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: _variantId,
                    Description: "Product",
                    Qty: 3m,
                    UnitPrice: 33.33m,
                    LineTotal: null,
                    VatRate: 20m
                ) // 99.99 * 0.20 = 19.998
            }
        );

        // Act
        var result = await _invoiceService.CreateDraftAsync(dto);

        // Assert
        Assert.Equal(99.99m, result.Subtotal); // 3 * 33.33
        Assert.Equal(20m, result.VatTotal); // Rounded to 20.00
        Assert.Equal(119.99m, result.GrandTotal);
    }

    [Fact]
    public async Task Test14_UpdateDraftOnly_IssuedInvoiceCannotBeUpdated()
    {
        // Arrange - create and issue
        var createDto = new CreateInvoiceDto(
            InvoiceNo: "INV-UPDATE",
            Type: "SALES",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: null,
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: _variantId,
                    Description: "Product",
                    Qty: 5m,
                    UnitPrice: 100m,
                    LineTotal: null,
                    VatRate: 20m
                )
            }
        );
        var invoice = await _invoiceService.CreateDraftAsync(createDto);
        await _invoiceService.IssueAsync(invoice.Id);

        // Act & Assert
        var updateDto = new UpdateInvoiceDto(
            "INV-UPDATE", "SALES", _partyId, _branchId, DateTime.Today, null, "TRY", "Updated",
            new List<UpdateInvoiceLineDto>()
        );
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _invoiceService.UpdateDraftAsync(invoice.Id, updateDto)
        );
    }

    [Fact]
    public async Task Test15_CancelOnlyIssued_DraftCannotBeCancelled()
    {
        // Arrange - create draft
        var createDto = new CreateInvoiceDto(
            InvoiceNo: "INV-CANCEL",
            Type: "SALES",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: null,
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: _variantId,
                    Description: "Product",
                    Qty: 5m,
                    UnitPrice: 100m,
                    LineTotal: null,
                    VatRate: 20m
                )
            }
        );
        var invoice = await _invoiceService.CreateDraftAsync(createDto);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _invoiceService.CancelAsync(invoice.Id)
        );
    }

    [Fact]
    public async Task Test16_DuplicateInvoiceNo_ThrowsException()
    {
        // Arrange - create first invoice
        await _invoiceService.CreateDraftAsync(new CreateInvoiceDto(
            InvoiceNo: "INV-DUP",
            Type: "SALES",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: null,
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: _variantId,
                    Description: "Product",
                    Qty: 5m,
                    UnitPrice: 100m,
                    LineTotal: null,
                    VatRate: 20m
                )
            }
        ));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _invoiceService.CreateDraftAsync(new CreateInvoiceDto(
                InvoiceNo: "INV-DUP",
                Type: "SALES",
                PartyId: _partyId,
                BranchId: _branchId,
                IssueDate: DateTime.Today,
                DueDate: null,
                Currency: "TRY",
                Note: null,
                Lines: new List<CreateInvoiceLineDto>
                {
                    new(
                        VariantId: _variantId,
                        Description: "Product",
                        Qty: 5m,
                        UnitPrice: 100m,
                        LineTotal: null,
                        VatRate: 20m
                    )
                }
            ))
        );
    }

    [Fact]
    public async Task Test17_DuplicatePaymentNo_ThrowsException()
    {
        // Arrange - create first payment
        await _paymentService.CreateAsync(new CreatePaymentDto(
            "PAY-DUP", _partyId, _branchId, DateTime.Today, "IN", "CASH", "TRY", 500m, null, null, null
        ));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _paymentService.CreateAsync(new CreatePaymentDto(
                "PAY-DUP", _partyId, _branchId, DateTime.Today, "IN", "CASH", "TRY", 500m, null, null, null
            ))
        );
    }

    [Fact]
    public async Task Test18_SearchInvoices_FilterByType()
    {
        // Arrange - create sales and purchase invoices
        await _invoiceService.CreateDraftAsync(new CreateInvoiceDto(
            InvoiceNo: "SALES-001",
            Type: "SALES",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: null,
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: _variantId,
                    Description: "Product",
                    Qty: 5m,
                    UnitPrice: 100m,
                    LineTotal: null,
                    VatRate: 20m
                )
            }
        ));

        await _invoiceService.CreateDraftAsync(new CreateInvoiceDto(
            InvoiceNo: "PURCH-001",
            Type: "PURCHASE",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: null,
            Lines: new List<CreateInvoiceLineDto>
            {
                new(
                    VariantId: null,
                    Description: "Service",
                    Qty: null,
                    UnitPrice: null,
                    LineTotal: 500m,
                    VatRate: 20m
                )
            }
        ));

        // Act - search for SALES only
        var result = await _invoiceService.SearchAsync(new InvoiceSearchDto(
            Q: null, Type: "SALES", Status: null, PartyId: null, From: null, To: null, Page: 1, Size: 50
        ));

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("SALES", result.Items[0].Type);
    }

    public void Dispose()
    {
        _context?.Dispose();
        _dbFactory?.Dispose();
    }
}
