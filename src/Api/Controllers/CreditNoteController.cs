using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/credit-notes")]
public class CreditNoteController : ControllerBase
{
    private readonly CreditNoteService _creditNoteService;

    public CreditNoteController(CreditNoteService creditNoteService)
    {
        _creditNoteService = creditNoteService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCreditNote([FromBody] CreateCreditNoteRequest request, CancellationToken ct)
    {
        var result = await _creditNoteService.CreateCreditNoteAsync(
            request.Type,
            request.SourceInvoiceId,
            request.Lines.Select(l => new CreateCreditNoteLineRequest(l.Description, l.Amount, l.VariantId, l.Qty)).ToList(),
            request.IssueDate,
            request.Note,
            ct);

        if (!result.IsSuccess)
        {
            var errorCode = result.Error.Code;
            if (errorCode.EndsWith("_not_found"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });
            if (errorCode.Contains("invalid") || errorCode.Contains("mismatch") || errorCode.Contains("exceeds"))
                return Conflict(new { error = result.Error.Code, message = result.Error.Message });

            return StatusCode(500, new { error = result.Error.Code, message = result.Error.Message });
        }

        var creditNote = result.Value;
        return Ok(new
        {
            id = creditNote.Id,
            creditNoteNo = creditNote.CreditNoteNo,
            type = creditNote.Type,
            sourceInvoiceId = creditNote.SourceInvoiceId,
            partyId = creditNote.PartyId,
            issueDate = creditNote.IssueDate,
            total = creditNote.Total,
            status = creditNote.Status,
            note = creditNote.Note,
            appliedAmount = creditNote.AppliedAmount,
            remainingAmount = creditNote.RemainingAmount,
            lines = creditNote.Lines.Select(l => new
            {
                id = l.Id,
                description = l.Description,
                amount = l.Amount,
                variantId = l.VariantId,
                qty = l.Qty
            })
        });
    }

    [HttpPost("{id}/issue")]
    public async Task<IActionResult> IssueCreditNote(Guid id, CancellationToken ct)
    {
        var result = await _creditNoteService.IssueCreditNoteAsync(id, ct);

        if (!result.IsSuccess)
        {
            var errorCode = result.Error.Code;
            if (errorCode.EndsWith("_not_found"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });
            if (errorCode.Contains("invalid"))
                return Conflict(new { error = result.Error.Code, message = result.Error.Message });

            return StatusCode(500, new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = "Credit note issued successfully" });
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelCreditNote(Guid id, CancellationToken ct)
    {
        var result = await _creditNoteService.CancelCreditNoteAsync(id, ct);

        if (!result.IsSuccess)
        {
            var errorCode = result.Error.Code;
            if (errorCode.EndsWith("_not_found"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });

            return Conflict(new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = "Credit note cancelled successfully" });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCreditNote(Guid id, CancellationToken ct)
    {
        var creditNote = await _creditNoteService.GetCreditNoteByIdAsync(id, ct);

        if (creditNote == null)
            return NotFound(new { error = "credit_note_not_found", message = "Credit note not found" });

        return Ok(new
        {
            id = creditNote.Id,
            creditNoteNo = creditNote.CreditNoteNo,
            type = creditNote.Type,
            sourceInvoice = creditNote.SourceInvoice != null ? new
            {
                id = creditNote.SourceInvoice.Id,
                invoiceNo = creditNote.SourceInvoice.InvoiceNo,
                type = creditNote.SourceInvoice.Type,
                grandTotal = creditNote.SourceInvoice.GrandTotal
            } : null,
            party = creditNote.Party != null ? new
            {
                id = creditNote.Party.Id,
                name = creditNote.Party.Name
            } : null,
            issueDate = creditNote.IssueDate,
            total = creditNote.Total,
            status = creditNote.Status,
            note = creditNote.Note,
            appliedAmount = creditNote.AppliedAmount,
            remainingAmount = creditNote.RemainingAmount,
            lines = creditNote.Lines.Select(l => new
            {
                id = l.Id,
                description = l.Description,
                amount = l.Amount,
                variant = l.Variant != null ? new
                {
                    id = l.Variant.Id,
                    sku = l.Variant.Sku,
                    name = l.Variant.Name
                } : null,
                qty = l.Qty
            }),
            createdAt = creditNote.CreatedAt
        });
    }

    [HttpGet]
    public async Task<IActionResult> SearchCreditNotes(
        [FromQuery] string? type,
        [FromQuery] Guid? sourceInvoiceId,
        [FromQuery] Guid? partyId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct)
    {
        var creditNotes = await _creditNoteService.SearchCreditNotesAsync(
            type, sourceInvoiceId, partyId, status, fromDate, toDate, ct);

        return Ok(creditNotes.Select(cn => new
        {
            id = cn.Id,
            creditNoteNo = cn.CreditNoteNo,
            type = cn.Type,
            sourceInvoice = cn.SourceInvoice != null ? new
            {
                id = cn.SourceInvoice.Id,
                invoiceNo = cn.SourceInvoice.InvoiceNo
            } : null,
            party = cn.Party != null ? new
            {
                id = cn.Party.Id,
                name = cn.Party.Name
            } : null,
            issueDate = cn.IssueDate,
            total = cn.Total,
            status = cn.Status,
            appliedAmount = cn.AppliedAmount,
            remainingAmount = cn.RemainingAmount,
            createdAt = cn.CreatedAt
        }));
    }

    [HttpGet("by-invoice/{invoiceId}")]
    public async Task<IActionResult> GetCreditNotesByInvoice(Guid invoiceId, CancellationToken ct)
    {
        var creditNotes = await _creditNoteService.GetCreditNotesByInvoiceAsync(invoiceId, ct);

        return Ok(creditNotes.Select(cn => new
        {
            id = cn.Id,
            creditNoteNo = cn.CreditNoteNo,
            type = cn.Type,
            issueDate = cn.IssueDate,
            total = cn.Total,
            status = cn.Status,
            appliedAmount = cn.AppliedAmount,
            remainingAmount = cn.RemainingAmount,
            lines = cn.Lines.Select(l => new
            {
                description = l.Description,
                amount = l.Amount,
                qty = l.Qty
            })
        }));
    }
}

public record CreateCreditNoteRequest(
    string Type,
    Guid SourceInvoiceId,
    DateTime IssueDate,
    List<CreateCreditNoteLineDto> Lines,
    string? Note);

public record CreateCreditNoteLineDto(
    string Description,
    decimal Amount,
    Guid? VariantId = null,
    decimal? Qty = null);
