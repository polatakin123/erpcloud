using ErpCloud.Api.Services;
using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Tests;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpCloud.Tests.Services;

/// <summary>
/// Integration tests for OEM-based variant search functionality with fitment filtering
/// Uses SQLite in-memory provider for ILike support
/// </summary>
public class VariantSearchServiceTests : IClassFixture<SqliteTestDbFactory>, IDisposable
{
    private readonly SqliteTestDbFactory _dbFactory;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly List<ErpDbContext> _contexts = new();

    public VariantSearchServiceTests(SqliteTestDbFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private (ErpDbContext Context, VariantSearchService SearchService, PartReferenceService PartRefService) CreateServices()
    {
        var (context, tenantContext) = _dbFactory.CreateContext(_tenantId, _userId);
        _contexts.Add(context);
        
        var searchService = new VariantSearchService(context, tenantContext);
        var partRefService = new PartReferenceService(context, tenantContext);
        
        return (context, searchService, partRefService);
    }

    private (ErpDbContext Context, TenantContext TenantContext) CreateContextWithTenantContext()
    {
        var (context, tenantContext) = _dbFactory.CreateContext(_tenantId, _userId);
        _contexts.Add(context);
        return (context, tenantContext);
    }

    private Product CreateProduct(ErpDbContext context, string name, string code)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = name,
            Code = code,
            IsActive = true
        };
        context.Products.Add(product);
        return product;
    }

    private ProductVariant CreateVariant(ErpDbContext context, Guid productId, string sku, string name)
    {
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = productId,
            Sku = sku,
            Name = name,
            Unit = "EA",
            VatRate = 0.18m,
            IsActive = true
        };
        context.ProductVariants.Add(variant);
        return variant;
    }

    private VehicleEngine CreateVehicleEngine(ErpDbContext context, Guid? engineId = null)
    {
        // Create minimal vehicle hierarchy for fitment tests
        var brand = new VehicleBrand
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = $"TST-{Guid.NewGuid().ToString("N").Substring(0, 8)}",  // Unique code per call
            Name = "Test Brand"
        };
        context.Set<VehicleBrand>().Add(brand);

        var model = new VehicleModel
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            BrandId = brand.Id,
            Name = "Test Model"
        };
        context.Set<VehicleModel>().Add(model);

        var yearRange = new VehicleYearRange
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ModelId = model.Id,
            YearFrom = 2020,
            YearTo = 2023
        };
        context.Set<VehicleYearRange>().Add(yearRange);

        var engine = new VehicleEngine
        {
            Id = engineId ?? Guid.NewGuid(),
            TenantId = _tenantId,
            YearRangeId = yearRange.Id,
            Code = "1.6 TSI",
            FuelType = "Petrol"
        };
        context.Set<VehicleEngine>().Add(engine);

        return engine;
    }

    private Warehouse CreateWarehouse(ErpDbContext context, Guid? warehouseId = null)
    {
        // Create minimal organization hierarchy for warehouse
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Test Organization",
            Code = $"ORG-{Guid.NewGuid().ToString("N").Substring(0, 8)}"
        };
        context.Set<Organization>().Add(organization);

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            OrganizationId = organization.Id,
            Name = "Test Branch",
            Code = $"BR-{Guid.NewGuid().ToString("N").Substring(0, 8)}"
        };
        context.Set<Branch>().Add(branch);

        var warehouse = new Warehouse
        {
            Id = warehouseId ?? Guid.NewGuid(),
            TenantId = _tenantId,
            BranchId = branch.Id,
            Name = "Test Warehouse",
            Code = $"WH-{Guid.NewGuid().ToString("N").Substring(0, 8)}"
        };
        context.Set<Warehouse>().Add(warehouse);

        return warehouse;
    }

    [Fact]
    public async Task SearchByProductName_ReturnsVariant()
    {
        // Arrange
        var (context, searchService, _) = CreateServices();
        
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Brake Pad Front",
            Code = "BP-001",
            IsActive = true
        };

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = product.Id,
            Sku = "BP-001-V1",
            Name = "Variant 1",
            Unit = "EA",
            VatRate = 0.18m,
            IsActive = true
        };

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        // Act
        var results = await searchService.SearchVariantsAsync("Brake", includeEquivalents: false);

        // Assert
        Assert.Single(results);
        Assert.Equal(variant.Id, results[0].VariantId);
        Assert.Equal("DIRECT", results[0].MatchType);
        Assert.Equal("NAME", results[0].MatchedBy);
    }

    [Fact]
    public async Task SearchByVariantName_ReturnsVariant()
    {
        // Arrange
        var (context, searchService, _) = CreateServices();
        var product = CreateProduct(context, "Test Product", "TP-001");
        var variant = CreateVariant(context, product.Id, "TP-001-V1", "Red Variant");
        await context.SaveChangesAsync();

        // Act
        var results = await searchService.SearchVariantsAsync("Red", includeEquivalents: false);

        // Assert
        Assert.Single(results);
        Assert.Equal(variant.Id, results[0].VariantId);
    }

    [Fact]
    public async Task SearchBySKU_ReturnsVariant()
    {
        // Arrange
        var (context, searchService, _) = CreateServices();
        var product = CreateProduct(context, "Test Product", "TP-002");
        var variant = CreateVariant(context, product.Id, "SKU12345", "Variant");
        await context.SaveChangesAsync();

        // Act - search with exact SKU
        var results = await searchService.SearchVariantsAsync("SKU12345", includeEquivalents: false);

        // Assert
        Assert.Single(results);
        Assert.Equal(variant.Id, results[0].VariantId);
        Assert.Equal("SKU", results[0].MatchedBy);
    }

    [Fact]
    public async Task SearchByBarcode_ReturnsVariant()
    {
        // Arrange
        var (context, searchService, _) = CreateServices();
        var product = CreateProduct(context, "Test Product", "TP-003");
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = product.Id,
            Sku = "TP-003-V1",
            Name = "Variant",
            Barcode = "1234567890123",
            Unit = "EA",
            VatRate = 0.18m,
            IsActive = true
        };

        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        // Act
        var results = await searchService.SearchVariantsAsync("1234567890123", includeEquivalents: false);

        // Assert
        Assert.Single(results);
        Assert.Equal(variant.Id, results[0].VariantId);
        Assert.Equal("BARCODE", results[0].MatchedBy);
    }

    [Fact]
    public async Task SearchByOEM_ReturnsVariant()
    {
        // Arrange
        var (context, searchService, partRefService) = CreateServices();
        var product = CreateProduct(context, "Test Product", "TP-004");
        var variant = CreateVariant(context, product.Id, "TP-004-V1", "Variant");
        await context.SaveChangesAsync();

        // Add OEM reference
        await partRefService.CreateReferenceAsync(variant.Id, "OEM", "ABC123XYZ");

        // Act
        var results = await searchService.SearchVariantsAsync("ABC123XYZ", includeEquivalents: false);

        // Assert
        Assert.Single(results);
        Assert.Equal(variant.Id, results[0].VariantId);
        Assert.Equal("OEM", results[0].MatchedBy);
        Assert.Contains("ABC123XYZ", results[0].OemRefs);
    }

    [Fact]
    public async Task SearchByOEM_WithDashesAndSpaces_FindsNormalizedMatch()
    {
        // Arrange
        var (context, searchService, partRefService) = CreateServices();
        var product = CreateProduct(context, "Part", "P-001");
        var variant = CreateVariant(context, product.Id, "P-001-V1", "Variant");
        await context.SaveChangesAsync();

        // Add OEM with dashes
        await partRefService.CreateReferenceAsync(variant.Id, "OEM", "12345-25648");

        // Act - search with different formatting
        var results1 = await searchService.SearchVariantsAsync("1234525648", includeEquivalents: false);
        var results2 = await searchService.SearchVariantsAsync("12345 25648", includeEquivalents: false);
        var results3 = await searchService.SearchVariantsAsync("12345/25648", includeEquivalents: false);

        // Assert - all formats should find the same variant
        Assert.Single(results1);
        Assert.Single(results2);
        Assert.Single(results3);
        Assert.Equal(variant.Id, results1[0].VariantId);
        Assert.Equal(variant.Id, results2[0].VariantId);
        Assert.Equal(variant.Id, results3[0].VariantId);
    }

    [Fact]
    public async Task SearchWithEquivalents_FindsAllEquivalentParts()
    {
        // Arrange
        var (context, searchService, partRefService) = CreateServices();
        var product = CreateProduct(context, "Filter", "FLT-001");
        
        var variant1 = CreateVariant(context, product.Id, "FLT-001-V1", "Original");
        var variant2 = CreateVariant(context, product.Id, "FLT-001-V2", "Aftermarket");
        var variant3 = CreateVariant(context, product.Id, "FLT-001-V3", "Compatible");
        
        await context.SaveChangesAsync();

        // variant1 has OEM1
        await partRefService.CreateReferenceAsync(variant1.Id, "OEM", "OEM123");
        
        // variant2 has OEM1 and OEM2 (equivalent to variant1)
        await partRefService.CreateReferenceAsync(variant2.Id, "OEM", "OEM123");
        await partRefService.CreateReferenceAsync(variant2.Id, "OEM", "OEM456");
        
        // variant3 has OEM2 (transitive equivalent to variant1 via variant2)
        await partRefService.CreateReferenceAsync(variant3.Id, "OEM", "OEM456");

        // Act
        var results = await searchService.SearchVariantsAsync("OEM123", includeEquivalents: true);

        // Assert - all 3 should be found
        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.VariantId == variant1.Id); // Found directly (has OEM123)
        Assert.Contains(results, r => r.VariantId == variant2.Id); // Found directly AND equivalent (has OEM123 and OEM456)
        Assert.Contains(results, r => r.VariantId == variant3.Id); // Found as equivalent (has OEM456, shares with variant2)
    }

    [Fact]
    public async Task SearchWithEquivalents_Disabled_ReturnsOnlyDirectMatch()
    {
        // Arrange
        var (context, searchService, partRefService) = CreateServices();
        var product = CreateProduct(context, "Part", "P-001");
        var variant1 = CreateVariant(context, product.Id, "P-001-V1", "V1");
        var variant2 = CreateVariant(context, product.Id, "P-001-V2", "V2");
        await context.SaveChangesAsync();

        // variant1 has OEM1 and OEM2
        await partRefService.CreateReferenceAsync(variant1.Id, "OEM", "OEM111");
        await partRefService.CreateReferenceAsync(variant1.Id, "OEM", "SHARED123");
        
        // variant2 has only OEM2 (shares SHARED123 with variant1, making them equivalents)
        await partRefService.CreateReferenceAsync(variant2.Id, "OEM", "SHARED123");

        // Act - search for OEM1 which only variant1 has directly
        var results = await searchService.SearchVariantsAsync("OEM111", includeEquivalents: false);

        // Assert - only variant1 (direct match), NOT variant2 (would be equivalent via SHARED123)
        Assert.Single(results);
        Assert.Equal(variant1.Id, results[0].VariantId);
        Assert.Equal("DIRECT", results[0].MatchType);
    }

    [Fact]
    public async Task TenantIsolation_DoesNotReturnOtherTenantVariants()
    {
        // Arrange - Create TWO separate tenant contexts/services
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        
        // Tenant 1's context and data
        var (context1, tenantContext1) = _dbFactory.CreateContext(tenant1Id, Guid.NewGuid());
        _contexts.Add(context1);
        var service1 = new VariantSearchService(context1, tenantContext1);
        
        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenant1Id,
            Name = "Tenant1 Product",
            Code = "T1-001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };
        var variant1 = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = tenant1Id,
            ProductId = product1.Id,
            Sku = "T1-001-V1",
            Name = "Tenant1 Variant",
            Unit = "EA",
            VatRate = 0.18m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };
        context1.Products.Add(product1);
        context1.ProductVariants.Add(variant1);
        await context1.SaveChangesAsync();
        
        // Tenant 2's context and data
        var (context2, tenantContext2) = _dbFactory.CreateContext(tenant2Id, Guid.NewGuid());
        _contexts.Add(context2);
        var service2 = new VariantSearchService(context2, tenantContext2);
        
        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenant2Id,
            Name = "Tenant2 Product",
            Code = "T2-001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };
        var variant2 = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = tenant2Id,
            ProductId = product2.Id,
            Sku = "T2-001-V1",
            Name = "Tenant2 Variant",
            Unit = "EA",
            VatRate = 0.18m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty
        };
        context2.Products.Add(product2);
        context2.ProductVariants.Add(variant2);
        await context2.SaveChangesAsync();

        // Act - Each tenant searches for "Product"
        var results1 = await service1.SearchVariantsAsync("Product", includeEquivalents: false);
        var results2 = await service2.SearchVariantsAsync("Product", includeEquivalents: false);

        // Assert - Each tenant only sees their own variant
        Assert.Single(results1);
        Assert.Equal(variant1.Id, results1[0].VariantId);
        
        Assert.Single(results2);
        Assert.Equal(variant2.Id, results2[0].VariantId);
    }

    // SPRINT-3.2-B: Vehicle Fitment Filtering Tests

    [Fact]
    public async Task SearchWithEngineId_ReturnsOnlyCompatibleVariants()
    {
        // Arrange
        var (context, searchService, _) = CreateServices();
        var engineId = Guid.NewGuid();
        var product = CreateProduct(context, "Brake Pad", "BP-001");
        
        var compatibleVariant = CreateVariant(context, product.Id, "BP-001-V1", "Compatible Variant");
        var incompatibleVariant = CreateVariant(context, product.Id, "BP-001-V2", "Incompatible Variant");
        var noFitmentVariant = CreateVariant(context, product.Id, "BP-001-V3", "No Fitment");
        
        // Create vehicle engines for fitment
        var targetEngine = CreateVehicleEngine(context, engineId);
        var otherEngine = CreateVehicleEngine(context);
        
        await context.SaveChangesAsync();

        // Add fitment for compatible variant
        context.Set<StockCardFitment>().Add(new StockCardFitment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = compatibleVariant.Id,
            VehicleEngineId = targetEngine.Id,
            Notes = "Test fitment"
        });

        // Add fitment for incompatible variant (different engine)
        context.Set<StockCardFitment>().Add(new StockCardFitment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = incompatibleVariant.Id,
            VehicleEngineId = otherEngine.Id,
            Notes = "Other engine"
        });
        await context.SaveChangesAsync();

        // Act
        var results = await searchService.SearchVariantsAsync(
            "Brake",
            engineId: engineId,
            includeEquivalents: false,
            includeUndefinedFitment: false);

        // Assert - only compatible variant
        Assert.Single(results);
        Assert.Equal(compatibleVariant.Id, results[0].VariantId);
        Assert.True(results[0].IsCompatible);
        Assert.True(results[0].HasFitment);
    }

    [Fact]
    public async Task SearchWithEngineId_IncludeUndefined_ReturnsAllWithCompatibleFirst()
    {
        // Arrange
        var (context, searchService, _) = CreateServices();
        var engineId = Guid.NewGuid();
        var warehouse = CreateWarehouse(context);
        var warehouseId = warehouse.Id;
        var product = CreateProduct(context, "Brake Pad", "BP-001");
        
        var compatibleInStock = CreateVariant(context, product.Id, "BP-001-V1", "Compatible In Stock");
        var compatibleOutOfStock = CreateVariant(context, product.Id, "BP-001-V2", "Compatible Out Stock");
        var undefinedVariant = CreateVariant(context, product.Id, "BP-001-V3", "Undefined Fitment");
        
        // Create vehicle engine for fitment
        var targetEngine = CreateVehicleEngine(context, engineId);
        
        await context.SaveChangesAsync();

        // Fitment for compatible variants
        context.Set<StockCardFitment>().AddRange(
            new StockCardFitment
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                VariantId = compatibleInStock.Id,
                VehicleEngineId = targetEngine.Id
            },
            new StockCardFitment
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                VariantId = compatibleOutOfStock.Id,
                VehicleEngineId = targetEngine.Id
            }
        );

        // Stock for in-stock variant
        context.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = compatibleInStock.Id,
            WarehouseId = warehouseId,
            OnHand = 10,
            Reserved = 0
        });
        await context.SaveChangesAsync();

        // Act
        var results = await searchService.SearchVariantsAsync(
            "Brake",
            warehouseId: warehouseId,
            engineId: engineId,
            includeEquivalents: false,
            includeUndefinedFitment: true);

        // Assert - all 3 returned
        Assert.Equal(3, results.Count);
        
        // Compatible in stock should be first (priority 1)
        Assert.Equal(compatibleInStock.Id, results[0].VariantId);
        Assert.Equal(1, results[0].FitmentPriority);
        Assert.True(results[0].IsCompatible);
        
        // Compatible out of stock should be second (priority 3)
        Assert.Equal(compatibleOutOfStock.Id, results[1].VariantId);
        Assert.Equal(3, results[1].FitmentPriority);
        Assert.True(results[1].IsCompatible);
        
        // Undefined should be last (priority 4)
        Assert.Equal(undefinedVariant.Id, results[2].VariantId);
        Assert.Equal(4, results[2].FitmentPriority);
        Assert.False(results[2].IsCompatible);
        Assert.False(results[2].HasFitment);
    }

    [Fact]
    public async Task SearchWithEquivalents_RespectsEngineFilter()
    {
        // Arrange
        var (context, searchService, partRefService) = CreateServices();
        var engineId = Guid.NewGuid();
        var product = CreateProduct(context, "Part", "P-001");
        
        var directMatch = CreateVariant(context, product.Id, "P-001-V1", "Direct");
        var compatibleEquiv = CreateVariant(context, product.Id, "P-001-V2", "Equiv Compatible");
        var incompatibleEquiv = CreateVariant(context, product.Id, "P-001-V3", "Equiv Incompatible");
        
        // Create vehicle engines for fitment
        var targetEngine = CreateVehicleEngine(context, engineId);
        var otherEngine = CreateVehicleEngine(context);
        
        await context.SaveChangesAsync();

        // OEM relationships
        await partRefService.CreateReferenceAsync(directMatch.Id, "OEM", "SHARED123");
        await partRefService.CreateReferenceAsync(compatibleEquiv.Id, "OEM", "SHARED123");
        await partRefService.CreateReferenceAsync(incompatibleEquiv.Id, "OEM", "SHARED123");

        // Fitment
        context.Set<StockCardFitment>().AddRange(
            new StockCardFitment
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                VariantId = directMatch.Id,
                VehicleEngineId = targetEngine.Id
            },
            new StockCardFitment
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                VariantId = compatibleEquiv.Id,
                VehicleEngineId = targetEngine.Id
            },
            new StockCardFitment
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                VariantId = incompatibleEquiv.Id,
                VehicleEngineId = otherEngine.Id
            }
        );
        await context.SaveChangesAsync();

        // Act
        var results = await searchService.SearchVariantsAsync(
            "SHARED123",
            engineId: engineId,
            includeEquivalents: true,
            includeUndefinedFitment: false);

        // Assert - only compatible variants (direct + equivalent)
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.VariantId == directMatch.Id && r.IsCompatible);
        Assert.Contains(results, r => r.VariantId == compatibleEquiv.Id && r.IsCompatible);
        Assert.DoesNotContain(results, r => r.VariantId == incompatibleEquiv.Id);
    }

    [Fact]
    public async Task FitmentPriority_DirectMatchInStock_GetsPriority1()
    {
        // Arrange
        var (context, searchService, _) = CreateServices();
        var engineId = Guid.NewGuid();
        var warehouse = CreateWarehouse(context);
        var warehouseId = warehouse.Id;
        var product = CreateProduct(context, "Part", "P-001");
        
        var variant = CreateVariant(context, product.Id, "P-001-V1", "Direct Match In Stock");
        
        // Create vehicle engine for fitment
        var targetEngine = CreateVehicleEngine(context, engineId);
        
        await context.SaveChangesAsync();

        context.Set<StockCardFitment>().Add(new StockCardFitment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = variant.Id,
            VehicleEngineId = targetEngine.Id
        });

        context.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = variant.Id,
            WarehouseId = warehouseId,
            OnHand = 5,
            Reserved = 0
        });
        await context.SaveChangesAsync();

        // Act
        var results = await searchService.SearchVariantsAsync(
            "Part",
            warehouseId: warehouseId,
            engineId: engineId,
            includeEquivalents: false);

        // Assert
        Assert.Single(results);
        Assert.Equal(variant.Id, results[0].VariantId);
        Assert.Equal(1, results[0].FitmentPriority); // Compatible + in stock + direct match
        Assert.Equal("DIRECT", results[0].MatchType);
    }

    [Fact]
    public async Task FitmentPriority_EquivalentMatchInStock_GetsPriority2()
    {
        // Arrange
        var (context, searchService, partRefService) = CreateServices();
        var engineId = Guid.NewGuid();
        var warehouse = CreateWarehouse(context);
        var warehouseId = warehouse.Id;
        var product = CreateProduct(context, "Part", "P-001");
        
        var direct = CreateVariant(context, product.Id, "P-001-V1", "Direct");
        var equiv = CreateVariant(context, product.Id, "P-001-V2", "Equivalent");
        
        // Create vehicle engine for fitment
        var targetEngine = CreateVehicleEngine(context, engineId);
        
        await context.SaveChangesAsync();

        // direct has OEM1 only
        await partRefService.CreateReferenceAsync(direct.Id, "OEM", "OEM111");
        
        // equiv has OEM1 AND OEM2 (sharing OEM1 makes it equivalent to direct)
        await partRefService.CreateReferenceAsync(equiv.Id, "OEM", "OEM111");
        await partRefService.CreateReferenceAsync(equiv.Id, "OEM", "OEM222");

        context.Set<StockCardFitment>().AddRange(
            new StockCardFitment
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                VariantId = direct.Id,
                VehicleEngineId = targetEngine.Id
            },
            new StockCardFitment
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                VariantId = equiv.Id,
                VehicleEngineId = targetEngine.Id
            }
        );

        // Only equiv has stock
        context.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = equiv.Id,
            WarehouseId = warehouseId,
            OnHand = 3,
            Reserved = 0
        });
        await context.SaveChangesAsync();

        // Act - Search for OEM2 which only equiv has directly
        var results = await searchService.SearchVariantsAsync(
            "OEM222",
            warehouseId: warehouseId,
            engineId: engineId,
            includeEquivalents: true);

        // Assert - direct should be found as equivalent (via OEM1 link)
        // equiv should be found as direct match for OEM2
        Assert.Equal(2, results.Count);
        
        // equiv is direct match for OEM222 AND has stock -> priority 1
        var equivResult = results.First(r => r.VariantId == equiv.Id);
        Assert.Equal(1, equivResult.FitmentPriority);
        
        // direct is equivalent match (via OEM1) with no stock -> priority 3
        var directResult = results.First(r => r.VariantId == direct.Id);
        Assert.Equal(3, directResult.FitmentPriority);
    }

    [Fact]
    public async Task FitmentPriority_CompatibleOutOfStock_GetsPriority3()
    {
        // Arrange
        var (context, searchService, _) = CreateServices();
        var engineId = Guid.NewGuid();
        var warehouse = CreateWarehouse(context);
        var warehouseId = warehouse.Id;
        var product = CreateProduct(context, "Compatible Part", "P-001");
        
        var variant = CreateVariant(context, product.Id, "P-001-V1", "Out of Stock");
        
        // Create vehicle engine for fitment
        var targetEngine = CreateVehicleEngine(context, engineId);
        
        await context.SaveChangesAsync();

        context.Set<StockCardFitment>().Add(new StockCardFitment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = variant.Id,
            VehicleEngineId = targetEngine.Id
        });

        context.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = variant.Id,
            WarehouseId = warehouseId,
            OnHand = 0,
            Reserved = 0
        });
        await context.SaveChangesAsync();

        // Act - Search by product name (not SKU)
        var results = await searchService.SearchVariantsAsync(
            "Compatible",
            warehouseId: warehouseId,
            engineId: engineId,
            includeEquivalents: false);

        // Assert
        Assert.Single(results);
        Assert.Equal(variant.Id, results[0].VariantId);
        Assert.Equal(3, results[0].FitmentPriority); // Compatible + out of stock
    }

    [Fact]
    public async Task SearchWithNoEngineId_ReturnsAllVariants()
    {
        // Arrange
        var (context, searchService, _) = CreateServices();
        var engineId = Guid.NewGuid();
        var product = CreateProduct(context, "Part", "P-001");
        
        var withFitment = CreateVariant(context, product.Id, "P-001-V1", "With Fitment");
        var noFitment = CreateVariant(context, product.Id, "P-001-V2", "No Fitment");
        
        // Create vehicle engine for fitment
        var targetEngine = CreateVehicleEngine(context, engineId);
        
        await context.SaveChangesAsync();

        context.Set<StockCardFitment>().Add(new StockCardFitment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = withFitment.Id,
            VehicleEngineId = targetEngine.Id
        });
        await context.SaveChangesAsync();

        // Act - NO engineId provided
        var results = await searchService.SearchVariantsAsync(
            "Part",
            includeEquivalents: false);

        // Assert - both returned (no fitment filtering)
        Assert.Equal(2, results.Count);
    }

    public void Dispose()
    {
        foreach (var context in _contexts)
        {
            context?.Dispose();
        }
        _contexts.Clear();
    }
}

