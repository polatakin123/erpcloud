using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IPartyLedgerService
{
    Task<PartyLedgerListDto> GetLedgerAsync(Guid partyId, PartyLedgerSearchDto search);
    Task<PartyBalanceDto> GetBalanceAsync(Guid partyId, DateTime? at = null);
}

public class PartyLedgerService : IPartyLedgerService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public PartyLedgerService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<PartyLedgerListDto> GetLedgerAsync(Guid partyId, PartyLedgerSearchDto search)
    {
        var tenantId = _tenantContext.TenantId;

        // Validate party exists
        var partyExists = await _context.Parties.AnyAsync(p => p.Id == partyId && p.TenantId == tenantId);
        if (!partyExists)
        {
            throw new InvalidOperationException("Party not found");
        }

        var query = _context.Set<PartyLedgerEntry>()
            .Where(e => e.TenantId == tenantId && e.PartyId == partyId);

        // Filter by date range
        if (search.From.HasValue)
        {
            query = query.Where(e => e.OccurredAt >= search.From.Value);
        }

        if (search.To.HasValue)
        {
            query = query.Where(e => e.OccurredAt <= search.To.Value);
        }

        // Count total
        var totalCount = await query.CountAsync();

        // Apply pagination
        var pageSize = Math.Min(search.Size, 200);
        var entries = await query
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.CreatedAt)
            .Skip((search.Page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entries.Select(e => new PartyLedgerEntryDto(
            Id: e.Id,
            OccurredAt: e.OccurredAt,
            SourceType: e.SourceType,
            SourceId: e.SourceId,
            Description: e.Description,
            AmountSigned: e.AmountSigned,
            Currency: e.Currency,
            OpenAmountSigned: e.OpenAmountSigned
        )).ToList();

        return new PartyLedgerListDto(items, totalCount, search.Page, pageSize);
    }

    public async Task<PartyBalanceDto> GetBalanceAsync(Guid partyId, DateTime? at = null)
    {
        var tenantId = _tenantContext.TenantId;

        // Validate party exists
        var partyExists = await _context.Parties.AnyAsync(p => p.Id == partyId && p.TenantId == tenantId);
        if (!partyExists)
        {
            throw new InvalidOperationException("Party not found");
        }

        var query = _context.Set<PartyLedgerEntry>()
            .Where(e => e.TenantId == tenantId && e.PartyId == partyId);

        // Filter by date if provided
        if (at.HasValue)
        {
            query = query.Where(e => e.OccurredAt <= at.Value);
        }

        // Calculate balance (sum of signed amounts)
        var balance = await query.SumAsync(e => (decimal?)e.AmountSigned) ?? 0;

        // Get currency (assume TRY for now, can be enhanced)
        var currency = "TRY";

        return new PartyBalanceDto(balance, currency);
    }
}
