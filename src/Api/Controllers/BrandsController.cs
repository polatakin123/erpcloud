using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Constants;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Controllers;

/// <summary>
/// Brand master data CRUD operations
/// </summary>
[Authorize(Roles = Roles.Admin)]
[ApiController]
[Route("api/brands")]
public class BrandsController : ControllerBase
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<BrandsController> _logger;

    public BrandsController(
        ErpDbContext context,
        ITenantContext tenantContext,
        ILogger<BrandsController> logger)
    {
        _context = context;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Search brands by name or code (case-insensitive)
    /// </summary>
    /// <param name="q">Search query</param>
    /// <param name="active">Filter by IsActive (null = all)</param>
    /// <param name="limit">Max results</param>
    [HttpGet]
    public async Task<ActionResult<List<BrandDto>>> SearchBrands(
        [FromQuery] string? q = null,
        [FromQuery] bool? active = null,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var query = _context.Brands
            .Where(b => b.TenantId == tenantId);

        // Filter by active status
        if (active.HasValue)
        {
            query = query.Where(b => b.IsActive == active.Value);
        }

        // Search by name or code
        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchTerm = q.Trim().ToUpper();
            query = query.Where(b =>
                b.Name.ToUpper().Contains(searchTerm) ||
                b.Code.ToUpper().Contains(searchTerm));
        }

        var brands = await query
            .OrderBy(b => b.Name)
            .Take(limit)
            .Select(b => new BrandDto
            {
                Id = b.Id,
                Code = b.Code,
                Name = b.Name,
                LogoUrl = b.LogoUrl,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(brands);
    }

    /// <summary>
    /// Get brand by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BrandDto>> GetBrand(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var brand = await _context.Brands
            .Where(b => b.Id == id && b.TenantId == tenantId)
            .Select(b => new BrandDto
            {
                Id = b.Id,
                Code = b.Code,
                Name = b.Name,
                LogoUrl = b.LogoUrl,
                IsActive = b.IsActive,
                CreatedAt = b.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (brand == null)
            return NotFound(new { message = "Marka bulunamadı" });

        return Ok(brand);
    }

    /// <summary>
    /// Create new brand
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BrandDto>> CreateBrand(
        [FromBody] CreateBrandRequest request,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = GetCurrentUserId();

        // Validate request
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Marka adı gerekli" });

        // Normalize code
        var code = string.IsNullOrWhiteSpace(request.Code)
            ? request.Name.Trim().ToUpper()
            : request.Code.Trim().ToUpper();

        // Check for duplicate code within tenant
        var existingBrand = await _context.Brands
            .AnyAsync(b => b.TenantId == tenantId && b.Code == code, ct);

        if (existingBrand)
            return Conflict(new { message = $"Bu marka kodu zaten kullanımda: {code}" });

        var brand = new Brand
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = request.Name.Trim(),
            LogoUrl = request.LogoUrl?.Trim(),
            IsActive = request.IsActive ?? true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        _context.Brands.Add(brand);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[BrandsController] Created brand {BrandId} ({Code}) for tenant {TenantId}",
            brand.Id, brand.Code, tenantId);

        return CreatedAtAction(
            nameof(GetBrand),
            new { id = brand.Id },
            new BrandDto
            {
                Id = brand.Id,
                Code = brand.Code,
                Name = brand.Name,
                LogoUrl = brand.LogoUrl,
                IsActive = brand.IsActive,
                CreatedAt = brand.CreatedAt
            });
    }

    /// <summary>
    /// Update existing brand
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<BrandDto>> UpdateBrand(
        Guid id,
        [FromBody] UpdateBrandRequest request,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var brand = await _context.Brands
            .Where(b => b.Id == id && b.TenantId == tenantId)
            .FirstOrDefaultAsync(ct);

        if (brand == null)
            return NotFound(new { message = "Marka bulunamadı" });

        // Check code uniqueness if changed
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var newCode = request.Code.Trim().ToUpper();
            if (newCode != brand.Code)
            {
                var codeExists = await _context.Brands
                    .AnyAsync(b => b.TenantId == tenantId && b.Code == newCode && b.Id != id, ct);

                if (codeExists)
                    return Conflict(new { message = $"Bu marka kodu zaten kullanımda: {newCode}" });

                brand.Code = newCode;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            brand.Name = request.Name.Trim();

        if (request.LogoUrl != null) // Allow clearing logoUrl with empty string
            brand.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim();

        if (request.IsActive.HasValue)
            brand.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[BrandsController] Updated brand {BrandId} ({Code}) for tenant {TenantId}",
            brand.Id, brand.Code, tenantId);

        return Ok(new BrandDto
        {
            Id = brand.Id,
            Code = brand.Code,
            Name = brand.Name,
            LogoUrl = brand.LogoUrl,
            IsActive = brand.IsActive,
            CreatedAt = brand.CreatedAt
        });
    }

    /// <summary>
    /// Delete brand (soft delete by marking inactive)
    /// Hard delete only if not referenced
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteBrand(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var brand = await _context.Brands
            .Where(b => b.Id == id && b.TenantId == tenantId)
            .FirstOrDefaultAsync(ct);

        if (brand == null)
            return NotFound(new { message = "Marka bulunamadı" });

        // Check if brand is referenced by products or price rules
        var isReferenced = await _context.Products
            .AnyAsync(p => p.BrandId == id, ct);

        if (!isReferenced)
        {
            isReferenced = await _context.PriceRules
                .AnyAsync(pr => pr.BrandId == id, ct);
        }

        if (isReferenced)
        {
            // Soft delete: mark as inactive
            brand.IsActive = false;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[BrandsController] Soft deleted (deactivated) brand {BrandId} ({Code}) - still referenced",
                brand.Id, brand.Code);

            return Ok(new { message = "Marka kullanımda olduğu için pasif hale getirildi" });
        }
        else
        {
            // Hard delete: no references
            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[BrandsController] Hard deleted brand {BrandId} ({Code})",
                brand.Id, brand.Code);

            return Ok(new { message = "Marka silindi" });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : Guid.Parse("00000000-0000-0000-0000-000000000001"); // Fallback system user
    }
}

// ==================== DTOs ====================

public class BrandDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateBrandRequest
{
    public string? Code { get; set; } // Optional, defaults to Name.ToUpper()
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool? IsActive { get; set; } // Defaults to true
}

public class UpdateBrandRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? LogoUrl { get; set; }
    public bool? IsActive { get; set; }
}
