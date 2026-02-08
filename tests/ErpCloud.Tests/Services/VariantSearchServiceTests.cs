using ErpCloud.Api.Services;
using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpCloud.Tests.Services;

/// <summary>
/// Integration tests for OEM-based variant search functionality
/// </summary>
public class VariantSearchServiceTests : IDisposable
{
    private readonly ErpDbContext _context;
    private readonly VariantSearchService _searchService;
    private readonly PartReferenceService _partReferenceService;
    private readonly Guid _tenantId = Guid.NewGuid();

    public VariantSearchServiceTests()
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ErpDbContext(options, new TestTenantContext(_tenantId));
        _searchService = new VariantSearchService(_context, new TestTenantContext(_tenantId));
        _partReferenceService = new PartReferenceService(_context, new TestTenantContext(_tenantId));
    }

    [Fact]
    public async Task SearchByProductName_ReturnsVariant()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = "Brake Pad Front",
            Code = "BP-001",
            Status = ProductStatus.Active,
            IsActive = true
        };

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = product.Id,
            Sku = "BP-001-V1",
            Name = "Variant 1",
            Price = 49.99m,
            Currency = "TRY",
            IsActive = true
        };

        _context.Products.Add(product);
        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();

        // Act
        var results = await _searchService.SearchVariantsAsync("Brake", includeEquivalents: false);

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
        var product = CreateProduct("Test Product", "TP-001");
        var variant = CreateVariant(product.Id, "TP-001-V1", "Red Variant");
        await _context.SaveChangesAsync();

        // Act
        var results = await _searchService.SearchVariantsAsync("Red", includeEquivalents: false);

        // Assert
        Assert.Single(results);
        Assert.Equal(variant.Id, results[0].VariantId);
    }

    [Fact]
    public async Task SearchBySKU_ReturnsVariant()
    {
        // Arrange
        var product = CreateProduct("Test Product", "TP-002");
        var variant = CreateVariant(product.Id, "SKU12345", "Variant");
        await _context.SaveChangesAsync();

        // Act - search with exact SKU
        var results = await _searchService.SearchVariantsAsync("SKU12345", includeEquivalents: false);

        // Assert
        Assert.Single(results);
        Assert.Equal(variant.Id, results[0].VariantId);
        Assert.Equal("SKU", results[0].MatchedBy);
    }

    [Fact]
    public async Task SearchByBarcode_ReturnsVariant()
    {
        // Arrange
        var product = CreateProduct("Test Product", "TP-003");
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = product.Id,
            Sku = "TP-003-V1",
            Name = "Variant",
            Barcode = "1234567890123",
            Price = 10m,
            Currency = "TRY",
            IsActive = true
        };

        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();

        // Act
        var results = await _searchService.SearchVariantsAsync("1234567890123", includeEquivalents: false);

        // Assert
        Assert.Single(results);
        Assert.Equal(variant.Id, results[0].VariantId);
        Assert.Equal("BARCODE", results[0].MatchedBy);
    }

    [Fact]
    public async Task SearchByOEM_ReturnsVariant()
    {
        // Arrange
        var product = CreateProduct("Brake Pad", "BP-001");
        var variant = CreateVariant(product.Id, "BP-001-V1", "Front");
        await _context.SaveChangesAsync();

        // Add OEM reference
        await _partReferenceService.CreateReferenceAsync(variant.Id, "OEM", "ABC123");

        // Act
        var results = await _searchService.SearchVariantsAsync("ABC123", includeEquivalents: false);

        // Assert
        Assert.Single(results);
        Assert.Equal(variant.Id, results[0].VariantId);
        Assert.Equal("OEM", results[0].MatchedBy);
        Assert.Contains("ABC123", results[0].OemRefs);
    }

    [Fact]
    public async Task SearchByOEM_WithDashesAndSpaces_FindsNormalizedMatch()
    {
        // Arrange
        var product = CreateProduct("Part", "P-001");
        var variant = CreateVariant(product.Id, "P-001-V1", "Variant");
        await _context.SaveChangesAsync();

        // Add OEM with dashes
        await _partReferenceService.CreateReferenceAsync(variant.Id, "OEM", "12345-25648");

        // Act - search with different formatting
        var results1 = await _searchService.SearchVariantsAsync("1234525648", includeEquivalents: false);
        var results2 = await _searchService.SearchVariantsAsync("12345 25648", includeEquivalents: false);
        var results3 = await _searchService.SearchVariantsAsync("12345/25648", includeEquivalents: false);

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
        // Arrange - Create 3 variants sharing OEM codes
        var product = CreateProduct("Filter", "FLT-001");
        
        var variant1 = CreateVariant(product.Id, "FLT-001-V1", "Original");
        var variant2 = CreateVariant(product.Id, "FLT-001-V2", "Aftermarket");
        var variant3 = CreateVariant(product.Id, "FLT-001-V3", "Compatible");
        
        await _context.SaveChangesAsync();

        // variant1 has OEM1
        await _partReferenceService.CreateReferenceAsync(variant1.Id, "OEM", "OEM123");
        
        // variant2 has OEM1 and OEM2 (equivalent to variant1)
        await _partReferenceService.CreateReferenceAsync(variant2.Id, "OEM", "OEM123");
        await _partReferenceService.CreateReferenceAsync(variant2.Id, "OEM", "OEM456");
        
        // variant3 has OEM2 (transitive equivalent to variant1 via variant2)
        await _partReferenceService.CreateReferenceAsync(variant3.Id, "OEM", "OEM456");

        // Act
        var results = await _searchService.SearchVariantsAsync("OEM123", includeEquivalents: true);

        // Assert - all 3 should be found
        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.VariantId == variant1.Id && r.MatchType == "DIRECT");
        Assert.Contains(results, r => r.VariantId == variant2.Id && r.MatchType == "EQUIVALENT");
        Assert.Contains(results, r => r.VariantId == variant3.Id && r.MatchType == "EQUIVALENT");
    }

    [Fact]
    public async Task SearchWithEquivalents_Disabled_ReturnsOnlyDirectMatch()
    {
        // Arrange
        var product = CreateProduct("Part", "P-001");
        var variant1 = CreateVariant(product.Id, "P-001-V1", "V1");
        var variant2 = CreateVariant(product.Id, "P-001-V2", "V2");
        await _context.SaveChangesAsync();

        await _partReferenceService.CreateReferenceAsync(variant1.Id, "OEM", "SHARED123");
        await _partReferenceService.CreateReferenceAsync(variant2.Id, "OEM", "SHARED123");

        // Act
        var results = await _searchService.SearchVariantsAsync("SHARED123", includeEquivalents: false);

        // Assert - only direct match
        Assert.Single(results);
        Assert.Equal("DIRECT", results[0].MatchType);
    }

    [Fact]
    public async Task TenantIsolation_DoesNotReturnOtherTenantVariants()
    {
        // Arrange
        var otherTenantId = Guid.NewGuid();
        
        // Our tenant's product
        var product1 = CreateProduct("Our Product", "OP-001");
        var variant1 = CreateVariant(product1.Id, "OP-001-V1", "Ours");
        
        // Other tenant's product
        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = otherTenantId,
            Name = "Their Product",
            Code = "TP-001",
            Status = ProductStatus.Active,
            IsActive = true
        };
        var variant2 = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = otherTenantId,
            ProductId = product2.Id,
            Sku = "TP-001-V1",
            Name = "Theirs",
            Price = 10m,
            Currency = "TRY",
            IsActive = true
        };

        _context.Products.AddRange(product1, product2);
        _context.ProductVariants.AddRange(variant1, variant2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _searchService.SearchVariantsAsync("Product", includeEquivalents: false);

        // Assert - only our tenant's variant
        Assert.Single(results);
        Assert.Equal(variant1.Id, results[0].VariantId);
    }

    private Product CreateProduct(string name, string code)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Name = name,
            Code = code,
            Status = ProductStatus.Active,
            IsActive = true
        };
        _context.Products.Add(product);
        return product;
    }

    private ProductVariant CreateVariant(Guid productId, string sku, string name)
    {
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            ProductId = productId,
            Sku = sku,
            Name = name,
            Price = 10m,
            Currency = "TRY",
            IsActive = true
        };
        _context.ProductVariants.Add(variant);
        return variant;
    }

    // SPRINT-3.2-B: Vehicle Fitment Filtering Tests

    [Fact]
    public async Task SearchWithEngineId_ReturnsOnlyCompatibleVariants()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var product = CreateProduct("Brake Pad", "BP-001");
        
        var compatibleVariant = CreateVariant(product.Id, "BP-001-V1", "Compatible Variant");
        var incompatibleVariant = CreateVariant(product.Id, "BP-001-V2", "Incompatible Variant");
        var noFitmentVariant = CreateVariant(product.Id, "BP-001-V3", "No Fitment");
        
        await _context.SaveChangesAsync();

        // Add fitment for compatible variant
        var fitment = new StockCardFitment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = compatibleVariant.Id,
            VehicleEngineId = engineId,
            Notes = "Test fitment"
        };
        _context.Set<StockCardFitment>().Add(fitment);

        // Add fitment for incompatible variant (different engine)
        var fitment2 = new StockCardFitment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = incompatibleVariant.Id,
            VehicleEngineId = Guid.NewGuid(), // Different engine
            Notes = "Other engine"
        };
        _context.Set<StockCardFitment>().Add(fitment2);
        await _context.SaveChangesAsync();

        // Act
        var results = await _searchService.SearchVariantsAsync(
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
        var engineId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var product = CreateProduct("Brake Pad", "BP-001");
        
        var compatibleInStock = CreateVariant(product.Id, "BP-001-V1", "Compatible In Stock");
        var compatibleOutOfStock = CreateVariant(product.Id, "BP-001-V2", "Compatible Out Stock");
        var undefinedVariant = CreateVariant(product.Id, "BP-001-V3", "Undefined Fitment");
        
        await _context.SaveChangesAsync();

        // Fitment for compatible variants
        _context.Set<StockCardFitment>().Add(new StockCardFitment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = compatibleInStock.Id,
            VehicleEngineId = engineId
        });
        _context.Set<StockCardFitment>().Add(new StockCardFitment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = compatibleOutOfStock.Id,
            VehicleEngineId = engineId
        });

        // Stock for in-stock variant
        _context.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = compatibleInStock.Id,
            WarehouseId = warehouseId,
            OnHand = 10,
            Reserved = 0,
            Available = 10
        });
        await _context.SaveChangesAsync();

        // Act
        var results = await _searchService.SearchVariantsAsync(
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
        var engineId = Guid.NewGuid();
        var product = CreateProduct("Part", "P-001");
        
        var directMatch = CreateVariant(product.Id, "P-001-V1", "Direct");
        var compatibleEquiv = CreateVariant(product.Id, "P-001-V2", "Equiv Compatible");
        var incompatibleEquiv = CreateVariant(product.Id, "P-001-V3", "Equiv Incompatible");
        
        await _context.SaveChangesAsync();

        // OEM relationships
        await _partReferenceService.CreateReferenceAsync(directMatch.Id, "OEM", "SHARED123");
        await _partReferenceService.CreateReferenceAsync(compatibleEquiv.Id, "OEM", "SHARED123");
        await _partReferenceService.CreateReferenceAsync(incompatibleEquiv.Id, "OEM", "SHARED123");

        // Fitment
        _context.Set<StockCardFitment>().AddRange(
            new StockCardFitment
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                VariantId = directMatch.Id,
                VehicleEngineId = engineId
            },
            new StockCardFitment
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                VariantId = compatibleEquiv.Id,
                VehicleEngineId = engineId
            },
            new StockCardFitment
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                VariantId = incompatibleEquiv.Id,
                VehicleEngineId = Guid.NewGuid() // Different engine
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var results = await _searchService.SearchVariantsAsync(
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
        var engineId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var product = CreateProduct("Part", "P-001");
        
        var variant = CreateVariant(product.Id, "P-001-V1", "Direct Match In Stock");
        await _context.SaveChangesAsync();

        _context.Set<StockCardFitment>().Add(new StockCardFitment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = variant.Id,
            VehicleEngineId = engineId
        });

        _context.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = variant.Id,
            WarehouseId = warehouseId,
            OnHand = 5,
            Reserved = 0,
            Available = 5
        });
        await _context.SaveChangesAsync();

        // Act
        var results = await _searchService.SearchVariantsAsync(
            variant.Sku,
            warehouseId: warehouseId,
            engineId: engineId,
            includeEquivalents: false);

        // Assert
        Assert.Single(results);
        Assert.Equal(1, results[0].FitmentPriority); // Compatible + InStock + Direct
        Assert.Equal("DIRECT", results[0].MatchType);
    }

    [Fact]
    public async Task FitmentPriority_EquivalentMatchInStock_GetsPriority2()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var product = CreateProduct("Part", "P-001");
        
        var direct = CreateVariant(product.Id, "P-001-V1", "Direct");
        var equiv = CreateVariant(product.Id, "P-001-V2", "Equivalent");
        await _context.SaveChangesAsync();

        await _partReferenceService.CreateReferenceAsync(direct.Id, "OEM", "OEM123");
        await _partReferenceService.CreateReferenceAsync(equiv.Id, "OEM", "OEM123");

        _context.Set<StockCardFitment>().AddRange(
            new StockCardFitment
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                VariantId = direct.Id,
                VehicleEngineId = engineId
            },
            new StockCardFitment
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                VariantId = equiv.Id,
                VehicleEngineId = engineId
            }
        );

        _context.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = equiv.Id,
            WarehouseId = warehouseId,
            OnHand = 3,
            Reserved = 0,
            Available = 3
        });
        await _context.SaveChangesAsync();

        // Act - search by OEM to find both
        var results = await _searchService.SearchVariantsAsync(
            "OEM123",
            warehouseId: warehouseId,
            engineId: engineId,
            includeEquivalents: true);

        // Assert
        var equivResult = results.First(r => r.VariantId == equiv.Id);
        Assert.Equal(2, equivResult.FitmentPriority); // Compatible + InStock + Equivalent
        Assert.Equal("EQUIVALENT", equivResult.MatchType);
    }

    [Fact]
    public async Task FitmentPriority_CompatibleOutOfStock_GetsPriority3()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var product = CreateProduct("Part", "P-001");
        
        var variant = CreateVariant(product.Id, "P-001-V1", "Out of Stock");
        await _context.SaveChangesAsync();

        _context.Set<StockCardFitment>().Add(new StockCardFitment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = variant.Id,
            VehicleEngineId = engineId
        });

        // No stock or zero stock
        _context.StockBalances.Add(new StockBalance
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = variant.Id,
            WarehouseId = warehouseId,
            OnHand = 0,
            Reserved = 0,
            Available = 0
        });
        await _context.SaveChangesAsync();

        // Act
        var results = await _searchService.SearchVariantsAsync(
            variant.Sku,
            warehouseId: warehouseId,
            engineId: engineId,
            includeEquivalents: false);

        // Assert
        Assert.Single(results);
        Assert.Equal(3, results[0].FitmentPriority); // Compatible + OutOfStock
    }

    [Fact]
    public async Task SearchWithNoEngineId_ReturnsAllVariants()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var product = CreateProduct("Part", "P-001");
        
        var withFitment = CreateVariant(product.Id, "P-001-V1", "With Fitment");
        var noFitment = CreateVariant(product.Id, "P-001-V2", "No Fitment");
        await _context.SaveChangesAsync();

        _context.Set<StockCardFitment>().Add(new StockCardFitment
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            VariantId = withFitment.Id,
            VehicleEngineId = engineId
        });
        await _context.SaveChangesAsync();

        // Act - NO engineId provided
        var results = await _searchService.SearchVariantsAsync(
            "Part",
            includeEquivalents: false);

        // Assert - both returned (no fitment filtering)
        Assert.Equal(2, results.Count);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    // Test tenant context implementation
    private class TestTenantContext : ITenantContext
    {
        private readonly Guid _tenantId;

        public TestTenantContext(Guid tenantId)
        {
            _tenantId = tenantId;
        }

        public Guid TenantId => _tenantId;
    }
}
