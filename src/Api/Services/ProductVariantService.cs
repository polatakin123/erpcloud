using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IProductVariantService
{
    Task<ProductVariantDto> CreateAsync(Guid productId, CreateProductVariantDto dto);
    Task<PaginatedResponse<ProductVariantDto>> GetAllByProductAsync(Guid productId, int page, int size, string? q, bool? active);
    Task<ProductVariantDto?> GetByIdAsync(Guid id);
    Task<ProductVariantDto?> UpdateAsync(Guid id, UpdateProductVariantDto dto);
    Task<bool> DeleteAsync(Guid id);
}

public class ProductVariantService : IProductVariantService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public ProductVariantService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<ProductVariantDto> CreateAsync(Guid productId, CreateProductVariantDto dto)
    {
        var normalizedSku = dto.Sku.Trim();
        var tenantId = _tenantContext.TenantId;

        // Verify product exists
        var productExists = await _context.Products
            .AnyAsync(p => p.Id == productId && p.TenantId == tenantId);

        if (!productExists)
        {
            throw new InvalidOperationException("Product not found.");
        }

        // Check SKU uniqueness
        var exists = await _context.ProductVariants
            .AnyAsync(v => v.TenantId == tenantId && v.Sku == normalizedSku);

        if (exists)
        {
            throw new InvalidOperationException($"Variant with SKU '{normalizedSku}' already exists.");
        }

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            Sku = normalizedSku,
            Barcode = dto.Barcode?.Trim(),
            Name = dto.Name.Trim(),
            Unit = dto.Unit.Trim(),
            VatRate = dto.VatRate,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();

        return MapToDto(variant);
    }

    public async Task<PaginatedResponse<ProductVariantDto>> GetAllByProductAsync(Guid productId, int page, int size, string? q, bool? active)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _context.ProductVariants
            .Where(v => v.TenantId == tenantId && v.ProductId == productId);

        if (active.HasValue)
        {
            query = query.Where(v => v.IsActive == active.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = $"%{q.Trim()}%";
            query = query.Where(v => EF.Functions.ILike(v.Sku, searchTerm) || EF.Functions.ILike(v.Name, searchTerm));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(v => v.Sku)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(v => MapToDto(v))
            .ToListAsync();

        return new PaginatedResponse<ProductVariantDto>(page, size, total, items);
    }

    public async Task<ProductVariantDto?> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId);

        return variant == null ? null : MapToDto(variant);
    }

    public async Task<ProductVariantDto?> UpdateAsync(Guid id, UpdateProductVariantDto dto)
    {
        var tenantId = _tenantContext.TenantId;
        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId);

        if (variant == null) return null;

        var normalizedSku = dto.Sku.Trim();

        // Check SKU uniqueness (excluding current)
        var exists = await _context.ProductVariants
            .AnyAsync(v => v.TenantId == tenantId && v.Sku == normalizedSku && v.Id != id);

        if (exists)
        {
            throw new InvalidOperationException($"Variant with SKU '{normalizedSku}' already exists.");
        }

        variant.Sku = normalizedSku;
        variant.Barcode = dto.Barcode?.Trim();
        variant.Name = dto.Name.Trim();
        variant.Unit = dto.Unit.Trim();
        variant.VatRate = dto.VatRate;
        variant.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return MapToDto(variant);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;
        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId);

        if (variant == null) return false;

        _context.ProductVariants.Remove(variant);
        await _context.SaveChangesAsync();

        return true;
    }

    private static ProductVariantDto MapToDto(ProductVariant variant)
    {
        return new ProductVariantDto(
            variant.Id,
            variant.ProductId,
            variant.Sku,
            variant.Barcode,
            variant.Name,
            variant.Unit,
            variant.VatRate,
            variant.IsActive,
            variant.CreatedAt,
            variant.CreatedBy
        );
    }
}
