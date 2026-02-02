using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/cashboxes")]
public class CashboxController : ControllerBase
{
    private readonly ICashboxService _cashboxService;

    public CashboxController(ICashboxService cashboxService)
    {
        _cashboxService = cashboxService;
    }

    [HttpPost]
    public async Task<ActionResult<CashboxDto>> Create([FromBody] CreateCashboxDto dto)
    {
        try
        {
            var cashbox = await _cashboxService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = cashbox.Id }, cashbox);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "validation_failed", message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CashboxListDto>>> Search([FromQuery] CashboxSearchDto dto)
    {
        var result = await _cashboxService.SearchAsync(dto);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CashboxDto>> GetById(Guid id)
    {
        var cashbox = await _cashboxService.GetByIdAsync(id);
        if (cashbox == null)
        {
            return NotFound(new { error = "not_found", message = "Cashbox not found" });
        }
        return Ok(cashbox);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CashboxDto>> Update(Guid id, [FromBody] UpdateCashboxDto dto)
    {
        try
        {
            var cashbox = await _cashboxService.UpdateAsync(id, dto);
            return Ok(cashbox);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "validation_failed", message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            await _cashboxService.DeleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "validation_failed", message = ex.Message });
        }
    }
}
