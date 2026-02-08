namespace ErpCloud.BuildingBlocks.Common;

/// <summary>
/// Centralized error codes for the application.
/// </summary>
public static class ErrorCodes
{
    public static class Vehicle
    {
        public const string BrandNotFound = "brand_not_found";
        public const string BrandCodeExists = "duplicate_code";
        public const string BrandHasModels = "brand_has_models";
        
        public const string ModelNotFound = "model_not_found";
        public const string ModelNameExists = "duplicate_name";
        public const string ModelHasYearRanges = "model_has_years";
        
        public const string YearRangeNotFound = "year_range_not_found";
        public const string YearRangeInvalid = "invalid_year_range";
        public const string YearRangeExists = "duplicate_year_range";
        public const string YearRangeHasEngines = "year_range_has_engines";
        
        public const string EngineNotFound = "engine_not_found";
        public const string EngineCodeExists = "duplicate_engine";
        public const string EngineHasFitments = "engine_has_fitments";
        
        public const string FitmentNotFound = "fitment_not_found";
        public const string FitmentExists = "fitment_exists";
        public const string EngineNotFoundForFitment = "engine_not_found";
    }
    
    public static class Common
    {
        public const string NotFound = "not_found";
        public const string Unauthorized = "unauthorized";
        public const string ValidationFailed = "validation_failed";
        public const string Conflict = "conflict";
    }
}
