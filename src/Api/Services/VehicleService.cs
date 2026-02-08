using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Common;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IVehicleService
{
    // Brands
    Task<List<VehicleBrandDto>> GetBrandsAsync(CancellationToken ct = default);
    Task<Result<VehicleBrandDto>> GetBrandByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<VehicleBrandDto>> CreateBrandAsync(CreateVehicleBrandDto dto, CancellationToken ct = default);
    Task<Result<VehicleBrandDto>> UpdateBrandAsync(Guid id, UpdateVehicleBrandDto dto, CancellationToken ct = default);
    Task<Result> DeleteBrandAsync(Guid id, CancellationToken ct = default);

    // Models
    Task<List<VehicleModelDto>> GetModelsAsync(Guid? brandId = null, CancellationToken ct = default);
    Task<Result<VehicleModelDto>> GetModelByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<VehicleModelDto>> CreateModelAsync(CreateVehicleModelDto dto, CancellationToken ct = default);
    Task<Result<VehicleModelDto>> UpdateModelAsync(Guid id, UpdateVehicleModelDto dto, CancellationToken ct = default);
    Task<Result> DeleteModelAsync(Guid id, CancellationToken ct = default);

    // Year Ranges
    Task<List<VehicleYearRangeDto>> GetYearRangesAsync(Guid? modelId = null, CancellationToken ct = default);
    Task<Result<VehicleYearRangeDto>> GetYearRangeByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<VehicleYearRangeDto>> CreateYearRangeAsync(CreateVehicleYearRangeDto dto, CancellationToken ct = default);
    Task<Result<VehicleYearRangeDto>> UpdateYearRangeAsync(Guid id, UpdateVehicleYearRangeDto dto, CancellationToken ct = default);
    Task<Result> DeleteYearRangeAsync(Guid id, CancellationToken ct = default);

    // Engines
    Task<List<VehicleEngineDto>> GetEnginesAsync(Guid? yearRangeId = null, CancellationToken ct = default);
    Task<Result<VehicleEngineDto>> GetEngineByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<VehicleEngineDto>> CreateEngineAsync(CreateVehicleEngineDto dto, CancellationToken ct = default);
    Task<Result<VehicleEngineDto>> UpdateEngineAsync(Guid id, UpdateVehicleEngineDto dto, CancellationToken ct = default);
    Task<Result> DeleteEngineAsync(Guid id, CancellationToken ct = default);

    // Fitments
    Task<List<StockCardFitmentDto>> GetFitmentsAsync(Guid variantId, CancellationToken ct = default);
    Task<Result<StockCardFitmentDto>> CreateFitmentAsync(Guid variantId, CreateStockCardFitmentDto dto, CancellationToken ct = default);
    Task<Result> DeleteFitmentAsync(Guid variantId, Guid fitmentId, CancellationToken ct = default);
}

public class VehicleService : IVehicleService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public VehicleService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    // ==================== Brands ====================

    public async Task<List<VehicleBrandDto>> GetBrandsAsync(CancellationToken ct = default)
    {
        return await _context.VehicleBrands
            .OrderBy(b => b.Name)
            .Select(b => new VehicleBrandDto
            {
                Id = b.Id,
                Code = b.Code,
                Name = b.Name,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<Result<VehicleBrandDto>> GetBrandByIdAsync(Guid id, CancellationToken ct = default)
    {
        var brand = await _context.VehicleBrands
            .Where(b => b.Id == id)
            .Select(b => new VehicleBrandDto
            {
                Id = b.Id,
                Code = b.Code,
                Name = b.Name,
                CreatedAt = b.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        return brand != null
            ? Result<VehicleBrandDto>.Success(brand)
            : Result<VehicleBrandDto>.Failure(new Error(ErrorCodes.Vehicle.BrandNotFound, "Marka bulunamadı."));
    }

    public async Task<Result<VehicleBrandDto>> CreateBrandAsync(CreateVehicleBrandDto dto, CancellationToken ct = default)
    {
        // Normalize code to uppercase for case-insensitive uniqueness
        var normalizedCode = dto.Code.Trim().ToUpperInvariant();
        
        // Check for duplicate code
        var exists = await _context.VehicleBrands
            .AnyAsync(b => b.Code == normalizedCode, ct);

        if (exists)
            return Result<VehicleBrandDto>.Failure(new Error(ErrorCodes.Vehicle.BrandCodeExists, "Bu kod zaten kullanılıyor."));

        var brand = new VehicleBrand
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Code = normalizedCode,
            Name = dto.Name.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.VehicleBrands.Add(brand);
        await _context.SaveChangesAsync(ct);

        return Result<VehicleBrandDto>.Success(new VehicleBrandDto
        {
            Id = brand.Id,
            Code = brand.Code,
            Name = brand.Name,
            CreatedAt = brand.CreatedAt
        });
    }

    public async Task<Result<VehicleBrandDto>> UpdateBrandAsync(Guid id, UpdateVehicleBrandDto dto, CancellationToken ct = default)
    {
        var brand = await _context.VehicleBrands.FindAsync(new object[] { id }, ct);
        if (brand == null)
            return Result<VehicleBrandDto>.Failure(new Error(ErrorCodes.Vehicle.BrandNotFound, "Marka bulunamadı."));

        // Normalize code to uppercase
        var normalizedCode = dto.Code.Trim().ToUpperInvariant();
        
        // Check for duplicate code (excluding current)
        var exists = await _context.VehicleBrands
            .AnyAsync(b => b.Code == normalizedCode && b.Id != id, ct);

        if (exists)
            return Result<VehicleBrandDto>.Failure(new Error(ErrorCodes.Vehicle.BrandCodeExists, "Bu kod zaten kullanılıyor."));

        brand.Code = normalizedCode;
        brand.Name = dto.Name.Trim();

        await _context.SaveChangesAsync(ct);

        return Result<VehicleBrandDto>.Success(new VehicleBrandDto
        {
            Id = brand.Id,
            Code = brand.Code,
            Name = brand.Name,
            CreatedAt = brand.CreatedAt
        });
    }

    public async Task<Result> DeleteBrandAsync(Guid id, CancellationToken ct = default)
    {
        var brand = await _context.VehicleBrands
            .Include(b => b.Models)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (brand == null)
            return Result.Failure(new Error(ErrorCodes.Vehicle.BrandNotFound, "Marka bulunamadı."));

        if (brand.Models.Any())
            return Result.Failure(new Error(ErrorCodes.Vehicle.BrandHasModels, "Bu markaya ait modeller var. Önce modelleri silin."));

        _context.VehicleBrands.Remove(brand);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    // ==================== Models ====================

    public async Task<List<VehicleModelDto>> GetModelsAsync(Guid? brandId = null, CancellationToken ct = default)
    {
        var query = _context.VehicleModels
            .Include(m => m.Brand)
            .AsQueryable();

        if (brandId.HasValue)
            query = query.Where(m => m.BrandId == brandId.Value);

        return await query
            .OrderBy(m => m.Brand.Name)
            .ThenBy(m => m.Name)
            .Select(m => new VehicleModelDto
            {
                Id = m.Id,
                BrandId = m.BrandId,
                BrandName = m.Brand.Name,
                Name = m.Name,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<Result<VehicleModelDto>> GetModelByIdAsync(Guid id, CancellationToken ct = default)
    {
        var model = await _context.VehicleModels
            .Include(m => m.Brand)
            .Where(m => m.Id == id)
            .Select(m => new VehicleModelDto
            {
                Id = m.Id,
                BrandId = m.BrandId,
                BrandName = m.Brand.Name,
                Name = m.Name,
                CreatedAt = m.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        return model != null
            ? Result<VehicleModelDto>.Success(model)
            : Result<VehicleModelDto>.Failure(new Error(ErrorCodes.Vehicle.ModelNotFound, "Model bulunamadı."));
    }

    public async Task<Result<VehicleModelDto>> CreateModelAsync(CreateVehicleModelDto dto, CancellationToken ct = default)
    {
        // Verify brand exists
        var brandExists = await _context.VehicleBrands.AnyAsync(b => b.Id == dto.BrandId, ct);
        if (!brandExists)
            return Result<VehicleModelDto>.Failure(new Error(ErrorCodes.Vehicle.BrandNotFound, "Marka bulunamadı."));

        // Check for duplicate name under same brand
        var exists = await _context.VehicleModels
            .AnyAsync(m => m.BrandId == dto.BrandId && m.Name == dto.Name, ct);

        if (exists)
            return Result<VehicleModelDto>.Failure(new Error(ErrorCodes.Vehicle.ModelNameExists, "Bu marka altında aynı isimde model zaten var."));

        var model = new VehicleModel
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            BrandId = dto.BrandId,
            Name = dto.Name,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.VehicleModels.Add(model);
        await _context.SaveChangesAsync(ct);

        var brand = await _context.VehicleBrands.FindAsync(new object[] { dto.BrandId }, ct);

        return Result<VehicleModelDto>.Success(new VehicleModelDto
        {
            Id = model.Id,
            BrandId = model.BrandId,
            BrandName = brand!.Name,
            Name = model.Name,
            CreatedAt = model.CreatedAt
        });
    }

    public async Task<Result<VehicleModelDto>> UpdateModelAsync(Guid id, UpdateVehicleModelDto dto, CancellationToken ct = default)
    {
        var model = await _context.VehicleModels.FindAsync(new object[] { id }, ct);
        if (model == null)
            return Result<VehicleModelDto>.Failure(new Error(ErrorCodes.Vehicle.ModelNotFound, "Model bulunamadı."));

        // Verify brand exists
        var brandExists = await _context.VehicleBrands.AnyAsync(b => b.Id == dto.BrandId, ct);
        if (!brandExists)
            return Result<VehicleModelDto>.Failure(new Error(ErrorCodes.Vehicle.BrandNotFound, "Marka bulunamadı."));

        // Check for duplicate name (excluding current)
        var exists = await _context.VehicleModels
            .AnyAsync(m => m.BrandId == dto.BrandId && m.Name == dto.Name && m.Id != id, ct);

        if (exists)
            return Result<VehicleModelDto>.Failure(new Error(ErrorCodes.Vehicle.ModelNameExists, "Bu marka altında aynı isimde model zaten var."));

        model.BrandId = dto.BrandId;
        model.Name = dto.Name;

        await _context.SaveChangesAsync(ct);

        var brand = await _context.VehicleBrands.FindAsync(new object[] { dto.BrandId }, ct);

        return Result<VehicleModelDto>.Success(new VehicleModelDto
        {
            Id = model.Id,
            BrandId = model.BrandId,
            BrandName = brand!.Name,
            Name = model.Name,
            CreatedAt = model.CreatedAt
        });
    }

    public async Task<Result> DeleteModelAsync(Guid id, CancellationToken ct = default)
    {
        var model = await _context.VehicleModels
            .Include(m => m.YearRanges)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (model == null)
            return Result.Failure(new Error(ErrorCodes.Vehicle.ModelNotFound, "Model bulunamadı."));

        if (model.YearRanges.Any())
            return Result.Failure(new Error(ErrorCodes.Vehicle.ModelHasYearRanges, "Bu modele ait yıl aralıkları var. Önce yıl aralıklarını silin."));

        _context.VehicleModels.Remove(model);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    // ==================== Year Ranges ====================

    public async Task<List<VehicleYearRangeDto>> GetYearRangesAsync(Guid? modelId = null, CancellationToken ct = default)
    {
        var query = _context.VehicleYearRanges
            .Include(y => y.Model)
            .AsQueryable();

        if (modelId.HasValue)
            query = query.Where(y => y.ModelId == modelId.Value);

        return await query
            .OrderBy(y => y.YearFrom)
            .Select(y => new VehicleYearRangeDto
            {
                Id = y.Id,
                ModelId = y.ModelId,
                ModelName = y.Model.Name,
                YearFrom = y.YearFrom,
                YearTo = y.YearTo,
                CreatedAt = y.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<Result<VehicleYearRangeDto>> GetYearRangeByIdAsync(Guid id, CancellationToken ct = default)
    {
        var yearRange = await _context.VehicleYearRanges
            .Include(y => y.Model)
            .Where(y => y.Id == id)
            .Select(y => new VehicleYearRangeDto
            {
                Id = y.Id,
                ModelId = y.ModelId,
                ModelName = y.Model.Name,
                YearFrom = y.YearFrom,
                YearTo = y.YearTo,
                CreatedAt = y.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        return yearRange != null
            ? Result<VehicleYearRangeDto>.Success(yearRange)
            : Result<VehicleYearRangeDto>.Failure(new Error(ErrorCodes.Vehicle.YearRangeNotFound, "Yıl aralığı bulunamadı."));
    }

    public async Task<Result<VehicleYearRangeDto>> CreateYearRangeAsync(CreateVehicleYearRangeDto dto, CancellationToken ct = default)
    {
        // Verify model exists
        var modelExists = await _context.VehicleModels.AnyAsync(m => m.Id == dto.ModelId, ct);
        if (!modelExists)
            return Result<VehicleYearRangeDto>.Failure(new Error(ErrorCodes.Vehicle.ModelNotFound, "Model bulunamadı."));

        // Validate year range
        if (dto.YearFrom > dto.YearTo)
            return Result<VehicleYearRangeDto>.Failure(new Error(ErrorCodes.Vehicle.YearRangeInvalid, "Başlangıç yılı bitiş yılından büyük olamaz."));

        // Check for duplicate year range under same model
        var exists = await _context.VehicleYearRanges
            .AnyAsync(y => y.ModelId == dto.ModelId && y.YearFrom == dto.YearFrom && y.YearTo == dto.YearTo, ct);

        if (exists)
            return Result<VehicleYearRangeDto>.Failure(new Error(ErrorCodes.Vehicle.YearRangeExists, "Bu model için aynı yıl aralığı zaten var."));

        var yearRange = new VehicleYearRange
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            ModelId = dto.ModelId,
            YearFrom = dto.YearFrom,
            YearTo = dto.YearTo,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.VehicleYearRanges.Add(yearRange);
        await _context.SaveChangesAsync(ct);

        var model = await _context.VehicleModels.FindAsync(new object[] { dto.ModelId }, ct);

        return Result<VehicleYearRangeDto>.Success(new VehicleYearRangeDto
        {
            Id = yearRange.Id,
            ModelId = yearRange.ModelId,
            ModelName = model!.Name,
            YearFrom = yearRange.YearFrom,
            YearTo = yearRange.YearTo,
            CreatedAt = yearRange.CreatedAt
        });
    }

    public async Task<Result<VehicleYearRangeDto>> UpdateYearRangeAsync(Guid id, UpdateVehicleYearRangeDto dto, CancellationToken ct = default)
    {
        var yearRange = await _context.VehicleYearRanges.FindAsync(new object[] { id }, ct);
        if (yearRange == null)
            return Result<VehicleYearRangeDto>.Failure(new Error(ErrorCodes.Vehicle.YearRangeNotFound, "Yıl aralığı bulunamadı."));

        // Verify model exists
        var modelExists = await _context.VehicleModels.AnyAsync(m => m.Id == dto.ModelId, ct);
        if (!modelExists)
            return Result<VehicleYearRangeDto>.Failure(new Error(ErrorCodes.Vehicle.ModelNotFound, "Model bulunamadı."));

        // Validate year range
        if (dto.YearFrom > dto.YearTo)
            return Result<VehicleYearRangeDto>.Failure(new Error(ErrorCodes.Vehicle.YearRangeInvalid, "Başlangıç yılı bitiş yılından büyük olamaz."));

        // Check for duplicate (excluding current)
        var exists = await _context.VehicleYearRanges
            .AnyAsync(y => y.ModelId == dto.ModelId && y.YearFrom == dto.YearFrom && y.YearTo == dto.YearTo && y.Id != id, ct);

        if (exists)
            return Result<VehicleYearRangeDto>.Failure(new Error(ErrorCodes.Vehicle.YearRangeExists, "Bu model için aynı yıl aralığı zaten var."));

        yearRange.ModelId = dto.ModelId;
        yearRange.YearFrom = dto.YearFrom;
        yearRange.YearTo = dto.YearTo;

        await _context.SaveChangesAsync(ct);

        var model = await _context.VehicleModels.FindAsync(new object[] { dto.ModelId }, ct);

        return Result<VehicleYearRangeDto>.Success(new VehicleYearRangeDto
        {
            Id = yearRange.Id,
            ModelId = yearRange.ModelId,
            ModelName = model!.Name,
            YearFrom = yearRange.YearFrom,
            YearTo = yearRange.YearTo,
            CreatedAt = yearRange.CreatedAt
        });
    }

    public async Task<Result> DeleteYearRangeAsync(Guid id, CancellationToken ct = default)
    {
        var yearRange = await _context.VehicleYearRanges
            .Include(y => y.Engines)
            .FirstOrDefaultAsync(y => y.Id == id, ct);

        if (yearRange == null)
            return Result.Failure(new Error(ErrorCodes.Vehicle.YearRangeNotFound, "Yıl aralığı bulunamadı."));

        if (yearRange.Engines.Any())
            return Result.Failure(new Error(ErrorCodes.Vehicle.YearRangeHasEngines, "Bu yıl aralığına ait motorlar var. Önce motorları silin."));

        _context.VehicleYearRanges.Remove(yearRange);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    // ==================== Engines ====================

    public async Task<List<VehicleEngineDto>> GetEnginesAsync(Guid? yearRangeId = null, CancellationToken ct = default)
    {
        var query = _context.VehicleEngines
            .Include(e => e.YearRange)
            .AsQueryable();

        if (yearRangeId.HasValue)
            query = query.Where(e => e.YearRangeId == yearRangeId.Value);

        return await query
            .OrderBy(e => e.Code)
            .ThenBy(e => e.FuelType)
            .Select(e => new VehicleEngineDto
            {
                Id = e.Id,
                YearRangeId = e.YearRangeId,
                YearRangeDisplay = e.YearRange.YearFrom + "-" + e.YearRange.YearTo,
                Code = e.Code,
                FuelType = e.FuelType,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<Result<VehicleEngineDto>> GetEngineByIdAsync(Guid id, CancellationToken ct = default)
    {
        var engine = await _context.VehicleEngines
            .Include(e => e.YearRange)
            .Where(e => e.Id == id)
            .Select(e => new VehicleEngineDto
            {
                Id = e.Id,
                YearRangeId = e.YearRangeId,
                YearRangeDisplay = e.YearRange.YearFrom + "-" + e.YearRange.YearTo,
                Code = e.Code,
                FuelType = e.FuelType,
                CreatedAt = e.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        return engine != null
            ? Result<VehicleEngineDto>.Success(engine)
            : Result<VehicleEngineDto>.Failure(new Error(ErrorCodes.Vehicle.EngineNotFound, "Motor bulunamadı."));
    }

    public async Task<Result<VehicleEngineDto>> CreateEngineAsync(CreateVehicleEngineDto dto, CancellationToken ct = default)
    {
        // Verify year range exists
        var yearRangeExists = await _context.VehicleYearRanges.AnyAsync(y => y.Id == dto.YearRangeId, ct);
        if (!yearRangeExists)
            return Result<VehicleEngineDto>.Failure(new Error(ErrorCodes.Vehicle.YearRangeNotFound, "Yıl aralığı bulunamadı."));

        // Check for duplicate
        var exists = await _context.VehicleEngines
            .AnyAsync(e => e.YearRangeId == dto.YearRangeId && e.Code == dto.Code && e.FuelType == dto.FuelType, ct);

        if (exists)
            return Result<VehicleEngineDto>.Failure(new Error(ErrorCodes.Vehicle.EngineCodeExists, "Bu yıl aralığı için aynı motor kodu ve yakıt tipi zaten var."));

        var engine = new VehicleEngine
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            YearRangeId = dto.YearRangeId,
            Code = dto.Code,
            FuelType = dto.FuelType,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.VehicleEngines.Add(engine);
        await _context.SaveChangesAsync(ct);

        var yearRange = await _context.VehicleYearRanges.FindAsync(new object[] { dto.YearRangeId }, ct);

        return Result<VehicleEngineDto>.Success(new VehicleEngineDto
        {
            Id = engine.Id,
            YearRangeId = engine.YearRangeId,
            YearRangeDisplay = $"{yearRange!.YearFrom}-{yearRange.YearTo}",
            Code = engine.Code,
            FuelType = engine.FuelType,
            CreatedAt = engine.CreatedAt
        });
    }

    public async Task<Result<VehicleEngineDto>> UpdateEngineAsync(Guid id, UpdateVehicleEngineDto dto, CancellationToken ct = default)
    {
        var engine = await _context.VehicleEngines.FindAsync(new object[] { id }, ct);
        if (engine == null)
            return Result<VehicleEngineDto>.Failure(new Error(ErrorCodes.Vehicle.EngineNotFound, "Motor bulunamadı."));

        // Verify year range exists
        var yearRangeExists = await _context.VehicleYearRanges.AnyAsync(y => y.Id == dto.YearRangeId, ct);
        if (!yearRangeExists)
            return Result<VehicleEngineDto>.Failure(new Error(ErrorCodes.Vehicle.YearRangeNotFound, "Yıl aralığı bulunamadı."));

        // Check for duplicate (excluding current)
        var exists = await _context.VehicleEngines
            .AnyAsync(e => e.YearRangeId == dto.YearRangeId && e.Code == dto.Code && e.FuelType == dto.FuelType && e.Id != id, ct);

        if (exists)
            return Result<VehicleEngineDto>.Failure(new Error(ErrorCodes.Vehicle.EngineCodeExists, "Bu yıl aralığı için aynı motor kodu ve yakıt tipi zaten var."));

        engine.YearRangeId = dto.YearRangeId;
        engine.Code = dto.Code;
        engine.FuelType = dto.FuelType;

        await _context.SaveChangesAsync(ct);

        var yearRange = await _context.VehicleYearRanges.FindAsync(new object[] { dto.YearRangeId }, ct);

        return Result<VehicleEngineDto>.Success(new VehicleEngineDto
        {
            Id = engine.Id,
            YearRangeId = engine.YearRangeId,
            YearRangeDisplay = $"{yearRange!.YearFrom}-{yearRange.YearTo}",
            Code = engine.Code,
            FuelType = engine.FuelType,
            CreatedAt = engine.CreatedAt
        });
    }

    public async Task<Result> DeleteEngineAsync(Guid id, CancellationToken ct = default)
    {
        var engine = await _context.VehicleEngines
            .Include(e => e.Fitments)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (engine == null)
            return Result.Failure(new Error(ErrorCodes.Vehicle.EngineNotFound, "Motor bulunamadı."));

        if (engine.Fitments.Any())
            return Result.Failure(new Error(ErrorCodes.Vehicle.EngineHasFitments, "Bu motor uyumluluk kayıtlarında kullanılıyor. Önce uyumlulukları silin."));

        _context.VehicleEngines.Remove(engine);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    // ==================== Fitments ====================

    public async Task<List<StockCardFitmentDto>> GetFitmentsAsync(Guid variantId, CancellationToken ct = default)
    {
        return await _context.StockCardFitments
            .Include(f => f.VehicleEngine)
                .ThenInclude(e => e.YearRange)
                .ThenInclude(y => y.Model)
                .ThenInclude(m => m.Brand)
            .Where(f => f.VariantId == variantId)
            .OrderBy(f => f.VehicleEngine.YearRange.Model.Brand.Name)
            .ThenBy(f => f.VehicleEngine.YearRange.Model.Name)
            .ThenBy(f => f.VehicleEngine.YearRange.YearFrom)
            .Select(f => new StockCardFitmentDto
            {
                Id = f.Id,
                VariantId = f.VariantId,
                VehicleEngineId = f.VehicleEngineId,
                Notes = f.Notes,
                BrandName = f.VehicleEngine.YearRange.Model.Brand.Name,
                ModelName = f.VehicleEngine.YearRange.Model.Name,
                YearRange = f.VehicleEngine.YearRange.YearFrom + "-" + f.VehicleEngine.YearRange.YearTo,
                EngineCode = f.VehicleEngine.Code,
                FuelType = f.VehicleEngine.FuelType,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<Result<StockCardFitmentDto>> CreateFitmentAsync(Guid variantId, CreateStockCardFitmentDto dto, CancellationToken ct = default)
    {
        // Verify variant exists
        var variantExists = await _context.ProductVariants.AnyAsync(v => v.Id == variantId, ct);
        if (!variantExists)
            return Result<StockCardFitmentDto>.Failure(new Error("variant_not_found", "Stok kartı bulunamadı."));

        // Verify engine exists
        var engineExists = await _context.VehicleEngines.AnyAsync(e => e.Id == dto.VehicleEngineId, ct);
        if (!engineExists)
            return Result<StockCardFitmentDto>.Failure(new Error(ErrorCodes.Vehicle.EngineNotFound, "Motor bulunamadı."));

        // Check for duplicate fitment
        var exists = await _context.StockCardFitments
            .AnyAsync(f => f.VariantId == variantId && f.VehicleEngineId == dto.VehicleEngineId, ct);

        if (exists)
            return Result<StockCardFitmentDto>.Failure(new Error(ErrorCodes.Vehicle.FitmentExists, "Bu uyumluluk kaydı zaten var."));

        var fitment = new StockCardFitment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            VariantId = variantId,
            VehicleEngineId = dto.VehicleEngineId,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.StockCardFitments.Add(fitment);
        await _context.SaveChangesAsync(ct);

        // Load full data for response
        var result = await _context.StockCardFitments
            .Include(f => f.VehicleEngine)
                .ThenInclude(e => e.YearRange)
                .ThenInclude(y => y.Model)
                .ThenInclude(m => m.Brand)
            .Where(f => f.Id == fitment.Id)
            .Select(f => new StockCardFitmentDto
            {
                Id = f.Id,
                VariantId = f.VariantId,
                VehicleEngineId = f.VehicleEngineId,
                Notes = f.Notes,
                BrandName = f.VehicleEngine.YearRange.Model.Brand.Name,
                ModelName = f.VehicleEngine.YearRange.Model.Name,
                YearRange = f.VehicleEngine.YearRange.YearFrom + "-" + f.VehicleEngine.YearRange.YearTo,
                EngineCode = f.VehicleEngine.Code,
                FuelType = f.VehicleEngine.FuelType,
                CreatedAt = f.CreatedAt
            })
            .FirstAsync(ct);

        return Result<StockCardFitmentDto>.Success(result);
    }

    public async Task<Result> DeleteFitmentAsync(Guid variantId, Guid fitmentId, CancellationToken ct = default)
    {
        var fitment = await _context.StockCardFitments
            .FirstOrDefaultAsync(f => f.Id == fitmentId && f.VariantId == variantId, ct);

        if (fitment == null)
            return Result.Failure(new Error(ErrorCodes.Vehicle.FitmentNotFound, "Uyumluluk kaydı bulunamadı."));

        _context.StockCardFitments.Remove(fitment);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
