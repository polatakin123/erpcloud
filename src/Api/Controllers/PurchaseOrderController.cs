using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/purchase-orders")]
[Authorize]
public class PurchaseOrderController : ControllerBase
{
    private readonly IPurchaseOrderService _service;

    public PurchaseOrderController(IPurchaseOrderService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrderDto>> Create([FromBody] CreatePurchaseOrderDto dto)
    {
        try
        {
            var result = await _service.CreateDraftAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "conflict", message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseOrderListDto>>> Search([FromQuery] PurchaseOrderSearchDto dto)
    {
        var result = await _service.SearchAsync(dto);
        return Ok(result);
    }

    [HttpGet("{id}", Name = "GetPurchaseOrder")]
    public async Task<ActionResult<PurchaseOrderDto>> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { error = "not_found", message = "Purchase order not found" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PurchaseOrderDto>> Update(Guid id, [FromBody] UpdatePurchaseOrderDto dto)
    {
        try
        {
            var result = await _service.UpdateDraftAsync(id, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "conflict", message = ex.Message });
        }
    }

    [HttpPost("{id}/confirm")]
    public async Task<ActionResult<PurchaseOrderDto>> Confirm(Guid id)
    {
        try
        {
            var result = await _service.ConfirmAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "conflict", message = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<PurchaseOrderDto>> Cancel(Guid id)
    {
        try
        {
            var result = await _service.CancelAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "conflict", message = ex.Message });
        }
    }
}
