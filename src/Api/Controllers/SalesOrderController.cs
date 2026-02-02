using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/sales-orders")]
[Authorize]
public class SalesOrderController : ControllerBase
{
    private readonly ISalesOrderService _salesOrderService;

    public SalesOrderController(ISalesOrderService salesOrderService)
    {
        _salesOrderService = salesOrderService;
    }

    /// <summary>
    /// Create a new sales order in DRAFT status
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "salesorder.write")]
    public async Task<ActionResult<SalesOrderDto>> CreateDraft([FromBody] CreateSalesOrderDto dto)
    {
        try
        {
            var result = await _salesOrderService.CreateDraftAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update a DRAFT sales order
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "salesorder.write")]
    public async Task<ActionResult<SalesOrderDto>> UpdateDraft(Guid id, [FromBody] UpdateSalesOrderDto dto)
    {
        try
        {
            var result = await _salesOrderService.UpdateDraftAsync(id, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = ex.Message });
            }
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Confirm sales order (reserves stock)
    /// </summary>
    [HttpPost("{id}/confirm")]
    [Authorize(Policy = "salesorder.write")]
    public async Task<ActionResult<SalesOrderDto>> Confirm(Guid id)
    {
        try
        {
            var result = await _salesOrderService.ConfirmAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = ex.Message });
            }
            if (ex.Message.Contains("Insufficient stock"))
            {
                return Conflict(new { error = "insufficient_stock", message = ex.Message });
            }
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cancel sales order (releases stock reservations)
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Policy = "salesorder.write")]
    public async Task<ActionResult<SalesOrderDto>> Cancel(Guid id)
    {
        try
        {
            var result = await _salesOrderService.CancelAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = ex.Message });
            }
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get sales order by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "salesorder.read")]
    public async Task<ActionResult<SalesOrderDto>> GetById(Guid id)
    {
        var result = await _salesOrderService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound(new { error = "Sales order not found" });
        }
        return Ok(result);
    }

    /// <summary>
    /// Search sales orders with filters and pagination
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "salesorder.read")]
    public async Task<ActionResult<SalesOrderListDto>> Search(
        [FromQuery] int page = 1,
        [FromQuery] int size = 50,
        [FromQuery] string? q = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? partyId = null)
    {
        var search = new SalesOrderSearchDto(page, size, q, status, partyId);
        var result = await _salesOrderService.SearchAsync(search);
        return Ok(result);
    }
}
