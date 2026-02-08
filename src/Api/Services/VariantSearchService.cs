using ErpCloud.BuildingBlocks.Tenant;
using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

/// <summary>
/// DTO for variant search results with equivalence info
/// </summary>
public class VariantSearchResultDto
{
    public Guid VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public Guid? BrandId { get; set; }
    public string? BrandCode { get; set; }
    public string? BrandLogoUrl { get; set; }
    public bool? IsBrandActive { get; set; }
    public List<string> OemRefs { get; set; } = new();
    
    // Stock info (if warehouse specified)
    public decimal? OnHand { get; set; }
    public decimal? Reserved { get; set; }
    public decimal? Available { get; set; }
    
    // Pricing (if available)
    public decimal? Price { get; set; }
    
    // Match metadata
    public string MatchType { get; set; } = "DIRECT"; // DIRECT | EQUIVALENT | BOTH
    public string MatchedBy { get; set; } = "NAME"; // NAME | SKU | BARCODE | OEM
    
    // Fitment metadata
    public bool HasFitment { get; set; }
    public bool IsCompatible { get; set; }
    public int FitmentPriority { get; set; } // 1=compatible+inStock+direct, 2=compatible+inStock+equiv, 3=compatible+outOfStock, 4=undefined
}

/// <summary>
/// Service for fast variant search with OEM-based equivalent detection
/// </summary>
public class VariantSearchService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;
    private const int MaxTransitiveDepth = 5;

    public VariantSearchService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
        Console.WriteLine($"[VariantSearchService.ctor] TenantId from context: {tenantContext.TenantId}");
    }

    /// <summary>
    /// Fast search for variants with optional equivalent expansion and vehicle fitment filtering
    /// </summary>
    public async Task<List<VariantSearchResultDto>> SearchVariantsAsync(
        string query,
        Guid? warehouseId = null,
        bool includeEquivalents = true,
        Guid? brandId = null,
        Guid? modelId = null,
        int? year = null,
        Guid? engineId = null,
        bool includeUndefinedFitment = false,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new List<VariantSearchResultDto>();

        var normalizedQuery = NormalizeQuery(query);
        
        Console.WriteLine($"[SEARCH] TenantId: {tenantId}, Query: '{query}', Normalized: '{normalizedQuery}'");

        // STEP 1: Find direct matches
        var directMatches = await FindDirectMatchesAsync(tenantId, query, normalizedQuery, ct);
        
        Console.WriteLine($"[SEARCH] Direct matches found: {directMatches.Count}");

        if (!includeEquivalents)
        {
            // Return only direct matches
            return await BuildResultsAsync(directMatches, warehouseId, engineId, includeUndefinedFitment, page, pageSize, ct);
        }

        // STEP 2: Find OEM codes from direct matches
        var directVariantIds = directMatches.Select(m => m.VariantId).ToHashSet();
        var oemCodes = await GetOemCodesForVariantsAsync(tenantId, directVariantIds, ct);

        if (oemCodes.Count == 0)
        {
            // No OEM codes, return direct matches only
            return await BuildResultsAsync(directMatches, warehouseId, engineId, includeUndefinedFitment, page, pageSize, ct);
        }

        // STEP 3: Transitive expansion (BFS-style)
        var allOemCodes = new HashSet<string>(oemCodes);
        var allVariantIds = new HashSet<Guid>(directVariantIds);
        var previousVariantCount = 0;
        var depth = 0;

        while (depth < MaxTransitiveDepth && allVariantIds.Count > previousVariantCount)
        {
            previousVariantCount = allVariantIds.Count;

            // Find variants sharing any of the current OEM codes
            var newVariants = await _context.Set<PartReference>()
                .Where(r => r.TenantId == tenantId 
                    && r.RefType == "OEM" 
                    && allOemCodes.Contains(r.RefCode))
                .Select(r => r.VariantId)
                .Distinct()
                .ToListAsync(ct);

            foreach (var vid in newVariants)
                allVariantIds.Add(vid);

            // Get new OEM codes from newly found variants
            var newOemCodes = await GetOemCodesForVariantsAsync(tenantId, newVariants, ct);
            foreach (var code in newOemCodes)
                allOemCodes.Add(code);

            depth++;
        }

        // STEP 4: Build match metadata
        var equivalentVariantIds = allVariantIds.Except(directVariantIds).ToList();
        
        var allMatches = directMatches.Concat(
            equivalentVariantIds.Select(vid => new VariantMatch
            {
                VariantId = vid,
                MatchType = "EQUIVALENT",
                MatchedBy = "OEM"
            })
        ).ToList();

        // STEP 5: Build results with stock/pricing
        return await BuildResultsAsync(allMatches, warehouseId, engineId, includeUndefinedFitment, page, pageSize, ct);
    }

    /// <summary>
    /// Find direct matches by name, SKU, barcode, or OEM
    /// </summary>
    private async Task<List<VariantMatch>> FindDirectMatchesAsync(
        Guid tenantId, 
        string originalQuery, 
        string normalizedQuery, 
        CancellationToken ct)
    {
        var matches = new List<VariantMatch>();

        // Split query into keywords for multi-word search
        var keywords = originalQuery.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(k => k.Length >= 2)
            .ToList();

        // Match by Variant Name (ILIKE) - supports multi-keyword
        IQueryable<ProductVariant> variantNameQuery = _context.ProductVariants
            .Where(v => v.TenantId == tenantId && v.IsActive);

        foreach (var keyword in keywords)
        {
            var kw = keyword; // capture for lambda
            variantNameQuery = variantNameQuery.Where(v => EF.Functions.ILike(v.Name, $"%{kw}%"));
        }

        var variantNameMatches = await variantNameQuery
            .Select(v => new VariantMatch
            {
                VariantId = v.Id,
                MatchType = "DIRECT",
                MatchedBy = "NAME"
            })
            .ToListAsync(ct);

        Console.WriteLine($"[SEARCH] Variant name matches: {variantNameMatches.Count}");
        matches.AddRange(variantNameMatches);

        // Match by Product Name (ILIKE) - supports multi-keyword
        IQueryable<ProductVariant> productNameQuery = _context.ProductVariants
            .Include(v => v.Product)
            .Where(v => v.TenantId == tenantId && v.IsActive && v.Product != null);

        foreach (var keyword in keywords)
        {
            var kw = keyword; // capture for lambda
            productNameQuery = productNameQuery.Where(v => EF.Functions.ILike(v.Product!.Name, $"%{kw}%"));
        }

        var productNameMatches = await productNameQuery
            .Select(v => new VariantMatch
            {
                VariantId = v.Id,
                MatchType = "DIRECT",
                MatchedBy = "NAME"
            })
            .ToListAsync(ct);

        Console.WriteLine($"[SEARCH] Product name matches: {productNameMatches.Count}");
        matches.AddRange(productNameMatches);

        // Match by SKU (partial match with ILIKE)
        var skuMatches = await _context.ProductVariants
            .Where(v => v.TenantId == tenantId 
                && v.IsActive 
                && EF.Functions.ILike(v.Sku, $"%{originalQuery}%"))
            .Select(v => new VariantMatch
            {
                VariantId = v.Id,
                MatchType = "DIRECT",
                MatchedBy = "SKU"
            })
            .ToListAsync(ct);

        matches.AddRange(skuMatches);

        // Match by Barcode (partial match with ILIKE)
        var barcodeMatches = await _context.ProductVariants
            .Where(v => v.TenantId == tenantId 
                && v.IsActive 
                && v.Barcode != null 
                && EF.Functions.ILike(v.Barcode, $"%{originalQuery}%"))
            .Select(v => new VariantMatch
            {
                VariantId = v.Id,
                MatchType = "DIRECT",
                MatchedBy = "BARCODE"
            })
            .ToListAsync(ct);

        matches.AddRange(barcodeMatches);

        // Match by OEM reference (partial match with ILIKE)
        var oemMatches = await _context.Set<PartReference>()
            .Where(r => r.TenantId == tenantId 
                && r.RefType == "OEM" 
                && EF.Functions.ILike(r.RefCode, $"%{originalQuery}%"))
            .Select(r => new VariantMatch
            {
                VariantId = r.VariantId,
                MatchType = "DIRECT",
                MatchedBy = "OEM"
            })
            .ToListAsync(ct);

        matches.AddRange(oemMatches);

        // Remove duplicates, prioritize match type
        return matches
            .GroupBy(m => m.VariantId)
            .Select(g =>
            {
                var match = g.First();
                // If multiple match types, mark as BOTH
                if (g.Count() > 1)
                    match.MatchType = "BOTH";
                return match;
            })
            .ToList();
    }

    /// <summary>
    /// Get all OEM codes for a set of variants
    /// </summary>
    private async Task<List<string>> GetOemCodesForVariantsAsync(
        Guid tenantId, 
        IEnumerable<Guid> variantIds, 
        CancellationToken ct)
    {
        if (!variantIds.Any())
            return new List<string>();

        return await _context.Set<PartReference>()
            .Where(r => r.TenantId == tenantId 
                && r.RefType == "OEM" 
                && variantIds.Contains(r.VariantId))
            .Select(r => r.RefCode)
            .Distinct()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Build final results with variant details, stock, pricing, and fitment filtering
    /// </summary>
    private async Task<List<VariantSearchResultDto>> BuildResultsAsync(
        List<VariantMatch> matches,
        Guid? warehouseId,
        Guid? engineId,
        bool includeUndefinedFitment,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var variantIds = matches.Select(m => m.VariantId).Distinct().ToList();
        var tenantId = _tenantContext.TenantId;

        // Get variant details
        var variants = await _context.ProductVariants
            .Include(v => v.Product)
                .ThenInclude(p => p.BrandNavigation)
            .Where(v => variantIds.Contains(v.Id) && v.TenantId == tenantId)
            .ToListAsync(ct);

        // Get OEM refs for all variants
        var oemRefs = await _context.Set<PartReference>()
            .Where(r => r.TenantId == tenantId 
                && r.RefType == "OEM" 
                && variantIds.Contains(r.VariantId))
            .GroupBy(r => r.VariantId)
            .Select(g => new
            {
                VariantId = g.Key,
                OemCodes = g.Select(r => r.RefCode).ToList()
            })
            .ToDictionaryAsync(x => x.VariantId, x => x.OemCodes, ct);

        // Get stock balances if warehouse specified
        Dictionary<Guid, (decimal onHand, decimal reserved, decimal available)>? stockBalances = null;
        if (warehouseId.HasValue)
        {
            stockBalances = await _context.StockBalances
                .Where(sb => sb.TenantId == tenantId 
                    && sb.WarehouseId == warehouseId.Value 
                    && variantIds.Contains(sb.VariantId))
                .ToDictionaryAsync(
                    sb => sb.VariantId,
                    sb => (sb.OnHand, sb.Reserved, sb.Available),
                    ct);
        }

        // Get fitment data
        var fitmentLookup = await _context.Set<StockCardFitment>()
            .Where(f => f.TenantId == tenantId && variantIds.Contains(f.VariantId))
            .GroupBy(f => f.VariantId)
            .Select(g => new
            {
                VariantId = g.Key,
                EngineIds = g.Select(f => f.VehicleEngineId).ToList()
            })
            .ToDictionaryAsync(x => x.VariantId, x => x.EngineIds, ct);

        // Build DTOs
        var results = variants.Select(v =>
        {
            var match = matches.First(m => m.VariantId == v.Id);
            oemRefs.TryGetValue(v.Id, out var oemList);
            fitmentLookup.TryGetValue(v.Id, out var engineIds);
            
            var hasFitment = engineIds != null && engineIds.Count > 0;
            var isCompatible = engineId.HasValue && hasFitment && engineIds!.Contains(engineId.Value);
            
            var brandNav = v.Product.BrandNavigation;
            
            // Suppress obsolete warning for intentional fallback to deprecated Brand field during migration
            #pragma warning disable CS0618
            var brandName = brandNav?.Name ?? v.Product.Brand;
            #pragma warning restore CS0618
            
            var result = new VariantSearchResultDto
            {
                VariantId = v.Id,
                Sku = v.Sku,
                Barcode = v.Barcode,
                Name = v.Name,
                Brand = brandName, // Use new Brand master data, fallback to deprecated string
                BrandId = v.Product.BrandId,
                BrandCode = brandNav?.Code,
                BrandLogoUrl = brandNav?.LogoUrl,
                IsBrandActive = brandNav?.IsActive,
                OemRefs = oemList ?? new List<string>(),
                MatchType = match.MatchType,
                MatchedBy = match.MatchedBy,
                HasFitment = hasFitment,
                IsCompatible = isCompatible
            };

            if (stockBalances != null && stockBalances.TryGetValue(v.Id, out var stock))
            {
                result.OnHand = stock.onHand;
                result.Reserved = stock.reserved;
                result.Available = stock.available;
            }

            // Calculate fitment priority for sorting
            // 1 = compatible + in stock + direct match
            // 2 = compatible + in stock + equivalent match
            // 3 = compatible + out of stock
            // 4 = undefined fitment
            if (!hasFitment)
            {
                result.FitmentPriority = 4; // Undefined
            }
            else if (isCompatible)
            {
                var inStock = result.Available > 0;
                var isDirect = match.MatchType == "DIRECT" || match.MatchType == "BOTH";
                
                if (inStock && isDirect)
                    result.FitmentPriority = 1;
                else if (inStock)
                    result.FitmentPriority = 2;
                else
                    result.FitmentPriority = 3;
            }
            else
            {
                result.FitmentPriority = 4; // Has fitment but not compatible with selected engine
            }

            return result;
        }).ToList();

        // Apply fitment filtering if engineId specified
        if (engineId.HasValue && !includeUndefinedFitment)
        {
            results = results.Where(r => r.IsCompatible).ToList();
        }

        // Sort by fitment priority, then match type, then name
        results = results
            .OrderBy(r => r.FitmentPriority)
            .ThenByDescending(r => r.MatchType == "DIRECT" || r.MatchType == "BOTH" ? 1 : 0)
            .ThenBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return results;
    }

    private static string NormalizeQuery(string query)
    {
        // Remove all non-alphanumeric characters (same as PartReferenceService)
        return new string(query.Trim()
            .ToUpper()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private class VariantMatch
    {
        public Guid VariantId { get; set; }
        public string MatchType { get; set; } = "DIRECT";
        public string MatchedBy { get; set; } = "NAME";
    }
}
