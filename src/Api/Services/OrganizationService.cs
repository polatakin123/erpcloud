using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IOrganizationService
{
    Task<OrganizationDto> CreateAsync(CreateOrganizationDto dto);
    Task<PaginatedResponse<OrganizationDto>> GetAllAsync(int page, int size, string? q);
    Task<OrganizationDto?> GetByIdAsync(Guid id);
    Task<OrganizationDto?> UpdateAsync(Guid id, UpdateOrganizationDto dto);
    Task<bool> DeleteAsync(Guid id);
}

public class OrganizationService : IOrganizationService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public OrganizationService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<OrganizationDto> CreateAsync(CreateOrganizationDto dto)
    {
        var normalizedCode = dto.Code.Trim().ToUpper();
        var tenantId = _tenantContext.TenantId;

        // Check uniqueness
        var exists = await _context.Organizations
            .AnyAsync(o => o.TenantId == tenantId && o.Code == normalizedCode);

        if (exists)
        {
            throw new InvalidOperationException($"Organization with code '{normalizedCode}' already exists.");
        }

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = normalizedCode,
            Name = dto.Name.Trim(),
            TaxNumber = dto.TaxNumber?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        return MapToDto(org);
    }

    public async Task<PaginatedResponse<OrganizationDto>> GetAllAsync(int page, int size, string? q)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _context.Organizations.Where(o => o.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = $"%{q.Trim()}%";
            query = query.Where(o => EF.Functions.ILike(o.Code, searchTerm) || EF.Functions.ILike(o.Name, searchTerm));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(o => o.Code)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(o => MapToDto(o))
            .ToListAsync();

        return new PaginatedResponse<OrganizationDto>(page, size, total, items);
    }

    public async Task<OrganizationDto?> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var org = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

        return org == null ? null : MapToDto(org);
    }

    public async Task<OrganizationDto?> UpdateAsync(Guid id, UpdateOrganizationDto dto)
    {
        var tenantId = _tenantContext.TenantId;
        var org = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

        if (org == null) return null;

        var normalizedCode = dto.Code.Trim().ToUpper();

        // Check uniqueness (excluding current)
        var exists = await _context.Organizations
            .AnyAsync(o => o.TenantId == tenantId && o.Code == normalizedCode && o.Id != id);

        if (exists)
        {
            throw new InvalidOperationException($"Organization with code '{normalizedCode}' already exists.");
        }

        org.Code = normalizedCode;
        org.Name = dto.Name.Trim();
        org.TaxNumber = dto.TaxNumber?.Trim();

        await _context.SaveChangesAsync();

        return MapToDto(org);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var org = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId);

        if (org == null) return false;

        _context.Organizations.Remove(org);
        await _context.SaveChangesAsync();

        return true;
    }

    private static OrganizationDto MapToDto(Organization org)
    {
        return new OrganizationDto(
            org.Id,
            org.Code,
            org.Name,
            org.TaxNumber,
            org.CreatedAt,
            org.CreatedBy
        );
    }
}
