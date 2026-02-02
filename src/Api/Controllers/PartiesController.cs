using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/parties")]
[Authorize]
public class PartiesController : ControllerBase
{
    private readonly IPartyService _service;

    public PartiesController(IPartyService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Policy = "party.write")]
    public async Task<ActionResult<PartyDto>> Create([FromBody] CreatePartyDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = "party.read")]
    public async Task<ActionResult<PaginatedResponse<PartyDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int size = 50,
        [FromQuery] string? q = null,
        [FromQuery] string? type = null)
    {
        size = Math.Min(size, 200);
        var result = await _service.GetAllAsync(page, size, q, type);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "party.read")]
    public async Task<ActionResult<PartyDto>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "party.write")]
    public async Task<ActionResult<PartyDto>> Update(Guid id, [FromBody] UpdatePartyDto dto)
    {
        try
        {
            var result = await _service.UpdateAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "party.write")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
