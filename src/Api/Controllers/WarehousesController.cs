using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Authorize]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _service;

    public WarehousesController(IWarehouseService service)
    {
        _service = service;
    }

    [HttpPost("api/branches/{branchId}/warehouses")]
    [Authorize(Policy = "warehouse.write")]
    public async Task<ActionResult<WarehouseDto>> Create(Guid branchId, [FromBody] CreateWarehouseDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(branchId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("api/branches/{branchId}/warehouses")]
    [Authorize(Policy = "warehouse.read")]
    public async Task<ActionResult<PaginatedResponse<WarehouseDto>>> GetAllByBranch(
        Guid branchId,
        [FromQuery] int page = 1,
        [FromQuery] int size = 50,
        [FromQuery] string? q = null)
    {
        size = Math.Min(size, 200);
        var result = await _service.GetAllByBranchAsync(branchId, page, size, q);
        return Ok(result);
    }

    [HttpGet("api/warehouses/{id}")]
    [Authorize(Policy = "warehouse.read")]
    public async Task<ActionResult<WarehouseDto>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("api/warehouses/{id}")]
    [Authorize(Policy = "warehouse.write")]
    public async Task<ActionResult<WarehouseDto>> Update(Guid id, [FromBody] UpdateWarehouseDto dto)
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

    [HttpDelete("api/warehouses/{id}")]
    [Authorize(Policy = "warehouse.write")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
