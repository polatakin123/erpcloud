using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpCloud.Api.Tests;

/// <summary>
/// Comprehensive tests for PaymentAllocationService auto-allocation functionality.
/// Tests: FIFO allocation, validation rules, idempotency, tenant isolation, transaction safety.
/// </summary>
public class PaymentAllocationServiceTests : IDisposable
{
    private readonly TestDbFactory _dbFactory;
    private readonly ErpDbContext _context;
    private readonly TestTenantContext _tenantContext;
    private readonly PaymentAllocationService _allocationService;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _orgId;
    private readonly Guid _branchId;
    private readonly Guid _partyId;

    public PaymentAllocationServiceTests()
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
        _partyId = SeedParty();

        // Create service
        _allocationService = new PaymentAllocationService(_context, _tenantContext);
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

    private Guid SeedParty()
    {
        var party = new Party
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = "PARTY001",
            Name = "Test Customer",
            Type = "CUSTOMER",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };
        _context.Parties.Add(party);
        _context.SaveChanges();
        return party.Id;
    }

    private Invoice CreateInvoice(decimal grandTotal, string currency = "TRY", string? invoiceNo = null, DateTime? issueDate = null)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            InvoiceNo = invoiceNo ?? $"INV-{Guid.NewGuid().ToString()[..8]}",
            Type = "SALES",
            BranchId = _branchId,
            PartyId = _partyId,
            IssueDate = issueDate ?? DateTime.UtcNow,
            Currency = currency,
            GrandTotal = grandTotal,
            PaidAmount = 0,
            OpenAmount = grandTotal,
            Status = "ISSUED",
            PaymentStatus = "OPEN",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };
        _context.Invoices.Add(invoice);
        _context.SaveChanges();
        return invoice;
    }

    private Payment CreatePayment(decimal amount, string currency = "TRY", string direction = "IN")
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PaymentNo = $"PAY-{Guid.NewGuid().ToString()[..8]}",
            PartyId = _partyId,
            BranchId = _branchId,
            Date = DateTime.UtcNow,
            Direction = direction,
            Method = "CASH",
            Currency = currency,
            Amount = amount,
            AllocatedAmount = 0,
            UnallocatedAmount = amount,
            SourceType = "CASHBOX",
            SourceId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };
        _context.Payments.Add(payment);
        _context.SaveChanges();
        return payment;
    }

    public void Dispose()
    {
        _context?.Dispose();
        _dbFactory?.Dispose();
    }

    // ==================== AUTO-ALLOCATE TESTS ====================

    [Fact]
    public async Task Test01_AutoAllocate_FullPayment_ClosesInvoice()
    {
        // Arrange: Payment 1000, Invoice 1000
        var invoice = CreateInvoice(1000m);
        var payment = CreatePayment(1000m);

        // Act
        var result = await _allocationService.AutoAllocateAsync(payment.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1000m, result.Value.AllocatedTotal);
        Assert.Equal(0m, result.Value.RemainingUnallocated);
        Assert.Single(result.Value.Allocations);

        // Verify invoice closed
        var updatedInvoice = await _context.Invoices.FindAsync(invoice.Id);
        Assert.NotNull(updatedInvoice);
        Assert.Equal(1000m, updatedInvoice.PaidAmount);
        Assert.Equal(0m, updatedInvoice.OpenAmount);
        Assert.Equal("PAID", updatedInvoice.PaymentStatus);

        // Verify payment fully allocated
        var updatedPayment = await _context.Payments.FindAsync(payment.Id);
        Assert.NotNull(updatedPayment);
        Assert.Equal(1000m, updatedPayment.AllocatedAmount);
        Assert.Equal(0m, updatedPayment.UnallocatedAmount);

        // Verify allocation record created
        var allocation = await _context.PaymentAllocations
            .FirstOrDefaultAsync(pa => pa.PaymentId == payment.Id && pa.InvoiceId == invoice.Id);
        Assert.NotNull(allocation);
        Assert.Equal(1000m, allocation.Amount);
    }

    [Fact]
    public async Task Test02_AutoAllocate_PartialPayment_SetsPartialStatus()
    {
        // Arrange: Payment 500, Invoice 1000
        var invoice = CreateInvoice(1000m);
        var payment = CreatePayment(500m);

        // Act
        var result = await _allocationService.AutoAllocateAsync(payment.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(500m, result.Value.AllocatedTotal);
        Assert.Equal(0m, result.Value.RemainingUnallocated);

        // Verify invoice partial status
        var updatedInvoice = await _context.Invoices.FindAsync(invoice.Id);
        Assert.NotNull(updatedInvoice);
        Assert.Equal(500m, updatedInvoice.PaidAmount);
        Assert.Equal(500m, updatedInvoice.OpenAmount);
        Assert.Equal("PARTIAL", updatedInvoice.PaymentStatus);

        // Verify payment fully allocated
        var updatedPayment = await _context.Payments.FindAsync(payment.Id);
        Assert.NotNull(updatedPayment);
        Assert.Equal(500m, updatedPayment.AllocatedAmount);
        Assert.Equal(0m, updatedPayment.UnallocatedAmount);
    }

    [Fact]
    public async Task Test03_AutoAllocate_OverPayment_AllocatesExactly()
    {
        // Arrange: Payment 1500, Invoice 1000 (when invoiceIds specified, should not over-allocate)
        var invoice = CreateInvoice(1000m);
        var payment = CreatePayment(1500m);

        // Act - try to allocate to specific invoice
        var result = await _allocationService.AutoAllocateAsync(payment.Id, new List<Guid> { invoice.Id });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1000m, result.Value.AllocatedTotal);
        Assert.Equal(500m, result.Value.RemainingUnallocated);

        // Verify invoice fully paid
        var updatedInvoice = await _context.Invoices.FindAsync(invoice.Id);
        Assert.NotNull(updatedInvoice);
        Assert.Equal(1000m, updatedInvoice.PaidAmount);
        Assert.Equal(0m, updatedInvoice.OpenAmount);
        Assert.Equal("PAID", updatedInvoice.PaymentStatus);

        // Verify payment partially allocated
        var updatedPayment = await _context.Payments.FindAsync(payment.Id);
        Assert.NotNull(updatedPayment);
        Assert.Equal(1000m, updatedPayment.AllocatedAmount);
        Assert.Equal(500m, updatedPayment.UnallocatedAmount);
    }

    [Fact]
    public async Task Test04_AutoAllocate_CalledTwice_Idempotent()
    {
        // Arrange: Payment 1000, Invoice 1000
        var invoice = CreateInvoice(1000m);
        var payment = CreatePayment(1000m);

        // Act - call twice
        var result1 = await _allocationService.AutoAllocateAsync(payment.Id, new List<Guid> { invoice.Id });
        var result2 = await _allocationService.AutoAllocateAsync(payment.Id, new List<Guid> { invoice.Id });

        // Assert - both succeed
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);

        // Second call should return 0 allocated (already done)
        Assert.Equal(1000m, result1.Value!.AllocatedTotal);
        Assert.Equal(0m, result2.Value!.AllocatedTotal);
        Assert.Equal(0m, result2.Value!.RemainingUnallocated);

        // Verify only one allocation record exists
        var allocationCount = await _context.PaymentAllocations
            .CountAsync(pa => pa.PaymentId == payment.Id && pa.InvoiceId == invoice.Id);
        Assert.Equal(1, allocationCount);

        // Verify amounts still correct
        var updatedInvoice = await _context.Invoices.FindAsync(invoice.Id);
        Assert.Equal(1000m, updatedInvoice!.PaidAmount);
        Assert.Equal("PAID", updatedInvoice.PaymentStatus);
    }

    [Fact]
    public async Task Test05_AutoAllocate_DifferentTenant_NotFound()
    {
        // Arrange: Create invoice/payment in different tenant
        var otherTenantId = Guid.NewGuid();
        var otherTenantContext = new TestTenantContext
        {
            TenantId = otherTenantId,
            UserId = _userId,
            IsBypassEnabled = true
        };
        var otherContext = _dbFactory.CreateContext(otherTenantContext);

        // Create org/branch/party in other tenant
        var otherOrg = new Organization
        {
            Id = Guid.NewGuid(),
            TenantId = otherTenantId,
            Code = "ORG-OTHER",
            Name = "Other Org",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };
        otherContext.Organizations.Add(otherOrg);
        otherContext.SaveChanges();

        var otherBranch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = otherTenantId,
            OrganizationId = otherOrg.Id,
            Code = "BR-OTHER",
            Name = "Other Branch",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };
        otherContext.Branches.Add(otherBranch);
        otherContext.SaveChanges();

        var otherParty = new Party
        {
            Id = Guid.NewGuid(),
            TenantId = otherTenantId,
            Code = "PARTY-OTHER",
            Name = "Other Party",
            Type = "CUSTOMER",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };
        otherContext.Parties.Add(otherParty);
        otherContext.SaveChanges();

        var otherInvoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = otherTenantId,
            InvoiceNo = "INV-OTHER",
            Type = "SALES",
            BranchId = otherBranch.Id,
            PartyId = otherParty.Id,
            IssueDate = DateTime.UtcNow,
            Currency = "TRY",
            GrandTotal = 1000m,
            PaidAmount = 0,
            OpenAmount = 1000m,
            Status = "ISSUED",
            PaymentStatus = "OPEN",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };
        otherContext.Invoices.Add(otherInvoice);
        otherContext.SaveChanges();

        // Create payment in current tenant
        var payment = CreatePayment(1000m);

        // Act - try to allocate to invoice in other tenant
        var result = await _allocationService.AutoAllocateAsync(payment.Id, new List<Guid> { otherInvoice.Id });

        // Assert - invoice not found (tenant isolation)
        Assert.False(result.IsSuccess);
        Assert.Equal("invoice_not_found", result.Error);

        otherContext.Dispose();
    }

    [Fact]
    public async Task Test06_AutoAllocate_CurrencyMismatch_ReturnsConflict()
    {
        // Arrange: Payment USD, Invoice TRY
        var invoice = CreateInvoice(1000m, "TRY");
        var payment = CreatePayment(1000m, "USD");

        // Act
        var result = await _allocationService.AutoAllocateAsync(payment.Id, new List<Guid> { invoice.Id });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("currency_mismatch", result.Error);

        // Verify no allocation created
        var allocationCount = await _context.PaymentAllocations
            .CountAsync(pa => pa.PaymentId == payment.Id);
        Assert.Equal(0, allocationCount);
    }

    [Fact]
    public async Task Test07_AutoAllocate_NoIds_AllocatesOldestFirst_FIFO()
    {
        // Arrange: Payment 1500, Invoices [1000 (2024-01-01), 800 (2024-01-15)]
        var oldInvoice = CreateInvoice(1000m, issueDate: new DateTime(2024, 1, 1));
        var newInvoice = CreateInvoice(800m, issueDate: new DateTime(2024, 1, 15));
        var payment = CreatePayment(1500m);

        // Act - no invoice IDs specified, should allocate FIFO
        var result = await _allocationService.AutoAllocateAsync(payment.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1500m, result.Value.AllocatedTotal);
        Assert.Equal(0m, result.Value.RemainingUnallocated);
        Assert.Equal(2, result.Value.Allocations.Count);

        // Verify oldest invoice fully paid first
        var updatedOldInvoice = await _context.Invoices.FindAsync(oldInvoice.Id);
        Assert.NotNull(updatedOldInvoice);
        Assert.Equal(1000m, updatedOldInvoice.PaidAmount);
        Assert.Equal(0m, updatedOldInvoice.OpenAmount);
        Assert.Equal("PAID", updatedOldInvoice.PaymentStatus);

        // Verify newer invoice partially paid with remainder
        var updatedNewInvoice = await _context.Invoices.FindAsync(newInvoice.Id);
        Assert.NotNull(updatedNewInvoice);
        Assert.Equal(500m, updatedNewInvoice.PaidAmount);
        Assert.Equal(300m, updatedNewInvoice.OpenAmount);
        Assert.Equal("PARTIAL", updatedNewInvoice.PaymentStatus);

        // Verify allocations in correct order
        var allocations = result.Value.Allocations.OrderBy(a => a.Amount).ToList();
        Assert.Equal(500m, allocations[0].Amount); // Partial to new invoice
        Assert.Equal(1000m, allocations[1].Amount); // Full to old invoice
    }

    [Fact]
    public async Task Test08_AutoAllocate_ZeroUnallocated_NoOp()
    {
        // Arrange: Payment already fully allocated
        var invoice = CreateInvoice(1000m);
        var payment = CreatePayment(1000m);

        // First allocation
        await _allocationService.AutoAllocateAsync(payment.Id, new List<Guid> { invoice.Id });

        // Act - try to allocate again
        var result = await _allocationService.AutoAllocateAsync(payment.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.AllocatedTotal);
        Assert.Equal(0m, result.Value.RemainingUnallocated);
        Assert.Empty(result.Value.Allocations);
    }

    [Fact]
    public async Task Test09_AutoAllocate_AllocationRecords_CreatedCorrectly()
    {
        // Arrange: Payment 1000, Invoice 1000
        var invoice = CreateInvoice(1000m);
        var payment = CreatePayment(1000m);

        // Act
        var result = await _allocationService.AutoAllocateAsync(payment.Id, new List<Guid> { invoice.Id });

        // Assert
        Assert.True(result.IsSuccess);

        // Verify allocation record fields
        var allocation = await _context.PaymentAllocations
            .FirstOrDefaultAsync(pa => pa.PaymentId == payment.Id && pa.InvoiceId == invoice.Id);
        
        Assert.NotNull(allocation);
        Assert.Equal(payment.Id, allocation.PaymentId);
        Assert.Equal(invoice.Id, allocation.InvoiceId);
        Assert.Equal(1000m, allocation.Amount);
        Assert.Equal(_tenantId, allocation.TenantId);
        Assert.Equal(_userId, allocation.CreatedBy);
        Assert.True(allocation.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
        Assert.Contains("Otomatik eşleştirme", allocation.Note);
    }

    [Fact]
    public async Task Test10_AutoAllocate_DirectionTypeMismatch_ReturnsConflict()
    {
        // Arrange: Payment IN (receipt), Invoice PURCHASE (should only match SALES)
        var purchaseInvoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            InvoiceNo = "INV-PURCHASE",
            Type = "PURCHASE",
            BranchId = _branchId,
            PartyId = _partyId,
            IssueDate = DateTime.UtcNow,
            Currency = "TRY",
            GrandTotal = 1000m,
            PaidAmount = 0,
            OpenAmount = 1000m,
            Status = "ISSUED",
            PaymentStatus = "OPEN",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };
        _context.Invoices.Add(purchaseInvoice);
        _context.SaveChanges();

        var paymentIn = CreatePayment(1000m, direction: "IN");

        // Act
        var result = await _allocationService.AutoAllocateAsync(paymentIn.Id, new List<Guid> { purchaseInvoice.Id });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("direction_type_mismatch", result.Error);

        // Verify no allocation created
        var allocationCount = await _context.PaymentAllocations
            .CountAsync(pa => pa.PaymentId == paymentIn.Id);
        Assert.Equal(0, allocationCount);
    }

    [Fact]
    public async Task Test11_AutoAllocate_PartyMismatch_Filtered()
    {
        // Arrange: Payment for Party A, Invoice for Party B
        var partyB = new Party
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = "PARTY-B",
            Name = "Different Party",
            Type = "CUSTOMER",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };
        _context.Parties.Add(partyB);
        _context.SaveChanges();

        var invoicePartyB = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            InvoiceNo = "INV-PARTY-B",
            Type = "SALES",
            BranchId = _branchId,
            PartyId = partyB.Id,
            IssueDate = DateTime.UtcNow,
            Currency = "TRY",
            GrandTotal = 1000m,
            PaidAmount = 0,
            OpenAmount = 1000m,
            Status = "ISSUED",
            PaymentStatus = "OPEN",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };
        _context.Invoices.Add(invoicePartyB);
        _context.SaveChanges();

        var payment = CreatePayment(1000m); // Uses _partyId (Party A)

        // Act - try to allocate explicitly to Party B invoice
        var result = await _allocationService.AutoAllocateAsync(payment.Id, new List<Guid> { invoicePartyB.Id });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("party_mismatch", result.Error);
    }

    [Fact]
    public async Task Test12_AutoAllocate_TransactionRollback_OnError()
    {
        // Arrange: Create invoice then delete it to force error mid-transaction
        var invoice = CreateInvoice(1000m);
        var payment = CreatePayment(1000m);

        // Delete invoice to cause error
        _context.Invoices.Remove(invoice);
        _context.SaveChanges();

        // Act
        var result = await _allocationService.AutoAllocateAsync(payment.Id, new List<Guid> { invoice.Id });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("invoice_not_found", result.Error);

        // Verify payment unchanged (transaction rolled back)
        var updatedPayment = await _context.Payments.FindAsync(payment.Id);
        Assert.NotNull(updatedPayment);
        Assert.Equal(0m, updatedPayment.AllocatedAmount);
        Assert.Equal(1000m, updatedPayment.UnallocatedAmount);

        // Verify no allocation records created
        var allocationCount = await _context.PaymentAllocations
            .CountAsync(pa => pa.PaymentId == payment.Id);
        Assert.Equal(0, allocationCount);
    }
}
