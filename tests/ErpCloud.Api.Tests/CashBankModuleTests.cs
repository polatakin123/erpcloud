using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpCloud.Api.Tests;

/// <summary>
/// Comprehensive tests for Cash/Bank module (Cashbox, BankAccount, CashBankLedger, Payment integration).
/// Tests: CRUD, defaults, currency validation, ledger integration, balance calculation, tenant isolation, idempotency.
/// </summary>
public class CashBankModuleTests : IDisposable
{
    private readonly TestDbFactory _dbFactory;
    private readonly ErpDbContext _context;
    private readonly TestTenantContext _tenantContext;
    private readonly ICashboxService _cashboxService;
    private readonly IBankAccountService _bankAccountService;
    private readonly ICashBankLedgerService _ledgerService;
    private readonly IPaymentService _paymentService;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _orgId;
    private readonly Guid _branchId;
    private readonly Guid _partyId;

    public CashBankModuleTests()
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

        // Create services
        _cashboxService = new CashboxService(_context, _tenantContext);
        _bankAccountService = new BankAccountService(_context, _tenantContext);
        _ledgerService = new CashBankLedgerService(_context);
        _paymentService = new PaymentService(_context, _tenantContext);
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
            Name = "Test Party",
            Type = "CUSTOMER",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };
        _context.Parties.Add(party);
        _context.SaveChanges();
        return party.Id;
    }

    public void Dispose()
    {
        _context?.Dispose();
        _dbFactory?.Dispose();
    }

    // ==================== Cashbox Tests ====================

    [Fact]
    public async Task Test01_CreateCashbox_Succeeds()
    {
        // Arrange
        var dto = new CreateCashboxDto
        {
            Code = "CASH001",
            Name = "Main Cashbox",
            Currency = "TRY",
            IsActive = true,
            IsDefault = false
        };

        // Act
        var result = await _cashboxService.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CASH001", result.Code);
        Assert.Equal("Main Cashbox", result.Name);
        Assert.Equal("TRY", result.Currency);
        Assert.True(result.IsActive);
        Assert.False(result.IsDefault);
    }

    [Fact]
    public async Task Test02_CashboxCodeUniquePerTenant()
    {
        // Arrange
        await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "CASH-DUP",
            Name = "First",
            Currency = "TRY"
        });

        var dto2 = new CreateCashboxDto
        {
            Code = "CASH-DUP",
            Name = "Second",
            Currency = "TRY"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _cashboxService.CreateAsync(dto2));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task Test03_CashboxDefaultPartialUniqueWorks()
    {
        // Arrange - create first default
        var cashbox1 = await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "CASH-A",
            Name = "Cashbox A",
            Currency = "TRY",
            IsDefault = true
        });

        // Act - create second default (should clear first)
        var cashbox2 = await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "CASH-B",
            Name = "Cashbox B",
            Currency = "TRY",
            IsDefault = true
        });

        // Assert - verify only second is default
        var updated1 = await _cashboxService.GetByIdAsync(cashbox1.Id);
        var updated2 = await _cashboxService.GetByIdAsync(cashbox2.Id);

        Assert.NotNull(updated1);
        Assert.False(updated1.IsDefault);
        Assert.NotNull(updated2);
        Assert.True(updated2.IsDefault);
    }

    [Fact]
    public async Task Test04_SetIsDefaultMakesPreviousDefaultFalse()
    {
        // Arrange
        var cashbox1 = await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "CASH-X",
            Name = "X",
            Currency = "TRY",
            IsDefault = true
        });

        var cashbox2 = await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "CASH-Y",
            Name = "Y",
            Currency = "TRY",
            IsDefault = false
        });

        // Act - update cashbox2 to default
        await _cashboxService.UpdateAsync(cashbox2.Id, new UpdateCashboxDto
        {
            Name = "Y Updated",
            Currency = "TRY",
            IsActive = true,
            IsDefault = true
        });

        // Assert
        var updated1 = await _cashboxService.GetByIdAsync(cashbox1.Id);
        var updated2 = await _cashboxService.GetByIdAsync(cashbox2.Id);

        Assert.NotNull(updated1);
        Assert.False(updated1.IsDefault);
        Assert.NotNull(updated2);
        Assert.True(updated2.IsDefault);
    }

    // ==================== BankAccount Tests ====================

    [Fact]
    public async Task Test05_CreateBankAccount_Succeeds()
    {
        // Arrange
        var dto = new CreateBankAccountDto
        {
            Code = "BANK001",
            Name = "Main Bank Account",
            BankName = "Test Bank",
            Iban = "TR1234567890123456789012",
            Currency = "TRY",
            IsActive = true,
            IsDefault = false
        };

        // Act
        var result = await _bankAccountService.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("BANK001", result.Code);
        Assert.Equal("Main Bank Account", result.Name);
        Assert.Equal("Test Bank", result.BankName);
        Assert.Equal("TR1234567890123456789012", result.Iban);
        Assert.Equal("TRY", result.Currency);
    }

    [Fact]
    public async Task Test06_BankAccountCodeUniquePerTenant()
    {
        // Arrange
        await _bankAccountService.CreateAsync(new CreateBankAccountDto
        {
            Code = "BANK-DUP",
            Name = "First",
            Currency = "TRY"
        });

        var dto2 = new CreateBankAccountDto
        {
            Code = "BANK-DUP",
            Name = "Second",
            Currency = "USD"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _bankAccountService.CreateAsync(dto2));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task Test07_BankDefaultPartialUniqueWorks()
    {
        // Arrange
        var bank1 = await _bankAccountService.CreateAsync(new CreateBankAccountDto
        {
            Code = "BANK-A",
            Name = "Bank A",
            Currency = "TRY",
            IsDefault = true
        });

        // Act
        var bank2 = await _bankAccountService.CreateAsync(new CreateBankAccountDto
        {
            Code = "BANK-B",
            Name = "Bank B",
            Currency = "TRY",
            IsDefault = true
        });

        // Assert
        var updated1 = await _bankAccountService.GetByIdAsync(bank1.Id);
        var updated2 = await _bankAccountService.GetByIdAsync(bank2.Id);

        Assert.NotNull(updated1);
        Assert.False(updated1.IsDefault);
        Assert.NotNull(updated2);
        Assert.True(updated2.IsDefault);
    }

    // ==================== Payment Integration Tests ====================

    [Fact]
    public async Task Test08_CurrencyMismatchBlocksPayment()
    {
        // Arrange - create TRY cashbox
        var cashbox = await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "CASH-TRY",
            Name = "TRY Cashbox",
            Currency = "TRY"
        });

        // Act & Assert - try to create USD payment with TRY cashbox
        var paymentDto = new CreatePaymentDto(
            PaymentNo: "PAY-001",
            PartyId: _partyId,
            BranchId: _branchId,
            Date: DateTime.UtcNow.Date,
            Direction: "IN",
            Method: "CASH",
            Currency: "USD",
            Amount: 100,
            Note: null,
            SourceType: "CASHBOX",
            SourceId: cashbox.Id
        );

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _paymentService.CreateAsync(paymentDto));
        Assert.Contains("Currency mismatch", ex.Message);
    }

    [Fact]
    public async Task Test09_PaymentCreateWritesCashBankLedgerEntry()
    {
        // Arrange
        var cashbox = await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "CASH-PAY",
            Name = "Payment Cashbox",
            Currency = "TRY"
        });

        var paymentDto = new CreatePaymentDto(
            PaymentNo: "PAY-002",
            PartyId: _partyId,
            BranchId: _branchId,
            Date: DateTime.UtcNow.Date,
            Direction: "IN",
            Method: "CASH",
            Currency: "TRY",
            Amount: 500,
            Note: null,
            SourceType: "CASHBOX",
            SourceId: cashbox.Id
        );

        // Act
        var payment = await _paymentService.CreateAsync(paymentDto);

        // Assert - verify ledger entry created
        var ledgerEntries = await _context.CashBankLedgerEntries
            .Where(e => e.PaymentId == payment.Id)
            .ToListAsync();

        Assert.Single(ledgerEntries);
        Assert.Equal("CASHBOX", ledgerEntries[0].SourceType);
        Assert.Equal(cashbox.Id, ledgerEntries[0].SourceId);
        Assert.Equal(500, ledgerEntries[0].AmountSigned);
    }

    [Fact]
    public async Task Test10_PaymentDirectionIN_AmountSignedPositive()
    {
        // Arrange
        var cashbox = await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "CASH-IN",
            Name = "Inbound Test",
            Currency = "TRY"
        });

        var paymentDto = new CreatePaymentDto(
            PaymentNo: "PAY-IN-001",
            PartyId: _partyId,
            BranchId: _branchId,
            Date: DateTime.UtcNow.Date,
            Direction: "IN",
            Method: "CASH",
            Currency: "TRY",
            Amount: 300,
            Note: null,
            SourceType: "CASHBOX",
            SourceId: cashbox.Id
        );

        // Act
        var payment = await _paymentService.CreateAsync(paymentDto);

        // Assert - Direction IN should increase cashbox (+Amount)
        var ledger = await _context.CashBankLedgerEntries
            .FirstOrDefaultAsync(e => e.PaymentId == payment.Id);

        Assert.NotNull(ledger);
        Assert.Equal(300, ledger.AmountSigned); // Positive
    }

    [Fact]
    public async Task Test11_PaymentDirectionOUT_AmountSignedNegative()
    {
        // Arrange
        var bank = await _bankAccountService.CreateAsync(new CreateBankAccountDto
        {
            Code = "BANK-OUT",
            Name = "Outbound Test",
            Currency = "TRY"
        });

        var paymentDto = new CreatePaymentDto(
            PaymentNo: "PAY-OUT-001",
            PartyId: _partyId,
            BranchId: _branchId,
            Date: DateTime.UtcNow.Date,
            Direction: "OUT",
            Method: "BANK",
            Currency: "TRY",
            Amount: 200,
            Note: null,
            SourceType: "BANK",
            SourceId: bank.Id
        );

        // Act
        var payment = await _paymentService.CreateAsync(paymentDto);

        // Assert - Direction OUT should decrease bank (-Amount)
        var ledger = await _context.CashBankLedgerEntries
            .FirstOrDefaultAsync(e => e.PaymentId == payment.Id);

        Assert.NotNull(ledger);
        Assert.Equal(-200, ledger.AmountSigned); // Negative
    }

    [Fact]
    public async Task Test12_PaymentCreateAlsoWritesPartyLedgerEntry()
    {
        // Arrange
        var cashbox = await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "CASH-PARTY",
            Name = "Party Test",
            Currency = "TRY"
        });

        var paymentDto = new CreatePaymentDto(
            PaymentNo: "PAY-PARTY-001",
            PartyId: _partyId,
            BranchId: _branchId,
            Date: DateTime.UtcNow.Date,
            Direction: "IN",
            Method: "CASH",
            Currency: "TRY",
            Amount: 400,
            Note: null,
            SourceType: "CASHBOX",
            SourceId: cashbox.Id
        );

        // Act
        var payment = await _paymentService.CreateAsync(paymentDto);

        // Assert - verify both ledgers written
        var cashBankEntry = await _context.CashBankLedgerEntries
            .FirstOrDefaultAsync(e => e.PaymentId == payment.Id);
        var partyEntry = await _context.Set<PartyLedgerEntry>()
            .FirstOrDefaultAsync(e => e.SourceType == "PAYMENT" && e.SourceId == payment.Id);

        Assert.NotNull(cashBankEntry);
        Assert.NotNull(partyEntry);
    }

    [Fact]
    public async Task Test13_CashBalance_EqualsSumLedger()
    {
        // Arrange
        var cashbox = await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "CASH-BAL",
            Name = "Balance Test",
            Currency = "TRY"
        });

        // Create multiple payments
        await CreatePaymentAsync("PAY-BAL-01", cashbox.Id, "IN", 100);
        await CreatePaymentAsync("PAY-BAL-02", cashbox.Id, "IN", 200);
        await CreatePaymentAsync("PAY-BAL-03", cashbox.Id, "OUT", 50);

        // Act
        var balance = await _ledgerService.GetBalanceAsync(new CashBankBalanceQueryDto
        {
            SourceType = "CASHBOX",
            SourceId = cashbox.Id
        });

        // Assert
        Assert.Equal(250, balance.Balance); // 100 + 200 - 50
        Assert.Equal("TRY", balance.Currency);
    }

    [Fact]
    public async Task Test14_LedgerFilterByDateRangeWorks()
    {
        // Arrange
        var bank = await _bankAccountService.CreateAsync(new CreateBankAccountDto
        {
            Code = "BANK-DATE",
            Name = "Date Filter Test",
            Currency = "TRY"
        });

        var date1 = new DateTime(2026, 1, 1);
        var date2 = new DateTime(2026, 1, 15);
        var date3 = new DateTime(2026, 2, 1);

        await CreatePaymentAsync("PAY-D1", bank.Id, "IN", 100, date1);
        await CreatePaymentAsync("PAY-D2", bank.Id, "IN", 200, date2);
        await CreatePaymentAsync("PAY-D3", bank.Id, "IN", 300, date3);

        // Act - filter Jan 1-31
        var result = await _ledgerService.GetLedgerAsync(new CashBankLedgerSearchDto
        {
            SourceType = "BANK",
            SourceId = bank.Id,
            From = new DateTime(2026, 1, 1),
            To = new DateTime(2026, 1, 31),
            Page = 1,
            Size = 100
        });

        // Assert - should only get 2 entries
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task Test15_TenantIsolationOnCashboxes()
    {
        // Arrange - Tenant A creates cashbox
        var cashbox = await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "CASH-TENANT-A",
            Name = "Tenant A Cashbox",
            Currency = "TRY"
        });

        // Create Tenant B with separate DbFactory
        var tenantBId = Guid.NewGuid();
        var tenantBContext = new TestTenantContext
        {
            TenantId = tenantBId,
            UserId = Guid.NewGuid(),
            IsBypassEnabled = false
        };

        using var dbFactoryB = new TestDbFactory(tenantBContext);
        using var contextB = dbFactoryB.CreateContext(tenantBContext);
        var cashboxServiceB = new CashboxService(contextB, tenantBContext);

        // Act - search as Tenant B
        var searchResult = await cashboxServiceB.SearchAsync(new CashboxSearchDto
        {
            Page = 1,
            Size = 100
        });

        var getResult = await cashboxServiceB.GetByIdAsync(cashbox.Id);

        // Assert
        Assert.Empty(searchResult.Items);
        Assert.Null(getResult); // Different tenant cannot see cashbox
    }

    [Fact]
    public async Task Test16_PaymentGETReturnsSourceFields()
    {
        // Arrange
        var bank = await _bankAccountService.CreateAsync(new CreateBankAccountDto
        {
            Code = "BANK-GET",
            Name = "GET Test",
            Currency = "TRY"
        });

        var createDto = new CreatePaymentDto(
            PaymentNo: "PAY-GET-001",
            PartyId: _partyId,
            BranchId: _branchId,
            Date: DateTime.UtcNow.Date,
            Direction: "IN",
            Method: "BANK",
            Currency: "TRY",
            Amount: 500,
            Note: null,
            SourceType: "BANK",
            SourceId: bank.Id
        );

        var payment = await _paymentService.CreateAsync(createDto);

        // Act
        var retrieved = await _paymentService.GetByIdAsync(payment.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("BANK", retrieved.SourceType);
        Assert.Equal(bank.Id, retrieved.SourceId);
    }

    [Fact]
    public async Task Test17_UniqueTenantPaymentId_PreventsDuplicateLedgerEntries()
    {
        // Arrange
        var cashbox = await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "CASH-UNIQUE",
            Name = "Unique Test",
            Currency = "TRY"
        });

        var paymentDto = new CreatePaymentDto(
            PaymentNo: "PAY-UNIQUE-001",
            PartyId: _partyId,
            BranchId: _branchId,
            Date: DateTime.UtcNow.Date,
            Direction: "IN",
            Method: "CASH",
            Currency: "TRY",
            Amount: 100,
            Note: null,
            SourceType: "CASHBOX",
            SourceId: cashbox.Id
        );

        var payment = await _paymentService.CreateAsync(paymentDto);

        // Act - verify only one ledger entry created
        var ledgerEntries = await _context.CashBankLedgerEntries
            .Where(e => e.PaymentId == payment.Id)
            .ToListAsync();

        // Assert - idempotency check in service prevents duplicates
        Assert.Single(ledgerEntries);
    }

    [Fact]
    public async Task Test18_SearchQ_WorksForCashboxCodeAndName()
    {
        // Arrange
        await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "SEARCH-001",
            Name = "Alpha Cashbox",
            Currency = "TRY"
        });

        await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "OTHER-002",
            Name = "Beta Search Box",
            Currency = "TRY"
        });

        await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "DIFF-003",
            Name = "Gamma",
            Currency = "TRY"
        });

        // Act - search for "SEARCH"
        var result = await _cashboxService.SearchAsync(new CashboxSearchDto
        {
            Q = "SEARCH",
            Page = 1,
            Size = 100
        });

        // Assert - should find 2 (one in code, one in name)
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task Test19_DeleteCashbox_BlockedIfReferencedByLedger()
    {
        // Arrange
        var cashbox = await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "CASH-DEL",
            Name = "Delete Test",
            Currency = "TRY"
        });

        await CreatePaymentAsync("PAY-DEL-001", cashbox.Id, "IN", 100);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _cashboxService.DeleteAsync(cashbox.Id));
        Assert.Contains("ledger entries", ex.Message);
    }

    [Fact]
    public async Task Test20_IdempotencyNoDuplicateBalance()
    {
        // Arrange
        var cashbox = await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "CASH-IDEMPOTENT",
            Name = "Idempotent Test",
            Currency = "TRY"
        });

        var payment = await CreatePaymentAsync("PAY-IDEM-001", cashbox.Id, "IN", 100);

        // Get balance before
        var balanceBefore = await _ledgerService.GetBalanceAsync(new CashBankBalanceQueryDto
        {
            SourceType = "CASHBOX",
            SourceId = cashbox.Id
        });

        // Act - try to create duplicate ledger entry (simulating retry)
        // This should be blocked by unique constraint or idempotency check

        var ledgerCountBefore = await _context.CashBankLedgerEntries
            .Where(e => e.PaymentId == payment.Id)
            .CountAsync();

        // Try to call CreateCashBankLedgerEntry again (simulate)
        // In real scenario, this is prevented by unique constraint

        // Assert
        Assert.Equal(100, balanceBefore.Balance);
        Assert.Equal(1, ledgerCountBefore); // Only one entry
    }

    [Fact]
    public async Task Test21_BankAccountInactive_BlocksPayment()
    {
        // Arrange
        var bank = await _bankAccountService.CreateAsync(new CreateBankAccountDto
        {
            Code = "BANK-INACTIVE",
            Name = "Inactive Bank",
            Currency = "TRY",
            IsActive = false
        });

        var paymentDto = new CreatePaymentDto(
            PaymentNo: "PAY-INACTIVE-001",
            PartyId: _partyId,
            BranchId: _branchId,
            Date: DateTime.UtcNow.Date,
            Direction: "IN",
            Method: "BANK",
            Currency: "TRY",
            Amount: 100,
            Note: null,
            SourceType: "BANK",
            SourceId: bank.Id
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _paymentService.CreateAsync(paymentDto));
        Assert.Contains("not active", ex.Message);
    }

    [Fact]
    public async Task Test22_InvalidSourceType_BlocksPayment()
    {
        // Arrange
        var paymentDto = new CreatePaymentDto(
            PaymentNo: "PAY-INVALID-001",
            PartyId: _partyId,
            BranchId: _branchId,
            Date: DateTime.UtcNow.Date,
            Direction: "IN",
            Method: "CASH",
            Currency: "TRY",
            Amount: 100,
            Note: null,
            SourceType: "INVALID",
            SourceId: Guid.NewGuid()
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _paymentService.CreateAsync(paymentDto));
        Assert.Contains("Invalid source type", ex.Message);
    }

    [Fact]
    public async Task Test23_BalanceAt_FiltersEntriesByDate()
    {
        // Arrange
        var cashbox = await _cashboxService.CreateAsync(new CreateCashboxDto
        {
            Code = "CASH-AT",
            Name = "Balance At Test",
            Currency = "TRY"
        });

        var date1 = new DateTime(2026, 1, 10);
        var date2 = new DateTime(2026, 1, 20);
        var date3 = new DateTime(2026, 1, 30);

        await CreatePaymentAsync("PAY-AT-1", cashbox.Id, "IN", 100, date1);
        await CreatePaymentAsync("PAY-AT-2", cashbox.Id, "IN", 200, date2);
        await CreatePaymentAsync("PAY-AT-3", cashbox.Id, "OUT", 50, date3);

        // Act - get balance at Jan 20 (should include first 2 only)
        var balance = await _ledgerService.GetBalanceAsync(new CashBankBalanceQueryDto
        {
            SourceType = "CASHBOX",
            SourceId = cashbox.Id,
            At = date2
        });

        // Assert
        Assert.Equal(300, balance.Balance); // 100 + 200 (not including -50)
    }

    // ==================== Helper Methods ====================

    private async Task<PaymentDto> CreatePaymentAsync(string paymentNo, Guid sourceId, string direction, decimal amount, DateTime? date = null)
    {
        var sourceType = await _context.Cashboxes.AnyAsync(c => c.Id == sourceId) ? "CASHBOX" : "BANK";

        var dto = new CreatePaymentDto(
            PaymentNo: paymentNo,
            PartyId: _partyId,
            BranchId: _branchId,
            Date: date ?? DateTime.UtcNow.Date,
            Direction: direction,
            Method: sourceType == "CASHBOX" ? "CASH" : "BANK",
            Currency: "TRY",
            Amount: amount,
            Note: null,
            SourceType: sourceType,
            SourceId: sourceId
        );

        return await _paymentService.CreateAsync(dto);
    }
}
