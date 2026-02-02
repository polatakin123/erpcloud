using ErpCloud.Api.Data;
using ErpCloud.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public class CashBankLedgerService : ICashBankLedgerService
{
    private readonly ErpDbContext _context;

    public CashBankLedgerService(ErpDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<CashBankLedgerDto>> GetLedgerAsync(CashBankLedgerSearchDto dto)
    {
        var query = _context.CashBankLedgerEntries
            .Include(e => e.Payment)
            .AsQueryable();

        // Filter by source type
        if (!string.IsNullOrWhiteSpace(dto.SourceType))
        {
            query = query.Where(e => e.SourceType == dto.SourceType.ToUpperInvariant());
        }

        // Filter by source ID
        if (dto.SourceId.HasValue)
        {
            query = query.Where(e => e.SourceId == dto.SourceId.Value);
        }

        // Filter by date range
        if (dto.From.HasValue)
        {
            query = query.Where(e => e.OccurredAt >= dto.From.Value);
        }

        if (dto.To.HasValue)
        {
            query = query.Where(e => e.OccurredAt <= dto.To.Value);
        }

        // Count total
        var total = await query.CountAsync();

        // Order by date descending
        query = query.OrderByDescending(e => e.OccurredAt);

        // Paginate
        var items = await query
            .Skip((dto.Page - 1) * dto.Size)
            .Take(dto.Size)
            .Select(e => new CashBankLedgerDto
            {
                Id = e.Id,
                OccurredAt = e.OccurredAt,
                SourceType = e.SourceType,
                SourceId = e.SourceId,
                PaymentId = e.PaymentId,
                PaymentNo = e.Payment != null ? e.Payment.PaymentNo : null,
                Description = e.Description,
                AmountSigned = e.AmountSigned,
                Currency = e.Currency
            })
            .ToListAsync();

        return new PagedResult<CashBankLedgerDto>(items, total, dto.Page, dto.Size);
    }

    public async Task<CashBankBalanceDto> GetBalanceAsync(CashBankBalanceQueryDto dto)
    {
        var query = _context.CashBankLedgerEntries
            .Where(e => e.SourceType == dto.SourceType.ToUpperInvariant() && 
                        e.SourceId == dto.SourceId);

        // Filter by date if specified
        if (dto.At.HasValue)
        {
            query = query.Where(e => e.OccurredAt <= dto.At.Value);
        }

        // Sum the signed amounts
        var balance = await query.SumAsync(e => e.AmountSigned);

        // Get currency from source
        string currency = "TRY";
        if (dto.SourceType.ToUpperInvariant() == "CASHBOX")
        {
            var cashbox = await _context.Cashboxes.FindAsync(dto.SourceId);
            if (cashbox != null)
                currency = cashbox.Currency;
        }
        else if (dto.SourceType.ToUpperInvariant() == "BANK")
        {
            var bankAccount = await _context.BankAccounts.FindAsync(dto.SourceId);
            if (bankAccount != null)
                currency = bankAccount.Currency;
        }

        return new CashBankBalanceDto
        {
            Balance = balance,
            Currency = currency
        };
    }
}
