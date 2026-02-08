using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IWarehouseService
{
    Task<WarehouseDto> CreateAsync(Guid branchId, CreateWarehouseDto dto);
    Task<PaginatedResponse<WarehouseDto>> GetAllAsync(int page, int size, string? q);
    Task<PaginatedResponse<WarehouseDto>> GetAllByBranchAsync(Guid branchId, int page, int size, string? q);
    Task<WarehouseDto?> GetByIdAsync(Guid id);
    Task<WarehouseDto?> UpdateAsync(Guid id, UpdateWarehouseDto dto);
    Task<bool> DeleteAsync(Guid id);
}

public class WarehouseService : IWarehouseService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public WarehouseService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<WarehouseDto> CreateAsync(Guid branchId, CreateWarehouseDto dto)
    {
        var normalizedCode = dto.Code.Trim().ToUpper();
        var normalizedType = dto.Type.Trim().ToUpper();
        var tenantId = _tenantContext.TenantId;

        // Verify branch exists and belongs to tenant
        var branchExists = await _context.Branches
            .AnyAsync(b => b.Id == branchId && b.TenantId == tenantId);

        if (!branchExists)
        {
            throw new InvalidOperationException("Branch not found.");
        }

        // Check uniqueness within branch
        var exists = await _context.Warehouses
            .AnyAsync(w => w.TenantId == tenantId && w.BranchId == branchId && w.Code == normalizedCode);

        if (exists)
        {
            throw new InvalidOperationException($"Warehouse with code '{normalizedCode}' already exists in this branch.");
        }

        // If setting as default, unset other defaults in same branch
        if (dto.IsDefault)
        {
            await UnsetDefaultsInBranch(branchId, tenantId);
        }

        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BranchId = branchId,
            Code = normalizedCode,
            Name = dto.Name.Trim(),
            Type = normalizedType,
            IsDefault = dto.IsDefault,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        return MapToDto(warehouse);
    }

    public async Task<PaginatedResponse<WarehouseDto>> GetAllAsync(int page, int size, string? q)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _context.Warehouses.Where(w => w.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = $"%{q.Trim()}%";
            query = query.Where(w => EF.Functions.ILike(w.Code, searchTerm) || EF.Functions.ILike(w.Name, searchTerm));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(w => w.Code)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(w => MapToDto(w))
            .ToListAsync();

        return new PaginatedResponse<WarehouseDto>(page, size, total, items);
    }

    public async Task<PaginatedResponse<WarehouseDto>> GetAllByBranchAsync(Guid branchId, int page, int size, string? q)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _context.Warehouses.Where(w => w.TenantId == tenantId && w.BranchId == branchId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = $"%{q.Trim()}%";
            query = query.Where(w => EF.Functions.ILike(w.Code, searchTerm) || EF.Functions.ILike(w.Name, searchTerm));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(w => w.Code)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(w => MapToDto(w))
            .ToListAsync();

        return new PaginatedResponse<WarehouseDto>(page, size, total, items);
    }

    public async Task<WarehouseDto?> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId);

        return warehouse == null ? null : MapToDto(warehouse);
    }

    public async Task<WarehouseDto?> UpdateAsync(Guid id, UpdateWarehouseDto dto)
    {
        var tenantId = _tenantContext.TenantId;
        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId);

        if (warehouse == null) return null;

        var normalizedCode = dto.Code.Trim().ToUpper();
        var normalizedType = dto.Type.Trim().ToUpper();

        // Check uniqueness within branch (excluding current)
        var exists = await _context.Warehouses
            .AnyAsync(w => w.TenantId == tenantId && w.BranchId == warehouse.BranchId && w.Code == normalizedCode && w.Id != id);

        if (exists)
        {
            throw new InvalidOperationException($"Warehouse with code '{normalizedCode}' already exists in this branch.");
        }

        // If setting as default, unset other defaults in same branch
        if (dto.IsDefault && !warehouse.IsDefault)
        {
            await UnsetDefaultsInBranch(warehouse.BranchId, tenantId, id);
        }

        warehouse.Code = normalizedCode;
        warehouse.Name = dto.Name.Trim();
        warehouse.Type = normalizedType;
        warehouse.IsDefault = dto.IsDefault;

        await _context.SaveChangesAsync();

        return MapToDto(warehouse);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId);

        if (warehouse == null) return false;

        _context.Warehouses.Remove(warehouse);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task UnsetDefaultsInBranch(Guid branchId, Guid tenantId, Guid? excludeId = null)
    {
        var query = _context.Warehouses
            .Where(w => w.TenantId == tenantId && w.BranchId == branchId && w.IsDefault);

        if (excludeId.HasValue)
        {
            query = query.Where(w => w.Id != excludeId.Value);
        }

        var defaults = await query.ToListAsync();
        foreach (var w in defaults)
        {
            w.IsDefault = false;
        }
    }

    private static WarehouseDto MapToDto(Warehouse warehouse)
    {
        return new WarehouseDto(
            warehouse.Id,
            warehouse.BranchId,
            warehouse.Code,
            warehouse.Name,
            warehouse.Type,
            warehouse.IsDefault,
            warehouse.CreatedAt,
            warehouse.CreatedBy
        );
    }
}
