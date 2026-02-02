using Microsoft.AspNetCore.Mvc;
using ErpCloud.Api.Dev;

namespace ErpCloud.Api.Controllers;

/// <summary>
/// Development helper endpoints - ONLY FOR TESTING
/// </summary>
[ApiController]
[Route("api/dev")]
public class DevController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public DevController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Generate a development JWT token with all permissions
    /// </summary>
    [HttpGet("token")]
    public IActionResult GenerateToken()
    {
        var env = _configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");
        if (env != "Development")
        {
            return Forbid();
        }

        var token = DevTokenGenerator.GenerateToken();

        return Ok(new
        {
            token,
            expiresIn = 7 * 24 * 60 * 60, // 7 days in seconds
            tokenType = "Bearer",
            message = "Copy this token and paste it in the login page",
            permissions = "all"
        });
    }
}
