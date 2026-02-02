using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IPriceListService
{
    Task<PriceListDto> CreateAsync(CreatePriceListDto dto);
    Task<PaginatedResponse<PriceListDto>> GetAllAsync(int page, int size, string? q);
    Task<PriceListDto?> GetByIdAsync(Guid id);
    Task<PriceListDto?> UpdateAsync(Guid id, UpdatePriceListDto dto);
    Task<bool> DeleteAsync(Guid id);
}

public class PriceListService : IPriceListService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public PriceListService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<PriceListDto> CreateAsync(CreatePriceListDto dto)
    {
        var normalizedCode = dto.Code.Trim().ToUpper();
        var normalizedCurrency = dto.Currency.Trim().ToUpper();
        var tenantId = _tenantContext.TenantId;

        var exists = await _context.PriceLists
            .AnyAsync(pl => pl.TenantId == tenantId && pl.Code == normalizedCode);

        if (exists)
        {
            throw new InvalidOperationException($"Price list with code '{normalizedCode}' already exists.");
        }

        // If setting as default, unset other defaults
        if (dto.IsDefault)
        {
            await UnsetDefaults(tenantId);
        }

        var priceList = new PriceList
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = normalizedCode,
            Name = dto.Name.Trim(),
            Currency = normalizedCurrency,
            IsDefault = dto.IsDefault,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.PriceLists.Add(priceList);
        await _context.SaveChangesAsync();

        return MapToDto(priceList);
    }

    public async Task<PaginatedResponse<PriceListDto>> GetAllAsync(int page, int size, string? q)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _context.PriceLists.Where(pl => pl.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = $"%{q.Trim()}%";
            query = query.Where(pl => EF.Functions.ILike(pl.Code, searchTerm) || EF.Functions.ILike(pl.Name, searchTerm));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(pl => pl.Code)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(pl => MapToDto(pl))
            .ToListAsync();

        return new PaginatedResponse<PriceListDto>(page, size, total, items);
    }

    public async Task<PriceListDto?> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var priceList = await _context.PriceLists
            .FirstOrDefaultAsync(pl => pl.Id == id && pl.TenantId == tenantId);

        return priceList == null ? null : MapToDto(priceList);
    }

    public async Task<PriceListDto?> UpdateAsync(Guid id, UpdatePriceListDto dto)
    {
        var tenantId = _tenantContext.TenantId;
        var priceList = await _context.PriceLists
            .FirstOrDefaultAsync(pl => pl.Id == id && pl.TenantId == tenantId);

        if (priceList == null) return null;

        var normalizedCode = dto.Code.Trim().ToUpper();
        var normalizedCurrency = dto.Currency.Trim().ToUpper();

        var exists = await _context.PriceLists
            .AnyAsync(pl => pl.TenantId == tenantId && pl.Code == normalizedCode && pl.Id != id);

        if (exists)
        {
            throw new InvalidOperationException($"Price list with code '{normalizedCode}' already exists.");
        }

        // If setting as default, unset other defaults
        if (dto.IsDefault && !priceList.IsDefault)
        {
            await UnsetDefaults(tenantId, id);
        }

        priceList.Code = normalizedCode;
        priceList.Name = dto.Name.Trim();
        priceList.Currency = normalizedCurrency;
        priceList.IsDefault = dto.IsDefault;

        await _context.SaveChangesAsync();

        return MapToDto(priceList);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var priceList = await _context.PriceLists
            .FirstOrDefaultAsync(pl => pl.Id == id && pl.TenantId == tenantId);

        if (priceList == null) return false;

        _context.PriceLists.Remove(priceList);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task UnsetDefaults(Guid tenantId, Guid? excludeId = null)
    {
        var query = _context.PriceLists
            .Where(pl => pl.TenantId == tenantId && pl.IsDefault);

        if (excludeId.HasValue)
        {
            query = query.Where(pl => pl.Id != excludeId.Value);
        }

        var defaults = await query.ToListAsync();
        foreach (var pl in defaults)
        {
            pl.IsDefault = false;
        }
    }

    private static PriceListDto MapToDto(PriceList priceList)
    {
        return new PriceListDto(
            priceList.Id,
            priceList.Code,
            priceList.Name,
            priceList.Currency,
            priceList.IsDefault,
            priceList.CreatedAt,
            priceList.CreatedBy
        );
    }
}
