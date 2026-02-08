using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IShipmentService
{
    Task<ShipmentDto> CreateDraftAsync(CreateShipmentDto dto);
    Task<ShipmentDto> UpdateDraftAsync(Guid id, UpdateShipmentDto dto);
    Task<ShipmentDto> ShipAsync(Guid id);
    Task<ShipmentDto> CancelAsync(Guid id);
    Task<ShipmentDto> GetByIdAsync(Guid id);
    Task<(List<ShipmentDto> Items, int Total)> SearchAsync(int page, int size, string? q, string? status, DateTime? from, DateTime? to);
}

public class ShipmentService : IShipmentService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IStockService _stockService;

    public ShipmentService(ErpDbContext context, ITenantContext tenantContext, IStockService stockService)
    {
        _context = context;
        _tenantContext = tenantContext;
        _stockService = stockService;
    }

    public async Task<ShipmentDto> CreateDraftAsync(CreateShipmentDto dto)
    {
        // Normalize ShipmentNo
        var shipmentNo = dto.ShipmentNo.ToUpperInvariant();

        // Check duplicate within tenant
        var tenantId = _tenantContext.TenantId;
        var exists = await _context.Shipments.AnyAsync(s => s.ShipmentNo == shipmentNo && s.TenantId == tenantId);
        if (exists)
        {
            throw new InvalidOperationException($"Shipment number '{shipmentNo}' already exists");
        }

        // Validate sales order
        var salesOrder = await _context.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == dto.SalesOrderId);

        if (salesOrder == null)
        {
            throw new InvalidOperationException("Sales order not found");
        }

        if (salesOrder.Status != "CONFIRMED")
        {
            throw new InvalidOperationException("Sales order must be CONFIRMED");
        }

        // Validate lines
        foreach (var lineDto in dto.Lines)
        {
            var orderLine = salesOrder.Lines.FirstOrDefault(l => l.Id == lineDto.SalesOrderLineId);
            if (orderLine == null)
            {
                throw new InvalidOperationException($"Sales order line {lineDto.SalesOrderLineId} not found");
            }
        }

        // Validate references
        await ValidateReferencesAsync(dto.BranchId, dto.WarehouseId);

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            ShipmentNo = shipmentNo,
            SalesOrderId = dto.SalesOrderId,
            BranchId = dto.BranchId,
            WarehouseId = dto.WarehouseId,
            ShipmentDate = dto.ShipmentDate.Date,
            Status = "DRAFT",
            Note = dto.Note,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        foreach (var lineDto in dto.Lines)
        {
            var orderLine = salesOrder.Lines.First(l => l.Id == lineDto.SalesOrderLineId);
            var line = new ShipmentLine
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                ShipmentId = shipment.Id,
                SalesOrderLineId = lineDto.SalesOrderLineId,
                VariantId = orderLine.VariantId,
                Qty = lineDto.Qty,
                Note = lineDto.Note,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _tenantContext.UserId ?? Guid.Empty
            };
            shipment.Lines.Add(line);
        }

        _context.Shipments.Add(shipment);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(shipment.Id);
    }

    public async Task<ShipmentDto> UpdateDraftAsync(Guid id, UpdateShipmentDto dto)
    {
        var tenantId = _tenantContext.TenantId;
        var shipment = await _context.Shipments
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found");
        }

        if (shipment.Status != "DRAFT")
        {
            throw new InvalidOperationException("Only DRAFT shipments can be updated");
        }

        // Normalize ShipmentNo
        var shipmentNo = dto.ShipmentNo.ToUpperInvariant();

        // Check duplicate within tenant (excluding current)
        var exists = await _context.Shipments.AnyAsync(s => s.ShipmentNo == shipmentNo && s.Id != id && s.TenantId == tenantId);
        if (exists)
        {
            throw new InvalidOperationException($"Shipment number '{shipmentNo}' already exists");
        }

        // Validate sales order lines
        var salesOrder = await _context.SalesOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == shipment.SalesOrderId);

        if (salesOrder == null)
        {
            throw new InvalidOperationException("Sales order not found");
        }

        foreach (var lineDto in dto.Lines)
        {
            var orderLine = salesOrder.Lines.FirstOrDefault(l => l.Id == lineDto.SalesOrderLineId);
            if (orderLine == null)
            {
                throw new InvalidOperationException($"Sales order line {lineDto.SalesOrderLineId} not found");
            }
        }

        await ValidateReferencesAsync(dto.BranchId, dto.WarehouseId);

        shipment.ShipmentNo = shipmentNo;
        shipment.BranchId = dto.BranchId;
        shipment.WarehouseId = dto.WarehouseId;
        shipment.ShipmentDate = dto.ShipmentDate.Date;
        shipment.Note = dto.Note;

        // Remove old lines
        _context.ShipmentLines.RemoveRange(shipment.Lines);

        // Add new lines
        shipment.Lines.Clear();
        foreach (var lineDto in dto.Lines)
        {
            var orderLine = salesOrder.Lines.First(l => l.Id == lineDto.SalesOrderLineId);
            var line = new ShipmentLine
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                ShipmentId = shipment.Id,
                SalesOrderLineId = lineDto.SalesOrderLineId,
                VariantId = orderLine.VariantId,
                Qty = lineDto.Qty,
                Note = lineDto.Note,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _tenantContext.UserId ?? Guid.Empty
            };
            shipment.Lines.Add(line);
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(shipment.Id);
    }

    public async Task<ShipmentDto> ShipAsync(Guid id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var tenantId = _tenantContext.TenantId;
            var shipment = await _context.Shipments
                .Include(s => s.Lines)
                .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);

            if (shipment == null)
            {
                throw new InvalidOperationException("Shipment not found");
            }

            // Idempotency: if already SHIPPED, return OK
            if (shipment.Status == "SHIPPED")
            {
                await transaction.CommitAsync();
                return await GetByIdAsync(shipment.Id);
            }

            if (shipment.Status != "DRAFT")
            {
                throw new InvalidOperationException("Only DRAFT shipments can be shipped");
            }

            // Load sales order with lines
            var salesOrder = await _context.SalesOrders
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == shipment.SalesOrderId);

            if (salesOrder == null)
            {
                throw new InvalidOperationException("Sales order not found");
            }

            // Validate and process each line
            foreach (var shipmentLine in shipment.Lines)
            {
                var orderLine = salesOrder.Lines.FirstOrDefault(l => l.Id == shipmentLine.SalesOrderLineId);
                if (orderLine == null)
                {
                    throw new InvalidOperationException($"Sales order line {shipmentLine.SalesOrderLineId} not found");
                }

                // Check available quantity to ship
                var availableToShip = orderLine.ReservedQty - orderLine.ShippedQty;
                if (shipmentLine.Qty > availableToShip)
                {
                    throw new InvalidOperationException($"Insufficient reserved quantity for variant {shipmentLine.VariantId}. Available: {availableToShip}, Requested: {shipmentLine.Qty}");
                }

                // Release reservation
                await _stockService.ReleaseReservationAsync(new ReleaseReservationDto(
                    shipment.WarehouseId,  // WarehouseId first
                    shipmentLine.VariantId, // VariantId second
                    shipmentLine.Qty,
                    "Shipment",
                    shipment.Id,
                    null
                ));

                // Issue stock (physical stock reduction)
                await _stockService.IssueStockAsync(new IssueStockDto(
                    shipment.WarehouseId,  // WarehouseId first
                    shipmentLine.VariantId, // VariantId second
                    shipmentLine.Qty,
                    "Shipment",
                    shipment.Id,
                    null
                ));

                // Update sales order line
                orderLine.ReservedQty -= shipmentLine.Qty;
                orderLine.ShippedQty += shipmentLine.Qty;
            }

            // Update shipment status
            shipment.Status = "SHIPPED";

            // Check if all order lines are fully shipped
            bool allShipped = salesOrder.Lines.All(l => l.ShippedQty >= l.Qty);
            if (allShipped)
            {
                salesOrder.Status = "COMPLETED";
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetByIdAsync(shipment.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ShipmentDto> CancelAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var shipment = await _context.Shipments.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found");
        }

        if (shipment.Status == "SHIPPED")
        {
            throw new InvalidOperationException("SHIPPED shipments cannot be cancelled");
        }

        if (shipment.Status == "CANCELLED")
        {
            throw new InvalidOperationException("Shipment is already cancelled");
        }

        shipment.Status = "CANCELLED";
        await _context.SaveChangesAsync();

        return await GetByIdAsync(shipment.Id);
    }

    public async Task<ShipmentDto> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var shipment = await _context.Shipments
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId); // Tenant isolation

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found");
        }

        return MapToDto(shipment);
    }

    public async Task<(List<ShipmentDto> Items, int Total)> SearchAsync(
        int page, int size, string? q, string? status, DateTime? from, DateTime? to)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _context.Shipments
            .Include(s => s.Lines)
            .Where(s => s.TenantId == tenantId) // Tenant isolation
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.ToUpperInvariant();
            query = query.Where(s => s.ShipmentNo.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(s => s.Status == status);
        }

        if (from.HasValue)
        {
            query = query.Where(s => s.ShipmentDate >= from.Value.Date);
        }

        if (to.HasValue)
        {
            query = query.Where(s => s.ShipmentDate <= to.Value.Date);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.ShipmentDate)
            .ThenByDescending(s => s.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return (items.Select(MapToDto).ToList(), total);
    }

    private async Task ValidateReferencesAsync(Guid branchId, Guid warehouseId)
    {
        var branchExists = await _context.Branches.AnyAsync(b => b.Id == branchId);
        if (!branchExists)
        {
            throw new InvalidOperationException("Branch not found");
        }

        var warehouseExists = await _context.Warehouses.AnyAsync(w => w.Id == warehouseId);
        if (!warehouseExists)
        {
            throw new InvalidOperationException("Warehouse not found");
        }
    }

    private static ShipmentDto MapToDto(Shipment shipment)
    {
        return new ShipmentDto(
            shipment.Id,
            shipment.ShipmentNo,
            shipment.SalesOrderId,
            shipment.BranchId,
            shipment.WarehouseId,
            shipment.ShipmentDate,
            shipment.Status,
            shipment.Note,
            shipment.Lines.Select(l => new ShipmentLineDto(
                l.Id,
                l.ShipmentId,
                l.SalesOrderLineId,
                l.VariantId,
                l.Qty,
                l.Note
            )).ToList(),
            shipment.CreatedAt,
            shipment.CreatedBy
        );
    }
}
