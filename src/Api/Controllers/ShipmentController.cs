using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/shipments")]
[Authorize]
public class ShipmentController : ControllerBase
{
    private readonly IShipmentService _shipmentService;
    private readonly IShipmentInvoicingService _shipmentInvoicingService;

    public ShipmentController(
        IShipmentService shipmentService,
        IShipmentInvoicingService shipmentInvoicingService)
    {
        _shipmentService = shipmentService;
        _shipmentInvoicingService = shipmentInvoicingService;
    }

    /// <summary>
    /// Create a new shipment in DRAFT status
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ShipmentDto>> CreateShipment([FromBody] CreateShipmentDto dto)
    {
        try
        {
            var result = await _shipmentService.CreateDraftAsync(dto);
            return CreatedAtAction(nameof(GetShipment), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update a DRAFT shipment
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ShipmentDto>> UpdateShipment(Guid id, [FromBody] UpdateShipmentDto dto)
    {
        try
        {
            var result = await _shipmentService.UpdateDraftAsync(id, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Ship the shipment (DRAFT -> SHIPPED)
    /// Issues stock and releases reservation
    /// </summary>
    [HttpPost("{id}/ship")]
    public async Task<ActionResult<ShipmentDto>> Ship(Guid id)
    {
        try
        {
            var result = await _shipmentService.ShipAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cancel a DRAFT shipment
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<ShipmentDto>> Cancel(Guid id)
    {
        try
        {
            var result = await _shipmentService.CancelAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get shipment by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ShipmentDto>> GetShipment(Guid id)
    {
        try
        {
            var result = await _shipmentService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Search shipments with filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> SearchShipments(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string? q = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var (items, total) = await _shipmentService.SearchAsync(page, size, q, status, from, to);
        return Ok(new { items, total, page, size });
    }

    /// <summary>
    /// Preview invoice from shipment without creating it
    /// </summary>
    [HttpPost("{shipmentId}/invoice/preview")]
    public async Task<ActionResult<ShipmentInvoicePreviewDto>> PreviewInvoice(
        Guid shipmentId,
        [FromBody] CreateInvoiceFromShipmentDto? request = null)
    {
        try
        {
            var result = await _shipmentInvoicingService.PreviewInvoiceFromShipmentAsync(shipmentId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message.Contains("not found") ? NotFound(new { error = ex.Message })
                : ex.Message.Contains("SHIPPED") ? Conflict(new { error = ex.Message })
                : BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create draft invoice from shipment
    /// </summary>
    [HttpPost("{shipmentId}/invoice")]
    public async Task<ActionResult<InvoiceWithSourceDto>> CreateInvoice(
        Guid shipmentId,
        [FromBody] CreateInvoiceFromShipmentDto? request = null)
    {
        try
        {
            var result = await _shipmentInvoicingService.CreateDraftInvoiceFromShipmentAsync(
                shipmentId, request ?? new CreateInvoiceFromShipmentDto(null, null, null, null, null));
            return CreatedAtRoute("GetInvoice", new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message.Contains("not found") ? NotFound(new { error = ex.Message })
                : ex.Message.Contains("already") ? Conflict(new { error = ex.Message })
                : ex.Message.Contains("SHIPPED") ? Conflict(new { error = ex.Message })
                : BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all invoices created from this shipment
    /// </summary>
    [HttpGet("{shipmentId}/invoices")]
    public async Task<ActionResult<List<InvoiceWithSourceDto>>> GetShipmentInvoices(Guid shipmentId)
    {
        try
        {
            var result = await _shipmentInvoicingService.GetShipmentInvoicesAsync(shipmentId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get shipment invoicing status (how much invoiced, remaining, etc.)
    /// </summary>
    [HttpGet("{shipmentId}/invoicing-status")]
    public async Task<ActionResult<ShipmentInvoicingStatusDto>> GetInvoicingStatus(Guid shipmentId)
    {
        try
        {
            var result = await _shipmentInvoicingService.GetShipmentInvoicingStatusAsync(shipmentId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
