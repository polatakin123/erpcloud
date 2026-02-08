using ErpCloud.BuildingBlocks.Common;
using ErpCloud.BuildingBlocks.Tenant;
using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public class PartReferenceService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public PartReferenceService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Add a new part reference (OEM/aftermarket code) to a variant
    /// </summary>
    public async Task<Result<PartReference>> CreateReferenceAsync(
        Guid variantId, 
        string refType, 
        string refCode, 
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        // Validate variant exists
        var variantExists = await _context.ProductVariants
            .AnyAsync(v => v.Id == variantId && v.TenantId == tenantId, ct);
        
        if (!variantExists)
            return Result<PartReference>.Failure(Error.NotFound("variant_not_found", "Variant not found"));

        // Validate refType
        var validTypes = new[] { "OEM", "AFTERMARKET", "SUPPLIER", "BARCODE" };
        if (!validTypes.Contains(refType.ToUpper()))
            return Result<PartReference>.Failure(Error.Validation("invalid_ref_type", "RefType must be OEM, AFTERMARKET, SUPPLIER, or BARCODE"));

        // Validate refCode
        if (string.IsNullOrWhiteSpace(refCode))
            return Result<PartReference>.Failure(Error.Validation("ref_code_required", "RefCode is required"));

        // Normalize refCode
        var normalizedCode = NormalizeRefCode(refCode);

        if (normalizedCode.Length < 3 || normalizedCode.Length > 64)
            return Result<PartReference>.Failure(Error.Validation("invalid_ref_code_length", "RefCode must be between 3 and 64 characters"));

        // Check duplicate
        var exists = await _context.Set<PartReference>()
            .AnyAsync(r => r.TenantId == tenantId 
                && r.VariantId == variantId 
                && r.RefType == refType.ToUpper() 
                && r.RefCode == normalizedCode, ct);

        if (exists)
            return Result<PartReference>.Failure(Error.Conflict("duplicate_reference", "This reference already exists for the variant"));

        var reference = new PartReference
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VariantId = variantId,
            RefType = refType.ToUpper(),
            RefCode = normalizedCode,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty // TODO: Use actual user context
        };

        _context.Set<PartReference>().Add(reference);
        await _context.SaveChangesAsync(ct);

        return Result<PartReference>.Success(reference);
    }

    /// <summary>
    /// Get all references for a variant
    /// </summary>
    public async Task<List<PartReference>> GetReferencesAsync(Guid variantId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        return await _context.Set<PartReference>()
            .Where(r => r.TenantId == tenantId && r.VariantId == variantId)
            .OrderBy(r => r.RefType)
            .ThenBy(r => r.RefCode)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Delete a reference
    /// </summary>
    public async Task<Result> DeleteReferenceAsync(Guid referenceId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var reference = await _context.Set<PartReference>()
            .FirstOrDefaultAsync(r => r.Id == referenceId && r.TenantId == tenantId, ct);

        if (reference == null)
            return Result.Failure(Error.NotFound("reference_not_found", "Reference not found"));

        _context.Set<PartReference>().Remove(reference);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    /// <summary>
    /// Normalize reference code: uppercase, trim, remove ALL non-alphanumeric characters
    /// </summary>
    private static string NormalizeRefCode(string code)
    {
        // Remove all non-alphanumeric characters (spaces, dashes, slashes, dots, etc.)
        return new string(code.Trim()
            .ToUpper()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }
}
