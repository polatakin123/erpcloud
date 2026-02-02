using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public class BankAccountService : IBankAccountService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public BankAccountService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<BankAccountDto> CreateAsync(CreateBankAccountDto dto)
    {
        // Normalize code to uppercase
        var code = dto.Code.ToUpperInvariant();

        // Check if code already exists
        var exists = await _context.BankAccounts
            .AnyAsync(b => b.Code == code);
        if (exists)
            throw new InvalidOperationException($"Bank account code '{code}' already exists");

        // If setting as default, clear existing defaults
        if (dto.IsDefault)
        {
            await ClearDefaultsAsync();
        }

        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = dto.Name,
            BankName = dto.BankName,
            Iban = dto.Iban?.ToUpperInvariant(),
            Currency = dto.Currency.ToUpperInvariant(),
            IsActive = dto.IsActive,
            IsDefault = dto.IsDefault,
            TenantId = _tenantContext.TenantId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.BankAccounts.Add(account);
        await _context.SaveChangesAsync();

        return MapToDto(account);
    }

    public async Task<BankAccountDto> UpdateAsync(Guid id, UpdateBankAccountDto dto)
    {
        var account = await _context.BankAccounts.FindAsync(id);
        if (account == null)
            throw new InvalidOperationException("Bank account not found");

        // If setting as default, clear existing defaults
        if (dto.IsDefault && !account.IsDefault)
        {
            await ClearDefaultsAsync();
        }

        account.Name = dto.Name;
        account.BankName = dto.BankName;
        account.Iban = dto.Iban?.ToUpperInvariant();
        account.Currency = dto.Currency.ToUpperInvariant();
        account.IsActive = dto.IsActive;
        account.IsDefault = dto.IsDefault;

        await _context.SaveChangesAsync();

        return MapToDto(account);
    }

    public async Task DeleteAsync(Guid id)
    {
        var account = await _context.BankAccounts.FindAsync(id);
        if (account == null)
            throw new InvalidOperationException("Bank account not found");

        // Check if referenced by ledger entries
        var hasEntries = await _context.CashBankLedgerEntries
            .AnyAsync(e => e.SourceType == "BANK" && e.SourceId == id);
        if (hasEntries)
            throw new InvalidOperationException("Cannot delete bank account with existing ledger entries");

        // Check if referenced by payments
        var hasPayments = await _context.Payments
            .AnyAsync(p => p.SourceType == "BANK" && p.SourceId == id);
        if (hasPayments)
            throw new InvalidOperationException("Cannot delete bank account with existing payments");

        _context.BankAccounts.Remove(account);
        await _context.SaveChangesAsync();
    }

    public async Task<BankAccountDto?> GetByIdAsync(Guid id)
    {
        var account = await _context.BankAccounts.FindAsync(id);
        return account == null ? null : MapToDto(account);
    }

    public async Task<PagedResult<BankAccountListDto>> SearchAsync(BankAccountSearchDto dto)
    {
        var query = _context.BankAccounts.AsQueryable();

        // Filter by search query (code, name, or bank name)
        if (!string.IsNullOrWhiteSpace(dto.Q))
        {
            var q = dto.Q.Trim().ToUpperInvariant();
            query = query.Where(b => 
                b.Code.Contains(q) || 
                b.Name.ToUpper().Contains(q) ||
                (b.BankName != null && b.BankName.ToUpper().Contains(q)));
        }

        // Filter by active status
        if (dto.Active.HasValue)
        {
            query = query.Where(b => b.IsActive == dto.Active.Value);
        }

        // Count total
        var total = await query.CountAsync();

        // Order by code
        query = query.OrderBy(b => b.Code);

        // Paginate
        var items = await query
            .Skip((dto.Page - 1) * dto.Size)
            .Take(dto.Size)
            .Select(b => new BankAccountListDto
            {
                Id = b.Id,
                Code = b.Code,
                Name = b.Name,
                BankName = b.BankName,
                Iban = b.Iban,
                Currency = b.Currency,
                IsActive = b.IsActive,
                IsDefault = b.IsDefault
            })
            .ToListAsync();

        return new PagedResult<BankAccountListDto>(items, total, dto.Page, dto.Size);
    }

    private async Task ClearDefaultsAsync()
    {
        var defaults = await _context.BankAccounts
            .Where(b => b.IsDefault)
            .ToListAsync();

        foreach (var account in defaults)
        {
            account.IsDefault = false;
        }
    }

    private static BankAccountDto MapToDto(BankAccount account)
    {
        return new BankAccountDto
        {
            Id = account.Id,
            Code = account.Code,
            Name = account.Name,
            BankName = account.BankName,
            Iban = account.Iban,
            Currency = account.Currency,
            IsActive = account.IsActive,
            IsDefault = account.IsDefault,
            CreatedAt = account.CreatedAt,
            CreatedBy = account.CreatedBy
        };
    }
}
