using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    // ==================== Brands ====================

    [HttpGet("brands")]
    public async Task<IActionResult> GetBrands(CancellationToken ct)
    {
        var brands = await _vehicleService.GetBrandsAsync(ct);
        return Ok(brands);
    }

    [HttpGet("brands/{id:guid}")]
    public async Task<IActionResult> GetBrand(Guid id, CancellationToken ct)
    {
        var result = await _vehicleService.GetBrandByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost("brands")]
    public async Task<IActionResult> CreateBrand([FromBody] CreateVehicleBrandDto dto, CancellationToken ct)
    {
        var result = await _vehicleService.CreateBrandAsync(dto, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("brands/{id:guid}")]
    public async Task<IActionResult> UpdateBrand(Guid id, [FromBody] UpdateVehicleBrandDto dto, CancellationToken ct)
    {
        var result = await _vehicleService.UpdateBrandAsync(id, dto, ct);
        return result.IsSuccess ? Ok(result.Value) : result.Error == "brand_not_found" ? NotFound(result.Error) : BadRequest(result.Error);
    }

    [HttpDelete("brands/{id:guid}")]
    public async Task<IActionResult> DeleteBrand(Guid id, CancellationToken ct)
    {
        var result = await _vehicleService.DeleteBrandAsync(id, ct);
        return result.IsSuccess ? NoContent() : result.Error == "brand_not_found" ? NotFound(result.Error) : Conflict(result.Error);
    }

    // ==================== Models ====================

    [HttpGet("models")]
    public async Task<IActionResult> GetModels([FromQuery] Guid? brandId, CancellationToken ct)
    {
        var models = await _vehicleService.GetModelsAsync(brandId, ct);
        return Ok(models);
    }

    [HttpGet("models/{id:guid}")]
    public async Task<IActionResult> GetModel(Guid id, CancellationToken ct)
    {
        var result = await _vehicleService.GetModelByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost("models")]
    public async Task<IActionResult> CreateModel([FromBody] CreateVehicleModelDto dto, CancellationToken ct)
    {
        var result = await _vehicleService.CreateModelAsync(dto, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("models/{id:guid}")]
    public async Task<IActionResult> UpdateModel(Guid id, [FromBody] UpdateVehicleModelDto dto, CancellationToken ct)
    {
        var result = await _vehicleService.UpdateModelAsync(id, dto, ct);
        return result.IsSuccess ? Ok(result.Value) : result.Error == "model_not_found" ? NotFound(result.Error) : BadRequest(result.Error);
    }

    [HttpDelete("models/{id:guid}")]
    public async Task<IActionResult> DeleteModel(Guid id, CancellationToken ct)
    {
        var result = await _vehicleService.DeleteModelAsync(id, ct);
        return result.IsSuccess ? NoContent() : result.Error == "model_not_found" ? NotFound(result.Error) : Conflict(result.Error);
    }

    // ==================== Year Ranges ====================

    [HttpGet("years")]
    public async Task<IActionResult> GetYearRanges([FromQuery] Guid? modelId, CancellationToken ct)
    {
        var years = await _vehicleService.GetYearRangesAsync(modelId, ct);
        return Ok(years);
    }

    [HttpGet("years/{id:guid}")]
    public async Task<IActionResult> GetYearRange(Guid id, CancellationToken ct)
    {
        var result = await _vehicleService.GetYearRangeByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost("years")]
    public async Task<IActionResult> CreateYearRange([FromBody] CreateVehicleYearRangeDto dto, CancellationToken ct)
    {
        var result = await _vehicleService.CreateYearRangeAsync(dto, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("years/{id:guid}")]
    public async Task<IActionResult> UpdateYearRange(Guid id, [FromBody] UpdateVehicleYearRangeDto dto, CancellationToken ct)
    {
        var result = await _vehicleService.UpdateYearRangeAsync(id, dto, ct);
        return result.IsSuccess ? Ok(result.Value) : result.Error == "year_range_not_found" ? NotFound(result.Error) : BadRequest(result.Error);
    }

    [HttpDelete("years/{id:guid}")]
    public async Task<IActionResult> DeleteYearRange(Guid id, CancellationToken ct)
    {
        var result = await _vehicleService.DeleteYearRangeAsync(id, ct);
        return result.IsSuccess ? NoContent() : result.Error == "year_range_not_found" ? NotFound(result.Error) : Conflict(result.Error);
    }

    // ==================== Engines ====================

    [HttpGet("engines")]
    public async Task<IActionResult> GetEngines([FromQuery] Guid? yearRangeId, CancellationToken ct)
    {
        var engines = await _vehicleService.GetEnginesAsync(yearRangeId, ct);
        return Ok(engines);
    }

    [HttpGet("engines/{id:guid}")]
    public async Task<IActionResult> GetEngine(Guid id, CancellationToken ct)
    {
        var result = await _vehicleService.GetEngineByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost("engines")]
    public async Task<IActionResult> CreateEngine([FromBody] CreateVehicleEngineDto dto, CancellationToken ct)
    {
        var result = await _vehicleService.CreateEngineAsync(dto, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("engines/{id:guid}")]
    public async Task<IActionResult> UpdateEngine(Guid id, [FromBody] UpdateVehicleEngineDto dto, CancellationToken ct)
    {
        var result = await _vehicleService.UpdateEngineAsync(id, dto, ct);
        return result.IsSuccess ? Ok(result.Value) : result.Error == "engine_not_found" ? NotFound(result.Error) : BadRequest(result.Error);
    }

    [HttpDelete("engines/{id:guid}")]
    public async Task<IActionResult> DeleteEngine(Guid id, CancellationToken ct)
    {
        var result = await _vehicleService.DeleteEngineAsync(id, ct);
        return result.IsSuccess ? NoContent() : result.Error == "engine_not_found" ? NotFound(result.Error) : Conflict(result.Error);
    }

    // ==================== Fitments ====================

    [HttpGet("fitments/variant/{variantId:guid}")]
    public async Task<IActionResult> GetFitments(Guid variantId, CancellationToken ct)
    {
        var fitments = await _vehicleService.GetFitmentsAsync(variantId, ct);
        return Ok(fitments);
    }

    [HttpPost("fitments/variant/{variantId:guid}")]
    public async Task<IActionResult> CreateFitment(Guid variantId, [FromBody] CreateStockCardFitmentDto dto, CancellationToken ct)
    {
        var result = await _vehicleService.CreateFitmentAsync(variantId, dto, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("fitments/variant/{variantId:guid}/{fitmentId:guid}")]
    public async Task<IActionResult> DeleteFitment(Guid variantId, Guid fitmentId, CancellationToken ct)
    {
        var result = await _vehicleService.DeleteFitmentAsync(variantId, fitmentId, ct);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }
}
