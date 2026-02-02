using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ProductVariantsController : ControllerBase
{
    private readonly IProductVariantService _variantService;

    public ProductVariantsController(IProductVariantService variantService)
    {
        _variantService = variantService;
    }

    [HttpPost("products/{productId}/variants")]
    [Authorize(Policy = "variant.write")]
    public async Task<ActionResult<ProductVariantDto>> CreateVariant(Guid productId, [FromBody] CreateProductVariantDto dto)
    {
        try
        {
            var variant = await _variantService.CreateAsync(productId, dto);
            return CreatedAtAction(nameof(GetById), new { id = variant.Id }, variant);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("products/{productId}/variants")]
    [Authorize(Policy = "variant.read")]
    public async Task<ActionResult<PaginatedResponse<ProductVariantDto>>> GetByProduct(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string? q = null,
        [FromQuery] bool? active = null)
    {
        var result = await _variantService.GetAllByProductAsync(productId, page, size, q, active);
        return Ok(result);
    }

    [HttpGet("variants/{id}")]
    [Authorize(Policy = "variant.read")]
    public async Task<ActionResult<ProductVariantDto>> GetById(Guid id)
    {
        var variant = await _variantService.GetByIdAsync(id);
        if (variant == null) return NotFound();
        return Ok(variant);
    }

    [HttpPut("variants/{id}")]
    [Authorize(Policy = "variant.write")]
    public async Task<ActionResult<ProductVariantDto>> Update(Guid id, [FromBody] UpdateProductVariantDto dto)
    {
        try
        {
            var variant = await _variantService.UpdateAsync(id, dto);
            if (variant == null) return NotFound();
            return Ok(variant);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("variants/{id}")]
    [Authorize(Policy = "variant.write")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _variantService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
