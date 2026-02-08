using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Services;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ErpCloud.Api.Tests;

/// <summary>
/// Tests for pricing & discount engine functionality
/// </summary>
public class PricingModuleTests
{
    private static ErpDbContext CreateTestDbContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
        mockTenantContext.Setup(x => x.UserId).Returns(Guid.NewGuid());
        mockTenantContext.Setup(x => x.IsBypassEnabled).Returns(true);

        return new ErpDbContext(options, mockTenantContext.Object);
    }

    private async Task<(Product product, ProductVariant variant, Party party)> CreateTestDataAsync(
        ErpDbContext dbContext, 
        Guid tenantId,
        decimal listPrice = 100.00m,
        decimal? cost = null)
    {
        // Create product and variant
        var product = new Product
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = "PROD001",
            Name = "Test Product",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        dbContext.Products.Add(product);

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = product.Id,
            Sku = "SKU001",
            Name = "Test Variant",
            Unit = "EA",
            VatRate = 20.00m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        dbContext.ProductVariants.Add(variant);

        // Create default price list
        var priceList = new PriceList
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = "DEFAULT",
            Name = "Default Price List",
            Currency = "TRY",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        dbContext.PriceLists.Add(priceList);

        // Add price list item
        var priceListItem = new PriceListItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PriceListId = priceList.Id,
            VariantId = variant.Id,
            UnitPrice = listPrice,
            MinQty = 1,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        dbContext.PriceListItems.Add(priceListItem);

        // Create party (customer)
        var party = new Party
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = "CUST001",
            Name = "Test Customer",
            Type = "CUSTOMER",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        dbContext.Parties.Add(party);

        // Create product cost if specified
        if (cost.HasValue)
        {
            var productCost = new ProductCost
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                VariantId = variant.Id,
                LastPurchaseCost = cost.Value,
                Currency = "TRY",
                LastUpdated = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid()
            };
            dbContext.ProductCosts.Add(productCost);
        }

        await dbContext.SaveChangesAsync();

        return (product, variant, party);
    }

    /// <summary>
    /// Helper to create a Brand entity for testing
    /// </summary>
    private async Task<Brand> CreateBrandAsync(
        ErpDbContext dbContext,
        Guid tenantId,
        string code,
        string name)
    {
        var brand = new Brand
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code.ToUpper(),
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        dbContext.Brands.Add(brand);
        await dbContext.SaveChangesAsync();
        return brand;
    }

    [Fact]
    public async Task NoRule_UsesListPrice()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var (product, variant, party) = await CreateTestDataAsync(dbContext, tenantId, listPrice: 150.00m);

        var pricingService = new PricingService(dbContext, mockTenantContext.Object);

        // Act
        var result = await pricingService.CalculateAsync(new PricingCalculationRequest
        {
            PartyId = party.Id,
            VariantId = variant.Id,
            Quantity = 2,
            Currency = "TRY"
        });

        // Assert
        Assert.Equal(150.00m, result.ListPrice);
        Assert.Equal(150.00m, result.NetPrice);
        Assert.Null(result.DiscountPercent);
        Assert.Equal(300.00m, result.LineTotal);
        Assert.Null(result.AppliedRuleId);
    }

    [Fact]
    public async Task CustomerFixedPrice_OverridesListPrice()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var (product, variant, party) = await CreateTestDataAsync(dbContext, tenantId, listPrice: 150.00m);

        // Create customer-specific fixed price rule
        var priceRule = new PriceRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Scope = "CUSTOMER",
            RuleType = "FIXED_PRICE",
            TargetId = party.Id,
            VariantId = variant.Id,
            Currency = "TRY",
            Value = 120.00m, // Fixed price for this customer
            Priority = 100,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(30),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        dbContext.PriceRules.Add(priceRule);
        await dbContext.SaveChangesAsync();

        var pricingService = new PricingService(dbContext, mockTenantContext.Object);

        // Act
        var result = await pricingService.CalculateAsync(new PricingCalculationRequest
        {
            PartyId = party.Id,
            VariantId = variant.Id,
            Quantity = 3,
            Currency = "TRY"
        });

        // Assert
        Assert.Equal(150.00m, result.ListPrice);
        Assert.Equal(120.00m, result.NetPrice);
        Assert.Equal(30.00m, result.DiscountAmount);
        Assert.Equal(20.00m, result.DiscountPercent);
        Assert.Equal(360.00m, result.LineTotal); // 120 * 3
        Assert.Equal(priceRule.Id, result.AppliedRuleId);
        Assert.Equal("CUSTOMER", result.AppliedRuleScope);
        Assert.Equal("FIXED_PRICE", result.AppliedRuleType);
    }

    [Fact]
    public async Task CustomerDiscountPercent_AppliesCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var (product, variant, party) = await CreateTestDataAsync(dbContext, tenantId, listPrice: 200.00m);

        // Create 15% discount rule
        var priceRule = new PriceRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Scope = "CUSTOMER",
            RuleType = "DISCOUNT_PERCENT",
            TargetId = party.Id,
            VariantId = variant.Id,
            Currency = "TRY",
            Value = 15.00m, // 15% discount
            Priority = 100,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        dbContext.PriceRules.Add(priceRule);
        await dbContext.SaveChangesAsync();

        var pricingService = new PricingService(dbContext, mockTenantContext.Object);

        // Act
        var result = await pricingService.CalculateAsync(new PricingCalculationRequest
        {
            PartyId = party.Id,
            VariantId = variant.Id,
            Quantity = 2,
            Currency = "TRY"
        });

        // Assert
        Assert.Equal(200.00m, result.ListPrice);
        Assert.Equal(15.00m, result.DiscountPercent);
        Assert.Equal(30.00m, result.DiscountAmount); // 200 * 0.15
        Assert.Equal(170.00m, result.NetPrice); // 200 - 30
        Assert.Equal(340.00m, result.LineTotal); // 170 * 2
    }

    [Fact]
    public async Task ProfitCalculation_Positive()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var (product, variant, party) = await CreateTestDataAsync(
            dbContext, 
            tenantId, 
            listPrice: 150.00m, 
            cost: 100.00m);

        var pricingService = new PricingService(dbContext, mockTenantContext.Object);

        // Act
        var result = await pricingService.CalculateAsync(new PricingCalculationRequest
        {
            PartyId = party.Id,
            VariantId = variant.Id,
            Quantity = 1,
            Currency = "TRY"
        });

        // Assert
        Assert.Equal(100.00m, result.UnitCost);
        Assert.Equal(50.00m, result.Profit); // 150 - 100
        Assert.NotNull(result.ProfitPercent);
        Assert.True(Math.Abs(result.ProfitPercent.Value - 33.33m) < 0.1m); // Approximately 33.33%
        Assert.False(result.HasWarning);
    }

    [Fact]
    public async Task ProfitCalculation_Negative_TriggersWarning()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var (product, variant, party) = await CreateTestDataAsync(
            dbContext, 
            tenantId, 
            listPrice: 100.00m, 
            cost: 120.00m); // Cost higher than list price

        var pricingService = new PricingService(dbContext, mockTenantContext.Object);

        // Act
        var result = await pricingService.CalculateAsync(new PricingCalculationRequest
        {
            PartyId = party.Id,
            VariantId = variant.Id,
            Quantity = 1,
            Currency = "TRY"
        });

        // Assert
        Assert.Equal(120.00m, result.UnitCost);
        Assert.Equal(-20.00m, result.Profit); // Loss
        Assert.True(result.ProfitPercent < 0);
        Assert.True(result.HasWarning);
        Assert.Contains("UYARI", result.WarningMessage);
        Assert.Contains("Zarar", result.WarningMessage);
    }

    [Fact]
    public async Task ValidFromValidTo_Respected()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var (product, variant, party) = await CreateTestDataAsync(dbContext, tenantId, listPrice: 150.00m);

        // Create expired rule
        var expiredRule = new PriceRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Scope = "CUSTOMER",
            RuleType = "FIXED_PRICE",
            TargetId = party.Id,
            VariantId = variant.Id,
            Currency = "TRY",
            Value = 50.00m,
            Priority = 100,
            ValidFrom = DateTime.UtcNow.AddDays(-30),
            ValidTo = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            CreatedBy = Guid.NewGuid()
        };
        dbContext.PriceRules.Add(expiredRule);
        await dbContext.SaveChangesAsync();

        var pricingService = new PricingService(dbContext, mockTenantContext.Object);

        // Act
        var result = await pricingService.CalculateAsync(new PricingCalculationRequest
        {
            PartyId = party.Id,
            VariantId = variant.Id,
            Quantity = 1,
            Currency = "TRY"
        });

        // Assert: Should fallback to list price since rule is expired
        Assert.Equal(150.00m, result.NetPrice);
        Assert.Null(result.AppliedRuleId);
    }

    [Fact]
    public async Task CurrencyMismatch_UsesCorrectCurrency()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var (product, variant, party) = await CreateTestDataAsync(dbContext, tenantId);

        // Create USD rule
        var usdRule = new PriceRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Scope = "CUSTOMER",
            RuleType = "FIXED_PRICE",
            TargetId = party.Id,
            VariantId = variant.Id,
            Currency = "USD",
            Value = 10.00m,
            Priority = 100,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        dbContext.PriceRules.Add(usdRule);
        await dbContext.SaveChangesAsync();

        var pricingService = new PricingService(dbContext, mockTenantContext.Object);

        // Act: Request in TRY
        var result = await pricingService.CalculateAsync(new PricingCalculationRequest
        {
            PartyId = party.Id,
            VariantId = variant.Id,
            Quantity = 1,
            Currency = "TRY"
        });

        // Assert: Should not apply USD rule when requesting TRY
        Assert.Null(result.AppliedRuleId);
    }

    [Fact]
    public async Task BatchCalculation_ProcessesMultipleItems()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var (product, variant, party) = await CreateTestDataAsync(dbContext, tenantId);

        var pricingService = new PricingService(dbContext, mockTenantContext.Object);

        var requests = new List<PricingCalculationRequest>
        {
            new() { PartyId = party.Id, VariantId = variant.Id, Quantity = 1, Currency = "TRY" },
            new() { PartyId = party.Id, VariantId = variant.Id, Quantity = 5, Currency = "TRY" },
            new() { PartyId = party.Id, VariantId = variant.Id, Quantity = 10, Currency = "TRY" }
        };

        // Act
        var results = await pricingService.CalculateBatchAsync(requests);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(100.00m, results[0].LineTotal); // 100 * 1
        Assert.Equal(500.00m, results[1].LineTotal); // 100 * 5
        Assert.Equal(1000.00m, results[2].LineTotal); // 100 * 10
    }

    // ==================== BRAND-BASED DISCOUNT TESTS ====================

    [Fact]
    public async Task CustomerBrandDiscount_Applies()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var (product, variant, party) = await CreateTestDataAsync(dbContext, tenantId);
        
        // Create Bosch brand
        var boschBrand = await CreateBrandAsync(dbContext, tenantId, "BOSCH", "Bosch");
        
        // Set product brand FK
        product.BrandId = boschBrand.Id;
        await dbContext.SaveChangesAsync();

        // Create brand-based discount rule for customer
        var brandRule = new PriceRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Scope = "CUSTOMER",
            RuleType = "DISCOUNT_PERCENT",
            TargetId = party.Id,
            VariantId = null,
            BrandId = boschBrand.Id, // Guid FK to Brand
            Currency = "TRY",
            Value = 20.00m, // 20% discount for Bosch products
            Priority = 100,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddDays(30),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        dbContext.PriceRules.Add(brandRule);
        await dbContext.SaveChangesAsync();

        var pricingService = new PricingService(dbContext, mockTenantContext.Object);

        // Act
        var result = await pricingService.CalculateAsync(new PricingCalculationRequest
        {
            PartyId = party.Id,
            VariantId = variant.Id,
            Quantity = 1,
            Currency = "TRY"
        });

        // Assert
        Assert.Equal(100.00m, result.ListPrice);
        Assert.Equal(20.00m, result.DiscountPercent);
        Assert.Equal(80.00m, result.NetPrice); // 100 - 20%
        Assert.Contains("Marka iskontosu", result.RuleDescription);
        Assert.Contains("Bosch", result.RuleDescription);
    }

    [Fact]
    public async Task CustomerVariant_Overrides_CustomerBrand()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var (product, variant, party) = await CreateTestDataAsync(dbContext, tenantId);
        
        // Create Bosch brand
        var boschBrand = await CreateBrandAsync(dbContext, tenantId, "BOSCH", "Bosch");
        product.BrandId = boschBrand.Id;
        await dbContext.SaveChangesAsync();

        // Create TWO rules: variant-specific (higher priority) and brand-based
        var variantRule = new PriceRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Scope = "CUSTOMER",
            RuleType = "DISCOUNT_PERCENT",
            TargetId = party.Id,
            VariantId = variant.Id, // Variant-specific
            BrandId = null,
            Currency = "TRY",
            Value = 15.00m, // 15% for this variant
            Priority = 100,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };

        var brandRule = new PriceRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Scope = "CUSTOMER",
            RuleType = "DISCOUNT_PERCENT",
            TargetId = party.Id,
            VariantId = null,
            BrandId = boschBrand.Id, // Brand FK
            Currency = "TRY",
            Value = 20.00m, // 20% for all Bosch
            Priority = 100,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };

        dbContext.PriceRules.AddRange(variantRule, brandRule);
        await dbContext.SaveChangesAsync();

        var pricingService = new PricingService(dbContext, mockTenantContext.Object);

        // Act
        var result = await pricingService.CalculateAsync(new PricingCalculationRequest
        {
            PartyId = party.Id,
            VariantId = variant.Id,
            Quantity = 1,
            Currency = "TRY"
        });

        // Assert - Variant rule should win over brand rule
        Assert.Equal(15.00m, result.DiscountPercent);
        Assert.Equal(85.00m, result.NetPrice); // 100 - 15%
        Assert.DoesNotContain("Marka iskontosu", result.RuleDescription ?? "");
    }

    [Fact]
    public async Task GroupBrand_Applies_WhenNoCustomerRule()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var (product, variant, party) = await CreateTestDataAsync(dbContext, tenantId);
        
        // Create NGK brand
        var ngkBrand = await CreateBrandAsync(dbContext, tenantId, "NGK", "NGK");
        product.BrandId = ngkBrand.Id;
        await dbContext.SaveChangesAsync();

        // Create customer group brand discount (no customer-specific rule)
        var groupBrandRule = new PriceRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Scope = "CUSTOMER_GROUP",
            RuleType = "DISCOUNT_PERCENT",
            TargetId = Guid.NewGuid(), // Some group ID
            VariantId = null,
            BrandId = ngkBrand.Id, // Brand FK
            Currency = "TRY",
            Value = 10.00m, // 10% for NGK products
            Priority = 50,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };

        dbContext.PriceRules.Add(groupBrandRule);
        await dbContext.SaveChangesAsync();

        var pricingService = new PricingService(dbContext, mockTenantContext.Object);

        // Act
        var result = await pricingService.CalculateAsync(new PricingCalculationRequest
        {
            PartyId = party.Id,
            VariantId = variant.Id,
            Quantity = 1,
            Currency = "TRY"
        });

        // Assert
        Assert.Equal(10.00m, result.DiscountPercent);
        Assert.Equal(90.00m, result.NetPrice); // 100 - 10%
        Assert.Contains("Marka iskontosu", result.RuleDescription);
    }

    [Fact]
    public async Task ProductGroupBrand_Fallback()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var (product, variant, party) = await CreateTestDataAsync(dbContext, tenantId);
        
        // Create Mobil brand
        var mobilBrand = await CreateBrandAsync(dbContext, tenantId, "MOBIL", "Mobil");
        product.BrandId = mobilBrand.Id;
        await dbContext.SaveChangesAsync();

        // Create product group brand discount (lowest priority)
        var productGroupBrandRule = new PriceRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Scope = "PRODUCT_GROUP",
            RuleType = "DISCOUNT_PERCENT",
            TargetId = Guid.NewGuid(), // Some product group ID
            VariantId = null,
            BrandId = mobilBrand.Id, // Brand FK
            Currency = "TRY",
            Value = 5.00m, // 5% for Mobil products
            Priority = 10,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };

        dbContext.PriceRules.Add(productGroupBrandRule);
        await dbContext.SaveChangesAsync();

        var pricingService = new PricingService(dbContext, mockTenantContext.Object);

        // Act
        var result = await pricingService.CalculateAsync(new PricingCalculationRequest
        {
            PartyId = party.Id,
            VariantId = variant.Id,
            Quantity = 1,
            Currency = "TRY"
        });

        // Assert
        Assert.Equal(5.00m, result.DiscountPercent);
        Assert.Equal(95.00m, result.NetPrice); // 100 - 5%
        Assert.Contains("Marka iskontosu", result.RuleDescription);
        Assert.Contains("Mobil", result.RuleDescription);
    }

    [Fact]
    public async Task BrandDiscount_DateValidity_Respected()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var (product, variant, party) = await CreateTestDataAsync(dbContext, tenantId);
        
        // Create Castrol brand
        var castrolBrand = await CreateBrandAsync(dbContext, tenantId, "CASTROL", "Castrol");
        product.BrandId = castrolBrand.Id;
        await dbContext.SaveChangesAsync();

        // Create EXPIRED brand discount
        var expiredRule = new PriceRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Scope = "CUSTOMER",
            RuleType = "DISCOUNT_PERCENT",
            TargetId = party.Id,
            VariantId = null,
            BrandId = castrolBrand.Id, // Brand FK
            Currency = "TRY",
            Value = 25.00m,
            Priority = 100,
            ValidFrom = DateTime.UtcNow.AddDays(-30),
            ValidTo = DateTime.UtcNow.AddDays(-1), // Expired yesterday
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };

        dbContext.PriceRules.Add(expiredRule);
        await dbContext.SaveChangesAsync();

        var pricingService = new PricingService(dbContext, mockTenantContext.Object);

        // Act
        var result = await pricingService.CalculateAsync(new PricingCalculationRequest
        {
            PartyId = party.Id,
            VariantId = variant.Id,
            Quantity = 1,
            Currency = "TRY"
        });

        // Assert - Expired rule should NOT apply
        Assert.Null(result.DiscountPercent);
        Assert.Equal(100.00m, result.NetPrice); // No discount
        Assert.Null(result.RuleDescription);
    }

    [Fact]
    public async Task BrandDiscount_CurrencyFiltering_Respected()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var (product, variant, party) = await CreateTestDataAsync(dbContext, tenantId);
        
        // Create Shell brand
        var shellBrand = await CreateBrandAsync(dbContext, tenantId, "SHELL", "Shell");
        product.BrandId = shellBrand.Id;
        await dbContext.SaveChangesAsync();

        // Create USD brand discount (but we'll request TRY)
        var usdRule = new PriceRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Scope = "CUSTOMER",
            RuleType = "DISCOUNT_PERCENT",
            TargetId = party.Id,
            VariantId = null,
            BrandId = shellBrand.Id, // Brand FK
            Currency = "USD", // Different currency
            Value = 30.00m,
            Priority = 100,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };

        dbContext.PriceRules.Add(usdRule);
        await dbContext.SaveChangesAsync();

        var pricingService = new PricingService(dbContext, mockTenantContext.Object);

        // Act - Request TRY pricing
        var result = await pricingService.CalculateAsync(new PricingCalculationRequest
        {
            PartyId = party.Id,
            VariantId = variant.Id,
            Quantity = 1,
            Currency = "TRY"
        });

        // Assert - USD rule should NOT apply to TRY request
        Assert.Null(result.DiscountPercent);
        Assert.Equal(100.00m, result.NetPrice);
        Assert.Null(result.RuleDescription);
    }

    [Fact]
    public async Task BrandDiscount_NoBrand_NoMatch()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateTestDbContext(tenantId);

        var mockTenantContext = new Mock<ITenantContext>();
        mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);

        var (product, variant, party) = await CreateTestDataAsync(dbContext, tenantId);
        
        // Create Total brand
        var totalBrand = await CreateBrandAsync(dbContext, tenantId, "TOTAL", "Total");
        
        // Product has NO brand
        product.BrandId = null;
        await dbContext.SaveChangesAsync();

        // Create brand discount for "Total"
        var brandRule = new PriceRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Scope = "CUSTOMER",
            RuleType = "DISCOUNT_PERCENT",
            TargetId = party.Id,
            VariantId = null,
            BrandId = totalBrand.Id, // Brand FK
            Currency = "TRY",
            Value = 15.00m,
            Priority = 100,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };

        dbContext.PriceRules.Add(brandRule);
        await dbContext.SaveChangesAsync();

        var pricingService = new PricingService(dbContext, mockTenantContext.Object);

        // Act
        var result = await pricingService.CalculateAsync(new PricingCalculationRequest
        {
            PartyId = party.Id,
            VariantId = variant.Id,
            Quantity = 1,
            Currency = "TRY"
        });

        // Assert - Brand rule should NOT apply when product has no brand
        Assert.Null(result.DiscountPercent);
        Assert.Equal(100.00m, result.NetPrice);
    }
}

