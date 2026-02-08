using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/purchase-returns")]
public class PurchaseReturnController : ControllerBase
{
    private readonly PurchaseReturnService _purchaseReturnService;

    public PurchaseReturnController(PurchaseReturnService purchaseReturnService)
    {
        _purchaseReturnService = purchaseReturnService;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePurchaseReturn([FromBody] CreatePurchaseReturnRequest request, CancellationToken ct)
    {
        var result = await _purchaseReturnService.CreatePurchaseReturnAsync(
            request.GoodsReceiptId,
            request.Lines.Select(l => new CreatePurchaseReturnLineRequest(l.GoodsReceiptLineId, l.Qty, l.ReasonCode)).ToList(),
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

        var purchaseReturn = result.Value;
        return Ok(new
        {
            id = purchaseReturn.Id,
            purchaseReturnNo = purchaseReturn.PurchaseReturnNo,
            goodsReceiptId = purchaseReturn.GoodsReceiptId,
            partyId = purchaseReturn.PartyId,
            warehouseId = purchaseReturn.WarehouseId,
            status = purchaseReturn.Status,
            returnDate = purchaseReturn.ReturnDate,
            note = purchaseReturn.Note,
            lines = purchaseReturn.Lines.Select(l => new
            {
                id = l.Id,
                goodsReceiptLineId = l.GoodsReceiptLineId,
                variantId = l.VariantId,
                qty = l.Qty,
                reasonCode = l.ReasonCode
            })
        });
    }

    [HttpPost("{id}/ship")]
    public async Task<IActionResult> ShipPurchaseReturn(Guid id, CancellationToken ct)
    {
        var result = await _purchaseReturnService.ShipPurchaseReturnAsync(id, ct);

        if (!result.IsSuccess)
        {
            var errorCode = result.Error.Code;
            if (errorCode.EndsWith("_not_found"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });
            if (errorCode.Contains("invalid") || errorCode.Contains("insufficient"))
                return Conflict(new { error = result.Error.Code, message = result.Error.Message });

            return StatusCode(500, new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = "Purchase return shipped successfully" });
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelPurchaseReturn(Guid id, CancellationToken ct)
    {
        var result = await _purchaseReturnService.CancelPurchaseReturnAsync(id, ct);

        if (!result.IsSuccess)
        {
            var errorCode = result.Error.Code;
            if (errorCode.EndsWith("_not_found"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return Conflict(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = "Purchase return cancelled successfully" });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPurchaseReturn(Guid id, CancellationToken ct)
    {
        var purchaseReturn = await _purchaseReturnService.GetPurchaseReturnByIdAsync(id, ct);

        if (purchaseReturn == null)
            return NotFound(new { error = "purchase_return_not_found", message = "Purchase return not found" });

        return Ok(new
        {
            id = purchaseReturn.Id,
            purchaseReturnNo = purchaseReturn.PurchaseReturnNo,
            goodsReceipt = purchaseReturn.GoodsReceipt != null ? new
            {
                id = purchaseReturn.GoodsReceipt.Id,
                grnNo = purchaseReturn.GoodsReceipt.GrnNo
            } : null,
            party = purchaseReturn.Party != null ? new
            {
                id = purchaseReturn.Party.Id,
                name = purchaseReturn.Party.Name
            } : null,
            warehouse = purchaseReturn.Warehouse != null ? new
            {
                id = purchaseReturn.Warehouse.Id,
                name = purchaseReturn.Warehouse.Name
            } : null,
            status = purchaseReturn.Status,
            returnDate = purchaseReturn.ReturnDate,
            note = purchaseReturn.Note,
            lines = purchaseReturn.Lines.Select(l => new
            {
                id = l.Id,
                goodsReceiptLineId = l.GoodsReceiptLineId,
                variant = l.Variant != null ? new
                {
                    id = l.Variant.Id,
                    sku = l.Variant.Sku,
                    name = l.Variant.Name
                } : null,
                qty = l.Qty,
                reasonCode = l.ReasonCode,
                goodsReceiptLine = l.GoodsReceiptLine != null ? new
                {
                    qty = l.GoodsReceiptLine.Qty,
                    returnedQty = l.GoodsReceiptLine.ReturnedQty,
                    remainingQty = l.GoodsReceiptLine.RemainingQty
                } : null
            }),
            createdAt = purchaseReturn.CreatedAt
        });
    }

    [HttpGet]
    public async Task<IActionResult> SearchPurchaseReturns(
        [FromQuery] Guid? goodsReceiptId,
        [FromQuery] Guid? partyId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct)
    {
        var purchaseReturns = await _purchaseReturnService.SearchPurchaseReturnsAsync(
            goodsReceiptId, partyId, status, fromDate, toDate, ct);

        return Ok(purchaseReturns.Select(pr => new
        {
            id = pr.Id,
            purchaseReturnNo = pr.PurchaseReturnNo,
            goodsReceipt = pr.GoodsReceipt != null ? new
            {
                id = pr.GoodsReceipt.Id,
                grnNo = pr.GoodsReceipt.GrnNo
            } : null,
            party = pr.Party != null ? new
            {
                id = pr.Party.Id,
                name = pr.Party.Name
            } : null,
            warehouse = pr.Warehouse != null ? new
            {
                id = pr.Warehouse.Id,
                name = pr.Warehouse.Name
            } : null,
            status = pr.Status,
            returnDate = pr.ReturnDate,
            createdAt = pr.CreatedAt
        }));
    }
}

public record CreatePurchaseReturnRequest(
    Guid GoodsReceiptId,
    DateTime ReturnDate,
    List<CreatePurchaseReturnLineDto> Lines,
    string? Note);

public record CreatePurchaseReturnLineDto(
    Guid GoodsReceiptLineId,
    decimal Qty,
    string? ReasonCode);
