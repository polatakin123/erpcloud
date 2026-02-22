using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/pricing")]
[Authorize]
public class PricingController : ControllerBase
{
    private readonly IPricingService _pricingService;

    public PricingController(IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    /// <summary>
    /// Get price for a variant from a specific price list or default price list.
    /// Optionally filter by date (defaults to current date).
    /// </summary>
    /// <param name="variantId">The variant ID to get price for</param>
    /// <param name="priceListCode">Optional price list code. If not provided, uses default price list.</param>
    /// <param name="at">Optional date to get price at. If not provided, uses current date.</param>
    [HttpGet("variant/{variantId}")]
    [Authorize(Policy = "pricing.read")]
    public async Task<ActionResult<VariantPriceDto>> GetVariantPrice(
        Guid variantId,
        [FromQuery] string? priceListCode = null,
        [FromQuery] DateTime? at = null)
    {
        var price = await _pricingService.GetVariantPriceAsync(variantId, priceListCode, at);
        
        if (price == null)
        {
            return NotFound(new { message = "No price found for the specified variant and criteria." });
        }

        return Ok(price);
    }

    /// <summary>
    /// Calculate pricing for a single line item with all applicable discount rules
    /// </summary>
    /// <param name="request">Pricing calculation request</param>
    /// <returns>Full pricing breakdown including profit analysis and warnings</returns>
    [HttpPost("calculate")]
    [Authorize(Policy = "pricing.calculate")]
    public async Task<ActionResult<PricingCalculationResult>> Calculate(
        [FromBody] PricingCalculationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _pricingService.CalculateAsync(request, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Fiyat hesaplama hatası", details = ex.Message });
        }
    }

    /// <summary>
    /// Calculate pricing for multiple line items in a single batch request
    /// </summary>
    /// <param name="requests">List of pricing calculation requests</param>
    /// <returns>List of pricing results with breakdowns</returns>
    [HttpPost("calculate/batch")]
    [Authorize(Policy = "pricing.calculate")]
    public async Task<ActionResult<List<PricingCalculationResult>>> CalculateBatch(
        [FromBody] List<PricingCalculationRequest> requests,
        CancellationToken ct = default)
    {
        try
        {
            if (requests == null || requests.Count == 0)
            {
                return BadRequest(new { error = "En az bir ürün gereklidir" });
            }

            if (requests.Count > 100)
            {
                return BadRequest(new { error = "Maksimum 100 ürün için fiyat hesaplanabilir" });
            }

            var results = await _pricingService.CalculateBatchAsync(requests, ct);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Toplu fiyat hesaplama hatası", details = ex.Message });
        }
    }
}
