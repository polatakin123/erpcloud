using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.BuildingBlocks.Common;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public class CreditNoteService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CreditNoteService(
        ErpDbContext context,
        ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Result<CreditNote>> CreateCreditNoteAsync(
        string type, // SALES | PURCHASE
        Guid sourceInvoiceId,
        List<CreateCreditNoteLineRequest> lines,
        DateTime issueDate,
        string? note,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        type = type.ToUpperInvariant();

        if (type != "SALES" && type != "PURCHASE")
            return Result<CreditNote>.Failure(Error.Validation("invalid_type", "Type must be SALES or PURCHASE"));

        // Validate source invoice
        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Id == sourceInvoiceId, ct);

        if (invoice == null)
            return Result<CreditNote>.Failure(Error.NotFound("invoice_not_found", "Source invoice not found"));

        if (invoice.Type != type)
            return Result<CreditNote>.Failure(
                Error.Validation("type_mismatch", $"Invoice type {invoice.Type} does not match credit note type {type}"));

        if (invoice.Status != "ISSUED")
            return Result<CreditNote>.Failure(
                Error.Validation("invalid_invoice_status", "Can only create credit notes for ISSUED invoices"));

        // Calculate total
        var total = lines.Sum(l => l.Amount);

        // Validate total doesn't exceed invoice grand total
        var existingCreditNotesTotal = await _context.Set<CreditNote>()
            .Where(cn => cn.TenantId == tenantId && cn.SourceInvoiceId == sourceInvoiceId && cn.Status == "ISSUED")
            .SumAsync(cn => cn.Total, ct);

        if (existingCreditNotesTotal + total > invoice.GrandTotal)
            return Result<CreditNote>.Failure(
                Error.Validation("exceeds_invoice_total", 
                    $"Total credit notes ({existingCreditNotesTotal + total}) cannot exceed invoice total ({invoice.GrandTotal})"));

        // Generate credit note number
        var count = await _context.Set<CreditNote>()
            .CountAsync(cn => cn.TenantId == tenantId && cn.Type == type, ct);
        var prefix = type == "SALES" ? "SCN" : "PCN";
        var creditNoteNo = $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{count + 1:D4}";

        var creditNoteLines = lines.Select(l => new CreditNoteLine
        {
            Id = Guid.NewGuid(),
            Description = l.Description,
            Amount = l.Amount,
            VariantId = l.VariantId,
            Qty = l.Qty
        }).ToList();

        var creditNote = new CreditNote
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreditNoteNo = creditNoteNo,
            Type = type,
            SourceInvoiceId = sourceInvoiceId,
            PartyId = invoice.PartyId,
            IssueDate = issueDate,
            Total = total,
            Status = "DRAFT",
            Note = note,
            AppliedAmount = 0,
            RemainingAmount = total,
            Lines = creditNoteLines,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty // TODO: get from user context
        };

        _context.Set<CreditNote>().Add(creditNote);
        await _context.SaveChangesAsync(ct);

        return Result<CreditNote>.Success(creditNote);
    }

    public async Task<Result> IssueCreditNoteAsync(Guid creditNoteId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var creditNote = await _context.Set<CreditNote>()
            .Include(cn => cn.SourceInvoice)
            .FirstOrDefaultAsync(cn => cn.TenantId == tenantId && cn.Id == creditNoteId, ct);

        if (creditNote == null)
            return Result.Failure(Error.NotFound("credit_note_not_found", "Credit note not found"));

        if (creditNote.Status != "DRAFT")
            return Result.Failure(Error.Validation("invalid_status", "Only DRAFT credit notes can be issued"));

        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            // Update status
            creditNote.Status = "ISSUED";

            // Create party ledger entry (reverse the original invoice effect)
            // For SALES credit note: negative entry (reduces customer debt)
            // For PURCHASE credit note: positive entry (reduces supplier credit)
            var amountSigned = creditNote.Type == "SALES" ? -creditNote.Total : creditNote.Total;

            var ledgerEntry = new PartyLedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PartyId = creditNote.PartyId,
                OccurredAt = creditNote.IssueDate,
                SourceType = "CreditNote",
                SourceId = creditNote.Id,
                Description = $"Credit Note {creditNote.CreditNoteNo} for Invoice {creditNote.SourceInvoice?.InvoiceNo}",
                AmountSigned = amountSigned,
                Currency = creditNote.SourceInvoice?.Currency ?? "TRY",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty
            };

            _context.Set<PartyLedgerEntry>().Add(ledgerEntry);

            // Reduce invoice OpenAmount
            if (creditNote.SourceInvoice != null)
            {
                creditNote.SourceInvoice.OpenAmount -= creditNote.Total;
                if (creditNote.SourceInvoice.OpenAmount < 0)
                    creditNote.SourceInvoice.OpenAmount = 0;

                UpdateInvoicePaymentStatus(creditNote.SourceInvoice);
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            return Result.Failure(Error.Unexpected("issue_failed", $"Failed to issue credit note: {ex.Message}"));
        }
    }

    public async Task<Result> CancelCreditNoteAsync(Guid creditNoteId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var creditNote = await _context.Set<CreditNote>()
            .FirstOrDefaultAsync(cn => cn.TenantId == tenantId && cn.Id == creditNoteId, ct);

        if (creditNote == null)
            return Result.Failure(Error.NotFound("credit_note_not_found", "Credit note not found"));

        if (creditNote.Status == "ISSUED")
            return Result.Failure(Error.Validation("cannot_cancel_issued", "Cannot cancel issued credit note"));

        if (creditNote.Status == "CANCELLED")
            return Result.Failure(Error.Validation("already_cancelled", "Credit note already cancelled"));

        creditNote.Status = "CANCELLED";
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<CreditNote?> GetCreditNoteByIdAsync(Guid creditNoteId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _context.Set<CreditNote>()
            .Include(cn => cn.SourceInvoice)
            .Include(cn => cn.Party)
            .Include(cn => cn.Lines)
                .ThenInclude(l => l.Variant)
            .FirstOrDefaultAsync(cn => cn.TenantId == tenantId && cn.Id == creditNoteId, ct);
    }

    public async Task<List<CreditNote>> SearchCreditNotesAsync(
        string? type = null,
        Guid? sourceInvoiceId = null,
        Guid? partyId = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _context.Set<CreditNote>()
            .Include(cn => cn.SourceInvoice)
            .Include(cn => cn.Party)
            .Where(cn => cn.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(cn => cn.Type == type.ToUpperInvariant());

        if (sourceInvoiceId.HasValue)
            query = query.Where(cn => cn.SourceInvoiceId == sourceInvoiceId.Value);

        if (partyId.HasValue)
            query = query.Where(cn => cn.PartyId == partyId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(cn => cn.Status == status.ToUpperInvariant());

        if (fromDate.HasValue)
            query = query.Where(cn => cn.IssueDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(cn => cn.IssueDate <= toDate.Value);

        return await query
            .OrderByDescending(cn => cn.IssueDate)
            .ToListAsync(ct);
    }

    public async Task<List<CreditNote>> GetCreditNotesByInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _context.Set<CreditNote>()
            .Include(cn => cn.Lines)
            .Where(cn => cn.TenantId == tenantId && cn.SourceInvoiceId == invoiceId && cn.Status == "ISSUED")
            .OrderByDescending(cn => cn.IssueDate)
            .ToListAsync(ct);
    }

    private void UpdateInvoicePaymentStatus(Invoice invoice)
    {
        var tolerance = 0.01m;
        if (invoice.PaidAmount <= tolerance)
        {
            invoice.PaymentStatus = "OPEN";
        }
        else if (Math.Abs(invoice.PaidAmount - invoice.GrandTotal) <= tolerance)
        {
            invoice.PaymentStatus = "PAID";
        }
        else
        {
            invoice.PaymentStatus = "PARTIAL";
        }
    }
}

public record CreateCreditNoteLineRequest(
    string Description,
    decimal Amount,
    Guid? VariantId = null,
    decimal? Qty = null);
