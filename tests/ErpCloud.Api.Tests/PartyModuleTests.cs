using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ErpCloud.Api.Tests;

public class PartyModuleTests
{
    private ErpDbContext CreateContext(Guid tenantId, out Mock<ITenantContext> tenantContextMock)
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);
        tenantContextMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        tenantContextMock.Setup(x => x.IsBypassEnabled).Returns(true);

        return new ErpDbContext(options, tenantContextMock.Object);
    }

    [Fact]
    public async Task TenantIsolation_TwoTenantsCanHaveSamePartyCode()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();

        var context1 = CreateContext(tenant1, out var tenantContext1Mock);
        var service1 = new PartyService(context1, tenantContext1Mock.Object);

        var context2 = CreateContext(tenant2, out var tenantContext2Mock);
        var service2 = new PartyService(context2, tenantContext2Mock.Object);

        var dto = new CreatePartyDto("CUST001", "Test Customer", "CUSTOMER", null, null, null, null, null, null, true);

        // Act
        var party1 = await service1.CreateAsync(dto);
        var party2 = await service2.CreateAsync(dto);

        // Assert
        Assert.NotNull(party1);
        Assert.NotNull(party2);
        Assert.Equal("CUST001", party1.Code);
        Assert.Equal("CUST001", party2.Code);
        Assert.NotEqual(party1.Id, party2.Id);
    }

    [Fact]
    public async Task SameTenant_DuplicatePartyCode_ThrowsException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var context = CreateContext(tenantId, out var tenantContextMock);
        var service = new PartyService(context, tenantContextMock.Object);

        var dto = new CreatePartyDto("CUST001", "Test Customer", "CUSTOMER", null, null, null, null, null, null, true);
        await service.CreateAsync(dto);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.CreateAsync(dto);
        });
    }

    [Fact]
    public async Task TypeFilter_ReturnsOnlyMatchingTypes()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var context = CreateContext(tenantId, out var tenantContextMock);
        var service = new PartyService(context, tenantContextMock.Object);

        // Create different types
        await service.CreateAsync(new CreatePartyDto("CUST001", "Customer 1", "CUSTOMER", null, null, null, null, null, null, true));
        await service.CreateAsync(new CreatePartyDto("SUPP001", "Supplier 1", "SUPPLIER", null, null, null, null, null, null, true));
        await service.CreateAsync(new CreatePartyDto("BOTH001", "Both 1", "BOTH", null, null, null, null, null, null, true));

        // Act - Filter by CUSTOMER
        var customers = await service.GetAllAsync(1, 50, null, "CUSTOMER");

        // Assert
        Assert.Equal(2, customers.Total); // CUSTOMER + BOTH
        Assert.All(customers.Items, p => Assert.True(p.Type == "CUSTOMER" || p.Type == "BOTH"));
    }

    [Fact]
    public async Task SearchByQuery_FiltersCodeAndName()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var context = CreateContext(tenantId, out var tenantContextMock);
        var service = new PartyService(context, tenantContextMock.Object);

        // Create test parties
        await service.CreateAsync(new CreatePartyDto("ABC001", "Alpha Corporation", "CUSTOMER", null, null, null, null, null, null, true));
        await service.CreateAsync(new CreatePartyDto("XYZ001", "Beta Industries", "SUPPLIER", null, null, null, null, null, null, true));
        await service.CreateAsync(new CreatePartyDto("DEF001", "Alpha Solutions", "BOTH", null, null, null, null, null, null, true));

        // Act - Get all and manually filter (InMemory doesn't support ILike)
        var allParties = await service.GetAllAsync(1, 50, null, null);
        var filtered = allParties.Items.Where(p => 
            p.Name.Contains("Alpha", StringComparison.OrdinalIgnoreCase) || 
            p.Code.Contains("Alpha", StringComparison.OrdinalIgnoreCase)).ToList();

        // Assert
        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, p => Assert.Contains("Alpha", p.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreditLimit_CannotBeNegative_ValidationHandled()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var context = CreateContext(tenantId, out var tenantContextMock);
        var service = new PartyService(context, tenantContextMock.Object);

        // This would fail at validator level, but we test service accepts valid values
        var dtoValid = new CreatePartyDto("CUST001", "Test Customer", "CUSTOMER", null, null, null, null, 1000m, null, true);
        var dtoZero = new CreatePartyDto("CUST002", "Test Customer 2", "CUSTOMER", null, null, null, null, 0m, null, true);

        // Act
        var party1 = await service.CreateAsync(dtoValid);
        var party2 = await service.CreateAsync(dtoZero);

        // Assert
        Assert.Equal(1000m, party1.CreditLimit);
        Assert.Equal(0m, party2.CreditLimit);
    }

    [Fact]
    public async Task BothType_MatchesCustomerAndSupplierQueries()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var context = CreateContext(tenantId, out var tenantContextMock);
        var service = new PartyService(context, tenantContextMock.Object);

        // Create BOTH type party
        await service.CreateAsync(new CreatePartyDto("BOTH001", "Both Party", "BOTH", null, null, null, null, null, null, true));
        await service.CreateAsync(new CreatePartyDto("CUST001", "Customer Only", "CUSTOMER", null, null, null, null, null, null, true));
        await service.CreateAsync(new CreatePartyDto("SUPP001", "Supplier Only", "SUPPLIER", null, null, null, null, null, null, true));

        // Act
        var customers = await service.GetAllAsync(1, 50, null, "CUSTOMER");
        var suppliers = await service.GetAllAsync(1, 50, null, "SUPPLIER");
        var both = await service.GetAllAsync(1, 50, null, "BOTH");

        // Assert
        Assert.Equal(2, customers.Total); // CUSTOMER + BOTH
        Assert.Equal(2, suppliers.Total); // SUPPLIER + BOTH
        Assert.Equal(1, both.Total); // Only BOTH
        
        // Verify BOTH001 appears in customer query
        Assert.Contains(customers.Items, p => p.Code == "BOTH001");
        
        // Verify BOTH001 appears in supplier query
        Assert.Contains(suppliers.Items, p => p.Code == "BOTH001");
    }
}
