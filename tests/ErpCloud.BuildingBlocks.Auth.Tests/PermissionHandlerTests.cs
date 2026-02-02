using Microsoft.AspNetCore.Authorization;
using Moq;

namespace ErpCloud.BuildingBlocks.Auth.Tests;

public class PermissionHandlerTests
{
    [Fact]
    public async Task HandleRequirementAsync_UserHasPermission_ShouldSucceed()
    {
        // Arrange
        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.HasPermission("stock.read")).Returns(true);
        
        var handler = new PermissionHandler(currentUserMock.Object);
        var requirement = new PermissionRequirement("stock.read");
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            null!, // No principal needed for this test
            null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_UserDoesNotHavePermission_ShouldFail()
    {
        // Arrange
        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.HasPermission("stock.read")).Returns(false);
        
        var handler = new PermissionHandler(currentUserMock.Object);
        var requirement = new PermissionRequirement("stock.read");
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            null!,
            null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
        Assert.True(context.HasFailed || !context.HasSucceeded);
    }

    [Theory]
    [InlineData("stock.read", true)]
    [InlineData("order.write", true)]
    [InlineData("admin.delete", false)]
    public async Task HandleRequirementAsync_WithVariousPermissions_ShouldHandleCorrectly(
        string permission,
        bool hasPermission)
    {
        // Arrange
        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.HasPermission(permission)).Returns(hasPermission);
        
        var handler = new PermissionHandler(currentUserMock.Object);
        var requirement = new PermissionRequirement(permission);
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            null!,
            null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.Equal(hasPermission, context.HasSucceeded);
    }
}
