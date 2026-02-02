using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ErpCloud.Api.Tests;

public class EDocumentModuleTests : IDisposable
{
    private readonly TestDbFactory _dbFactory;
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IEDocumentService _edocumentService;
    private readonly IInvoiceService _invoiceService;
    private readonly UblGenerator _ublGenerator;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _branchId;
    private readonly Guid _partyId;
    private readonly Guid _variantId;

    public EDocumentModuleTests()
    {
        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(_tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(_userId);
        mockTenantContext.Setup(x => x.IsBypassEnabled).Returns(true);  // Bypass query filter for tests
        _tenantContext = mockTenantContext.Object;

        // Use SQLite in-memory for proper relational behavior
        _dbFactory = new TestDbFactory(_tenantContext);
        _context = _dbFactory.CreateContext(_tenantContext);

        // Seed test data FIRST
        _branchId = SeedBranch();
        _partyId = SeedParty();
        _variantId = SeedVariant();

        // Clear change tracker to ensure fresh queries
        _context.ChangeTracker.Clear();

        // Create services with SAME context (they share the connection/transaction)
        _invoiceService = new InvoiceService(_context, _tenantContext);
        _edocumentService = new EDocumentService(_context, _tenantContext);
        _ublGenerator = new UblGenerator();
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

    private async Task<Guid> CreateIssuedInvoiceAsync()
    {
        var createDto = new CreateInvoiceDto(
            InvoiceNo: "INV-E" + Guid.NewGuid().ToString("N").Substring(0, 6),
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
                    Description: "Test Line",
                    Qty: 10m,
                    UnitPrice: 100m,
                    LineTotal: null,
                    VatRate: 20m
                )
            }
        );

        var invoice = await _invoiceService.CreateDraftAsync(createDto);
        var issued = await _invoiceService.IssueAsync(invoice.Id);
        return issued.Id;
    }

    // ===== TESTS =====

    [Fact]
    public async Task Test01_CreateEDocument_FromIssuedInvoice_Success()
    {
        // Arrange
        var invoiceId = await CreateIssuedInvoiceAsync();
        
        var dto = new CreateEDocumentDto(
            InvoiceId: invoiceId,
            DocumentType: "EARCHIVE",
            Scenario: "BASIC"
        );

        // Act
        var result = await _edocumentService.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(invoiceId, result.InvoiceId);
        Assert.Equal("EARCHIVE", result.DocumentType);
        Assert.Equal("DRAFT", result.Status);
        Assert.NotEqual(Guid.Empty, result.Uuid);
    }

    [Fact]
    public async Task Test02_CreateEDocument_UniqueConstraint_Idempotent()
    {
        // Arrange
        var invoiceId = await CreateIssuedInvoiceAsync();
        
        var dto = new CreateEDocumentDto(
            InvoiceId: invoiceId,
            DocumentType: "EINVOICE",
            Scenario: "COMMERCIAL"
        );

        // Act
        var first = await _edocumentService.CreateAsync(dto);
        var second = await _edocumentService.CreateAsync(dto); // Should return same

        // Assert
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.Uuid, second.Uuid);
    }

    [Fact]
    public async Task Test03_CreateEDocument_DraftInvoice_ThrowsError()
    {
        // Arrange
        var draftDto = new CreateInvoiceDto(
            InvoiceNo: "INV-DRAFT" + Guid.NewGuid().ToString("N").Substring(0, 4),
            Type: "SALES",
            PartyId: _partyId,
            BranchId: _branchId,
            IssueDate: DateTime.Today,
            DueDate: null,
            Currency: "TRY",
            Note: null,
            Lines: new List<CreateInvoiceLineDto>
            {
                new(_variantId, "Line", 1m, 100m, 20m, null)
            }
        );

        var draftInvoice = await _invoiceService.CreateDraftAsync(draftDto);

        var edocDto = new CreateEDocumentDto(
            InvoiceId: draftInvoice.Id,
            DocumentType: "EARCHIVE"
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _edocumentService.CreateAsync(edocDto);
        });
    }

    [Fact]
    public async Task Test04_CreateEDocument_StatusHistoryCreated()
    {
        // Arrange
        var invoiceId = await CreateIssuedInvoiceAsync();
        
        var dto = new CreateEDocumentDto(
            InvoiceId: invoiceId,
            DocumentType: "EARCHIVE"
        );

        // Act
        var result = await _edocumentService.CreateAsync(dto);
        var withHistory = await _edocumentService.GetByIdWithHistoryAsync(result.Id);

        // Assert
        Assert.NotNull(withHistory);
        Assert.Single(withHistory.StatusHistory);
        Assert.Equal("DRAFT", withHistory.StatusHistory[0].Status);
    }

    [Fact]
    public async Task Test05_SearchEDocuments_ByInvoiceId()
    {
        // Arrange
        var invoiceId = await CreateIssuedInvoiceAsync();
        
        await _edocumentService.CreateAsync(new CreateEDocumentDto(invoiceId, "EARCHIVE"));
        await _edocumentService.CreateAsync(new CreateEDocumentDto(invoiceId, "EINVOICE"));

        var query = new EDocumentQuery(InvoiceId: invoiceId);

        // Act
        var result = await _edocumentService.SearchAsync(query);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, x => Assert.Equal(invoiceId, x.InvoiceId));
    }

    [Fact]
    public async Task Test06_SearchEDocuments_ByStatus()
    {
        // Arrange
        var invoiceId = await CreateIssuedInvoiceAsync();
        
        await _edocumentService.CreateAsync(new CreateEDocumentDto(invoiceId, "EARCHIVE"));

        var query = new EDocumentQuery(Status: "DRAFT");

        // Act
        var result = await _edocumentService.SearchAsync(query);

        // Assert
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, x => Assert.Equal("DRAFT", x.Status));
    }

    [Fact]
    public async Task Test07_SearchEDocuments_ByDocumentType()
    {
        // Arrange
        var invoiceId = await CreateIssuedInvoiceAsync();
        
        await _edocumentService.CreateAsync(new CreateEDocumentDto(invoiceId, "EARCHIVE"));

        var query = new EDocumentQuery(DocumentType: "EARCHIVE");

        // Act
        var result = await _edocumentService.SearchAsync(query);

        // Assert
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, x => Assert.Equal("EARCHIVE", x.DocumentType));
    }

    [Fact]
    public async Task Test08_RetryEDocument_OnlyErrorStatus()
    {
        // Arrange
        var invoiceId = await CreateIssuedInvoiceAsync();
        
        var edoc = await _edocumentService.CreateAsync(new CreateEDocumentDto(invoiceId, "EARCHIVE"));

        // Simulate ERROR status
        var entity = await _context.EDocuments.FirstAsync(x => x.Id == edoc.Id);
        entity.Status = "ERROR";
        entity.RetryCount = 1;
        await _context.SaveChangesAsync();

        // Act
        var result = await _edocumentService.RetryAsync(edoc.Id);

        // Assert
        Assert.Equal("DRAFT", result.Status);
        Assert.Equal(1, result.RetryCount); // Count preserved
    }

    [Fact]
    public async Task Test09_CancelEDocument_NotSentOrAccepted()
    {
        // Arrange
        var invoiceId = await CreateIssuedInvoiceAsync();
        
        var edoc = await _edocumentService.CreateAsync(new CreateEDocumentDto(invoiceId, "EARCHIVE"));

        // Act
        var result = await _edocumentService.CancelAsync(edoc.Id);

        // Assert
        Assert.Equal("CANCELLED", result.Status);
    }

    [Fact]
    public async Task Test10_CancelEDocument_StatusHistoryUpdated()
    {
        // Arrange
        var invoiceId = await CreateIssuedInvoiceAsync();
        
        var edoc = await _edocumentService.CreateAsync(new CreateEDocumentDto(invoiceId, "EARCHIVE"));

        // Act
        await _edocumentService.CancelAsync(edoc.Id);
        var withHistory = await _edocumentService.GetByIdWithHistoryAsync(edoc.Id);

        // Assert
        Assert.NotNull(withHistory);
        Assert.Equal(2, withHistory.StatusHistory.Count);
        Assert.Equal("CANCELLED", withHistory.StatusHistory[0].Status); // Latest first
        Assert.Equal("DRAFT", withHistory.StatusHistory[1].Status);
    }

    [Fact]
    public async Task Test11_UblGenerator_GeneratesValidXml()
    {
        // Arrange
        var invoiceId = await CreateIssuedInvoiceAsync();
        
        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .FirstAsync(i => i.Id == invoiceId);

        var supplier = await _context.Parties.FirstAsync(p => p.Id == _partyId);
        var customer = supplier; // Same for test
        var branch = await _context.Branches.FirstAsync(b => b.Id == _branchId);

        // Act
        var xml = _ublGenerator.GenerateInvoiceXml(invoice, supplier, customer, branch);

        // Assert
        Assert.NotNull(xml);
        Assert.Contains("<Invoice", xml);
        Assert.Contains("urn:oasis:names:specification:ubl", xml);
        Assert.Contains(invoice.InvoiceNo, xml);
        Assert.Contains("SATIS", xml); // Invoice type
        Assert.Contains("<cbc:UBLVersionID>2.1</cbc:UBLVersionID>", xml);
    }

    [Fact]
    public async Task Test12_TenantIsolation_CannotAccessOtherTenantDocs()
    {
        // Arrange
        var invoiceId = await CreateIssuedInvoiceAsync();
        
        var dto = new CreateEDocumentDto(invoiceId, "EARCHIVE");
        var edoc = await _edocumentService.CreateAsync(dto);

        // Create another tenant context
        var otherTenantId = Guid.NewGuid();
        var mockOtherContext = new Mock<ITenantContext>();
        mockOtherContext.Setup(x => x.TenantId).Returns(otherTenantId);
        mockOtherContext.Setup(x => x.UserId).Returns(_userId);

        var otherService = new EDocumentService(_context, mockOtherContext.Object);

        // Act
        var result = await otherService.GetByIdAsync(edoc.Id);

        // Assert
        Assert.Null(result); // Should not find document from another tenant
    }

    public void Dispose()
    {
        _context?.Dispose();
        _dbFactory?.Dispose();
    }
}
