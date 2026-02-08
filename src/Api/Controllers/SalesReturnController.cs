using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/sales-returns")]
public class SalesReturnController : ControllerBase
{
    private readonly SalesReturnService _salesReturnService;

    public SalesReturnController(SalesReturnService salesReturnService)
    {
        _salesReturnService = salesReturnService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateSalesReturn([FromBody] CreateSalesReturnRequest request, CancellationToken ct)
    {
        var result = await _salesReturnService.CreateSalesReturnAsync(
            request.InvoiceId,
            request.Lines.Select(l => new CreateSalesReturnLineRequest(l.InvoiceLineId, l.Qty, l.ReasonCode)).ToList(),
            request.WarehouseId,
            request.ReturnDate,
            request.Note,
            ct);

        if (!result.IsSuccess)
        {
            var errorCode = result.Error.Code;
            if (errorCode.EndsWith("_not_found"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });
            if (errorCode.Contains("over_return") || errorCode.Contains("invalid") || errorCode.Contains("cancelled"))
                return Conflict(new { error = result.Error.Code, message = result.Error.Message });

            return StatusCode(500, new { error = result.Error.Code, message = result.Error.Message });
        }

        var salesReturn = result.Value;
        return Ok(new
        {
            id = salesReturn.Id,
            returnNo = salesReturn.ReturnNo,
            invoiceId = salesReturn.SalesInvoiceId,
            partyId = salesReturn.PartyId,
            warehouseId = salesReturn.WarehouseId,
            status = salesReturn.Status,
            returnDate = salesReturn.ReturnDate,
            note = salesReturn.Note,
            lines = salesReturn.Lines.Select(l => new
            {
                id = l.Id,
                invoiceLineId = l.InvoiceLineId,
                variantId = l.VariantId,
                qty = l.Qty,
                reasonCode = l.ReasonCode
            })
        });
    }

    [HttpPost("{id}/receive")]
    public async Task<IActionResult> ReceiveSalesReturn(Guid id, CancellationToken ct)
    {
        var result = await _salesReturnService.ReceiveSalesReturnAsync(id, ct);

        if (!result.IsSuccess)
        {
            var errorCode = result.Error.Code;
            if (errorCode.EndsWith("_not_found"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });
            if (errorCode.Contains("invalid") || errorCode.Contains("insufficient"))
                return Conflict(new { error = result.Error.Code, message = result.Error.Message });

            return StatusCode(500, new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = "Sales return received successfully" });
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelSalesReturn(Guid id, CancellationToken ct)
    {
        var result = await _salesReturnService.CancelSalesReturnAsync(id, ct);

        if (!result.IsSuccess)
        {
            var errorCode = result.Error.Code;
            if (errorCode.EndsWith("_not_found"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return Conflict(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = "Sales return cancelled successfully" });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSalesReturn(Guid id, CancellationToken ct)
    {
        var salesReturn = await _salesReturnService.GetSalesReturnByIdAsync(id, ct);

        if (salesReturn == null)
            return NotFound(new { error = "sales_return_not_found", message = "Sales return not found" });

        return Ok(new
        {
            id = salesReturn.Id,
            returnNo = salesReturn.ReturnNo,
            invoice = salesReturn.Invoice != null ? new
            {
                id = salesReturn.Invoice.Id,
                invoiceNo = salesReturn.Invoice.InvoiceNo,
                type = salesReturn.Invoice.Type
            } : null,
            party = salesReturn.Party != null ? new
            {
                id = salesReturn.Party.Id,
                name = salesReturn.Party.Name
            } : null,
            warehouse = salesReturn.Warehouse != null ? new
            {
                id = salesReturn.Warehouse.Id,
                name = salesReturn.Warehouse.Name
            } : null,
            status = salesReturn.Status,
            returnDate = salesReturn.ReturnDate,
            note = salesReturn.Note,
            lines = salesReturn.Lines.Select(l => new
            {
                id = l.Id,
                invoiceLineId = l.InvoiceLineId,
                variant = l.Variant != null ? new
                {
                    id = l.Variant.Id,
                    sku = l.Variant.Sku,
                    name = l.Variant.Name
                } : null,
                qty = l.Qty,
                reasonCode = l.ReasonCode,
                invoiceLine = l.InvoiceLine != null ? new
                {
                    description = l.InvoiceLine.Description,
                    unitPrice = l.InvoiceLine.UnitPrice,
                    returnedQty = l.InvoiceLine.ReturnedQty,
                    remainingQty = l.InvoiceLine.RemainingQty
                } : null
            }),
            createdAt = salesReturn.CreatedAt
        });
    }

    [HttpGet]
    public async Task<IActionResult> SearchSalesReturns(
        [FromQuery] Guid? invoiceId,
        [FromQuery] Guid? partyId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct)
    {
        var salesReturns = await _salesReturnService.SearchSalesReturnsAsync(
            invoiceId, partyId, status, fromDate, toDate, ct);

        return Ok(salesReturns.Select(sr => new
        {
            id = sr.Id,
            returnNo = sr.ReturnNo,
            invoice = sr.Invoice != null ? new
            {
                id = sr.Invoice.Id,
                invoiceNo = sr.Invoice.InvoiceNo
            } : null,
            party = sr.Party != null ? new
            {
                id = sr.Party.Id,
                name = sr.Party.Name
            } : null,
            warehouse = sr.Warehouse != null ? new
            {
                id = sr.Warehouse.Id,
                name = sr.Warehouse.Name
            } : null,
            status = sr.Status,
            returnDate = sr.ReturnDate,
            createdAt = sr.CreatedAt
        }));
    }
}

public record CreateSalesReturnRequest(
    Guid InvoiceId,
    Guid WarehouseId,
    DateTime ReturnDate,
    List<CreateSalesReturnLineDto> Lines,
    string? Note);

public record CreateSalesReturnLineDto(
    Guid InvoiceLineId,
    decimal Qty,
    string? ReasonCode);
