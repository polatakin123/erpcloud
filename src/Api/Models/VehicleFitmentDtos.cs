namespace ErpCloud.Api.Models;

// ==================== Vehicle Brand DTOs ====================

public class VehicleBrandDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateVehicleBrandDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class UpdateVehicleBrandDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

// ==================== Vehicle Model DTOs ====================

public class VehicleModelDto
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateVehicleModelDto
{
    public Guid BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UpdateVehicleModelDto
{
    public Guid BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ==================== Vehicle Year Range DTOs ====================

public class VehicleYearRangeDto
{
    public Guid Id { get; set; }
    public Guid ModelId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int YearFrom { get; set; }
    public int YearTo { get; set; }
    public string DisplayName => $"{YearFrom}-{YearTo}";
    public DateTime CreatedAt { get; set; }
}

public class CreateVehicleYearRangeDto
{
    public Guid ModelId { get; set; }
    public int YearFrom { get; set; }
    public int YearTo { get; set; }
}

public class UpdateVehicleYearRangeDto
{
    public Guid ModelId { get; set; }
    public int YearFrom { get; set; }
    public int YearTo { get; set; }
}

// ==================== Vehicle Engine DTOs ====================

public class VehicleEngineDto
{
    public Guid Id { get; set; }
    public Guid YearRangeId { get; set; }
    public string YearRangeDisplay { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public string DisplayName => $"{Code} {FuelType}";
    public DateTime CreatedAt { get; set; }
}

public class CreateVehicleEngineDto
{
    public Guid YearRangeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
}

public class UpdateVehicleEngineDto
{
    public Guid YearRangeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
}

// ==================== Stock Card Fitment DTOs ====================

public class StockCardFitmentDto
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public Guid VehicleEngineId { get; set; }
    public string? Notes { get; set; }
    
    // Populated for display
    public string BrandName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string YearRange { get; set; } = string.Empty;
    public string EngineCode { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public string FullDisplay => $"{BrandName} {ModelName} ({YearRange}) {EngineCode} {FuelType}";
    
    public DateTime CreatedAt { get; set; }
}

public class CreateStockCardFitmentDto
{
    public Guid VehicleEngineId { get; set; }
    public string? Notes { get; set; }
}
