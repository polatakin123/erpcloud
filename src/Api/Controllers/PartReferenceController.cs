using ErpCloud.BuildingBlocks.Common;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/variants/{variantId}/references")]
public class PartReferenceController : ControllerBase
{
    private readonly PartReferenceService _service;

    public PartReferenceController(PartReferenceService service)
    {
        _service = service;
    }

    /// <summary>
    /// Add a part reference (OEM/aftermarket code) to a variant
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateReference(
        Guid variantId,
        [FromBody] CreatePartReferenceRequest request,
        CancellationToken ct)
    {
        var result = await _service.CreateReferenceAsync(
            variantId,
            request.RefType,
            request.RefCode,
            ct);

        if (result.IsFailure)
        {
            var errorCode = result.Error.Code;
            if (errorCode.EndsWith("_not_found"))
                return NotFound(new { error = errorCode, message = result.Error.Message });
            if (errorCode.Contains("duplicate"))
                return Conflict(new { error = errorCode, message = result.Error.Message });
            if (errorCode.Contains("invalid") || errorCode.Contains("required"))
                return BadRequest(new { error = errorCode, message = result.Error.Message });
            
            return StatusCode(500, new { error = errorCode, message = result.Error.Message });
        }

        return Ok(new
        {
            id = result.Value.Id,
            variantId = result.Value.VariantId,
            refType = result.Value.RefType,
            refCode = result.Value.RefCode,
            createdAt = result.Value.CreatedAt
        });
    }

    /// <summary>
    /// Get all references for a variant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReferences(Guid variantId, CancellationToken ct)
    {
        var references = await _service.GetReferencesAsync(variantId, ct);

        return Ok(references.Select(r => new
        {
            id = r.Id,
            variantId = r.VariantId,
            refType = r.RefType,
            refCode = r.RefCode,
            createdAt = r.CreatedAt
        }));
    }

    /// <summary>
    /// Delete a part reference
    /// </summary>
    [HttpDelete("{referenceId}")]
    public async Task<IActionResult> DeleteReference(
        Guid variantId,
        Guid referenceId,
        CancellationToken ct)
    {
        var result = await _service.DeleteReferenceAsync(referenceId, ct);

        if (result.IsFailure)
        {
            var errorCode = result.Error.Code;
            if (errorCode.EndsWith("_not_found"))
                return NotFound(new { error = errorCode, message = result.Error.Message });
            
            return StatusCode(500, new { error = errorCode, message = result.Error.Message });
        }

        return Ok(new { message = "Reference deleted successfully" });
    }
}

public record CreatePartReferenceRequest(string RefType, string RefCode);
