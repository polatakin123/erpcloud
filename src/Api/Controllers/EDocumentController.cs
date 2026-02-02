using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/e-documents")]
[Authorize]
public class EDocumentController : ControllerBase
{
    private readonly IEDocumentService _service;

    public EDocumentController(IEDocumentService service)
    {
        _service = service;
    }

    /// <summary>
    /// Create e-document from an issued invoice
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EDocumentDto>> Create([FromBody] CreateEDocumentDto dto)
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

    /// <summary>
    /// Get e-document by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EDocumentWithHistoryDto>> GetById(Guid id)
    {
        var result = await _service.GetByIdWithHistoryAsync(id);
        
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Search e-documents
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<EDocumentDto>>> Search([FromQuery] EDocumentQuery query)
    {
        var result = await _service.SearchAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Retry failed e-document
    /// </summary>
    [HttpPost("{id}/retry")]
    public async Task<ActionResult<EDocumentDto>> Retry(Guid id)
    {
        try
        {
            var result = await _service.RetryAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cancel e-document (only if not SENT/ACCEPTED)
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<EDocumentDto>> Cancel(Guid id)
    {
        try
        {
            var result = await _service.CancelAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}
