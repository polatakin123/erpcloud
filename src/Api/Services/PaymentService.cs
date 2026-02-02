using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IPaymentService
{
    Task<PaymentDto> CreateAsync(CreatePaymentDto dto);
    Task<PaymentDto?> GetByIdAsync(Guid id);
    Task<PaymentListDto> SearchAsync(PaymentSearchDto search);
}

public class PaymentService : IPaymentService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public PaymentService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<PaymentDto> CreateAsync(CreatePaymentDto dto)
    {
        var tenantId = _tenantContext.TenantId;

        // Check uniqueness
        var exists = await _context.Set<Payment>()
            .AnyAsync(p => p.TenantId == tenantId && p.PaymentNo == dto.PaymentNo.Trim().ToUpperInvariant());

        if (exists)
        {
            throw new InvalidOperationException($"Payment number '{dto.PaymentNo}' already exists");
        }

        // Validate references
        await ValidateReferencesAsync(dto.PartyId, dto.BranchId);

        // Validate source (if provided)
        if (!string.IsNullOrWhiteSpace(dto.SourceType) && dto.SourceId.HasValue)
        {
            await ValidateSourceAsync(dto.SourceType, dto.SourceId.Value, dto.Currency ?? "TRY");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PaymentNo = dto.PaymentNo.Trim().ToUpperInvariant(),
                PartyId = dto.PartyId,
                BranchId = dto.BranchId,
                Date = dto.Date.Date,
                Direction = dto.Direction.ToUpperInvariant(),
                Method = dto.Method.ToUpperInvariant(),
                Currency = (dto.Currency ?? "TRY").ToUpperInvariant(),
                Amount = dto.Amount,
                Note = dto.Note?.Trim(),
                SourceType = dto.SourceType?.ToUpperInvariant(),
                SourceId = dto.SourceId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _tenantContext.UserId ?? Guid.Empty
            };

            _context.Set<Payment>().Add(payment);

            // Create party ledger entry
            await CreatePartyLedgerEntryAsync(payment);

            // Create cash/bank ledger entry (if source is specified)
            if (!string.IsNullOrWhiteSpace(payment.SourceType) && payment.SourceId.HasValue)
            {
                await CreateCashBankLedgerEntryAsync(payment);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetByIdRequiredAsync(payment.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PaymentDto?> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;

        var payment = await _context.Set<Payment>()
            .Include(p => p.Party)
            .Include(p => p.Branch)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (payment == null)
        {
            return null;
        }

        return MapToDto(payment);
    }

    public async Task<PaymentListDto> SearchAsync(PaymentSearchDto search)
    {
        var tenantId = _tenantContext.TenantId;

        var query = _context.Set<Payment>()
            .Include(p => p.Party)
            .Include(p => p.Branch)
            .Where(p => p.TenantId == tenantId);

        // Filter by search term (PaymentNo)
        if (!string.IsNullOrWhiteSpace(search.Q))
        {
            var searchTerm = search.Q.ToUpperInvariant();
            query = query.Where(p => p.PaymentNo.Contains(searchTerm));
        }

        // Filter by party
        if (search.PartyId.HasValue)
        {
            query = query.Where(p => p.PartyId == search.PartyId.Value);
        }

        // Filter by direction
        if (!string.IsNullOrWhiteSpace(search.Direction))
        {
            query = query.Where(p => p.Direction == search.Direction.ToUpperInvariant());
        }

        // Filter by date range
        if (search.From.HasValue)
        {
            query = query.Where(p => p.Date >= search.From.Value.Date);
        }

        if (search.To.HasValue)
        {
            query = query.Where(p => p.Date <= search.To.Value.Date);
        }

        // Filter by unallocated amount (payments with remaining balance to allocate)
        if (search.UnallocatedOnly)
        {
            query = query.Where(p => p.UnallocatedAmount > 0);
        }

        // Count total
        var totalCount = await query.CountAsync();

        // Apply pagination
        var pageSize = Math.Min(search.Size, 200);
        var payments = await query
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.PaymentNo)
            .Skip((search.Page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = payments.Select(MapToDto).ToList();

        return new PaymentListDto(items, totalCount, search.Page, pageSize);
    }

    // ==================== PRIVATE HELPERS ====================

    private async Task<PaymentDto> GetByIdRequiredAsync(Guid id)
    {
        var dto = await GetByIdAsync(id);
        if (dto == null)
        {
            throw new InvalidOperationException("Payment not found");
        }
        return dto;
    }

    private async Task ValidateReferencesAsync(Guid partyId, Guid branchId)
    {
        var tenantId = _tenantContext.TenantId;

        var partyExists = await _context.Parties.AnyAsync(p => p.Id == partyId && p.TenantId == tenantId);
        if (!partyExists)
        {
            throw new InvalidOperationException("Party not found");
        }

        var branchExists = await _context.Branches.AnyAsync(b => b.Id == branchId && b.TenantId == tenantId);
        if (!branchExists)
        {
            throw new InvalidOperationException("Branch not found");
        }
    }

    private async Task ValidateSourceAsync(string sourceType, Guid sourceId, string currency)
    {
        var tenantId = _tenantContext.TenantId;
        var normalizedType = sourceType.ToUpperInvariant();
        var normalizedCurrency = currency.ToUpperInvariant();

        if (normalizedType == "CASHBOX")
        {
            var cashbox = await _context.Cashboxes.FindAsync(sourceId);
            if (cashbox == null || cashbox.TenantId != tenantId)
            {
                throw new InvalidOperationException("Cashbox not found");
            }

            if (cashbox.Currency != normalizedCurrency)
            {
                throw new InvalidOperationException($"Currency mismatch: payment is {normalizedCurrency} but cashbox is {cashbox.Currency}");
            }

            if (!cashbox.IsActive)
            {
                throw new InvalidOperationException("Cashbox is not active");
            }
        }
        else if (normalizedType == "BANK")
        {
            var bankAccount = await _context.BankAccounts.FindAsync(sourceId);
            if (bankAccount == null || bankAccount.TenantId != tenantId)
            {
                throw new InvalidOperationException("Bank account not found");
            }

            if (bankAccount.Currency != normalizedCurrency)
            {
                throw new InvalidOperationException($"Currency mismatch: payment is {normalizedCurrency} but bank account is {bankAccount.Currency}");
            }

            if (!bankAccount.IsActive)
            {
                throw new InvalidOperationException("Bank account is not active");
            }
        }
        else
        {
            throw new InvalidOperationException($"Invalid source type: {sourceType}. Must be CASHBOX or BANK");
        }
    }

    private async Task CreatePartyLedgerEntryAsync(Payment payment)
    {
        var tenantId = _tenantContext.TenantId;

        // Check idempotency
        var exists = await _context.Set<PartyLedgerEntry>()
            .AnyAsync(e => e.TenantId == tenantId 
                && e.SourceType == "PAYMENT" 
                && e.SourceId == payment.Id);

        if (exists)
        {
            // Idempotent: entry already created, skip
            return;
        }

        // Calculate signed amount based on direction
        decimal amountSigned;
        if (payment.Direction == "IN")
        {
            // Payment received: reduces party receivable (-)
            amountSigned = -payment.Amount;
        }
        else // OUT
        {
            // Payment paid: reduces party payable (+)
            amountSigned = payment.Amount;
        }

        var entry = new PartyLedgerEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PartyId = payment.PartyId,
            BranchId = payment.BranchId,
            OccurredAt = DateTime.UtcNow,
            SourceType = "PAYMENT",
            SourceId = payment.Id,
            Description = $"Payment {payment.Direction} {payment.PaymentNo}",
            AmountSigned = amountSigned,
            Currency = payment.Currency,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.Set<PartyLedgerEntry>().Add(entry);
    }

    private async Task CreateCashBankLedgerEntryAsync(Payment payment)
    {
        var tenantId = _tenantContext.TenantId;

        // Check idempotency: unique (TenantId, PaymentId)
        var exists = await _context.CashBankLedgerEntries
            .AnyAsync(e => e.TenantId == tenantId && e.PaymentId == payment.Id);

        if (exists)
        {
            // Idempotent: entry already created, skip
            return;
        }

        // Calculate signed amount based on direction
        decimal amountSigned;
        if (payment.Direction == "IN")
        {
            // Payment received: increases cashbox/bank balance (+)
            amountSigned = payment.Amount;
        }
        else // OUT
        {
            // Payment paid: decreases cashbox/bank balance (-)
            amountSigned = -payment.Amount;
        }

        var entry = new CashBankLedgerEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OccurredAt = payment.Date,
            SourceType = payment.SourceType!,
            SourceId = payment.SourceId!.Value,
            PaymentId = payment.Id,
            Description = $"Payment {payment.Direction} {payment.PaymentNo}",
            AmountSigned = amountSigned,
            Currency = payment.Currency,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.CashBankLedgerEntries.Add(entry);
    }

    private static PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto(
            Id: payment.Id,
            PaymentNo: payment.PaymentNo,
            PartyId: payment.PartyId,
            PartyName: payment.Party.Name,
            BranchId: payment.BranchId,
            BranchName: payment.Branch.Name,
            Date: payment.Date,
            Direction: payment.Direction,
            Method: payment.Method,
            Currency: payment.Currency,
            Amount: payment.Amount,
            Note: payment.Note,
            SourceType: payment.SourceType,
            SourceId: payment.SourceId,
            CreatedAt: payment.CreatedAt,
            AllocatedAmount: payment.AllocatedAmount,
            UnallocatedAmount: payment.UnallocatedAmount
        );
    }
}
