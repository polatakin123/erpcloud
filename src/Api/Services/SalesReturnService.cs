using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.BuildingBlocks.Common;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public class SalesReturnService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IStockService _stockService;

    public SalesReturnService(
        ErpDbContext context,
        ITenantContext tenantContext,
        IStockService stockService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _stockService = stockService;
    }

    public async Task<Result<SalesReturn>> CreateSalesReturnAsync(
        Guid invoiceId,
        List<CreateSalesReturnLineRequest> lines,
        Guid warehouseId,
        DateTime returnDate,
        string? note,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        // Validate invoice
        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Id == invoiceId, ct);

        if (invoice == null)
            return Result<SalesReturn>.Failure(Error.NotFound("invoice_not_found", "Invoice not found"));

        if (invoice.Type != "SALES")
            return Result<SalesReturn>.Failure(Error.Validation("invalid_invoice_type", "Only SALES invoices can be returned"));

        if (invoice.Status == "CANCELLED")
            return Result<SalesReturn>.Failure(Error.Validation("invoice_cancelled", "Cannot return cancelled invoice"));

        // Validate lines
        var returnLines = new List<SalesReturnLine>();
        foreach (var lineReq in lines)
        {
            var invoiceLine = invoice.Lines.FirstOrDefault(l => l.Id == lineReq.InvoiceLineId);
            if (invoiceLine == null)
                return Result<SalesReturn>.Failure(Error.NotFound("invoice_line_not_found", $"Invoice line {lineReq.InvoiceLineId} not found"));

            if (invoiceLine.VariantId == null)
                return Result<SalesReturn>.Failure(Error.Validation("non_product_line", "Cannot return non-product lines"));

            var remainingQty = (invoiceLine.Qty ?? 0) - invoiceLine.ReturnedQty;
            if (lineReq.Qty > remainingQty)
                return Result<SalesReturn>.Failure(
                    Error.Validation("over_return", 
                        $"Return quantity {lineReq.Qty} exceeds remaining quantity {remainingQty} for line {invoiceLine.Description}"));

            returnLines.Add(new SalesReturnLine
            {
                Id = Guid.NewGuid(),
                InvoiceLineId = lineReq.InvoiceLineId,
                VariantId = invoiceLine.VariantId.Value,
                Qty = lineReq.Qty,
                ReasonCode = lineReq.ReasonCode
            });
        }

        // Generate return number
        var count = await _context.Set<SalesReturn>()
            .CountAsync(sr => sr.TenantId == tenantId, ct);
        var returnNo = $"SR-{DateTime.UtcNow:yyyyMMdd}-{count + 1:D4}";

        var salesReturn = new SalesReturn
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReturnNo = returnNo,
            SalesInvoiceId = invoiceId,
            PartyId = invoice.PartyId,
            WarehouseId = warehouseId,
            Status = "DRAFT",
            ReturnDate = returnDate,
            Note = note,
            Lines = returnLines,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty // TODO: get from user context
        };

        _context.Set<SalesReturn>().Add(salesReturn);
        await _context.SaveChangesAsync(ct);

        return Result<SalesReturn>.Success(salesReturn);
    }

    public async Task<Result> ReceiveSalesReturnAsync(Guid salesReturnId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var salesReturn = await _context.Set<SalesReturn>()
            .Include(sr => sr.Lines)
                .ThenInclude(l => l.InvoiceLine)
            .Include(sr => sr.Invoice)
            .FirstOrDefaultAsync(sr => sr.TenantId == tenantId && sr.Id == salesReturnId, ct);

        if (salesReturn == null)
            return Result.Failure(Error.NotFound("sales_return_not_found", "Sales return not found"));

        if (salesReturn.Status != "DRAFT")
            return Result.Failure(Error.Validation("invalid_status", "Only DRAFT returns can be received"));

        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            // Update status
            salesReturn.Status = "RECEIVED";

            // Process each line
            foreach (var line in salesReturn.Lines)
            {
                // Update invoice line returned qty
                if (line.InvoiceLine != null)
                {
                    line.InvoiceLine.ReturnedQty += line.Qty;
                }

                // Receive stock back (INBOUND)
                var stockResult = await _stockService.ReceiveStockAsync(
                    warehouseId: salesReturn.WarehouseId,
                    variantId: line.VariantId,
                    qty: line.Qty,
                    referenceType: "SalesReturn",
                    referenceId: salesReturn.Id,
                    note: $"Sales return from invoice {salesReturn.Invoice?.InvoiceNo}",
                    ct: ct);

                if (!stockResult.IsSuccess)
                {
                    await transaction.RollbackAsync(ct);
                    return Result.Failure(stockResult.Error);
                }
            }

            // Recalculate invoice OpenAmount
            // When items are returned, the invoice OpenAmount should increase
            // because the customer owes less money for the actual goods they kept
            var totalReturnedValue = salesReturn.Lines.Sum(l =>
            {
                var invoiceLine = l.InvoiceLine;
                if (invoiceLine == null) return 0;
                var unitPrice = invoiceLine.UnitPrice ?? 0;
                return l.Qty * unitPrice;
            });

            if (salesReturn.Invoice != null)
            {
                salesReturn.Invoice.OpenAmount += totalReturnedValue;
                // Note: We don't touch PaidAmount - that tracks actual payment allocations
                // Payment status recalculation might be needed
                UpdateInvoicePaymentStatus(salesReturn.Invoice);
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            return Result.Failure(Error.Unexpected("receive_failed", $"Failed to receive sales return: {ex.Message}"));
        }
    }

    public async Task<Result> CancelSalesReturnAsync(Guid salesReturnId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var salesReturn = await _context.Set<SalesReturn>()
            .FirstOrDefaultAsync(sr => sr.TenantId == tenantId && sr.Id == salesReturnId, ct);

        if (salesReturn == null)
            return Result.Failure(Error.NotFound("sales_return_not_found", "Sales return not found"));

        if (salesReturn.Status == "RECEIVED")
            return Result.Failure(Error.Validation("cannot_cancel_received", "Cannot cancel received sales return"));

        if (salesReturn.Status == "CANCELLED")
            return Result.Failure(Error.Validation("already_cancelled", "Sales return already cancelled"));

        salesReturn.Status = "CANCELLED";
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<SalesReturn?> GetSalesReturnByIdAsync(Guid salesReturnId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _context.Set<SalesReturn>()
            .Include(sr => sr.Invoice)
            .Include(sr => sr.Party)
            .Include(sr => sr.Warehouse)
            .Include(sr => sr.Lines)
                .ThenInclude(l => l.Variant)
            .Include(sr => sr.Lines)
                .ThenInclude(l => l.InvoiceLine)
            .FirstOrDefaultAsync(sr => sr.TenantId == tenantId && sr.Id == salesReturnId, ct);
    }

    public async Task<List<SalesReturn>> SearchSalesReturnsAsync(
        Guid? invoiceId = null,
        Guid? partyId = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _context.Set<SalesReturn>()
            .Include(sr => sr.Invoice)
            .Include(sr => sr.Party)
            .Include(sr => sr.Warehouse)
            .Where(sr => sr.TenantId == tenantId);

        if (invoiceId.HasValue)
            query = query.Where(sr => sr.SalesInvoiceId == invoiceId.Value);

        if (partyId.HasValue)
            query = query.Where(sr => sr.PartyId == partyId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(sr => sr.Status == status.ToUpperInvariant());

        if (fromDate.HasValue)
            query = query.Where(sr => sr.ReturnDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(sr => sr.ReturnDate <= toDate.Value);

        return await query
            .OrderByDescending(sr => sr.ReturnDate)
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

public record CreateSalesReturnLineRequest(
    Guid InvoiceLineId,
    decimal Qty,
    string? ReasonCode);
