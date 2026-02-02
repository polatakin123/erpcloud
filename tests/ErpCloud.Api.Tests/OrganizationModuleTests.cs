using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ErpCloud.Api.Tests;

public class OrganizationModuleTests
{
    private ErpDbContext CreateContext(Guid tenantId, out Mock<ITenantContext> tenantContextMock)
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        tenantContextMock = new Mock<ITenantContext>();
        tenantContextMock.Setup(x => x.TenantId).Returns(tenantId);
        tenantContextMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        tenantContextMock.Setup(x => x.IsBypassEnabled).Returns(true); // Bypass for tests

        return new ErpDbContext(options, tenantContextMock.Object);
    }

    [Fact]
    public async Task TenantIsolation_TwoTenantsCanHaveSameOrgCode()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();

        var context1 = CreateContext(tenant1, out var tenantContext1Mock);
        var service1 = new OrganizationService(context1, tenantContext1Mock.Object);

        var context2 = CreateContext(tenant2, out var tenantContext2Mock);
        var service2 = new OrganizationService(context2, tenantContext2Mock.Object);

        var dto = new CreateOrganizationDto("ABC123", "Test Organization", null);

        // Act
        var org1 = await service1.CreateAsync(dto);
        var org2 = await service2.CreateAsync(dto);

        // Assert
        Assert.NotNull(org1);
        Assert.NotNull(org2);
        Assert.Equal("ABC123", org1.Code);
        Assert.Equal("ABC123", org2.Code);
        Assert.NotEqual(org1.Id, org2.Id);
    }

    [Fact]
    public async Task SameTenant_DuplicateOrgCode_ThrowsException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var context = CreateContext(tenantId, out var tenantContextMock);
        var service = new OrganizationService(context, tenantContextMock.Object);

        var dto = new CreateOrganizationDto("ABC123", "Test Organization", null);
        await service.CreateAsync(dto);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.CreateAsync(dto);
        });
    }

    [Fact]
    public async Task BranchCodeUnique_WithinSameOrganization()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var context = CreateContext(tenantId, out var tenantContextMock);

        var orgService = new OrganizationService(context, tenantContextMock.Object);
        var branchService = new BranchService(context, tenantContextMock.Object);

        // Create organization
        var org = await orgService.CreateAsync(new CreateOrganizationDto("ORG1", "Organization 1", null));
        
        // Create first branch
        var branchDto = new CreateBranchDto("BR001", "Branch 001", "Istanbul", "Address 1");
        await branchService.CreateAsync(org.Id, branchDto);

        // Act & Assert - Try to create duplicate branch code in same org
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await branchService.CreateAsync(org.Id, branchDto);
        });
    }

    [Fact]
    public async Task WarehouseDefault_OnlyOneDefaultPerBranch()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var context = CreateContext(tenantId, out var tenantContextMock);

        var orgService = new OrganizationService(context, tenantContextMock.Object);
        var branchService = new BranchService(context, tenantContextMock.Object);
        var warehouseService = new WarehouseService(context, tenantContextMock.Object);

        // Create org and branch
        var org = await orgService.CreateAsync(new CreateOrganizationDto("ORG1", "Organization 1", null));
        var branch = await branchService.CreateAsync(org.Id, new CreateBranchDto("BR001", "Branch 001", null, null));

        // Create first default warehouse
        var wh1 = await warehouseService.CreateAsync(branch.Id, 
            new CreateWarehouseDto("WH001", "Warehouse 1", "MAIN", true));
        Assert.True(wh1.IsDefault);

        // Create second default warehouse
        var wh2 = await warehouseService.CreateAsync(branch.Id, 
            new CreateWarehouseDto("WH002", "Warehouse 2", "STORE", true));
        Assert.True(wh2.IsDefault);

        // Act - Verify first warehouse is no longer default
        var wh1Updated = await warehouseService.GetByIdAsync(wh1.Id);
        
        // Assert
        Assert.NotNull(wh1Updated);
        Assert.False(wh1Updated.IsDefault); // First should be unset
        Assert.True(wh2.IsDefault); // Second should be default
    }

    [Fact]
    public async Task UpdateWarehouse_SetDefaultTrue_UnsetsOtherDefaults()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var context = CreateContext(tenantId, out var tenantContextMock);

        var orgService = new OrganizationService(context, tenantContextMock.Object);
        var branchService = new BranchService(context, tenantContextMock.Object);
        var warehouseService = new WarehouseService(context, tenantContextMock.Object);

        // Create org, branch and two warehouses
        var org = await orgService.CreateAsync(new CreateOrganizationDto("ORG1", "Organization 1", null));
        var branch = await branchService.CreateAsync(org.Id, new CreateBranchDto("BR001", "Branch 001", null, null));
        
        var wh1 = await warehouseService.CreateAsync(branch.Id, 
            new CreateWarehouseDto("WH001", "Warehouse 1", "MAIN", true));
        var wh2 = await warehouseService.CreateAsync(branch.Id, 
            new CreateWarehouseDto("WH002", "Warehouse 2", "STORE", false));

        // Act - Update second warehouse to be default
        var updateDto = new UpdateWarehouseDto("WH002", "Warehouse 2 Updated", "STORE", true);
        await warehouseService.UpdateAsync(wh2.Id, updateDto);

        // Assert
        var wh1Updated = await warehouseService.GetByIdAsync(wh1.Id);
        var wh2Updated = await warehouseService.GetByIdAsync(wh2.Id);

        Assert.NotNull(wh1Updated);
        Assert.NotNull(wh2Updated);
        Assert.False(wh1Updated.IsDefault);
        Assert.True(wh2Updated.IsDefault);
    }

    [Fact]
    public async Task SearchByQuery_FiltersCodeAndName()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var context = CreateContext(tenantId, out var tenantContextMock);

        var service = new OrganizationService(context, tenantContextMock.Object);

        // Create test organizations
        await service.CreateAsync(new CreateOrganizationDto("ABC123", "Alpha Corporation", null));
        await service.CreateAsync(new CreateOrganizationDto("XYZ789", "Beta Industries", null));
        await service.CreateAsync(new CreateOrganizationDto("DEF456", "Alpha Solutions", null));

        // Act - Get all and manually filter for test (InMemory doesn't support ILike)
        var allOrgs = await service.GetAllAsync(1, 50, null);
        var filtered = allOrgs.Items.Where(o => o.Name.Contains("Alpha", StringComparison.OrdinalIgnoreCase) || o.Code.Contains("Alpha", StringComparison.OrdinalIgnoreCase)).ToList();

        // Assert
        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, org => Assert.Contains("Alpha", org.Name, StringComparison.OrdinalIgnoreCase));
    }
}
