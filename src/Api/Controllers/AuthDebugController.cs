using ErpCloud.BuildingBlocks.Auth;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthDebugController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenantContext;

    public AuthDebugController(ICurrentUser currentUser, ITenantContext tenantContext)
    {
        _currentUser = currentUser;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Debug endpoint to verify authentication and tenant context
    /// </summary>
    [HttpGet("debug")]
    [Authorize]
    public IActionResult GetDebugInfo()
    {
        return Ok(new
        {
            tenantId = _tenantContext.TenantId,
            user = new
            {
                id = _currentUser.UserId,
                email = _currentUser.Email,
                roles = _currentUser.Roles,
                permissions = _currentUser.Permissions,
                isAuthenticated = _currentUser.IsAuthenticated
            }
        });
    }

    /// <summary>
    /// Test role-based authorization - requires Admin role
    /// </summary>
    [HttpGet("role-test")]
    [Authorize(Roles = "Admin")]
    public IActionResult TestRoleAuthorization()
    {
        return Ok(new
        {
            message = "role ok",
            role = "Admin",
            userRoles = _currentUser.Roles
        });
    }

    /// <summary>
    /// Test permission-based authorization - requires stock.read permission
    /// </summary>
    [HttpGet("perm-test")]
    [Authorize(Policy = "perm:stock.read")]
    public IActionResult TestPermissionAuthorization()
    {
        return Ok(new
        {
            message = "perm ok",
            permission = "stock.read",
            userPermissions = _currentUser.Permissions
        });
    }
}
