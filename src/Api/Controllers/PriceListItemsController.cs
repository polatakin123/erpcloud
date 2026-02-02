using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class PriceListItemsController : ControllerBase
{
    private readonly IPriceListItemService _itemService;

    public PriceListItemsController(IPriceListItemService itemService)
    {
        _itemService = itemService;
    }

    [HttpPost("price-lists/{priceListId}/items")]
    [Authorize(Policy = "pricelist.write")]
    public async Task<ActionResult<PriceListItemDto>> CreateItem(Guid priceListId, [FromBody] CreatePriceListItemDto dto)
    {
        try
        {
            var item = await _itemService.CreateAsync(priceListId, dto);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("price-lists/{priceListId}/items")]
    [Authorize(Policy = "pricelist.read")]
    public async Task<ActionResult<PaginatedResponse<PriceListItemDto>>> GetByPriceList(
        Guid priceListId,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] Guid? variantId = null)
    {
        var result = await _itemService.GetAllByPriceListAsync(priceListId, page, size, variantId);
        return Ok(result);
    }

    [HttpGet("price-list-items/{id}")]
    [Authorize(Policy = "pricelist.read")]
    public async Task<ActionResult<PriceListItemDto>> GetById(Guid id)
    {
        var item = await _itemService.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPut("price-list-items/{id}")]
    [Authorize(Policy = "pricelist.write")]
    public async Task<ActionResult<PriceListItemDto>> Update(Guid id, [FromBody] UpdatePriceListItemDto dto)
    {
        try
        {
            var item = await _itemService.UpdateAsync(id, dto);
            if (item == null) return NotFound();
            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("price-list-items/{id}")]
    [Authorize(Policy = "pricelist.write")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _itemService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
