using ErpCloud.Api.Data;
using ErpCloud.Api.Reports.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IReportsService
{
    Task<PagedReportResult<StockBalanceDto>> GetStockBalancesAsync(Guid warehouseId, string? q, int page, int size);
    Task<PagedReportResult<StockMovementDto>> GetStockMovementsAsync(Guid? warehouseId, Guid? variantId, string? movementType, DateTime? from, DateTime? to, int page, int size);
    Task<List<SalesSummaryDto>> GetSalesSummaryAsync(DateTime? from, DateTime? to, string groupBy);
    Task<List<SalesSummaryDto>> GetPurchaseSummaryAsync(DateTime? from, DateTime? to, string groupBy);
    Task<PagedReportResult<PartyBalanceDto>> GetPartyBalancesAsync(string? q, string? type, int page, int size, DateTime? at);
    Task<PagedReportResult<PartyAgingDto>> GetPartyAgingAsync(string? q, string? type, int page, int size, DateTime? at);
    Task<List<CashBankBalanceDto>> GetCashBankBalancesAsync(DateTime? at);
}

public class ReportsService : IReportsService
{
    private readonly ErpDbContext _context;

    public ReportsService(ErpDbContext context)
    {
        _context = context;
    }

    public async Task<PagedReportResult<StockBalanceDto>> GetStockBalancesAsync(Guid warehouseId, string? q, int page, int size)
    {
        var query = _context.StockBalances
            .AsNoTracking()
            .Where(sb => sb.WarehouseId == warehouseId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = q.ToLower();
            query = query.Where(sb => 
                sb.Variant.Sku.ToLower().Contains(searchTerm) ||
                sb.Variant.Name.ToLower().Contains(searchTerm));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(sb => sb.Variant.Sku)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(sb => new StockBalanceDto
            {
                VariantId = sb.VariantId,
                Sku = sb.Variant.Sku,
                VariantName = sb.Variant.Name,
                Unit = sb.Variant.Unit,
                OnHand = sb.OnHand,
                Reserved = sb.Reserved,
                Available = sb.Available
            })
            .ToListAsync();

        return new PagedReportResult<StockBalanceDto>
        {
            Page = page,
            Size = size,
            Total = total,
            Items = items
        };
    }

    public async Task<PagedReportResult<StockMovementDto>> GetStockMovementsAsync(
        Guid? warehouseId, Guid? variantId, string? movementType, DateTime? from, DateTime? to, int page, int size)
    {
        var query = _context.StockLedgerEntries.AsNoTracking();

        if (warehouseId.HasValue)
            query = query.Where(e => e.WarehouseId == warehouseId.Value);

        if (variantId.HasValue)
            query = query.Where(e => e.VariantId == variantId.Value);

        if (!string.IsNullOrWhiteSpace(movementType))
            query = query.Where(e => e.MovementType == movementType);

        if (from.HasValue)
            query = query.Where(e => e.OccurredAt >= from.Value);

        if (to.HasValue)
        {
            var toEndOfDay = to.Value.Date.AddDays(1);
            query = query.Where(e => e.OccurredAt < toEndOfDay);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(e => new StockMovementDto
            {
                OccurredAt = e.OccurredAt,
                MovementType = e.MovementType,
                Quantity = e.Quantity,
                ReferenceType = e.ReferenceType ?? string.Empty,
                ReferenceId = e.ReferenceId ?? Guid.Empty,
                Note = e.Note ?? string.Empty
            })
            .ToListAsync();

        return new PagedReportResult<StockMovementDto>
        {
            Page = page,
            Size = size,
            Total = total,
            Items = items
        };
    }

    public async Task<List<SalesSummaryDto>> GetSalesSummaryAsync(DateTime? from, DateTime? to, string groupBy)
    {
        var query = _context.Invoices
            .AsNoTracking()
            .Where(i => i.Type == "SALES" && i.Status == "ISSUED");

        if (from.HasValue)
            query = query.Where(i => i.IssueDate >= from.Value);

        if (to.HasValue)
        {
            var toEndOfDay = to.Value.Date.AddDays(1);
            query = query.Where(i => i.IssueDate < toEndOfDay);
        }

        if (groupBy?.ToUpper() == "MONTH")
        {
            var grouped = await query
                .GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month })
                .Select(g => new SalesSummaryDto
                {
                    Period = $"{g.Key.Year:0000}-{g.Key.Month:00}",
                    InvoiceCount = g.Count(),
                    TotalNet = g.Sum(i => i.Subtotal),
                    TotalVat = g.Sum(i => i.VatTotal),
                    TotalGross = g.Sum(i => i.GrandTotal)
                })
                .OrderBy(s => s.Period)
                .ToListAsync();

            return grouped;
        }
        else // DAY
        {
            var grouped = await query
                .GroupBy(i => i.IssueDate.Date)
                .Select(g => new SalesSummaryDto
                {
                    Period = g.Key.ToString("yyyy-MM-dd"),
                    InvoiceCount = g.Count(),
                    TotalNet = g.Sum(i => i.Subtotal),
                    TotalVat = g.Sum(i => i.VatTotal),
                    TotalGross = g.Sum(i => i.GrandTotal)
                })
                .OrderBy(s => s.Period)
                .ToListAsync();

            return grouped;
        }
    }

    public async Task<List<SalesSummaryDto>> GetPurchaseSummaryAsync(DateTime? from, DateTime? to, string groupBy)
    {
        var query = _context.Invoices
            .AsNoTracking()
            .Where(i => i.Type == "PURCHASE" && i.Status == "ISSUED");

        if (from.HasValue)
            query = query.Where(i => i.IssueDate >= from.Value);

        if (to.HasValue)
        {
            var toEndOfDay = to.Value.Date.AddDays(1);
            query = query.Where(i => i.IssueDate < toEndOfDay);
        }

        if (groupBy?.ToUpper() == "MONTH")
        {
            var grouped = await query
                .GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month })
                .Select(g => new SalesSummaryDto
                {
                    Period = $"{g.Key.Year:0000}-{g.Key.Month:00}",
                    InvoiceCount = g.Count(),
                    TotalNet = g.Sum(i => i.Subtotal),
                    TotalVat = g.Sum(i => i.VatTotal),
                    TotalGross = g.Sum(i => i.GrandTotal)
                })
                .OrderBy(s => s.Period)
                .ToListAsync();

            return grouped;
        }
        else // DAY
        {
            var grouped = await query
                .GroupBy(i => i.IssueDate.Date)
                .Select(g => new SalesSummaryDto
                {
                    Period = g.Key.ToString("yyyy-MM-dd"),
                    InvoiceCount = g.Count(),
                    TotalNet = g.Sum(i => i.Subtotal),
                    TotalVat = g.Sum(i => i.VatTotal),
                    TotalGross = g.Sum(i => i.GrandTotal)
                })
                .OrderBy(s => s.Period)
                .ToListAsync();

            return grouped;
        }
    }

    public async Task<PagedReportResult<PartyBalanceDto>> GetPartyBalancesAsync(string? q, string? type, int page, int size, DateTime? at)
    {
        var ledgerQuery = _context.PartyLedgerEntries.AsNoTracking();

        if (at.HasValue)
        {
            var atEndOfDay = at.Value.Date.AddDays(1);
            ledgerQuery = ledgerQuery.Where(e => e.OccurredAt < atEndOfDay);
        }

        var balances = await ledgerQuery
            .GroupBy(e => e.PartyId)
            .Select(g => new
            {
                PartyId = g.Key,
                Balance = g.Sum(e => e.AmountSigned)
            })
            .ToListAsync();

        var partyQuery = _context.Parties.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = q.ToLower();
            partyQuery = partyQuery.Where(p => 
                p.Code.ToLower().Contains(searchTerm) ||
                p.Name.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(type))
            partyQuery = partyQuery.Where(p => p.Type == type);

        var total = await partyQuery.CountAsync();

        var parties = await partyQuery
            .OrderBy(p => p.Code)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        var items = parties.Select(p => new PartyBalanceDto
        {
            PartyId = p.Id,
            Code = p.Code,
            Name = p.Name,
            Type = p.Type,
            Balance = balances.FirstOrDefault(b => b.PartyId == p.Id)?.Balance ?? 0,
            Currency = "TRY"
        }).ToList();

        return new PagedReportResult<PartyBalanceDto>
        {
            Page = page,
            Size = size,
            Total = total,
            Items = items
        };
    }

    public async Task<PagedReportResult<PartyAgingDto>> GetPartyAgingAsync(string? q, string? type, int page, int size, DateTime? at)
    {
        var atDate = at?.Date ?? DateTime.UtcNow.Date;

        // Get SALES ISSUED invoices
        var invoices = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.Type == "SALES" && i.Status == "ISSUED")
            .Select(i => new
            {
                i.PartyId,
                i.IssueDate,
                DueDate = i.DueDate ?? i.IssueDate,
                i.GrandTotal
            })
            .ToListAsync();

        // Calculate aging buckets
        var agingData = invoices
            .GroupBy(i => i.PartyId)
            .Select(g => new
            {
                PartyId = g.Key,
                Bucket0_30 = g.Where(i => (atDate - i.DueDate).Days >= 0 && (atDate - i.DueDate).Days <= 30).Sum(i => i.GrandTotal),
                Bucket31_60 = g.Where(i => (atDate - i.DueDate).Days >= 31 && (atDate - i.DueDate).Days <= 60).Sum(i => i.GrandTotal),
                Bucket61_90 = g.Where(i => (atDate - i.DueDate).Days >= 61 && (atDate - i.DueDate).Days <= 90).Sum(i => i.GrandTotal),
                Bucket90Plus = g.Where(i => (atDate - i.DueDate).Days > 90).Sum(i => i.GrandTotal),
                Total = g.Sum(i => i.GrandTotal)
            })
            .ToList();

        var partyQuery = _context.Parties.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = q.ToLower();
            partyQuery = partyQuery.Where(p => 
                p.Code.ToLower().Contains(searchTerm) ||
                p.Name.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(type))
            partyQuery = partyQuery.Where(p => p.Type == type);

        // Only include parties with invoices
        var partyIds = agingData.Select(a => a.PartyId).ToList();
        partyQuery = partyQuery.Where(p => partyIds.Contains(p.Id));

        var total = await partyQuery.CountAsync();

        var parties = await partyQuery
            .OrderBy(p => p.Code)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        var items = parties.Select(p =>
        {
            var aging = agingData.FirstOrDefault(a => a.PartyId == p.Id);
            return new PartyAgingDto
            {
                PartyId = p.Id,
                Code = p.Code,
                Name = p.Name,
                Bucket0_30 = aging?.Bucket0_30 ?? 0,
                Bucket31_60 = aging?.Bucket31_60 ?? 0,
                Bucket61_90 = aging?.Bucket61_90 ?? 0,
                Bucket90Plus = aging?.Bucket90Plus ?? 0,
                Total = aging?.Total ?? 0
            };
        }).ToList();

        return new PagedReportResult<PartyAgingDto>
        {
            Page = page,
            Size = size,
            Total = total,
            Items = items
        };
    }

    public async Task<List<CashBankBalanceDto>> GetCashBankBalancesAsync(DateTime? at)
    {
        var ledgerQuery = _context.CashBankLedgerEntries.AsNoTracking();

        if (at.HasValue)
        {
            var atEndOfDay = at.Value.Date.AddDays(1);
            ledgerQuery = ledgerQuery.Where(e => e.OccurredAt < atEndOfDay);
        }

        var balances = await ledgerQuery
            .GroupBy(e => new { e.SourceType, e.SourceId })
            .Select(g => new
            {
                g.Key.SourceType,
                g.Key.SourceId,
                Balance = g.Sum(e => e.AmountSigned)
            })
            .ToListAsync();

        var result = new List<CashBankBalanceDto>();

        // Get cashboxes
        var cashboxes = await _context.Cashboxes
            .AsNoTracking()
            .ToListAsync();

        foreach (var cashbox in cashboxes)
        {
            result.Add(new CashBankBalanceDto
            {
                SourceType = "CASHBOX",
                SourceId = cashbox.Id,
                Code = cashbox.Code,
                Name = cashbox.Name,
                Currency = cashbox.Currency,
                Balance = balances.FirstOrDefault(b => b.SourceType == "CASHBOX" && b.SourceId == cashbox.Id)?.Balance ?? 0
            });
        }

        // Get bank accounts
        var bankAccounts = await _context.BankAccounts
            .AsNoTracking()
            .ToListAsync();

        foreach (var bank in bankAccounts)
        {
            result.Add(new CashBankBalanceDto
            {
                SourceType = "BANK",
                SourceId = bank.Id,
                Code = bank.Code,
                Name = bank.Name,
                Currency = bank.Currency,
                Balance = balances.FirstOrDefault(b => b.SourceType == "BANK" && b.SourceId == bank.Id)?.Balance ?? 0
            });
        }

        return result.OrderBy(r => r.SourceType).ThenBy(r => r.Code).ToList();
    }
}
