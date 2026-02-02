using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IShipmentInvoicingService
{
    Task<ShipmentInvoicePreviewDto> PreviewInvoiceFromShipmentAsync(Guid shipmentId, CreateInvoiceFromShipmentDto? request);
    Task<InvoiceWithSourceDto> CreateDraftInvoiceFromShipmentAsync(Guid shipmentId, CreateInvoiceFromShipmentDto request);
    Task OnInvoiceIssuedAsync(Guid invoiceId);
    Task<List<InvoiceWithSourceDto>> GetShipmentInvoicesAsync(Guid shipmentId);
    Task<ShipmentInvoicingStatusDto> GetShipmentInvoicingStatusAsync(Guid shipmentId);
}

public class ShipmentInvoicingService : IShipmentInvoicingService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public ShipmentInvoicingService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<ShipmentInvoicePreviewDto> PreviewInvoiceFromShipmentAsync(
        Guid shipmentId, CreateInvoiceFromShipmentDto? request)
    {
        var tenantId = _tenantContext.TenantId;

        // Get shipment with all necessary data
        var shipment = await _context.Shipments
            .Include(s => s.Lines).ThenInclude(l => l.Variant)
            .Include(s => s.Lines).ThenInclude(l => l.SalesOrderLine)
            .Include(s => s.SalesOrder)
            .FirstOrDefaultAsync(s => s.Id == shipmentId && s.TenantId == tenantId);

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found");
        }

        if (shipment.Status != "SHIPPED")
        {
            throw new InvalidOperationException("Only SHIPPED shipments can be invoiced");
        }

        // Determine which lines to invoice
        var linesToInvoice = await GetLinesToInvoiceAsync(shipment, request?.Lines);

        // Calculate totals
        var previewLines = new List<ShipmentInvoiceLinePreviewDto>();
        decimal subtotal = 0;
        decimal vatTotal = 0;

        foreach (var (shipmentLine, qty) in linesToInvoice)
        {
            var orderLine = shipmentLine.SalesOrderLine!;
            var unitPrice = orderLine.UnitPrice;
            var vatRate = orderLine.VatRate;

            var lineTotal = qty * unitPrice;
            var lineVat = lineTotal * (vatRate / 100);

            subtotal += lineTotal;
            vatTotal += lineVat;

            previewLines.Add(new ShipmentInvoiceLinePreviewDto(
                shipmentLine.Id,
                orderLine.Id,
                shipmentLine.VariantId,
                shipmentLine.Variant?.Name ?? "Unknown",
                qty,
                unitPrice,
                vatRate,
                lineTotal,
                lineVat,
                $"{shipmentLine.Variant?.Name ?? "Item"} - Shipment {shipment.ShipmentNo}"
            ));
        }

        return new ShipmentInvoicePreviewDto(
            subtotal,
            vatTotal,
            subtotal + vatTotal,
            previewLines
        );
    }

    public async Task<InvoiceWithSourceDto> CreateDraftInvoiceFromShipmentAsync(
        Guid shipmentId, CreateInvoiceFromShipmentDto request)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _tenantContext.UserId ?? Guid.Empty;

        // Validate invoice number uniqueness
        var invoiceNo = request.InvoiceNo.ToUpperInvariant();
        var invoiceExists = await _context.Invoices.AnyAsync(i => 
            i.InvoiceNo == invoiceNo && i.TenantId == tenantId);
        
        if (invoiceExists)
        {
            throw new InvalidOperationException($"Invoice number '{invoiceNo}' already exists");
        }

        // Get shipment with all necessary data
        var shipment = await _context.Shipments
            .Include(s => s.Lines).ThenInclude(l => l.Variant)
            .Include(s => s.Lines).ThenInclude(l => l.SalesOrderLine)
            .Include(s => s.SalesOrder)
            .FirstOrDefaultAsync(s => s.Id == shipmentId && s.TenantId == tenantId);

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found");
        }

        if (shipment.Status != "SHIPPED")
        {
            throw new InvalidOperationException("Only SHIPPED shipments can be invoiced");
        }

        // Determine which lines to invoice
        var linesToInvoice = await GetLinesToInvoiceAsync(shipment, request.Lines);

        // Create invoice
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceNo = invoiceNo,
            Type = "SALES",
            PartyId = shipment.SalesOrder!.PartyId,
            BranchId = shipment.BranchId,
            IssueDate = request.IssueDate == default ? DateTime.Today : request.IssueDate.Date,
            DueDate = request.DueDate,
            Currency = "TRY",
            Status = "DRAFT",
            SourceType = "SHIPMENT",
            SourceId = (Guid?)shipmentId,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            Lines = new List<InvoiceLine>()
        };

        decimal subtotal = 0;
        decimal vatTotal = 0;

        foreach (var (shipmentLine, qty) in linesToInvoice)
        {
            var orderLine = shipmentLine.SalesOrderLine!;
            var unitPrice = orderLine.UnitPrice;
            var vatRate = orderLine.VatRate;

            var lineTotal = qty * unitPrice;
            var lineVat = lineTotal * (vatRate / 100);

            subtotal += lineTotal;
            vatTotal += lineVat;

            var invoiceLine = new InvoiceLine
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                InvoiceId = invoice.Id,
                ShipmentLineId = (Guid?)shipmentLine.Id,
                SalesOrderLineId = (Guid?)orderLine.Id,
                VariantId = (Guid?)shipmentLine.VariantId,
                Description = $"{shipmentLine.Variant?.Name ?? "Item"} - Shipment {shipment.ShipmentNo}",
                Qty = qty,
                UnitPrice = unitPrice,
                VatRate = vatRate,
                LineTotal = lineTotal,
                VatAmount = lineVat,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            invoice.Lines.Add(invoiceLine);
        }

        invoice.Subtotal = subtotal;
        invoice.VatTotal = vatTotal;
        invoice.GrandTotal = subtotal + vatTotal;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        return await MapToInvoiceWithSourceDtoAsync(invoice);
    }

    public async Task OnInvoiceIssuedAsync(Guid invoiceId)
    {
        var tenantId = _tenantContext.TenantId;

        // Get invoice with lines
        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId);

        if (invoice == null || invoice.SourceType != "SHIPMENT")
        {
            return; // Not a shipment-based invoice, skip
        }

        // Update ShipmentLine.InvoicedQty for each line
        foreach (var invoiceLine in invoice.Lines.Where(l => l.ShipmentLineId.HasValue))
        {
            var shipmentLine = await _context.ShipmentLines
                .FirstOrDefaultAsync(sl => sl.Id == invoiceLine.ShipmentLineId!.Value && sl.TenantId == tenantId);

            if (shipmentLine == null)
            {
                throw new InvalidOperationException($"ShipmentLine {invoiceLine.ShipmentLineId} not found");
            }

            var qtyToInvoice = invoiceLine.Qty ?? 0;
            var newInvoicedQty = shipmentLine.InvoicedQty + qtyToInvoice;

            // Validate over-invoicing
            if (newInvoicedQty > shipmentLine.Qty)
            {
                throw new InvalidOperationException(
                    $"Cannot invoice {qtyToInvoice} for shipment line {shipmentLine.Id}. " +
                    $"Remaining: {shipmentLine.Qty - shipmentLine.InvoicedQty}");
            }

            shipmentLine.InvoicedQty = newInvoicedQty;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<InvoiceWithSourceDto>> GetShipmentInvoicesAsync(Guid shipmentId)
    {
        var tenantId = _tenantContext.TenantId;

        var invoices = await _context.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Party)
            .Where(i => i.SourceType == "SHIPMENT" && i.SourceId == shipmentId && i.TenantId == tenantId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        var result = new List<InvoiceWithSourceDto>();
        foreach (var invoice in invoices)
        {
            result.Add(await MapToInvoiceWithSourceDtoAsync(invoice));
        }

        return result;
    }

    public async Task<ShipmentInvoicingStatusDto> GetShipmentInvoicingStatusAsync(Guid shipmentId)
    {
        var tenantId = _tenantContext.TenantId;

        var shipment = await _context.Shipments
            .Include(s => s.Lines).ThenInclude(l => l.Variant)
            .FirstOrDefaultAsync(s => s.Id == shipmentId && s.TenantId == tenantId);

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found");
        }

        var lineStatuses = shipment.Lines.Select(l => new ShipmentLineInvoicingStatusDto(
            l.Id,
            l.VariantId,
            l.Variant?.Name ?? "Unknown",
            l.Qty,
            l.InvoicedQty,
            l.Qty - l.InvoicedQty
        )).ToList();

        var totalQty = shipment.Lines.Sum(l => l.Qty);
        var invoicedQty = shipment.Lines.Sum(l => l.InvoicedQty);
        var isFullyInvoiced = shipment.Lines.All(l => l.InvoicedQty >= l.Qty);

        return new ShipmentInvoicingStatusDto(
            isFullyInvoiced,
            totalQty,
            invoicedQty,
            totalQty - invoicedQty,
            lineStatuses
        );
    }

    private async Task<List<(ShipmentLine Line, decimal Qty)>> GetLinesToInvoiceAsync(
        Shipment shipment, List<ShipmentInvoiceLineRequestDto>? requestLines)
    {
        var result = new List<(ShipmentLine, decimal)>();

        if (requestLines == null || requestLines.Count == 0)
        {
            // Invoice all remaining qty
            foreach (var line in shipment.Lines)
            {
                var remaining = line.Qty - line.InvoicedQty;
                if (remaining > 0)
                {
                    result.Add((line, remaining));
                }
            }
        }
        else
        {
            // Invoice specified lines
            foreach (var requestLine in requestLines)
            {
                var shipmentLine = shipment.Lines.FirstOrDefault(l => l.Id == requestLine.ShipmentLineId);
                
                if (shipmentLine == null)
                {
                    throw new InvalidOperationException(
                        $"ShipmentLine {requestLine.ShipmentLineId} does not belong to shipment {shipment.Id}");
                }

                var remaining = shipmentLine.Qty - shipmentLine.InvoicedQty;
                
                if (requestLine.Qty <= 0)
                {
                    throw new InvalidOperationException("Quantity must be greater than 0");
                }

                if (requestLine.Qty > remaining)
                {
                    throw new InvalidOperationException(
                        $"Cannot invoice {requestLine.Qty} for shipment line {shipmentLine.Id}. " +
                        $"Remaining: {remaining}");
                }

                result.Add((shipmentLine, requestLine.Qty));
            }
        }

        // Check for already invoiced lines (unique constraint violation)
        var shipmentLineIds = result.Select(r => r.Item1.Id).ToList();
        var existingInvoiceLines = await _context.InvoiceLines
            .Where(il => shipmentLineIds.Contains(il.ShipmentLineId!.Value) && il.TenantId == _tenantContext.TenantId)
            .Select(il => il.ShipmentLineId!.Value)
            .ToListAsync();

        if (existingInvoiceLines.Any())
        {
            throw new InvalidOperationException(
                $"ShipmentLine(s) already invoiced: {string.Join(", ", existingInvoiceLines)}");
        }

        if (result.Count == 0)
        {
            throw new InvalidOperationException("No lines to invoice. All lines are fully invoiced.");
        }

        return result;
    }

    private async Task<InvoiceWithSourceDto> MapToInvoiceWithSourceDtoAsync(Invoice invoice)
    {
        var party = invoice.Party ?? await _context.Parties.FindAsync(invoice.PartyId);

        return new InvoiceWithSourceDto(
            invoice.Id,
            invoice.InvoiceNo,
            invoice.Type,
            invoice.PartyId,
            party?.Name ?? "Unknown",
            invoice.BranchId,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.Currency,
            invoice.Status,
            invoice.SourceType,
            invoice.SourceId,
            invoice.Subtotal,
            invoice.VatTotal,
            invoice.GrandTotal,
            invoice.Note,
            invoice.Lines.Select(l => new InvoiceLineWithSourceDto(
                l.Id,
                l.InvoiceId,
                l.ShipmentLineId,
                l.SalesOrderLineId,
                l.VariantId,
                l.Description,
                l.Qty,
                l.UnitPrice,
                l.VatRate,
                l.LineTotal,
                l.VatAmount
            )).ToList(),
            invoice.CreatedAt,
            invoice.CreatedBy
        );
    }
}
