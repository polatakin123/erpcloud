using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.BuildingBlocks.Common;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public class PurchaseReturnService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IStockService _stockService;

    public PurchaseReturnService(
        ErpDbContext context,
        ITenantContext tenantContext,
        IStockService stockService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _stockService = stockService;
    }

    public async Task<Result<PurchaseReturn>> CreatePurchaseReturnAsync(
        Guid goodsReceiptId,
        List<CreatePurchaseReturnLineRequest> lines,
        DateTime returnDate,
        string? note,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        // Validate goods receipt
        var goodsReceipt = await _context.GoodsReceipts
            .Include(gr => gr.Lines)
            .Include(gr => gr.PurchaseOrder)
            .FirstOrDefaultAsync(gr => gr.TenantId == tenantId && gr.Id == goodsReceiptId, ct);

        if (goodsReceipt == null)
            return Result<PurchaseReturn>.Failure(Error.NotFound("goods_receipt_not_found", "Goods receipt not found"));

        if (goodsReceipt.Status == "CANCELLED")
            return Result<PurchaseReturn>.Failure(Error.Validation("goods_receipt_cancelled", "Cannot return from cancelled goods receipt"));

        // Validate lines
        var returnLines = new List<PurchaseReturnLine>();
        foreach (var lineReq in lines)
        {
            var grLine = goodsReceipt.Lines.FirstOrDefault(l => l.Id == lineReq.GoodsReceiptLineId);
            if (grLine == null)
                return Result<PurchaseReturn>.Failure(Error.NotFound("goods_receipt_line_not_found", $"Goods receipt line {lineReq.GoodsReceiptLineId} not found"));

            var remainingQty = grLine.Qty - grLine.ReturnedQty;
            if (lineReq.Qty > remainingQty)
                return Result<PurchaseReturn>.Failure(
                    Error.Validation("over_return",
                        $"Return quantity {lineReq.Qty} exceeds remaining quantity {remainingQty}"));

            returnLines.Add(new PurchaseReturnLine
            {
                Id = Guid.NewGuid(),
                GoodsReceiptLineId = lineReq.GoodsReceiptLineId,
                VariantId = grLine.VariantId,
                Qty = lineReq.Qty,
                ReasonCode = lineReq.ReasonCode
            });
        }

        // Generate return number
        var count = await _context.Set<PurchaseReturn>()
            .CountAsync(pr => pr.TenantId == tenantId, ct);
        var returnNo = $"PR-{DateTime.UtcNow:yyyyMMdd}-{count + 1:D4}";

        var purchaseReturn = new PurchaseReturn
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PurchaseReturnNo = returnNo,
            GoodsReceiptId = goodsReceiptId,
            PartyId = goodsReceipt.PurchaseOrder.PartyId,
            WarehouseId = goodsReceipt.WarehouseId,
            Status = "DRAFT",
            ReturnDate = returnDate,
            Note = note,
            Lines = returnLines,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty // TODO: get from user context
        };

        _context.Set<PurchaseReturn>().Add(purchaseReturn);
        await _context.SaveChangesAsync(ct);

        return Result<PurchaseReturn>.Success(purchaseReturn);
    }

    public async Task<Result> ShipPurchaseReturnAsync(Guid purchaseReturnId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var purchaseReturn = await _context.Set<PurchaseReturn>()
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.GoodsReceiptLine)
            .Include(pr => pr.GoodsReceipt)
            .FirstOrDefaultAsync(pr => pr.TenantId == tenantId && pr.Id == purchaseReturnId, ct);

        if (purchaseReturn == null)
            return Result.Failure(Error.NotFound("purchase_return_not_found", "Purchase return not found"));

        if (purchaseReturn.Status != "DRAFT")
            return Result.Failure(Error.Validation("invalid_status", "Only DRAFT returns can be shipped"));

        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            // Update status
            purchaseReturn.Status = "SHIPPED";

            // Process each line
            foreach (var line in purchaseReturn.Lines)
            {
                // Update goods receipt line returned qty
                if (line.GoodsReceiptLine != null)
                {
                    line.GoodsReceiptLine.ReturnedQty += line.Qty;
                }

                // Issue stock out (OUTBOUND)
                var stockResult = await _stockService.IssueStockAsync(
                    warehouseId: purchaseReturn.WarehouseId,
                    variantId: line.VariantId,
                    qty: line.Qty,
                    referenceType: "PurchaseReturn",
                    referenceId: purchaseReturn.Id,
                    note: $"Purchase return to supplier - GRN {purchaseReturn.GoodsReceipt?.GrnNo}",
                    ct: ct);

                if (!stockResult.IsSuccess)
                {
                    await transaction.RollbackAsync(ct);
                    return Result.Failure(stockResult.Error);
                }
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            return Result.Failure(Error.Unexpected("ship_failed", $"Failed to ship purchase return: {ex.Message}"));
        }
    }

    public async Task<Result> CancelPurchaseReturnAsync(Guid purchaseReturnId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var purchaseReturn = await _context.Set<PurchaseReturn>()
            .FirstOrDefaultAsync(pr => pr.TenantId == tenantId && pr.Id == purchaseReturnId, ct);

        if (purchaseReturn == null)
            return Result.Failure(Error.NotFound("purchase_return_not_found", "Purchase return not found"));

        if (purchaseReturn.Status == "SHIPPED")
            return Result.Failure(Error.Validation("cannot_cancel_shipped", "Cannot cancel shipped purchase return"));

        if (purchaseReturn.Status == "CANCELLED")
            return Result.Failure(Error.Validation("already_cancelled", "Purchase return already cancelled"));

        purchaseReturn.Status = "CANCELLED";
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<PurchaseReturn?> GetPurchaseReturnByIdAsync(Guid purchaseReturnId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _context.Set<PurchaseReturn>()
            .Include(pr => pr.GoodsReceipt)
            .Include(pr => pr.Party)
            .Include(pr => pr.Warehouse)
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.Variant)
            .Include(pr => pr.Lines)
                .ThenInclude(l => l.GoodsReceiptLine)
            .FirstOrDefaultAsync(pr => pr.TenantId == tenantId && pr.Id == purchaseReturnId, ct);
    }

    public async Task<List<PurchaseReturn>> SearchPurchaseReturnsAsync(
        Guid? goodsReceiptId = null,
        Guid? partyId = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _context.Set<PurchaseReturn>()
            .Include(pr => pr.GoodsReceipt)
            .Include(pr => pr.Party)
            .Include(pr => pr.Warehouse)
            .Where(pr => pr.TenantId == tenantId);

        if (goodsReceiptId.HasValue)
            query = query.Where(pr => pr.GoodsReceiptId == goodsReceiptId.Value);

        if (partyId.HasValue)
            query = query.Where(pr => pr.PartyId == partyId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(pr => pr.Status == status.ToUpperInvariant());

        if (fromDate.HasValue)
            query = query.Where(pr => pr.ReturnDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(pr => pr.ReturnDate <= toDate.Value);

        return await query
            .OrderByDescending(pr => pr.ReturnDate)
            .ToListAsync(ct);
    }
}

public record CreatePurchaseReturnLineRequest(
    Guid GoodsReceiptLineId,
    decimal Qty,
    string? ReasonCode);
