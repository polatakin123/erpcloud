using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IInvoiceService
{
    Task<InvoiceDto> CreateDraftAsync(CreateInvoiceDto dto);
    Task<InvoiceDto> UpdateDraftAsync(Guid id, UpdateInvoiceDto dto);
    Task<InvoiceDto> IssueAsync(Guid id);
    Task<InvoiceDto> CancelAsync(Guid id);
    Task<InvoiceDto?> GetByIdAsync(Guid id);
    Task<InvoiceListDto> SearchAsync(InvoiceSearchDto search);
}

public class InvoiceService : IInvoiceService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IShipmentInvoicingService? _shipmentInvoicingService;

    public InvoiceService(
        ErpDbContext context, 
        ITenantContext tenantContext,
        IShipmentInvoicingService? shipmentInvoicingService = null)
    {
        _context = context;
        _tenantContext = tenantContext;
        _shipmentInvoicingService = shipmentInvoicingService;
    }

    public async Task<InvoiceDto> CreateDraftAsync(CreateInvoiceDto dto)
    {
        var tenantId = _tenantContext.TenantId;

        // Check uniqueness
        var exists = await _context.Set<Invoice>()
            .AnyAsync(i => i.TenantId == tenantId && i.InvoiceNo == dto.InvoiceNo.Trim().ToUpperInvariant());

        if (exists)
        {
            throw new InvalidOperationException($"Invoice number '{dto.InvoiceNo}' already exists");
        }

        // Validate references
        await ValidateReferencesAsync(dto.PartyId, dto.BranchId);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceNo = dto.InvoiceNo.Trim().ToUpperInvariant(),
            Type = dto.Type.ToUpperInvariant(),
            PartyId = dto.PartyId,
            BranchId = dto.BranchId,
            IssueDate = dto.IssueDate.Date,
            DueDate = dto.DueDate?.Date,
            Currency = (dto.Currency ?? "TRY").ToUpperInvariant(),
            Status = "DRAFT",
            Note = dto.Note?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        // Add lines and calculate totals
        foreach (var lineDto in dto.Lines)
        {
            var line = await CreateLineAsync(invoice.Id, lineDto);
            invoice.Lines.Add(line);
        }

        CalculateTotals(invoice);

        _context.Set<Invoice>().Add(invoice);
        await _context.SaveChangesAsync();

        return await GetByIdRequiredAsync(invoice.Id);
    }

    public async Task<InvoiceDto> UpdateDraftAsync(Guid id, UpdateInvoiceDto dto)
    {
        var tenantId = _tenantContext.TenantId;

        var invoice = await _context.Set<Invoice>()
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

        if (invoice == null)
        {
            throw new InvalidOperationException("Invoice not found");
        }

        if (invoice.Status != "DRAFT")
        {
            throw new InvalidOperationException($"Cannot update invoice in status '{invoice.Status}'. Only DRAFT invoices can be updated.");
        }

        // Check uniqueness if InvoiceNo changed
        if (invoice.InvoiceNo != dto.InvoiceNo.Trim().ToUpperInvariant())
        {
            var exists = await _context.Set<Invoice>()
                .AnyAsync(i => i.TenantId == tenantId && i.InvoiceNo == dto.InvoiceNo.Trim().ToUpperInvariant());

            if (exists)
            {
                throw new InvalidOperationException($"Invoice number '{dto.InvoiceNo}' already exists");
            }
        }

        // Validate references
        await ValidateReferencesAsync(dto.PartyId, dto.BranchId);

        // Update header
        invoice.InvoiceNo = dto.InvoiceNo.Trim().ToUpperInvariant();
        invoice.Type = dto.Type.ToUpperInvariant();
        invoice.PartyId = dto.PartyId;
        invoice.BranchId = dto.BranchId;
        invoice.IssueDate = dto.IssueDate.Date;
        invoice.DueDate = dto.DueDate?.Date;
        invoice.Currency = (dto.Currency ?? "TRY").ToUpperInvariant();
        invoice.Note = dto.Note?.Trim();

        // Remove all existing lines - mark them for deletion
        foreach (var line in invoice.Lines.ToList())
        {
            _context.Entry(line).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
        }
        invoice.Lines.Clear();

        // Add new lines
        foreach (var lineDto in dto.Lines)
        {
            var line = await CreateLineFromUpdateDtoAsync(invoice.Id, lineDto);
            invoice.Lines.Add(line);
            _context.Entry(line).State = Microsoft.EntityFrameworkCore.EntityState.Added;
        }

        CalculateTotals(invoice);

        await _context.SaveChangesAsync();

        return await GetByIdRequiredAsync(invoice.Id);
    }

    public async Task<InvoiceDto> IssueAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var invoice = await _context.Set<Invoice>()
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

            if (invoice == null)
            {
                throw new InvalidOperationException("Invoice not found");
            }

            // Idempotency: if already ISSUED, return success (no-op)
            if (invoice.Status == "ISSUED")
            {
                await transaction.CommitAsync();
                return await GetByIdRequiredAsync(invoice.Id);
            }

            if (invoice.Status == "CANCELLED")
            {
                throw new InvalidOperationException("Cannot issue a cancelled invoice");
            }

            // Create ledger entry
            await CreateLedgerEntryAsync(invoice, "INVOICE", invoice.GrandTotal);

            // Update shipment invoiced qty if shipment-based invoice
            if (invoice.SourceType == "SHIPMENT" && _shipmentInvoicingService != null)
            {
                await _shipmentInvoicingService.OnInvoiceIssuedAsync(invoice.Id);
            }

            invoice.Status = "ISSUED";
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetByIdRequiredAsync(invoice.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<InvoiceDto> CancelAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var invoice = await _context.Set<Invoice>()
                .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

            if (invoice == null)
            {
                throw new InvalidOperationException("Invoice not found");
            }

            // Idempotency: if already CANCELLED, return success
            if (invoice.Status == "CANCELLED")
            {
                await transaction.CommitAsync();
                return await GetByIdRequiredAsync(invoice.Id);
            }

            if (invoice.Status != "ISSUED")
            {
                throw new InvalidOperationException("Only ISSUED invoices can be cancelled");
            }

            // Create reverse ledger entry
            await CreateLedgerEntryAsync(invoice, "INVOICE_CANCEL", -invoice.GrandTotal);

            invoice.Status = "CANCELLED";
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetByIdRequiredAsync(invoice.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;

        var invoice = await _context.Set<Invoice>()
            .Include(i => i.Party)
            .Include(i => i.Branch)
            .Include(i => i.Lines)
                .ThenInclude(l => l.Variant)
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

        if (invoice == null)
        {
            return null;
        }

        return MapToDto(invoice);
    }

    public async Task<InvoiceListDto> SearchAsync(InvoiceSearchDto search)
    {
        var tenantId = _tenantContext.TenantId;

        var query = _context.Set<Invoice>()
            .Include(i => i.Party)
            .Include(i => i.Branch)
            .Include(i => i.Lines)
                .ThenInclude(l => l.Variant)
            .Where(i => i.TenantId == tenantId);

        // Filter by search term (InvoiceNo)
        if (!string.IsNullOrWhiteSpace(search.Q))
        {
            var searchTerm = search.Q.ToUpperInvariant();
            query = query.Where(i => i.InvoiceNo.Contains(searchTerm));
        }

        // Filter by type
        if (!string.IsNullOrWhiteSpace(search.Type))
        {
            query = query.Where(i => i.Type == search.Type.ToUpperInvariant());
        }

        // Filter by status
        if (!string.IsNullOrWhiteSpace(search.Status))
        {
            query = query.Where(i => i.Status == search.Status.ToUpperInvariant());
        }

        // Filter by party
        if (search.PartyId.HasValue)
        {
            query = query.Where(i => i.PartyId == search.PartyId.Value);
        }

        // Filter by date range
        if (search.From.HasValue)
        {
            query = query.Where(i => i.IssueDate >= search.From.Value.Date);
        }

        if (search.To.HasValue)
        {
            query = query.Where(i => i.IssueDate <= search.To.Value.Date);
        }

        // Filter by payment status
        if (!string.IsNullOrWhiteSpace(search.PaymentStatus))
        {
            query = query.Where(i => i.PaymentStatus == search.PaymentStatus.ToUpperInvariant());
        }

        // Filter by open amount (invoices with outstanding balance)
        if (search.OpenOnly)
        {
            query = query.Where(i => i.OpenAmount > 0);
        }

        // Count total
        var totalCount = await query.CountAsync();

        // Apply pagination
        var pageSize = Math.Min(search.Size, 200);
        var invoices = await query
            .OrderByDescending(i => i.IssueDate)
            .ThenByDescending(i => i.InvoiceNo)
            .Skip((search.Page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = invoices.Select(MapToDto).ToList();

        return new InvoiceListDto(items, totalCount, search.Page, pageSize);
    }

    // ==================== PRIVATE HELPERS ====================

    private async Task<InvoiceDto> GetByIdRequiredAsync(Guid id)
    {
        var dto = await GetByIdAsync(id);
        if (dto == null)
        {
            throw new InvalidOperationException("Invoice not found");
        }
        return dto;
    }

    private async Task ValidateReferencesAsync(Guid partyId, Guid branchId)
    {
        var tenantId = _tenantContext.TenantId;

        var partyExists = await _context.Parties.AnyAsync(p => p.Id == partyId && p.TenantId == tenantId);
        if (!partyExists)
        {
            throw new InvalidOperationException("Party not found");
        }

        var branchExists = await _context.Branches.AnyAsync(b => b.Id == branchId && b.TenantId == tenantId);
        if (!branchExists)
        {
            throw new InvalidOperationException("Branch not found");
        }
    }

    private async Task<InvoiceLine> CreateLineAsync(Guid invoiceId, CreateInvoiceLineDto dto)
    {
        var tenantId = _tenantContext.TenantId;

        // Validate variant if provided
        if (dto.VariantId.HasValue)
        {
            var variantExists = await _context.ProductVariants
                .AnyAsync(v => v.Id == dto.VariantId.Value && v.TenantId == tenantId);
            if (!variantExists)
            {
                throw new InvalidOperationException($"Product variant {dto.VariantId} not found");
            }
        }

        // Calculate line total
        decimal lineTotal;
        if (dto.LineTotal.HasValue)
        {
            lineTotal = dto.LineTotal.Value;
        }
        else if (dto.Qty.HasValue && dto.UnitPrice.HasValue)
        {
            lineTotal = dto.Qty.Value * dto.UnitPrice.Value;
        }
        else
        {
            throw new InvalidOperationException("Either provide LineTotal or both Qty and UnitPrice");
        }

        // Calculate VAT amount (rounded to 2 decimals)
        var vatAmount = Math.Round(lineTotal * dto.VatRate / 100, 2);

        return new InvoiceLine
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceId = invoiceId,
            VariantId = dto.VariantId,
            Description = dto.Description.Trim(),
            Qty = dto.Qty,
            UnitPrice = dto.UnitPrice,
            VatRate = dto.VatRate,
            LineTotal = lineTotal,
            VatAmount = vatAmount,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };
    }

    private async Task<InvoiceLine> CreateLineFromUpdateDtoAsync(Guid invoiceId, UpdateInvoiceLineDto dto)
    {
        var tenantId = _tenantContext.TenantId;

        // Validate variant if provided
        if (dto.VariantId.HasValue)
        {
            var variantExists = await _context.ProductVariants
                .AnyAsync(v => v.Id == dto.VariantId.Value && v.TenantId == tenantId);
            if (!variantExists)
            {
                throw new InvalidOperationException($"Product variant {dto.VariantId} not found");
            }
        }

        // Calculate line total
        decimal lineTotal;
        if (dto.LineTotal.HasValue)
        {
            lineTotal = dto.LineTotal.Value;
        }
        else if (dto.Qty.HasValue && dto.UnitPrice.HasValue)
        {
            lineTotal = dto.Qty.Value * dto.UnitPrice.Value;
        }
        else
        {
            throw new InvalidOperationException("Either provide LineTotal or both Qty and UnitPrice");
        }

        // Calculate VAT amount (rounded to 2 decimals)
        var vatAmount = Math.Round(lineTotal * dto.VatRate / 100, 2);

        return new InvoiceLine
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceId = invoiceId,
            VariantId = dto.VariantId,
            Description = dto.Description.Trim(),
            Qty = dto.Qty,
            UnitPrice = dto.UnitPrice,
            VatRate = dto.VatRate,
            LineTotal = lineTotal,
            VatAmount = vatAmount,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };
    }

    private void CalculateTotals(Invoice invoice)
    {
        invoice.Subtotal = invoice.Lines.Sum(l => l.LineTotal);
        invoice.VatTotal = invoice.Lines.Sum(l => l.VatAmount);
        invoice.GrandTotal = invoice.Subtotal + invoice.VatTotal;
    }

    private async Task CreateLedgerEntryAsync(Invoice invoice, string sourceType, decimal amountMultiplier)
    {
        var tenantId = _tenantContext.TenantId;

        // Check idempotency: if entry already exists, skip
        var exists = await _context.Set<PartyLedgerEntry>()
            .AnyAsync(e => e.TenantId == tenantId 
                && e.SourceType == sourceType 
                && e.SourceId == invoice.Id);

        if (exists)
        {
            // Idempotent: entry already created, skip
            return;
        }

        // Calculate signed amount based on invoice type
        decimal amountSigned;
        if (invoice.Type == "SALES")
        {
            // Sales invoice: party owes us (+)
            amountSigned = amountMultiplier;
        }
        else // PURCHASE
        {
            // Purchase invoice: we owe party (-)
            amountSigned = -amountMultiplier;
        }

        var entry = new PartyLedgerEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PartyId = invoice.PartyId,
            BranchId = invoice.BranchId,
            OccurredAt = DateTime.UtcNow,
            SourceType = sourceType,
            SourceId = invoice.Id,
            Description = $"{invoice.Type} Invoice {invoice.InvoiceNo}",
            AmountSigned = amountSigned,
            Currency = invoice.Currency,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.Set<PartyLedgerEntry>().Add(entry);
    }

    private static InvoiceDto MapToDto(Invoice invoice)
    {
        return new InvoiceDto(
            Id: invoice.Id,
            InvoiceNo: invoice.InvoiceNo,
            Type: invoice.Type,
            PartyId: invoice.PartyId,
            PartyName: invoice.Party.Name,
            BranchId: invoice.BranchId,
            BranchName: invoice.Branch.Name,
            IssueDate: invoice.IssueDate,
            DueDate: invoice.DueDate,
            Currency: invoice.Currency,
            Status: invoice.Status,
            Subtotal: invoice.Subtotal,
            VatTotal: invoice.VatTotal,
            GrandTotal: invoice.GrandTotal,
            Note: invoice.Note,
            Lines: invoice.Lines.Select(l => new InvoiceLineDto(
                Id: l.Id,
                VariantId: l.VariantId,
                Sku: l.Variant?.Sku,
                Description: l.Description,
                Qty: l.Qty,
                UnitPrice: l.UnitPrice,
                VatRate: l.VatRate,
                LineTotal: l.LineTotal,
                VatAmount: l.VatAmount
            )).ToList(),
            CreatedAt: invoice.CreatedAt,
            PaidAmount: invoice.PaidAmount,
            OpenAmount: invoice.OpenAmount,
            PaymentStatus: invoice.PaymentStatus
        );
    }
}
