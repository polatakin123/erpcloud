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
}
