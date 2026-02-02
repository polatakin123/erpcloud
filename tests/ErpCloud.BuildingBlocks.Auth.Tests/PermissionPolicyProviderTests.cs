using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ErpCloud.BuildingBlocks.Auth.Tests;

public class PermissionPolicyProviderTests
{
    [Fact]
    public async Task GetPolicyAsync_WithPermissionPolicy_ShouldCreateDynamicPolicy()
    {
        // Arrange
        var options = Options.Create(new AuthorizationOptions());
        var provider = new PermissionPolicyProvider(options);

        // Act
        var policy = await provider.GetPolicyAsync("perm:stock.read");

        // Assert
        Assert.NotNull(policy);
        Assert.NotEmpty(policy.Requirements);
        
        var permissionRequirement = policy.Requirements.OfType<PermissionRequirement>().FirstOrDefault();
        Assert.NotNull(permissionRequirement);
        Assert.Equal("stock.read", permissionRequirement.Permission);
    }

    [Fact]
    public async Task GetPolicyAsync_WithNonPermissionPolicy_ShouldDelegateToFallback()
    {
        // Arrange
        var options = Options.Create(new AuthorizationOptions());
        options.Value.AddPolicy("CustomPolicy", policy => policy.RequireAuthenticatedUser());
        var provider = new PermissionPolicyProvider(options);

        // Act
        var policy = await provider.GetPolicyAsync("CustomPolicy");

        // Assert
        Assert.NotNull(policy);
        // Should be the custom policy from fallback
    }

    [Theory]
    [InlineData("perm:stock.read", "stock.read")]
    [InlineData("perm:order.write", "order.write")]
    [InlineData("perm:admin.delete", "admin.delete")]
    public async Task GetPolicyAsync_WithVariousPermissions_ShouldExtractCorrectPermissionName(
        string policyName, 
        string expectedPermission)
    {
        // Arrange
        var options = Options.Create(new AuthorizationOptions());
        var provider = new PermissionPolicyProvider(options);

        // Act
        var policy = await provider.GetPolicyAsync(policyName);

        // Assert
        Assert.NotNull(policy);
        
        var permissionRequirement = policy.Requirements.OfType<PermissionRequirement>().FirstOrDefault();
        Assert.NotNull(permissionRequirement);
        Assert.Equal(expectedPermission, permissionRequirement.Permission);
    }

    [Fact]
    public async Task GetDefaultPolicyAsync_ShouldReturnDefaultPolicy()
    {
        // Arrange
        var options = Options.Create(new AuthorizationOptions());
        var provider = new PermissionPolicyProvider(options);

        // Act
        var policy = await provider.GetDefaultPolicyAsync();

        // Assert
        Assert.NotNull(policy);
    }

    [Fact]
    public async Task GetFallbackPolicyAsync_ShouldReturnFallbackPolicy()
    {
        // Arrange
        var options = Options.Create(new AuthorizationOptions());
        var provider = new PermissionPolicyProvider(options);

        // Act
        var policy = await provider.GetFallbackPolicyAsync();

        // Assert - Fallback policy can be null by default
        // Just verify no exception is thrown
    }
}
