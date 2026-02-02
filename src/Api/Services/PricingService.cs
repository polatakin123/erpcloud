using ErpCloud.Api.Data;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IPricingService
{
    Task<VariantPriceDto?> GetVariantPriceAsync(Guid variantId, string? priceListCode, DateTime? at);
}

public class PricingService : IPricingService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public PricingService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<VariantPriceDto?> GetVariantPriceAsync(Guid variantId, string? priceListCode, DateTime? at)
    {
        var tenantId = _tenantContext.TenantId;
        var priceDate = at ?? DateTime.UtcNow;

        // Get variant with product info
        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.TenantId == tenantId);

        if (variant == null) return null;

        // Determine which price list to use
        Guid? priceListId = null;

        if (string.IsNullOrWhiteSpace(priceListCode))
        {
            // Use default price list
            var defaultList = await _context.PriceLists
                .Where(pl => pl.TenantId == tenantId && pl.IsDefault)
                .FirstOrDefaultAsync();

            priceListId = defaultList?.Id;
        }
        else
        {
            // Use specified price list
            var normalizedCode = priceListCode.Trim().ToUpper();
            var priceList = await _context.PriceLists
                .Where(pl => pl.TenantId == tenantId && pl.Code == normalizedCode)
                .FirstOrDefaultAsync();

            priceListId = priceList?.Id;
        }

        if (!priceListId.HasValue) return null;

        // Find applicable price item
        // Filter by variant, price list, and date range
        var priceItem = await _context.PriceListItems
            .Include(i => i.PriceList)
            .Where(i => i.TenantId == tenantId 
                && i.VariantId == variantId 
                && i.PriceListId == priceListId.Value
                && (i.ValidFrom == null || i.ValidFrom <= priceDate)
                && (i.ValidTo == null || i.ValidTo >= priceDate))
            .OrderByDescending(i => i.MinQty ?? 0) // Highest tier first
            .FirstOrDefaultAsync();

        if (priceItem == null) return null;

        return new VariantPriceDto(
            variant.Id,
            variant.Sku,
            variant.Name,
            priceItem.PriceList!.Code,
            priceItem.PriceList.Currency,
            priceItem.UnitPrice,
            variant.VatRate,
            priceItem.MinQty,
            priceItem.ValidFrom,
            priceItem.ValidTo
        );
    }
}
