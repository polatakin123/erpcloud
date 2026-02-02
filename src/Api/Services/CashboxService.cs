using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public class CashboxService : ICashboxService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CashboxService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<CashboxDto> CreateAsync(CreateCashboxDto dto)
    {
        // Normalize code to uppercase
        var code = dto.Code.ToUpperInvariant();

        // Check if code already exists
        var exists = await _context.Cashboxes
            .AnyAsync(c => c.Code == code);
        if (exists)
            throw new InvalidOperationException($"Cashbox code '{code}' already exists");

        // If setting as default, clear existing defaults
        if (dto.IsDefault)
        {
            await ClearDefaultsAsync();
        }

        var cashbox = new Cashbox
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = dto.Name,
            Currency = dto.Currency.ToUpperInvariant(),
            IsActive = dto.IsActive,
            IsDefault = dto.IsDefault,
            TenantId = _tenantContext.TenantId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.Cashboxes.Add(cashbox);
        await _context.SaveChangesAsync();

        return MapToDto(cashbox);
    }

    public async Task<CashboxDto> UpdateAsync(Guid id, UpdateCashboxDto dto)
    {
        var cashbox = await _context.Cashboxes.FindAsync(id);
        if (cashbox == null)
            throw new InvalidOperationException("Cashbox not found");

        // If setting as default, clear existing defaults
        if (dto.IsDefault && !cashbox.IsDefault)
        {
            await ClearDefaultsAsync();
        }

        cashbox.Name = dto.Name;
        cashbox.Currency = dto.Currency.ToUpperInvariant();
        cashbox.IsActive = dto.IsActive;
        cashbox.IsDefault = dto.IsDefault;

        await _context.SaveChangesAsync();

        return MapToDto(cashbox);
    }

    public async Task DeleteAsync(Guid id)
    {
        var cashbox = await _context.Cashboxes.FindAsync(id);
        if (cashbox == null)
            throw new InvalidOperationException("Cashbox not found");

        // Check if referenced by ledger entries
        var hasEntries = await _context.CashBankLedgerEntries
            .AnyAsync(e => e.SourceType == "CASHBOX" && e.SourceId == id);
        if (hasEntries)
            throw new InvalidOperationException("Cannot delete cashbox with existing ledger entries");

        // Check if referenced by payments
        var hasPayments = await _context.Payments
            .AnyAsync(p => p.SourceType == "CASHBOX" && p.SourceId == id);
        if (hasPayments)
            throw new InvalidOperationException("Cannot delete cashbox with existing payments");

        _context.Cashboxes.Remove(cashbox);
        await _context.SaveChangesAsync();
    }

    public async Task<CashboxDto?> GetByIdAsync(Guid id)
    {
        var cashbox = await _context.Cashboxes.FindAsync(id);
        return cashbox == null ? null : MapToDto(cashbox);
    }

    public async Task<PagedResult<CashboxListDto>> SearchAsync(CashboxSearchDto dto)
    {
        var query = _context.Cashboxes.AsQueryable();

        // Filter by search query (code or name)
        if (!string.IsNullOrWhiteSpace(dto.Q))
        {
            var q = dto.Q.Trim().ToUpperInvariant();
            query = query.Where(c => c.Code.Contains(q) || c.Name.ToUpper().Contains(q));
        }

        // Filter by active status
        if (dto.Active.HasValue)
        {
            query = query.Where(c => c.IsActive == dto.Active.Value);
        }

        // Count total
        var total = await query.CountAsync();

        // Order by code
        query = query.OrderBy(c => c.Code);

        // Paginate
        var items = await query
            .Skip((dto.Page - 1) * dto.Size)
            .Take(dto.Size)
            .Select(c => new CashboxListDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                Currency = c.Currency,
                IsActive = c.IsActive,
                IsDefault = c.IsDefault
            })
            .ToListAsync();

        return new PagedResult<CashboxListDto>(items, total, dto.Page, dto.Size);
    }

    private async Task ClearDefaultsAsync()
    {
        var defaults = await _context.Cashboxes
            .Where(c => c.IsDefault)
            .ToListAsync();

        foreach (var cashbox in defaults)
        {
            cashbox.IsDefault = false;
        }
    }

    private static CashboxDto MapToDto(Cashbox cashbox)
    {
        return new CashboxDto
        {
            Id = cashbox.Id,
            Code = cashbox.Code,
            Name = cashbox.Name,
            Currency = cashbox.Currency,
            IsActive = cashbox.IsActive,
            IsDefault = cashbox.IsDefault,
            CreatedAt = cashbox.CreatedAt,
            CreatedBy = cashbox.CreatedBy
        };
    }
}
