using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IStockService
{
    Task<StockBalanceDto?> GetBalanceAsync(Guid warehouseId, Guid variantId);
    Task<PaginatedResponse<StockBalanceDto>> GetBalancesAsync(Guid? warehouseId, Guid? variantId, int page, int size);
    Task<PaginatedResponse<StockLedgerDto>> GetLedgerAsync(Guid? warehouseId, Guid? variantId, DateTime? from, DateTime? to, int page, int size);
    
    Task<StockLedgerDto> ReceiveStockAsync(ReceiveStockDto dto);
    Task<StockLedgerDto> IssueStockAsync(IssueStockDto dto);
    Task<StockLedgerDto> ReserveStockAsync(ReserveStockDto dto);
    Task<StockLedgerDto> ReleaseReservationAsync(ReleaseReservationDto dto);
    Task<(StockLedgerDto outEntry, StockLedgerDto inEntry)> TransferStockAsync(TransferStockDto dto);
    Task<StockLedgerDto> AdjustStockAsync(AdjustStockDto dto);
}

public class StockService : IStockService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public StockService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<StockBalanceDto?> GetBalanceAsync(Guid warehouseId, Guid variantId)
    {
        var tenantId = _tenantContext.TenantId;
        var balance = await _context.StockBalances
            .FirstOrDefaultAsync(b => b.TenantId == tenantId 
                && b.WarehouseId == warehouseId 
                && b.VariantId == variantId);

        if (balance == null)
        {
            return new StockBalanceDto(warehouseId, variantId, 0, 0, 0, DateTime.UtcNow);
        }

        return MapBalanceToDto(balance);
    }

    public async Task<PaginatedResponse<StockBalanceDto>> GetBalancesAsync(Guid? warehouseId, Guid? variantId, int page, int size)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _context.StockBalances.Where(b => b.TenantId == tenantId);

        if (warehouseId.HasValue)
        {
            query = query.Where(b => b.WarehouseId == warehouseId.Value);
        }

        if (variantId.HasValue)
        {
            query = query.Where(b => b.VariantId == variantId.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(b => b.WarehouseId)
            .ThenBy(b => b.VariantId)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(b => MapBalanceToDto(b))
            .ToListAsync();

        return new PaginatedResponse<StockBalanceDto>(page, size, total, items);
    }

    public async Task<PaginatedResponse<StockLedgerDto>> GetLedgerAsync(Guid? warehouseId, Guid? variantId, DateTime? from, DateTime? to, int page, int size)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _context.StockLedgerEntries.Where(e => e.TenantId == tenantId);

        if (warehouseId.HasValue)
        {
            query = query.Where(e => e.WarehouseId == warehouseId.Value);
        }

        if (variantId.HasValue)
        {
            query = query.Where(e => e.VariantId == variantId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(e => e.OccurredAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.OccurredAt <= to.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(e => MapLedgerToDto(e))
            .ToListAsync();

        return new PaginatedResponse<StockLedgerDto>(page, size, total, items);
    }

    public async Task<StockLedgerDto> ReceiveStockAsync(ReceiveStockDto dto)
    {
        var tenantId = _tenantContext.TenantId;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Verify warehouse and variant exist
            await VerifyWarehouseAndVariantAsync(dto.WarehouseId, dto.VariantId);

            // Lock and get/create balance
            var balance = await GetOrCreateBalanceWithLockAsync(dto.WarehouseId, dto.VariantId);

            // Create ledger entry
            var entry = new StockLedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OccurredAt = DateTime.UtcNow,
                WarehouseId = dto.WarehouseId,
                VariantId = dto.VariantId,
                MovementType = StockMovementType.INBOUND,
                Quantity = dto.Qty,
                UnitCost = dto.UnitCost,
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId,
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _tenantContext.UserId ?? Guid.Empty
            };

            _context.StockLedgerEntries.Add(entry);

            // Update balance
            balance.OnHand += dto.Qty;
            balance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapLedgerToDto(entry);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<StockLedgerDto> IssueStockAsync(IssueStockDto dto)
    {
        var tenantId = _tenantContext.TenantId;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Verify warehouse and variant exist
            await VerifyWarehouseAndVariantAsync(dto.WarehouseId, dto.VariantId);

            // Lock and get/create balance
            var balance = await GetOrCreateBalanceWithLockAsync(dto.WarehouseId, dto.VariantId);

            // Check available stock
            var available = balance.OnHand - balance.Reserved;
            if (available < dto.Qty)
            {
                throw new InvalidOperationException($"Insufficient available stock. Available: {available}, Requested: {dto.Qty}");
            }

            // Create ledger entry (negative quantity)
            var entry = new StockLedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OccurredAt = DateTime.UtcNow,
                WarehouseId = dto.WarehouseId,
                VariantId = dto.VariantId,
                MovementType = StockMovementType.OUTBOUND,
                Quantity = -dto.Qty,
                UnitCost = null,
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId,
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _tenantContext.UserId ?? Guid.Empty
            };

            _context.StockLedgerEntries.Add(entry);

            // Update balance
            balance.OnHand -= dto.Qty;
            balance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapLedgerToDto(entry);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<StockLedgerDto> ReserveStockAsync(ReserveStockDto dto)
    {
        var tenantId = _tenantContext.TenantId;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Verify warehouse and variant exist
            await VerifyWarehouseAndVariantAsync(dto.WarehouseId, dto.VariantId);

            // Lock and get/create balance
            var balance = await GetOrCreateBalanceWithLockAsync(dto.WarehouseId, dto.VariantId);

            // Check available stock
            var available = balance.OnHand - balance.Reserved;
            if (available < dto.Qty)
            {
                throw new InvalidOperationException($"Insufficient available stock to reserve. Available: {available}, Requested: {dto.Qty}");
            }

            // Create ledger entry
            var entry = new StockLedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OccurredAt = DateTime.UtcNow,
                WarehouseId = dto.WarehouseId,
                VariantId = dto.VariantId,
                MovementType = StockMovementType.RESERVE,
                Quantity = dto.Qty,
                UnitCost = null,
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId,
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _tenantContext.UserId ?? Guid.Empty
            };

            _context.StockLedgerEntries.Add(entry);

            // Update balance
            balance.Reserved += dto.Qty;
            balance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapLedgerToDto(entry);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<StockLedgerDto> ReleaseReservationAsync(ReleaseReservationDto dto)
    {
        var tenantId = _tenantContext.TenantId;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Verify warehouse and variant exist
            await VerifyWarehouseAndVariantAsync(dto.WarehouseId, dto.VariantId);

            // Lock and get/create balance
            var balance = await GetOrCreateBalanceWithLockAsync(dto.WarehouseId, dto.VariantId);

            // Check reserved stock
            if (balance.Reserved < dto.Qty)
            {
                throw new InvalidOperationException($"Insufficient reserved stock to release. Reserved: {balance.Reserved}, Requested: {dto.Qty}");
            }

            // Create ledger entry (negative quantity)
            var entry = new StockLedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OccurredAt = DateTime.UtcNow,
                WarehouseId = dto.WarehouseId,
                VariantId = dto.VariantId,
                MovementType = StockMovementType.RELEASE,
                Quantity = -dto.Qty,
                UnitCost = null,
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId,
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _tenantContext.UserId ?? Guid.Empty
            };

            _context.StockLedgerEntries.Add(entry);

            // Update balance
            balance.Reserved -= dto.Qty;
            balance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapLedgerToDto(entry);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<(StockLedgerDto outEntry, StockLedgerDto inEntry)> TransferStockAsync(TransferStockDto dto)
    {
        var tenantId = _tenantContext.TenantId;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Verify both warehouses and variant exist
            await VerifyWarehouseAndVariantAsync(dto.FromWarehouseId, dto.VariantId);
            await VerifyWarehouseAndVariantAsync(dto.ToWarehouseId, dto.VariantId);

            var correlationId = Guid.NewGuid();

            // Lock FROM warehouse balance
            var fromBalance = await GetOrCreateBalanceWithLockAsync(dto.FromWarehouseId, dto.VariantId);

            // Check available stock in FROM warehouse
            var available = fromBalance.OnHand - fromBalance.Reserved;
            if (available < dto.Qty)
            {
                throw new InvalidOperationException($"Insufficient available stock in source warehouse. Available: {available}, Requested: {dto.Qty}");
            }

            // Lock TO warehouse balance
            var toBalance = await GetOrCreateBalanceWithLockAsync(dto.ToWarehouseId, dto.VariantId);

            // Create TRANSFER_OUT entry
            var outEntry = new StockLedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OccurredAt = DateTime.UtcNow,
                WarehouseId = dto.FromWarehouseId,
                VariantId = dto.VariantId,
                MovementType = StockMovementType.TRANSFER_OUT,
                Quantity = -dto.Qty,
                UnitCost = null,
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId,
                CorrelationId = correlationId,
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _tenantContext.UserId ?? Guid.Empty
            };

            // Create TRANSFER_IN entry
            var inEntry = new StockLedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OccurredAt = DateTime.UtcNow,
                WarehouseId = dto.ToWarehouseId,
                VariantId = dto.VariantId,
                MovementType = StockMovementType.TRANSFER_IN,
                Quantity = dto.Qty,
                UnitCost = null,
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId,
                CorrelationId = correlationId,
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _tenantContext.UserId ?? Guid.Empty
            };

            _context.StockLedgerEntries.Add(outEntry);
            _context.StockLedgerEntries.Add(inEntry);

            // Update balances
            fromBalance.OnHand -= dto.Qty;
            fromBalance.UpdatedAt = DateTime.UtcNow;

            toBalance.OnHand += dto.Qty;
            toBalance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (MapLedgerToDto(outEntry), MapLedgerToDto(inEntry));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<StockLedgerDto> AdjustStockAsync(AdjustStockDto dto)
    {
        var tenantId = _tenantContext.TenantId;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Verify warehouse and variant exist
            await VerifyWarehouseAndVariantAsync(dto.WarehouseId, dto.VariantId);

            // Lock and get/create balance
            var balance = await GetOrCreateBalanceWithLockAsync(dto.WarehouseId, dto.VariantId);

            // Check if adjustment would cause negative stock
            var newOnHand = balance.OnHand + dto.Qty;
            if (newOnHand < 0)
            {
                throw new InvalidOperationException($"Adjustment would result in negative stock. Current OnHand: {balance.OnHand}, Adjustment: {dto.Qty}");
            }

            // Create ledger entry
            var entry = new StockLedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OccurredAt = DateTime.UtcNow,
                WarehouseId = dto.WarehouseId,
                VariantId = dto.VariantId,
                MovementType = StockMovementType.ADJUSTMENT,
                Quantity = dto.Qty,
                UnitCost = null,
                ReferenceType = dto.ReferenceType,
                ReferenceId = dto.ReferenceId,
                Note = dto.Note,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _tenantContext.UserId ?? Guid.Empty
            };

            _context.StockLedgerEntries.Add(entry);

            // Update balance
            balance.OnHand += dto.Qty;
            balance.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapLedgerToDto(entry);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    private async Task VerifyWarehouseAndVariantAsync(Guid warehouseId, Guid variantId)
    {
        var tenantId = _tenantContext.TenantId;

        var warehouseExists = await _context.Warehouses
            .AnyAsync(w => w.Id == warehouseId && w.TenantId == tenantId);

        if (!warehouseExists)
        {
            throw new InvalidOperationException("Warehouse not found.");
        }

        var variantExists = await _context.ProductVariants
            .AnyAsync(v => v.Id == variantId && v.TenantId == tenantId);

        if (!variantExists)
        {
            throw new InvalidOperationException("Product variant not found.");
        }
    }

    /// <summary>
    /// Get or create stock balance with row-level lock (SELECT FOR UPDATE).
    /// This prevents concurrent modifications to the same balance row.
    /// </summary>
    private async Task<StockBalance> GetOrCreateBalanceWithLockAsync(Guid warehouseId, Guid variantId)
    {
        var tenantId = _tenantContext.TenantId;

        // Try raw SQL for SELECT FOR UPDATE (PostgreSQL)
        // Fallback to regular query for in-memory database (tests)
        StockBalance? balance = null;
        
        try
        {
            balance = await _context.StockBalances
                .FromSqlRaw(@"
                    SELECT * FROM stock_balances 
                    WHERE ""TenantId"" = {0} 
                    AND ""WarehouseId"" = {1} 
                    AND ""VariantId"" = {2}
                    FOR UPDATE
                ", tenantId, warehouseId, variantId)
                .FirstOrDefaultAsync();
        }
        catch (InvalidOperationException)
        {
            // Fallback for in-memory database (testing)
            balance = await _context.StockBalances
                .Where(b => b.TenantId == tenantId && b.WarehouseId == warehouseId && b.VariantId == variantId)
                .FirstOrDefaultAsync();
        }

        if (balance == null)
        {
            // Create new balance (still in transaction, so locked)
            balance = new StockBalance
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                WarehouseId = warehouseId,
                VariantId = variantId,
                OnHand = 0,
                Reserved = 0,
                UpdatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _tenantContext.UserId ?? Guid.Empty
            };

            _context.StockBalances.Add(balance);
            await _context.SaveChangesAsync(); // Ensure it exists for lock
        }

        return balance;
    }

    private static StockBalanceDto MapBalanceToDto(StockBalance balance)
    {
        return new StockBalanceDto(
            balance.WarehouseId,
            balance.VariantId,
            balance.OnHand,
            balance.Reserved,
            balance.Available,
            balance.UpdatedAt
        );
    }

    private static StockLedgerDto MapLedgerToDto(StockLedgerEntry entry)
    {
        return new StockLedgerDto(
            entry.Id,
            entry.OccurredAt,
            entry.WarehouseId,
            entry.VariantId,
            entry.MovementType,
            entry.Quantity,
            entry.UnitCost,
            entry.ReferenceType,
            entry.ReferenceId,
            entry.CorrelationId,
            entry.Note,
            entry.CreatedAt,
            entry.CreatedBy
        );
    }
}
