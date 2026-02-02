using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/price-lists")]
[Authorize]
public class PriceListsController : ControllerBase
{
    private readonly IPriceListService _priceListService;

    public PriceListsController(IPriceListService priceListService)
    {
        _priceListService = priceListService;
    }

    [HttpPost]
    [Authorize(Policy = "pricelist.write")]
    public async Task<ActionResult<PriceListDto>> Create([FromBody] CreatePriceListDto dto)
    {
        try
        {
            var priceList = await _priceListService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = priceList.Id }, priceList);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = "pricelist.read")]
    public async Task<ActionResult<PaginatedResponse<PriceListDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string? q = null)
    {
        var result = await _priceListService.GetAllAsync(page, size, q);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "pricelist.read")]
    public async Task<ActionResult<PriceListDto>> GetById(Guid id)
    {
        var priceList = await _priceListService.GetByIdAsync(id);
        if (priceList == null) return NotFound();
        return Ok(priceList);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "pricelist.write")]
    public async Task<ActionResult<PriceListDto>> Update(Guid id, [FromBody] UpdatePriceListDto dto)
    {
        try
        {
            var priceList = await _priceListService.UpdateAsync(id, dto);
            if (priceList == null) return NotFound();
            return Ok(priceList);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "pricelist.write")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _priceListService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
