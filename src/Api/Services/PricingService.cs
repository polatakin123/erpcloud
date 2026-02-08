using ErpCloud.Api.Data;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

/// <summary>
/// Request for pricing calculation with discount rules
/// </summary>
public class PricingCalculationRequest
{
    public Guid PartyId { get; set; }
    public Guid VariantId { get; set; }
    public decimal Quantity { get; set; }
    public Guid? WarehouseId { get; set; }
    public string Currency { get; set; } = "TRY";
}

/// <summary>
/// Result of pricing calculation with full breakdown
/// </summary>
public class PricingCalculationResult
{
    public Guid VariantId { get; set; }
    public string VariantSku { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    
    public decimal Quantity { get; set; }
    public string Currency { get; set; } = "TRY";
    
    // Pricing breakdown
    public decimal ListPrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal NetPrice { get; set; }
    public decimal LineTotal { get; set; }
    
    // Cost & profit
    public decimal? UnitCost { get; set; }
    public decimal? Profit { get; set; }
    public decimal? ProfitPercent { get; set; }
    
    // Rule information
    public Guid? AppliedRuleId { get; set; }
    public string? AppliedRuleScope { get; set; }
    public string? AppliedRuleType { get; set; }
    public string? RuleDescription { get; set; }
    
    // Warnings
    public bool HasWarning { get; set; }
    public string? WarningMessage { get; set; }
}

public interface IPricingService
{
    Task<VariantPriceDto?> GetVariantPriceAsync(Guid variantId, string? priceListCode, DateTime? at);
    Task<PricingCalculationResult> CalculateAsync(PricingCalculationRequest request, CancellationToken ct = default);
    Task<List<PricingCalculationResult>> CalculateBatchAsync(List<PricingCalculationRequest> requests, CancellationToken ct = default);
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

    /// <summary>
    /// Calculate pricing for a variant with all applicable discount rules and profit breakdown
    /// </summary>
    public async Task<PricingCalculationResult> CalculateAsync(
        PricingCalculationRequest request,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var now = DateTime.UtcNow;

        // Validate inputs
        if (request.Quantity <= 0)
            throw new ArgumentException("Miktar sıfırdan büyük olmalıdır", nameof(request.Quantity));

        // Fetch variant with product and brand info
        var variant = await _context.ProductVariants
            .Include(v => v.Product)
                .ThenInclude(p => p.BrandNavigation)
            .Where(v => v.Id == request.VariantId)
            .FirstOrDefaultAsync(ct);

        if (variant == null)
            throw new InvalidOperationException($"Ürün varyantı bulunamadı: {request.VariantId}");

        // Get default list price
        var listPrice = await GetListPriceAsync(tenantId, request.VariantId, request.Currency, ct);

        // Find applicable price rule by priority (now includes brand-based rules)
        var appliedRule = await FindApplicablePriceRuleAsync(
            tenantId, 
            request.PartyId, 
            request.VariantId,
            variant.Product.BrandId,  // Pass BrandId Guid for brand-based rules
            now, 
            request.Currency, 
            ct);

        decimal netPrice = listPrice;
        decimal? discountPercent = null;
        decimal? discountAmount = null;
        string? ruleDescription = null;

        // Apply rule if found
        if (appliedRule != null)
        {
            if (appliedRule.RuleType == "FIXED_PRICE")
            {
                netPrice = appliedRule.Value;
                if (listPrice > 0)
                {
                    discountAmount = listPrice - netPrice;
                    discountPercent = (discountAmount / listPrice) * 100;
                }
                ruleDescription = $"Sabit fiyat: {appliedRule.Value:N2} {request.Currency}";
            }
            else if (appliedRule.RuleType == "DISCOUNT_PERCENT")
            {
                discountPercent = appliedRule.Value;
                discountAmount = listPrice * (appliedRule.Value / 100);
                netPrice = listPrice - discountAmount.Value;
                
                // Build rule description based on type
                if (appliedRule.BrandId.HasValue)
                {
                    // Load brand name if needed (should be eager-loaded via Include)
                    var brandName = variant.Product.BrandNavigation?.Name ?? "Unknown";
                    ruleDescription = $"Marka iskontosu ({brandName}): %{appliedRule.Value:N2}";
                }
                else
                {
                    ruleDescription = $"İndirim: %{appliedRule.Value:N2}";
                }
            }
        }

        // Get cost information
        var productCost = await _context.ProductCosts
            .Where(c => c.VariantId == request.VariantId)
            .FirstOrDefaultAsync(ct);

        decimal? unitCost = productCost?.LastPurchaseCost ?? productCost?.AverageCost;
        decimal? profit = null;
        decimal? profitPercent = null;
        bool hasWarning = false;
        string? warningMessage = null;

        if (unitCost.HasValue && unitCost.Value > 0)
        {
            profit = netPrice - unitCost.Value;
            profitPercent = (profit / netPrice) * 100;

            // Check for loss
            if (profit < 0)
            {
                hasWarning = true;
                warningMessage = $"UYARI: Zarar var! Maliyet: {unitCost.Value:N2} {request.Currency}, Satış: {netPrice:N2} {request.Currency}, Zarar: {profit:N2} {request.Currency}";
            }

            // Check against minimum sale price
            if (productCost?.MinSalePrice.HasValue == true && netPrice < productCost.MinSalePrice.Value)
            {
                hasWarning = true;
                warningMessage = (warningMessage ?? "") + 
                    $" | Minimum satış fiyatının altında! Min: {productCost.MinSalePrice.Value:N2} {request.Currency}";
            }
        }

        var lineTotal = netPrice * request.Quantity;

        return new PricingCalculationResult
        {
            VariantId = variant.Id,
            VariantSku = variant.Sku,
            VariantName = variant.Name,
            Quantity = request.Quantity,
            Currency = request.Currency,
            ListPrice = listPrice,
            DiscountPercent = discountPercent,
            DiscountAmount = discountAmount,
            NetPrice = netPrice,
            LineTotal = lineTotal,
            UnitCost = unitCost,
            Profit = profit,
            ProfitPercent = profitPercent,
            AppliedRuleId = appliedRule?.Id,
            AppliedRuleScope = appliedRule?.Scope,
            AppliedRuleType = appliedRule?.RuleType,
            RuleDescription = ruleDescription,
            HasWarning = hasWarning,
            WarningMessage = warningMessage
        };
    }

    /// <summary>
    /// Get list price for a variant from default price list
    /// </summary>
    private async Task<decimal> GetListPriceAsync(
        Guid tenantId, 
        Guid variantId, 
        string currency, 
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Get default price list for the currency
        var priceListItem = await _context.PriceListItems
            .Include(i => i.PriceList)
            .Where(i => 
                i.VariantId == variantId &&
                i.PriceList.Currency == currency &&
                i.PriceList.IsDefault &&
                (i.ValidFrom == null || i.ValidFrom <= now) &&
                (i.ValidTo == null || i.ValidTo >= now))
            .OrderByDescending(i => i.MinQty)
            .FirstOrDefaultAsync(ct);

        return priceListItem?.UnitPrice ?? 0;
    }

    /// <summary>
    /// Find the highest priority applicable price rule with brand support
    /// Priority order (highest to lowest):
    /// 1. CUSTOMER + Variant
    /// 2. CUSTOMER + Brand
    /// 3. CUSTOMER_GROUP + Variant
    /// 4. CUSTOMER_GROUP + Brand
    /// 5. PRODUCT_GROUP + Variant
    /// 6. PRODUCT_GROUP + Brand
    /// Within same level: Priority DESC, then CreatedAt DESC
    /// </summary>
    private async Task<Entities.PriceRule?> FindApplicablePriceRuleAsync(
        Guid tenantId,
        Guid partyId,
        Guid variantId,
        Guid? brandId,
        DateTime now,
        string currency,
        CancellationToken ct)
    {
        // Get all potentially applicable rules with one query
        var rules = await _context.PriceRules
            .Where(r =>
                r.IsActive &&
                r.Currency == currency &&
                r.ValidFrom <= now &&
                (r.ValidTo == null || r.ValidTo >= now) &&
                (
                    // Customer-specific rules
                    (r.Scope == "CUSTOMER" && r.TargetId == partyId && 
                        (r.VariantId == variantId || r.VariantId == null)) ||
                    (r.Scope == "CUSTOMER" && r.TargetId == partyId && 
                        brandId.HasValue && r.BrandId == brandId.Value) ||
                    
                    // Customer group rules (for now, match all groups)
                    (r.Scope == "CUSTOMER_GROUP" && 
                        (r.VariantId == variantId || r.VariantId == null)) ||
                    (r.Scope == "CUSTOMER_GROUP" && 
                        brandId.HasValue && r.BrandId == brandId.Value) ||
                    
                    // Product group rules
                    (r.Scope == "PRODUCT_GROUP" && 
                        (r.VariantId == variantId || r.VariantId == null)) ||
                    (r.Scope == "PRODUCT_GROUP" && 
                        brandId.HasValue && r.BrandId == brandId.Value)
                ))
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        if (rules.Count == 0)
            return null;

        // Apply hierarchical resolution
        // 1. CUSTOMER + Variant (highest priority)
        var rule = rules.FirstOrDefault(r => 
            r.Scope == "CUSTOMER" && 
            r.TargetId == partyId && 
            r.VariantId == variantId);
        
        if (rule != null) return rule;

        // 2. CUSTOMER + Brand
        rule = rules.FirstOrDefault(r => 
            r.Scope == "CUSTOMER" && 
            r.TargetId == partyId && 
            r.BrandId.HasValue && 
            r.BrandId == brandId);
        
        if (rule != null) return rule;

        // 3. CUSTOMER + All (no variant, no brand)
        rule = rules.FirstOrDefault(r => 
            r.Scope == "CUSTOMER" && 
            r.TargetId == partyId && 
            r.VariantId == null && 
            !r.BrandId.HasValue);
        
        if (rule != null) return rule;

        // 4. CUSTOMER_GROUP + Variant
        rule = rules.FirstOrDefault(r => 
            r.Scope == "CUSTOMER_GROUP" && 
            r.VariantId == variantId);
        
        if (rule != null) return rule;

        // 5. CUSTOMER_GROUP + Brand
        rule = rules.FirstOrDefault(r => 
            r.Scope == "CUSTOMER_GROUP" && 
            r.BrandId.HasValue && 
            r.BrandId == brandId);
        
        if (rule != null) return rule;

        // 6. CUSTOMER_GROUP + All
        rule = rules.FirstOrDefault(r => 
            r.Scope == "CUSTOMER_GROUP" && 
            r.VariantId == null && 
            !r.BrandId.HasValue);
        
        if (rule != null) return rule;

        // 7. PRODUCT_GROUP + Variant
        rule = rules.FirstOrDefault(r => 
            r.Scope == "PRODUCT_GROUP" && 
            r.VariantId == variantId);
        
        if (rule != null) return rule;

        // 8. PRODUCT_GROUP + Brand
        rule = rules.FirstOrDefault(r => 
            r.Scope == "PRODUCT_GROUP" && 
            r.BrandId.HasValue && 
            r.BrandId == brandId);
        
        if (rule != null) return rule;

        // 9. PRODUCT_GROUP + All (lowest priority fallback)
        return rules.FirstOrDefault(r => 
            r.Scope == "PRODUCT_GROUP" && 
            r.VariantId == null && 
            !r.BrandId.HasValue);
    }

    /// <summary>
    /// Batch calculate pricing for multiple line items
    /// </summary>
    public async Task<List<PricingCalculationResult>> CalculateBatchAsync(
        List<PricingCalculationRequest> requests,
        CancellationToken ct = default)
    {
        var results = new List<PricingCalculationResult>();

        foreach (var request in requests)
        {
            try
            {
                var result = await CalculateAsync(request, ct);
                results.Add(result);
            }
            catch (Exception ex)
            {
                // Log error but continue with other items
                Console.WriteLine($"[PricingService] Error calculating price for variant {request.VariantId}: {ex.Message}");
                
                // Add error result
                results.Add(new PricingCalculationResult
                {
                    VariantId = request.VariantId,
                    Quantity = request.Quantity,
                    Currency = request.Currency,
                    HasWarning = true,
                    WarningMessage = $"Fiyat hesaplanamadı: {ex.Message}"
                });
            }
        }

        return results;
    }
}
