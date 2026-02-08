using Xunit;
using FluentAssertions;
using ErpCloud.Api.Services;
using ErpCloud.Api.Data;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Common;
using ErpCloud.BuildingBlocks.Tenant;
using System;
using System.Threading.Tasks;

namespace ErpCloud.Api.Tests;

/// <summary>
/// Collection definition to disable parallel execution for VehicleFitmentModuleTests.
/// </summary>
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialCollection
{
}

/// <summary>
/// Vehicle fitment module tests using SQLite in-memory for reliable constraint enforcement.
/// </summary>
[Collection("Sequential")]
public class VehicleFitmentModuleTests : IClassFixture<SqliteTestDbFactory>
{
    private readonly SqliteTestDbFactory _dbFactory;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public VehicleFitmentModuleTests(SqliteTestDbFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private VehicleService CreateServiceWithContext(ErpDbContext context, TenantContext tenantContext)
    {
        return new VehicleService(context, tenantContext);
    }

    /// <summary>
    /// Asserts that a Result operation succeeded. Throws with error details if failed.
    /// </summary>
    private void AssertSuccess(Result result, string operation = "Operation")
    {
        result.IsSuccess.Should().BeTrue(
            $"{operation} failed with error code '{result.Error?.Code}': {result.Error?.Message}");
    }

    /// <summary>
    /// Asserts that a Result<T> operation succeeded. Throws with error details if failed.
    /// </summary>
    private void AssertSuccess<T>(Result<T> result, string operation = "Operation")
    {
        result.IsSuccess.Should().BeTrue(
            $"{operation} failed with error code '{result.Error?.Code}': {result.Error?.Message}");
    }

    [Fact]
    public async Task Test1_CreateBrand_Success()
    {
        // Arrange
        var (context, tenantContext) = _dbFactory.CreateContext(_tenantId, _userId);
        using (context)
        {
            var service = CreateServiceWithContext(context, tenantContext);
            var dto = new CreateVehicleBrandDto { Code = "BMW", Name = "BMW" };

            // Act
            var result = await service.CreateBrandAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Code.Should().Be("BMW");
            result.Value.Name.Should().Be("BMW");
        }
    }

    [Fact]
    public async Task Test2_CreateBrand_DuplicateCode_Fails_CaseInsensitive()
    {
        // Arrange
        var (context, tenantContext) = _dbFactory.CreateContext(_tenantId, _userId);
        using (context)
        {
            var service = CreateServiceWithContext(context, tenantContext);
        
            // Create first brand with lowercase - should normalize to "BMW"
            var dto1 = new CreateVehicleBrandDto { Code = "bmw", Name = "BMW" };
            var result1 = await service.CreateBrandAsync(dto1);
            
            // Verify first brand was created successfully
            result1.IsSuccess.Should().BeTrue("First brand creation should succeed");
            result1.Value.Code.Should().Be("BMW", "Code should be normalized to uppercase");

            // Try to create second brand with uppercase - should be detected as duplicate
            var dto2 = new CreateVehicleBrandDto { Code = "BMW", Name = "Bayerische Motoren Werke" };

            // Act
            var result = await service.CreateBrandAsync(dto2);

            // Assert - Should fail due to service-level duplicate detection, NOT DB exception
            result.IsSuccess.Should().BeFalse("Service should detect duplicate before DB");
            result.Error.Code.Should().Be(ErrorCodes.Vehicle.BrandCodeExists);
        }
    }

    [Fact]
    public async Task Test3_DeleteBrand_WithModels_Fails()
    {
        // Arrange
        var (context, tenantContext) = _dbFactory.CreateContext(_tenantId, _userId);
        using (context)
        {
            var service = CreateServiceWithContext(context, tenantContext);
        
            var brandResult = await service.CreateBrandAsync(new CreateVehicleBrandDto { Code = "AUDI", Name = "Audi" });
            AssertSuccess(brandResult, "CreateBrand");
            var brandId = brandResult.Value.Id;
            var modelResult = await service.CreateModelAsync(new CreateVehicleModelDto { BrandId = brandId, Name = "A4" });
            AssertSuccess(modelResult, "CreateModel");
            
            // Act
            var deleteResult = await service.DeleteBrandAsync(brandId);

            // Assert
            deleteResult.IsSuccess.Should().BeFalse();
            deleteResult.Error.Code.Should().Be(ErrorCodes.Vehicle.BrandHasModels);
        }
    }

    [Fact]
    public async Task Test4_DeleteModel_WithYearRanges_Fails()
    {
        // Arrange
        var (context, tenantContext) = _dbFactory.CreateContext(_tenantId, _userId);
        using (context)
        {
            var service = CreateServiceWithContext(context, tenantContext);
        
            var brandResult = await service.CreateBrandAsync(new CreateVehicleBrandDto { Code = "MERC", Name = "Mercedes" });
            AssertSuccess(brandResult, "CreateBrand");
            var modelResult = await service.CreateModelAsync(new CreateVehicleModelDto 
            { 
                BrandId = brandResult.Value.Id, 
                Name = "C-Class" 
            });
            AssertSuccess(modelResult, "CreateModel");
            var yearResult = await service.CreateYearRangeAsync(new CreateVehicleYearRangeDto
            {
                ModelId = modelResult.Value.Id,
                YearFrom = 2015,
                YearTo = 2020
            });
            AssertSuccess(yearResult, "CreateYearRange");
            
            // Act
            var deleteResult = await service.DeleteModelAsync(modelResult.Value.Id);

            // Assert
            deleteResult.IsSuccess.Should().BeFalse();
            deleteResult.Error.Code.Should().Be(ErrorCodes.Vehicle.ModelHasYearRanges);
        }
    }

    [Fact]
    public async Task Test5_DeleteYearRange_WithEngines_Fails()
    {
        // Arrange
        var (context, tenantContext) = _dbFactory.CreateContext(_tenantId, _userId);
        using (context)
        {
            var service = CreateServiceWithContext(context, tenantContext);
        
            var brandResult = await service.CreateBrandAsync(new CreateVehicleBrandDto { Code = "VW", Name = "Volkswagen" });
            AssertSuccess(brandResult, "CreateBrand");
            var modelResult = await service.CreateModelAsync(new CreateVehicleModelDto 
            { 
                BrandId = brandResult.Value.Id, 
                Name = "Golf" 
            });
            AssertSuccess(modelResult, "CreateModel");
            var yearResult = await service.CreateYearRangeAsync(new CreateVehicleYearRangeDto
            {
                ModelId = modelResult.Value.Id,
                YearFrom = 2018,
                YearTo = 2022
            });
            AssertSuccess(yearResult, "CreateYearRange");
            var engineResult = await service.CreateEngineAsync(new CreateVehicleEngineDto
            {
                YearRangeId = yearResult.Value.Id,
                Code = "TSI",
                FuelType = "Benzin"
            });
            AssertSuccess(engineResult, "CreateEngine");
            
            // Act
            var deleteResult = await service.DeleteYearRangeAsync(yearResult.Value.Id);

            // Assert
            deleteResult.IsSuccess.Should().BeFalse();
            deleteResult.Error.Code.Should().Be(ErrorCodes.Vehicle.YearRangeHasEngines);
        }
    }

    [Fact]
    public async Task Test6_AddFitment_Success()
    {
        // Arrange
        var (context, tenantContext) = _dbFactory.CreateContext(_tenantId, _userId);
        using (context)
        {
            var service = CreateServiceWithContext(context, tenantContext);
        
            var variantId = await CreateTestVariantAsync(context);
            var engineId = await CreateFullVehicleHierarchyAsync(context, service);

            var dto = new CreateStockCardFitmentDto
            {
                VehicleEngineId = engineId,
                Notes = "Test fitment"
            };

            // Act
            var result = await service.CreateFitmentAsync(variantId, dto);

            // Assert
            AssertSuccess(result, "CreateFitment");
            result.Value.Should().NotBeNull();
            result.Value.VehicleEngineId.Should().Be(engineId);
            result.Value.Notes.Should().Be("Test fitment");
        }
    }

    [Fact]
    public async Task Test7_AddFitment_Duplicate_Fails()
    {
        // Arrange
        var (context, tenantContext) = _dbFactory.CreateContext(_tenantId, _userId);
        using (context)
        {
            var service = CreateServiceWithContext(context, tenantContext);
        
            var variantId = await CreateTestVariantAsync(context);
            var engineId = await CreateFullVehicleHierarchyAsync(context, service);

            var dto = new CreateStockCardFitmentDto { VehicleEngineId = engineId };

            // Act
            var firstResult = await service.CreateFitmentAsync(variantId, dto);
            AssertSuccess(firstResult, "First CreateFitment");
            var result = await service.CreateFitmentAsync(variantId, dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.Vehicle.FitmentExists);
        }
    }

    [Fact]
    public async Task Test8_DeleteEngine_WithFitments_Fails()
    {
        // Arrange
        var (context, tenantContext) = _dbFactory.CreateContext(_tenantId, _userId);
        using (context)
        {
            var service = CreateServiceWithContext(context, tenantContext);
        
            var variantId = await CreateTestVariantAsync(context);
            var engineId = await CreateFullVehicleHierarchyAsync(context, service);

            var fitmentResult = await service.CreateFitmentAsync(variantId, new CreateStockCardFitmentDto
            {
                VehicleEngineId = engineId
            });
            AssertSuccess(fitmentResult, "CreateFitment");

            // Act
            var deleteResult = await service.DeleteEngineAsync(engineId);

            // Assert
            deleteResult.IsSuccess.Should().BeFalse();
            deleteResult.Error.Code.Should().Be(ErrorCodes.Vehicle.EngineHasFitments);
        }
    }

    [Fact]
    public async Task Test9_TenantIsolation_CrossTenantAccess_Blocked()
    {
        // Arrange - Create brand with tenant1
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        Guid brandId;
        
        var (context1, tenantContext1) = _dbFactory.CreateContext(tenant1Id, _userId);
        using (context1)
        {
            var service1 = new VehicleService(context1, tenantContext1);
        
            var brandResult = await service1.CreateBrandAsync(new CreateVehicleBrandDto { Code = "FORD", Name = "Ford" });
            brandId = brandResult.Value.Id;
        }

        // Act - Try to access with tenant2
        var (context2, tenantContext2) = _dbFactory.CreateContext(tenant2Id, _userId);
        using (context2)
        {
            var service2 = new VehicleService(context2, tenantContext2);
            var getResult = await service2.GetBrandByIdAsync(brandId);

            // Assert
            getResult.IsSuccess.Should().BeFalse();
            getResult.Error.Code.Should().Be(ErrorCodes.Vehicle.BrandNotFound);
        }
    }

    [Fact]
    public async Task Test10_CreateYearRange_InvalidRange_Fails()
    {
        // Arrange
        var (context, tenantContext) = _dbFactory.CreateContext(_tenantId, _userId);
        using (context)
        {
            var service = CreateServiceWithContext(context, tenantContext);
        
            var brandResult = await service.CreateBrandAsync(new CreateVehicleBrandDto { Code = "TOYT", Name = "Toyota" });
            AssertSuccess(brandResult, "CreateBrand");
            var modelResult = await service.CreateModelAsync(new CreateVehicleModelDto 
            { 
                BrandId = brandResult.Value.Id, 
                Name = "Corolla" 
            });
            AssertSuccess(modelResult, "CreateModel");
            var dto = new CreateVehicleYearRangeDto
            {
                ModelId = modelResult.Value.Id,
                YearFrom = 2020,
                YearTo = 2015 // Invalid: YearTo < YearFrom
            };

            // Act
            var result = await service.CreateYearRangeAsync(dto);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be(ErrorCodes.Vehicle.YearRangeInvalid);
        }
    }

    [Fact]
    public async Task Test11_GetModels_FilterByBrand_ReturnsCorrectModels()
    {
        // Arrange
        var (context, tenantContext) = _dbFactory.CreateContext(_tenantId, _userId);
        using (context)
        {
            var service = CreateServiceWithContext(context, tenantContext);
        
            var hondaResult = await service.CreateBrandAsync(new CreateVehicleBrandDto { Code = "HOND", Name = "Honda" });
            var nissanResult = await service.CreateBrandAsync(new CreateVehicleBrandDto { Code = "NISS", Name = "Nissan" });

            await service.CreateModelAsync(new CreateVehicleModelDto { BrandId = hondaResult.Value.Id, Name = "Civic" });
            await service.CreateModelAsync(new CreateVehicleModelDto { BrandId = hondaResult.Value.Id, Name = "Accord" });
            await service.CreateModelAsync(new CreateVehicleModelDto { BrandId = nissanResult.Value.Id, Name = "Altima" });

            // Act
            var hondaModels = await service.GetModelsAsync(hondaResult.Value.Id);

            // Assert
            hondaModels.Should().HaveCount(2);
            hondaModels.Should().OnlyContain(m => m.BrandName == "Honda");
        }
    }

    // Helper methods
    private async Task<Guid> CreateTestVariantAsync(ErpDbContext context)
    {
        var product = new Entities.Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = "TEST-PROD",
            Name = "Test Product",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };
        context.Products.Add(product);

        var variant = new Entities.ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = product.Id,
            Sku = "TEST-SKU",
            Name = "Test Variant",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _userId
        };
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        return variant.Id;
    }

    private async Task<Guid> CreateFullVehicleHierarchyAsync(ErpDbContext context, VehicleService service)
    {
        var brandResult = await service.CreateBrandAsync(new CreateVehicleBrandDto { Code = "TEST", Name = "Test Brand" });
        AssertSuccess(brandResult, "CreateBrand in helper");
        
        var modelResult = await service.CreateModelAsync(new CreateVehicleModelDto 
        { 
            BrandId = brandResult.Value.Id, 
            Name = "Test Model" 
        });
        AssertSuccess(modelResult, "CreateModel in helper");
        
        var yearResult = await service.CreateYearRangeAsync(new CreateVehicleYearRangeDto
        {
            ModelId = modelResult.Value.Id,
            YearFrom = 2015,
            YearTo = 2020
        });
        AssertSuccess(yearResult, "CreateYearRange in helper");
        
        var engineResult = await service.CreateEngineAsync(new CreateVehicleEngineDto
        {
            YearRangeId = yearResult.Value.Id,
            Code = "T123",
            FuelType = "Test Fuel"
        });
        AssertSuccess(engineResult, "CreateEngine in helper");

        return engineResult.Value.Id;
    }
}
