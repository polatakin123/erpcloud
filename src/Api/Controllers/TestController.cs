using ErpCloud.Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/test")]
[AllowAnonymous] // Public for testing purposes
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;

    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates test JWT tokens for different tenants.
    /// Development only endpoint for testing multi-tenancy.
    /// </summary>
    [HttpGet("generate-token")]
    public IActionResult GenerateToken([FromQuery] Guid? tenantId, [FromQuery] Guid? userId)
    {
        var tenant = tenantId ?? Guid.NewGuid();
        var user = userId ?? Guid.NewGuid();

        var token = JwtTestHelper.GenerateTestToken(tenant, user);

        _logger.LogInformation("Generated test token for Tenant: {TenantId}, User: {UserId}", tenant, user);

        return Ok(new
        {
            token,
            tenant_id = tenant,
            user_id = user,
            usage = "Add this token to your requests: Authorization: Bearer <token>",
            example_curl = $"curl -H \"Authorization: Bearer {token}\" http://localhost:5000/api/tenant/me"
        });
    }
}
