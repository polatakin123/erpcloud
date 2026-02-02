using Microsoft.AspNetCore.Authorization;

namespace ErpCloud.BuildingBlocks.Auth;

/// <summary>
/// Authorization handler for permission-based access control
/// </summary>
public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ICurrentUser _currentUser;

    public PermissionHandler(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (_currentUser.HasPermission(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
