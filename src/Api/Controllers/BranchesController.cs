using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Authorize]
public class BranchesController : ControllerBase
{
    private readonly IBranchService _service;

    public BranchesController(IBranchService service)
    {
        _service = service;
    }

    [HttpPost("api/orgs/{orgId}/branches")]
    [Authorize(Policy = "branch.write")]
    public async Task<ActionResult<BranchDto>> Create(Guid orgId, [FromBody] CreateBranchDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(orgId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("api/orgs/{orgId}/branches")]
    [Authorize(Policy = "branch.read")]
    public async Task<ActionResult<PaginatedResponse<BranchDto>>> GetAllByOrg(
        Guid orgId,
        [FromQuery] int page = 1,
        [FromQuery] int size = 50,
        [FromQuery] string? q = null)
    {
        size = Math.Min(size, 200);
        var result = await _service.GetAllByOrgAsync(orgId, page, size, q);
        return Ok(result);
    }

    [HttpGet("api/branches/{id}")]
    [Authorize(Policy = "branch.read")]
    public async Task<ActionResult<BranchDto>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("api/branches/{id}")]
    [Authorize(Policy = "branch.write")]
    public async Task<ActionResult<BranchDto>> Update(Guid id, [FromBody] UpdateBranchDto dto)
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

    [HttpDelete("api/branches/{id}")]
    [Authorize(Policy = "branch.write")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
