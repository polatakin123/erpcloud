using ErpCloud.Api.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("test")]
public class TestTokenController : ControllerBase
{
    /// <summary>
    /// Generates a test JWT token for development/testing
    /// </summary>
    [HttpGet("token")]
    public IActionResult GenerateToken(
        [FromQuery] string? email = "test@example.com",
        [FromQuery] string? roles = "user,admin",
        [FromQuery] string? permissions = "stock.read,stock.write,order.read")
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var roleList = roles?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
        var permissionList = permissions?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];

        var token = JwtTestHelper.GenerateTestToken(
            tenantId: tenantId,
            userId: userId,
            email: email ?? "test@example.com",
            roles: roleList,
            permissions: permissionList
        );

        return Ok(new
        {
            Token = token,
            TenantId = tenantId,
            UserId = userId,
            Email = email,
            Roles = roleList,
            Permissions = permissionList,
            Instructions = new
            {
                Usage = "Add this token to your requests with header: Authorization: Bearer {token}",
                SwaggerUsage = "Click 'Authorize' button in Swagger, enter: Bearer {token}",
                CurlExample = $"curl -H \"Authorization: Bearer {token}\" http://localhost:5000/auth/debug"
            }
        });
    }

    /// <summary>
    /// Generates admin token with all permissions
    /// </summary>
    [HttpGet("token/admin")]
    public IActionResult GenerateAdminToken()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        var token = JwtTestHelper.GenerateTestToken(
            tenantId: tenantId,
            userId: userId,
            email: "admin@example.com",
            roles: ["admin", "user"],
            permissions: ["admin", "stock.read", "stock.write", "order.read", "order.write"]
        );

        return Ok(new
        {
            Token = token,
            Type = "Admin Token",
            Instructions = "Use this token for admin operations. Add header: Authorization: Bearer {token}"
        });
    }

    /// <summary>
    /// Generates readonly user token (only read permissions)
    /// </summary>
    [HttpGet("token/readonly")]
    public IActionResult GenerateReadOnlyToken()
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var token = JwtTestHelper.GenerateTestToken(
            tenantId: tenantId,
            userId: userId,
            email: "readonly@example.com",
            roles: ["user"],
            permissions: ["stock.read", "order.read"]
        );

        return Ok(new
        {
            Token = token,
            Type = "Read-Only Token",
            Instructions = "Use this token for testing read-only access. Add header: Authorization: Bearer {token}"
        });
    }
}
