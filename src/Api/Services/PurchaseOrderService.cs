using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public PurchaseOrderService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<PurchaseOrderDto> CreateDraftAsync(CreatePurchaseOrderDto dto)
    {
        var userId = _tenantContext.UserId ?? Guid.Empty;

        // Normalize PoNo
        var poNo = dto.PoNo.Trim().ToUpperInvariant();

        // Check unique PoNo
        var exists = await _context.PurchaseOrders.AnyAsync(p => p.PoNo == poNo);
        if (exists)
            throw new InvalidOperationException($"Purchase order number '{poNo}' already exists");

        // Validate party type (SUPPLIER or BOTH)
        var party = await _context.Parties.FindAsync(dto.PartyId);
        if (party == null)
            throw new InvalidOperationException("Party not found");

        if (party.Type != "SUPPLIER" && party.Type != "BOTH")
            throw new InvalidOperationException("Party must be a SUPPLIER or BOTH");

        // Validate referenced entities exist
        if (!await _context.Branches.AnyAsync(b => b.Id == dto.BranchId))
            throw new InvalidOperationException("Branch not found");

        if (!await _context.Warehouses.AnyAsync(w => w.Id == dto.WarehouseId))
            throw new InvalidOperationException("Warehouse not found");

        // Validate variants and check for duplicates
        var variantIds = dto.Lines.Select(l => l.VariantId).ToList();
        if (variantIds.Count != variantIds.Distinct().Count())
            throw new InvalidOperationException("Duplicate variants in lines");

        var variants = await _context.ProductVariants
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync();

        if (variants.Count != variantIds.Count)
            throw new InvalidOperationException("One or more variants not found");

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            PoNo = poNo,
            PartyId = dto.PartyId,
            BranchId = dto.BranchId,
            WarehouseId = dto.WarehouseId,
            Status = "DRAFT",
            OrderDate = dto.OrderDate,
            ExpectedDate = dto.ExpectedDate,
            Note = dto.Note,
            TenantId = _tenantContext.TenantId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        foreach (var lineDto in dto.Lines)
        {
            po.Lines.Add(new PurchaseOrderLine
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = po.Id,
                VariantId = lineDto.VariantId,
                Qty = lineDto.Qty,
                UnitCost = lineDto.UnitCost,
                VatRate = lineDto.VatRate,
                ReceivedQty = 0,
                Note = lineDto.Note,
                TenantId = _tenantContext.TenantId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            });
        }

        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(po.Id);
    }

    public async Task<PurchaseOrderDto> UpdateDraftAsync(Guid id, UpdatePurchaseOrderDto dto)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po == null)
            throw new InvalidOperationException("Purchase order not found");

        if (po.Status != "DRAFT")
            throw new InvalidOperationException("Only DRAFT purchase orders can be updated");

        // Validate variants
        var variantIds = dto.Lines.Select(l => l.VariantId).ToList();
        if (variantIds.Count != variantIds.Distinct().Count())
            throw new InvalidOperationException("Duplicate variants in lines");

        var variants = await _context.ProductVariants
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync();

        if (variants.Count != variantIds.Count)
            throw new InvalidOperationException("One or more variants not found");

        // Update header
        po.OrderDate = dto.OrderDate;
        po.ExpectedDate = dto.ExpectedDate;
        po.Note = dto.Note;

        // Replace lines
        _context.PurchaseOrderLines.RemoveRange(po.Lines);

        var userId = _tenantContext.UserId ?? Guid.Empty;
        foreach (var lineDto in dto.Lines)
        {
            po.Lines.Add(new PurchaseOrderLine
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = po.Id,
                VariantId = lineDto.VariantId,
                Qty = lineDto.Qty,
                UnitCost = lineDto.UnitCost,
                VatRate = lineDto.VatRate,
                ReceivedQty = 0,
                Note = lineDto.Note,
                TenantId = _tenantContext.TenantId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            });
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(po.Id);
    }

    public async Task<PurchaseOrderDto> ConfirmAsync(Guid id)
    {
        var po = await _context.PurchaseOrders.FindAsync(id);

        if (po == null)
            throw new InvalidOperationException("Purchase order not found");

        // Idempotency: if already CONFIRMED, return as-is
        if (po.Status == "CONFIRMED" || po.Status == "COMPLETED")
            return await GetByIdAsync(po.Id);

        if (po.Status == "CANCELLED")
            throw new InvalidOperationException("Cannot confirm a cancelled purchase order");

        po.Status = "CONFIRMED";
        await _context.SaveChangesAsync();

        return await GetByIdAsync(po.Id);
    }

    public async Task<PurchaseOrderDto> CancelAsync(Guid id)
    {
        var po = await _context.PurchaseOrders.FindAsync(id);

        if (po == null)
            throw new InvalidOperationException("Purchase order not found");

        if (po.Status != "DRAFT")
            throw new InvalidOperationException("Only DRAFT purchase orders can be cancelled");

        po.Status = "CANCELLED";
        await _context.SaveChangesAsync();

        return await GetByIdAsync(po.Id);
    }

    public async Task<PurchaseOrderDto> GetByIdAsync(Guid id)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Party)
            .Include(p => p.Branch)
            .Include(p => p.Warehouse)
            .Include(p => p.Lines)
                .ThenInclude(l => l.Variant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po == null)
            throw new InvalidOperationException("Purchase order not found");

        return MapToDto(po);
    }

    public async Task<PagedResult<PurchaseOrderListDto>> SearchAsync(PurchaseOrderSearchDto dto)
    {
        var query = _context.PurchaseOrders
            .Include(p => p.Party)
            .Include(p => p.Warehouse)
            .AsQueryable();

        // Search by PoNo
        if (!string.IsNullOrWhiteSpace(dto.Q))
        {
            var q = dto.Q.Trim();
            query = query.Where(p => EF.Functions.ILike(p.PoNo, $"%{q}%"));
        }

        // Filter by status
        if (!string.IsNullOrWhiteSpace(dto.Status))
            query = query.Where(p => p.Status == dto.Status);

        // Filter by party
        if (dto.PartyId.HasValue)
            query = query.Where(p => p.PartyId == dto.PartyId.Value);

        // Filter by date range
        if (dto.From.HasValue)
            query = query.Where(p => p.OrderDate >= dto.From.Value);

        if (dto.To.HasValue)
            query = query.Where(p => p.OrderDate <= dto.To.Value);

        // Count total
        var total = await query.CountAsync();

        // Order by date desc
        query = query.OrderByDescending(p => p.OrderDate);

        // Paginate
        var items = await query
            .Skip((dto.Page - 1) * dto.Size)
            .Take(dto.Size)
            .Select(p => new PurchaseOrderListDto
            {
                Id = p.Id,
                PoNo = p.PoNo,
                PartyName = p.Party.Name,
                WarehouseName = p.Warehouse.Name,
                Status = p.Status,
                OrderDate = p.OrderDate,
                TotalAmount = p.Lines.Sum(l => l.Qty * (l.UnitCost ?? 0)),
                LineCount = p.Lines.Count,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<PurchaseOrderListDto>(items, total, dto.Page, dto.Size);
    }

    private PurchaseOrderDto MapToDto(PurchaseOrder po)
    {
        return new PurchaseOrderDto
        {
            Id = po.Id,
            PoNo = po.PoNo,
            PartyId = po.PartyId,
            PartyName = po.Party.Name,
            BranchId = po.BranchId,
            BranchName = po.Branch.Name,
            WarehouseId = po.WarehouseId,
            WarehouseName = po.Warehouse.Name,
            Status = po.Status,
            OrderDate = po.OrderDate,
            ExpectedDate = po.ExpectedDate,
            Note = po.Note,
            TotalAmount = po.Lines.Sum(l => l.Qty * (l.UnitCost ?? 0)),
            ReceivedAmount = po.Lines.Sum(l => l.ReceivedQty * (l.UnitCost ?? 0)),
            Lines = po.Lines.Select(l => new PurchaseOrderLineDetailDto
            {
                Id = l.Id,
                VariantId = l.VariantId,
                VariantName = $"{l.Variant.Product.Name} - {l.Variant.Name}",
                VariantSku = l.Variant.Sku,
                Qty = l.Qty,
                ReceivedQty = l.ReceivedQty,
                RemainingQty = l.Qty - l.ReceivedQty,
                UnitCost = l.UnitCost,
                VatRate = l.VatRate,
                LineTotal = l.Qty * (l.UnitCost ?? 0),
                Note = l.Note
            }).ToList(),
            CreatedAt = po.CreatedAt
        };
    }
}
