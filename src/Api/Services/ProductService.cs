using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IProductService
{
    Task<ProductDto> CreateAsync(CreateProductDto dto);
    Task<PaginatedResponse<ProductDto>> GetAllAsync(int page, int size, string? q, bool? active);
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto);
    Task<bool> DeleteAsync(Guid id);
}

public class ProductService : IProductService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public ProductService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        var normalizedCode = dto.Code.Trim().ToUpper();
        var tenantId = _tenantContext.TenantId;

        var exists = await _context.Products
            .AnyAsync(p => p.TenantId == tenantId && p.Code == normalizedCode);

        if (exists)
        {
            throw new InvalidOperationException($"Product with code '{normalizedCode}' already exists.");
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = normalizedCode,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return MapToDto(product);
    }

    public async Task<PaginatedResponse<ProductDto>> GetAllAsync(int page, int size, string? q, bool? active)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _context.Products.Where(p => p.TenantId == tenantId);

        if (active.HasValue)
        {
            query = query.Where(p => p.IsActive == active.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = $"%{q.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Code, searchTerm) || EF.Functions.ILike(p.Name, searchTerm));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Code)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(p => MapToDto(p))
            .ToListAsync();

        return new PaginatedResponse<ProductDto>(page, size, total, items);
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        return product == null ? null : MapToDto(product);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto)
    {
        var tenantId = _tenantContext.TenantId;
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (product == null) return null;

        var normalizedCode = dto.Code.Trim().ToUpper();

        var exists = await _context.Products
            .AnyAsync(p => p.TenantId == tenantId && p.Code == normalizedCode && p.Id != id);

        if (exists)
        {
            throw new InvalidOperationException($"Product with code '{normalizedCode}' already exists.");
        }

        product.Code = normalizedCode;
        product.Name = dto.Name.Trim();
        product.Description = dto.Description?.Trim();
        product.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return MapToDto(product);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (product == null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return true;
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto(
            product.Id,
            product.Code,
            product.Name,
            product.Description,
            product.IsActive,
            product.CreatedAt,
            product.CreatedBy
        );
    }
}
