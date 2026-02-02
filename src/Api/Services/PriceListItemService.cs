using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IPriceListItemService
{
    Task<PriceListItemDto> CreateAsync(Guid priceListId, CreatePriceListItemDto dto);
    Task<PaginatedResponse<PriceListItemDto>> GetAllByPriceListAsync(Guid priceListId, int page, int size, Guid? variantId);
    Task<PriceListItemDto?> GetByIdAsync(Guid id);
    Task<PriceListItemDto?> UpdateAsync(Guid id, UpdatePriceListItemDto dto);
    Task<bool> DeleteAsync(Guid id);
}

public class PriceListItemService : IPriceListItemService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public PriceListItemService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<PriceListItemDto> CreateAsync(Guid priceListId, CreatePriceListItemDto dto)
    {
        var tenantId = _tenantContext.TenantId;

        // Verify price list exists
        var priceListExists = await _context.PriceLists
            .AnyAsync(pl => pl.Id == priceListId && pl.TenantId == tenantId);

        if (!priceListExists)
        {
            throw new InvalidOperationException("Price list not found.");
        }

        // Verify variant exists
        var variantExists = await _context.ProductVariants
            .AnyAsync(v => v.Id == dto.VariantId && v.TenantId == tenantId);

        if (!variantExists)
        {
            throw new InvalidOperationException("Variant not found.");
        }

        var item = new PriceListItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PriceListId = priceListId,
            VariantId = dto.VariantId,
            UnitPrice = dto.UnitPrice,
            MinQty = dto.MinQty,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.PriceListItems.Add(item);
        await _context.SaveChangesAsync();

        return MapToDto(item);
    }

    public async Task<PaginatedResponse<PriceListItemDto>> GetAllByPriceListAsync(Guid priceListId, int page, int size, Guid? variantId)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _context.PriceListItems
            .Where(i => i.TenantId == tenantId && i.PriceListId == priceListId);

        if (variantId.HasValue)
        {
            query = query.Where(i => i.VariantId == variantId.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(i => i.VariantId)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(i => MapToDto(i))
            .ToListAsync();

        return new PaginatedResponse<PriceListItemDto>(page, size, total, items);
    }

    public async Task<PriceListItemDto?> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var item = await _context.PriceListItems
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

        return item == null ? null : MapToDto(item);
    }

    public async Task<PriceListItemDto?> UpdateAsync(Guid id, UpdatePriceListItemDto dto)
    {
        var tenantId = _tenantContext.TenantId;
        var item = await _context.PriceListItems
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

        if (item == null) return null;

        // Verify variant exists
        var variantExists = await _context.ProductVariants
            .AnyAsync(v => v.Id == dto.VariantId && v.TenantId == tenantId);

        if (!variantExists)
        {
            throw new InvalidOperationException("Variant not found.");
        }

        item.VariantId = dto.VariantId;
        item.UnitPrice = dto.UnitPrice;
        item.MinQty = dto.MinQty;
        item.ValidFrom = dto.ValidFrom;
        item.ValidTo = dto.ValidTo;

        await _context.SaveChangesAsync();

        return MapToDto(item);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var item = await _context.PriceListItems
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

        if (item == null) return false;

        _context.PriceListItems.Remove(item);
        await _context.SaveChangesAsync();

        return true;
    }

    private static PriceListItemDto MapToDto(PriceListItem item)
    {
        return new PriceListItemDto(
            item.Id,
            item.PriceListId,
            item.VariantId,
            item.UnitPrice,
            item.MinQty,
            item.ValidFrom,
            item.ValidTo,
            item.CreatedAt,
            item.CreatedBy
        );
    }
}
