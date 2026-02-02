using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IBranchService
{
    Task<BranchDto> CreateAsync(Guid organizationId, CreateBranchDto dto);
    Task<PaginatedResponse<BranchDto>> GetAllByOrgAsync(Guid organizationId, int page, int size, string? q);
    Task<BranchDto?> GetByIdAsync(Guid id);
    Task<BranchDto?> UpdateAsync(Guid id, UpdateBranchDto dto);
    Task<bool> DeleteAsync(Guid id);
}

public class BranchService : IBranchService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public BranchService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<BranchDto> CreateAsync(Guid organizationId, CreateBranchDto dto)
    {
        var normalizedCode = dto.Code.Trim().ToUpper();
        var tenantId = _tenantContext.TenantId;

        // Verify organization exists and belongs to tenant
        var orgExists = await _context.Organizations
            .AnyAsync(o => o.Id == organizationId && o.TenantId == tenantId);

        if (!orgExists)
        {
            throw new InvalidOperationException("Organization not found.");
        }

        // Check uniqueness within organization
        var exists = await _context.Branches
            .AnyAsync(b => b.TenantId == tenantId && b.OrganizationId == organizationId && b.Code == normalizedCode);

        if (exists)
        {
            throw new InvalidOperationException($"Branch with code '{normalizedCode}' already exists in this organization.");
        }

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrganizationId = organizationId,
            Code = normalizedCode,
            Name = dto.Name.Trim(),
            City = dto.City?.Trim(),
            Address = dto.Address?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();

        return MapToDto(branch);
    }

    public async Task<PaginatedResponse<BranchDto>> GetAllByOrgAsync(Guid organizationId, int page, int size, string? q)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _context.Branches.Where(b => b.TenantId == tenantId && b.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = $"%{q.Trim()}%";
            query = query.Where(b => EF.Functions.ILike(b.Code, searchTerm) || EF.Functions.ILike(b.Name, searchTerm));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(b => b.Code)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(b => MapToDto(b))
            .ToListAsync();

        return new PaginatedResponse<BranchDto>(page, size, total, items);
    }

    public async Task<BranchDto?> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var branch = await _context.Branches
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId);

        return branch == null ? null : MapToDto(branch);
    }

    public async Task<BranchDto?> UpdateAsync(Guid id, UpdateBranchDto dto)
    {
        var tenantId = _tenantContext.TenantId;
        var branch = await _context.Branches
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId);

        if (branch == null) return null;

        var normalizedCode = dto.Code.Trim().ToUpper();

        // Check uniqueness within organization (excluding current)
        var exists = await _context.Branches
            .AnyAsync(b => b.TenantId == tenantId && b.OrganizationId == branch.OrganizationId && b.Code == normalizedCode && b.Id != id);

        if (exists)
        {
            throw new InvalidOperationException($"Branch with code '{normalizedCode}' already exists in this organization.");
        }

        branch.Code = normalizedCode;
        branch.Name = dto.Name.Trim();
        branch.City = dto.City?.Trim();
        branch.Address = dto.Address?.Trim();

        await _context.SaveChangesAsync();

        return MapToDto(branch);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var branch = await _context.Branches
            .FirstOrDefaultAsync(b => b.Id == id && b.TenantId == tenantId);

        if (branch == null) return false;

        _context.Branches.Remove(branch);
        await _context.SaveChangesAsync();

        return true;
    }

    private static BranchDto MapToDto(Branch branch)
    {
        return new BranchDto(
            branch.Id,
            branch.OrganizationId,
            branch.Code,
            branch.Name,
            branch.City,
            branch.Address,
            branch.CreatedAt,
            branch.CreatedBy
        );
    }
}
