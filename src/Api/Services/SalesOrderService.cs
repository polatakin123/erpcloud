using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface ISalesOrderService
{
    Task<SalesOrderDto> CreateDraftAsync(CreateSalesOrderDto dto);
    Task<SalesOrderDto> UpdateDraftAsync(Guid id, UpdateSalesOrderDto dto);
    Task<SalesOrderDto> ConfirmAsync(Guid id);
    Task<SalesOrderDto> CancelAsync(Guid id);
    Task<SalesOrderDto?> GetByIdAsync(Guid id);
    Task<SalesOrderListDto> SearchAsync(SalesOrderSearchDto search);
}

public class SalesOrderService : ISalesOrderService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IStockService _stockService;

    public SalesOrderService(
        ErpDbContext context,
        ITenantContext tenantContext,
        IStockService stockService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _stockService = stockService;
    }

    public async Task<SalesOrderDto> CreateDraftAsync(CreateSalesOrderDto dto)
    {
        var tenantId = _tenantContext.TenantId;

        // Check uniqueness
        var exists = await _context.SalesOrders
            .AnyAsync(o => o.TenantId == tenantId && o.OrderNo == dto.OrderNo.Trim().ToUpperInvariant());

        if (exists)
        {
            throw new InvalidOperationException($"Order number '{dto.OrderNo}' already exists");
        }

        // Validate references
        await ValidateReferencesAsync(dto.PartyId, dto.BranchId, dto.WarehouseId, dto.PriceListId);

        // Validate lines uniqueness (same variant only once)
        var variantIds = dto.Lines.Select(l => l.VariantId).ToList();
        if (variantIds.Count != variantIds.Distinct().Count())
        {
            throw new InvalidOperationException("Duplicate variant in order lines");
        }

        var order = new SalesOrder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderNo = dto.OrderNo.Trim().ToUpperInvariant(),
            PartyId = dto.PartyId,
            BranchId = dto.BranchId,
            WarehouseId = dto.WarehouseId,
            PriceListId = dto.PriceListId,
            Status = "DRAFT",
            OrderDate = dto.OrderDate,
            Note = dto.Note?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        // Add lines
        foreach (var lineDto in dto.Lines)
        {
            var (unitPrice, vatRate) = await GetPricingAsync(lineDto.VariantId, dto.PriceListId, lineDto.UnitPrice, lineDto.VatRate);

            var line = new SalesOrderLine
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SalesOrderId = order.Id,
                VariantId = lineDto.VariantId,
                Qty = lineDto.Qty,
                UnitPrice = unitPrice,
                VatRate = vatRate,
                LineTotal = lineDto.Qty * unitPrice,
                ReservedQty = 0,
                Note = lineDto.Note?.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _tenantContext.UserId ?? Guid.Empty
            };

            order.Lines.Add(line);
        }

        _context.SalesOrders.Add(order);
        await _context.SaveChangesAsync();

        return await GetByIdRequiredAsync(order.Id);
    }

    public async Task<SalesOrderDto> UpdateDraftAsync(Guid id, UpdateSalesOrderDto dto)
    {
        var tenantId = _tenantContext.TenantId;

        var order = await _context.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

        if (order == null)
        {
            throw new InvalidOperationException("Sales order not found");
        }

        if (order.Status != "DRAFT")
        {
            throw new InvalidOperationException($"Cannot update order in status '{order.Status}'. Only DRAFT orders can be updated.");
        }

        // Check uniqueness if OrderNo changed
        if (order.OrderNo != dto.OrderNo.Trim().ToUpperInvariant())
        {
            var exists = await _context.SalesOrders
                .AnyAsync(o => o.TenantId == tenantId && o.OrderNo == dto.OrderNo.Trim().ToUpperInvariant());

            if (exists)
            {
                throw new InvalidOperationException($"Order number '{dto.OrderNo}' already exists");
            }
        }

        // Validate references
        await ValidateReferencesAsync(dto.PartyId, dto.BranchId, dto.WarehouseId, dto.PriceListId);

        // Validate lines uniqueness
        var variantIds = dto.Lines.Select(l => l.VariantId).ToList();
        if (variantIds.Count != variantIds.Distinct().Count())
        {
            throw new InvalidOperationException("Duplicate variant in order lines");
        }

        // Update header
        order.OrderNo = dto.OrderNo.Trim().ToUpperInvariant();
        order.PartyId = dto.PartyId;
        order.BranchId = dto.BranchId;
        order.WarehouseId = dto.WarehouseId;
        order.PriceListId = dto.PriceListId;
        order.OrderDate = dto.OrderDate;
        order.Note = dto.Note?.Trim();

        // Remove all existing lines
        _context.SalesOrderLines.RemoveRange(order.Lines);

        // Add new lines
        order.Lines.Clear();
        foreach (var lineDto in dto.Lines)
        {
            var (unitPrice, vatRate) = await GetPricingAsync(lineDto.VariantId, dto.PriceListId, lineDto.UnitPrice, lineDto.VatRate);

            var line = new SalesOrderLine
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SalesOrderId = order.Id,
                VariantId = lineDto.VariantId,
                Qty = lineDto.Qty,
                UnitPrice = unitPrice,
                VatRate = vatRate,
                LineTotal = lineDto.Qty * unitPrice,
                ReservedQty = 0,
                Note = lineDto.Note?.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _tenantContext.UserId ?? Guid.Empty
            };

            order.Lines.Add(line);
        }

        await _context.SaveChangesAsync();

        return await GetByIdRequiredAsync(order.Id);
    }

    public async Task<SalesOrderDto> ConfirmAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = await _context.SalesOrders
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

            if (order == null)
            {
                throw new InvalidOperationException("Sales order not found");
            }

            // Idempotency: if already CONFIRMED, return success (no-op)
            if (order.Status == "CONFIRMED")
            {
                await transaction.CommitAsync();
                return await GetByIdRequiredAsync(order.Id);
            }

            if (order.Status == "CANCELLED")
            {
                throw new InvalidOperationException("Cannot confirm a cancelled order");
            }

            // Reserve stock for each line
            foreach (var line in order.Lines)
            {
                try
                {
                    await _stockService.ReserveStockAsync(new ReserveStockDto(
                        WarehouseId: order.WarehouseId,
                        VariantId: line.VariantId,
                        Qty: line.Qty,
                        ReferenceType: "SalesOrder",
                        ReferenceId: order.Id,
                        Note: $"Reserved for order {order.OrderNo}"
                    ));

                    line.ReservedQty = line.Qty;
                }
                catch (InvalidOperationException ex)
                {
                    // Insufficient stock
                    throw new InvalidOperationException($"Insufficient stock for variant {line.VariantId}: {ex.Message}");
                }
            }

            order.Status = "CONFIRMED";
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetByIdRequiredAsync(order.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<SalesOrderDto> CancelAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = await _context.SalesOrders
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

            if (order == null)
            {
                throw new InvalidOperationException("Sales order not found");
            }

            if (order.Status == "CANCELLED")
            {
                // Already cancelled, return success (idempotent)
                await transaction.CommitAsync();
                return await GetByIdRequiredAsync(order.Id);
            }

            // Release reservations if confirmed
            if (order.Status == "CONFIRMED")
            {
                foreach (var line in order.Lines.Where(l => l.ReservedQty > 0))
                {
                    await _stockService.ReleaseReservationAsync(new ReleaseReservationDto(
                        WarehouseId: order.WarehouseId,
                        VariantId: line.VariantId,
                        Qty: line.ReservedQty,
                        ReferenceType: "SalesOrder",
                        ReferenceId: order.Id,
                        Note: $"Cancelled order {order.OrderNo}"
                    ));

                    line.ReservedQty = 0;
                }
            }

            order.Status = "CANCELLED";
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetByIdRequiredAsync(order.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<SalesOrderDto?> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;

        var order = await _context.SalesOrders
            .Include(o => o.Party)
            .Include(o => o.Branch)
            .Include(o => o.Warehouse)
            .Include(o => o.PriceList)
            .Include(o => o.Lines)
                .ThenInclude(l => l.Variant)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

        if (order == null)
        {
            return null;
        }

        return MapToDto(order);
    }

    public async Task<SalesOrderListDto> SearchAsync(SalesOrderSearchDto search)
    {
        var tenantId = _tenantContext.TenantId;

        var query = _context.SalesOrders
            .Include(o => o.Party)
            .Include(o => o.Branch)
            .Include(o => o.Warehouse)
            .Include(o => o.PriceList)
            .Include(o => o.Lines)
                .ThenInclude(l => l.Variant)
            .Where(o => o.TenantId == tenantId);

        // Filter by search term (OrderNo)
        if (!string.IsNullOrWhiteSpace(search.Q))
        {
            var searchTerm = search.Q.ToUpperInvariant();
            query = query.Where(o => o.OrderNo.Contains(searchTerm));
        }

        // Filter by status
        if (!string.IsNullOrWhiteSpace(search.Status))
        {
            query = query.Where(o => o.Status == search.Status.ToUpperInvariant());
        }

        // Filter by party
        if (search.PartyId.HasValue)
        {
            query = query.Where(o => o.PartyId == search.PartyId.Value);
        }

        // Count total
        var totalCount = await query.CountAsync();

        // Apply pagination
        var pageSize = Math.Min(search.Size, 200);
        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.OrderNo)
            .Skip((search.Page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = orders.Select(MapToDto).ToList();

        return new SalesOrderListDto(items, totalCount, search.Page, pageSize);
    }

    // ==================== PRIVATE HELPERS ====================

    private async Task<SalesOrderDto> GetByIdRequiredAsync(Guid id)
    {
        var dto = await GetByIdAsync(id);
        if (dto == null)
        {
            throw new InvalidOperationException("Sales order not found");
        }
        return dto;
    }

    private async Task ValidateReferencesAsync(Guid partyId, Guid branchId, Guid warehouseId, Guid? priceListId)
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

        var warehouseExists = await _context.Warehouses.AnyAsync(w => w.Id == warehouseId && w.TenantId == tenantId);
        if (!warehouseExists)
        {
            throw new InvalidOperationException("Warehouse not found");
        }

        if (priceListId.HasValue)
        {
            var priceListExists = await _context.PriceLists.AnyAsync(pl => pl.Id == priceListId.Value && pl.TenantId == tenantId);
            if (!priceListExists)
            {
                throw new InvalidOperationException("Price list not found");
            }
        }
    }

    private async Task<(decimal unitPrice, decimal vatRate)> GetPricingAsync(
        Guid variantId,
        Guid? priceListId,
        decimal? providedUnitPrice,
        decimal? providedVatRate)
    {
        var tenantId = _tenantContext.TenantId;

        // Get variant for VAT rate
        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && v.TenantId == tenantId);

        if (variant == null)
        {
            throw new InvalidOperationException($"Product variant {variantId} not found");
        }

        var vatRate = providedVatRate ?? variant.VatRate;

        // If unit price provided, use it
        if (providedUnitPrice.HasValue)
        {
            return (providedUnitPrice.Value, vatRate);
        }

        // Otherwise, fetch from price list
        Guid? effectivePriceListId = priceListId;

        if (!effectivePriceListId.HasValue)
        {
            // Get default price list
            var defaultPriceList = await _context.PriceLists
                .FirstOrDefaultAsync(pl => pl.TenantId == tenantId && pl.IsDefault);

            effectivePriceListId = defaultPriceList?.Id;
        }

        if (!effectivePriceListId.HasValue)
        {
            throw new InvalidOperationException("No price list available and unit price not provided");
        }

        // Get price from price list
        var now = DateTime.UtcNow;
        var priceItem = await _context.PriceListItems
            .Where(i => i.TenantId == tenantId
                && i.PriceListId == effectivePriceListId.Value
                && i.VariantId == variantId
                && (i.ValidFrom == null || i.ValidFrom <= now)
                && (i.ValidTo == null || i.ValidTo >= now))
            .OrderByDescending(i => i.MinQty ?? 0)
            .FirstOrDefaultAsync();

        if (priceItem == null)
        {
            throw new InvalidOperationException($"No price found for variant {variantId} in price list");
        }

        return (priceItem.UnitPrice, vatRate);
    }

    private static SalesOrderDto MapToDto(SalesOrder order)
    {
        return new SalesOrderDto(
            Id: order.Id,
            OrderNo: order.OrderNo,
            PartyId: order.PartyId,
            PartyName: order.Party.Name,
            BranchId: order.BranchId,
            BranchName: order.Branch.Name,
            WarehouseId: order.WarehouseId,
            WarehouseName: order.Warehouse.Name,
            PriceListId: order.PriceListId,
            PriceListCode: order.PriceList?.Code,
            Status: order.Status,
            OrderDate: order.OrderDate,
            Note: order.Note,
            Lines: order.Lines.Select(l => new SalesOrderLineDto(
                Id: l.Id,
                VariantId: l.VariantId,
                Sku: l.Variant.Sku,
                VariantName: l.Variant.Name,
                Qty: l.Qty,
                UnitPrice: l.UnitPrice,
                VatRate: l.VatRate,
                LineTotal: l.LineTotal,
                ReservedQty: l.ReservedQty,
                Note: l.Note
            )).ToList(),
            CreatedAt: order.CreatedAt
        );
    }
}
