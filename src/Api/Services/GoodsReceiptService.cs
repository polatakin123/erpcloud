using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public class GoodsReceiptService : IGoodsReceiptService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IStockService _stockService;

    public GoodsReceiptService(
        ErpDbContext context,
        ITenantContext tenantContext,
        IStockService stockService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _stockService = stockService;
    }

    public async Task<GoodsReceiptDto> CreateDraftAsync(CreateGoodsReceiptDto dto)
    {
        var userId = _tenantContext.UserId ?? Guid.Empty;

        // Normalize GrnNo
        var grnNo = dto.GrnNo.Trim().ToUpperInvariant();

        // Check unique GrnNo
        var exists = await _context.GoodsReceipts.AnyAsync(g => g.GrnNo == grnNo);
        if (exists)
            throw new InvalidOperationException($"Goods receipt number '{grnNo}' already exists");

        // Load PO with lines
        var po = await _context.PurchaseOrders
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == dto.PurchaseOrderId);

        if (po == null)
            throw new InvalidOperationException("Purchase order not found");

        if (po.Status != "CONFIRMED" && po.Status != "COMPLETED")
            throw new InvalidOperationException("Purchase order must be CONFIRMED to create goods receipt");

        // Validate receipt lines
        var poLineIds = dto.Lines.Select(l => l.PurchaseOrderLineId).ToList();
        if (poLineIds.Count != poLineIds.Distinct().Count())
            throw new InvalidOperationException("Duplicate purchase order lines");

        var poLines = po.Lines.Where(l => poLineIds.Contains(l.Id)).ToList();
        if (poLines.Count != poLineIds.Count)
            throw new InvalidOperationException("One or more purchase order lines not found");

        // Validate quantities
        foreach (var lineDto in dto.Lines)
        {
            var poLine = poLines.First(l => l.Id == lineDto.PurchaseOrderLineId);
            var remaining = poLine.Qty - poLine.ReceivedQty;

            if (lineDto.Qty > remaining)
            {
                throw new InvalidOperationException(
                    $"Cannot receive {lineDto.Qty} units. Only {remaining} units remaining for variant {poLine.VariantId}");
            }
        }

        var grn = new GoodsReceipt
        {
            Id = Guid.NewGuid(),
            GrnNo = grnNo,
            PurchaseOrderId = dto.PurchaseOrderId,
            BranchId = po.BranchId,
            WarehouseId = po.WarehouseId,
            ReceiptDate = dto.ReceiptDate,
            Status = "DRAFT",
            Note = dto.Note,
            TenantId = _tenantContext.TenantId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        foreach (var lineDto in dto.Lines)
        {
            var poLine = poLines.First(l => l.Id == lineDto.PurchaseOrderLineId);

            grn.Lines.Add(new GoodsReceiptLine
            {
                Id = Guid.NewGuid(),
                GoodsReceiptId = grn.Id,
                PurchaseOrderLineId = lineDto.PurchaseOrderLineId,
                VariantId = poLine.VariantId,
                Qty = lineDto.Qty,
                UnitCost = lineDto.UnitCost ?? poLine.UnitCost,
                Note = lineDto.Note,
                TenantId = _tenantContext.TenantId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            });
        }

        _context.GoodsReceipts.Add(grn);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(grn.Id);
    }

    public async Task<GoodsReceiptDto> UpdateDraftAsync(Guid id, UpdateGoodsReceiptDto dto)
    {
        var grn = await _context.GoodsReceipts
            .Include(g => g.Lines)
            .Include(g => g.PurchaseOrder)
                .ThenInclude(p => p.Lines)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grn == null)
            throw new InvalidOperationException("Goods receipt not found");

        if (grn.Status != "DRAFT")
            throw new InvalidOperationException("Only DRAFT goods receipts can be updated");

        // Validate lines
        var poLineIds = dto.Lines.Select(l => l.PurchaseOrderLineId).ToList();
        if (poLineIds.Count != poLineIds.Distinct().Count())
            throw new InvalidOperationException("Duplicate purchase order lines");

        var poLines = grn.PurchaseOrder.Lines.Where(l => poLineIds.Contains(l.Id)).ToList();
        if (poLines.Count != poLineIds.Count)
            throw new InvalidOperationException("One or more purchase order lines not found");

        // Validate quantities
        foreach (var lineDto in dto.Lines)
        {
            var poLine = poLines.First(l => l.Id == lineDto.PurchaseOrderLineId);
            var remaining = poLine.Qty - poLine.ReceivedQty;

            if (lineDto.Qty > remaining)
            {
                throw new InvalidOperationException(
                    $"Cannot receive {lineDto.Qty} units. Only {remaining} units remaining");
            }
        }

        // Update header
        grn.ReceiptDate = dto.ReceiptDate;
        grn.Note = dto.Note;

        // Replace lines
        _context.GoodsReceiptLines.RemoveRange(grn.Lines);

        var userId = _tenantContext.UserId ?? Guid.Empty;
        foreach (var lineDto in dto.Lines)
        {
            var poLine = poLines.First(l => l.Id == lineDto.PurchaseOrderLineId);

            grn.Lines.Add(new GoodsReceiptLine
            {
                Id = Guid.NewGuid(),
                GoodsReceiptId = grn.Id,
                PurchaseOrderLineId = lineDto.PurchaseOrderLineId,
                VariantId = poLine.VariantId,
                Qty = lineDto.Qty,
                UnitCost = lineDto.UnitCost ?? poLine.UnitCost,
                Note = lineDto.Note,
                TenantId = _tenantContext.TenantId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            });
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(grn.Id);
    }

    public async Task<GoodsReceiptDto> ReceiveAsync(Guid id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var grn = await _context.GoodsReceipts
                .Include(g => g.Lines)
                .Include(g => g.PurchaseOrder)
                    .ThenInclude(p => p.Lines)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grn == null)
                throw new InvalidOperationException("Goods receipt not found");

            // Idempotency: if already RECEIVED, return as-is
            if (grn.Status == "RECEIVED")
            {
                await transaction.CommitAsync();
                return await GetByIdAsync(grn.Id);
            }

            if (grn.Status == "CANCELLED")
                throw new InvalidOperationException("Cannot receive a cancelled goods receipt");

            if (grn.Status != "DRAFT")
                throw new InvalidOperationException("Only DRAFT goods receipts can be received");

            // Validate PO is still CONFIRMED
            if (grn.PurchaseOrder.Status != "CONFIRMED" && grn.PurchaseOrder.Status != "COMPLETED")
                throw new InvalidOperationException("Purchase order is not in valid status");

            // Process each line
            foreach (var line in grn.Lines)
            {
                var poLine = grn.PurchaseOrder.Lines.First(l => l.Id == line.PurchaseOrderLineId);

                // Validate remaining quantity
                var remaining = poLine.Qty - poLine.ReceivedQty;
                if (line.Qty > remaining)
                {
                    throw new InvalidOperationException(
                        $"Over-receive detected. Requested: {line.Qty}, Available: {remaining}");
                }

                // Call StockService.ReceiveStock
                await _stockService.ReceiveStockAsync(new ReceiveStockDto(
                    grn.WarehouseId,
                    line.VariantId,
                    line.Qty,
                    line.UnitCost ?? 0,
                    "GoodsReceipt",
                    grn.Id,
                    null
                ));

                // Update PO line ReceivedQty
                poLine.ReceivedQty += line.Qty;
            }

            // Update GRN status
            grn.Status = "RECEIVED";

            // Check if PO is fully received
            var allFullyReceived = grn.PurchaseOrder.Lines.All(l => l.ReceivedQty >= l.Qty);
            if (allFullyReceived && grn.PurchaseOrder.Status == "CONFIRMED")
            {
                grn.PurchaseOrder.Status = "COMPLETED";
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetByIdAsync(grn.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<GoodsReceiptDto> CancelAsync(Guid id)
    {
        var grn = await _context.GoodsReceipts.FindAsync(id);

        if (grn == null)
            throw new InvalidOperationException("Goods receipt not found");

        if (grn.Status != "DRAFT")
            throw new InvalidOperationException("Only DRAFT goods receipts can be cancelled");

        grn.Status = "CANCELLED";
        await _context.SaveChangesAsync();

        return await GetByIdAsync(grn.Id);
    }

    public async Task<GoodsReceiptDto> GetByIdAsync(Guid id)
    {
        var grn = await _context.GoodsReceipts
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Branch)
            .Include(g => g.Warehouse)
            .Include(g => g.Lines)
                .ThenInclude(l => l.Variant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grn == null)
            throw new InvalidOperationException("Goods receipt not found");

        return MapToDto(grn);
    }

    public async Task<PagedResult<GoodsReceiptListDto>> SearchAsync(GoodsReceiptSearchDto dto)
    {
        var query = _context.GoodsReceipts
            .Include(g => g.PurchaseOrder)
            .Include(g => g.Warehouse)
            .AsQueryable();

        // Search by GrnNo
        if (!string.IsNullOrWhiteSpace(dto.Q))
        {
            var q = dto.Q.Trim();
            query = query.Where(g => EF.Functions.ILike(g.GrnNo, $"%{q}%"));
        }

        // Filter by status
        if (!string.IsNullOrWhiteSpace(dto.Status))
            query = query.Where(g => g.Status == dto.Status);

        // Filter by PO
        if (dto.PoId.HasValue)
            query = query.Where(g => g.PurchaseOrderId == dto.PoId.Value);

        // Filter by date range
        if (dto.From.HasValue)
            query = query.Where(g => g.ReceiptDate >= dto.From.Value);

        if (dto.To.HasValue)
            query = query.Where(g => g.ReceiptDate <= dto.To.Value);

        // Count total
        var total = await query.CountAsync();

        // Order by date desc
        query = query.OrderByDescending(g => g.ReceiptDate);

        // Paginate
        var items = await query
            .Skip((dto.Page - 1) * dto.Size)
            .Take(dto.Size)
            .Select(g => new GoodsReceiptListDto
            {
                Id = g.Id,
                GrnNo = g.GrnNo,
                PoNo = g.PurchaseOrder.PoNo,
                WarehouseName = g.Warehouse.Name,
                Status = g.Status,
                ReceiptDate = g.ReceiptDate,
                TotalAmount = g.Lines.Sum(l => l.Qty * (l.UnitCost ?? 0)),
                LineCount = g.Lines.Count,
                CreatedAt = g.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<GoodsReceiptListDto>(items, total, dto.Page, dto.Size);
    }

    private GoodsReceiptDto MapToDto(GoodsReceipt grn)
    {
        return new GoodsReceiptDto
        {
            Id = grn.Id,
            GrnNo = grn.GrnNo,
            PurchaseOrderId = grn.PurchaseOrderId,
            PoNo = grn.PurchaseOrder.PoNo,
            BranchId = grn.BranchId,
            BranchName = grn.Branch.Name,
            WarehouseId = grn.WarehouseId,
            WarehouseName = grn.Warehouse.Name,
            Status = grn.Status,
            ReceiptDate = grn.ReceiptDate,
            Note = grn.Note,
            TotalAmount = grn.Lines.Sum(l => l.Qty * (l.UnitCost ?? 0)),
            Lines = grn.Lines.Select(l => new GoodsReceiptLineDetailDto
            {
                Id = l.Id,
                PurchaseOrderLineId = l.PurchaseOrderLineId,
                VariantId = l.VariantId,
                VariantName = $"{l.Variant.Product.Name} - {l.Variant.Name}",
                VariantSku = l.Variant.Sku,
                Qty = l.Qty,
                UnitCost = l.UnitCost,
                LineTotal = l.Qty * (l.UnitCost ?? 0),
                Note = l.Note
            }).ToList(),
            CreatedAt = grn.CreatedAt
        };
    }
}
