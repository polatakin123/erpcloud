using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/search")]
public class VariantSearchController : ControllerBase
{
    private readonly VariantSearchService _service;

    public VariantSearchController(VariantSearchService service)
    {
        _service = service;
    }

    /// <summary>
    /// Fast search for product variants with OEM-based equivalent detection and vehicle fitment filtering
    /// </summary>
    /// <param name="q">Search query (name, SKU, barcode, or OEM code)</param>
    /// <param name="warehouseId">Optional warehouse ID for stock balances</param>
    /// <param name="includeEquivalents">Include equivalent parts (default: true)</param>
    /// <param name="brandId">Filter by vehicle brand</param>
    /// <param name="modelId">Filter by vehicle model</param>
    /// <param name="year">Filter by vehicle year</param>
    /// <param name="engineId">Filter by vehicle engine (requires exact match for fitment)</param>
    /// <param name="includeUndefinedFitment">Include variants without any fitment data (default: false)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    [HttpGet("variants")]
    public async Task<IActionResult> SearchVariants(
        [FromQuery] string? q,
        [FromQuery] Guid? warehouseId,
        [FromQuery] bool includeEquivalents = true,
        [FromQuery] Guid? brandId = null,
        [FromQuery] Guid? modelId = null,
        [FromQuery] int? year = null,
        [FromQuery] Guid? engineId = null,
        [FromQuery] bool includeUndefinedFitment = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new { results = new List<object>(), total = 0 });

        var results = await _service.SearchVariantsAsync(
            q,
            warehouseId,
            includeEquivalents,
            brandId,
            modelId,
            year,
            engineId,
            includeUndefinedFitment,
            page,
            pageSize,
            ct);

        return Ok(new
        {
            results = results.Select(r => new
            {
                variantId = r.VariantId,
                sku = r.Sku,
                barcode = r.Barcode,
                name = r.Name,
                brand = r.Brand,
                brandId = r.BrandId,
                brandCode = r.BrandCode,
                brandLogoUrl = r.BrandLogoUrl,
                isBrandActive = r.IsBrandActive,
                oemRefs = r.OemRefs,
                onHand = r.OnHand,
                reserved = r.Reserved,
                available = r.Available,
                price = r.Price,
                matchType = r.MatchType,
                matchedBy = r.MatchedBy
            }),
            total = results.Count,
            page,
            pageSize,
            query = q,
            includeEquivalents
        });
    }
}
