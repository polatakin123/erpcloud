using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/debug")]
[Authorize]
public class DebugController : ControllerBase
{
    private readonly TenantContextAccessor _tenantAccessor;
    private readonly ILogger<DebugController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IServiceProvider _serviceProvider;

    public DebugController(
        TenantContextAccessor tenantAccessor,
        ILogger<DebugController> logger,
        IWebHostEnvironment env,
        IServiceProvider serviceProvider)
    {
        _tenantAccessor = tenantAccessor;
        _logger = logger;
        _env = env;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Demonstrates tenant bypass functionality (Development only).
    /// WARNING: For testing purposes only!
    /// </summary>
    [HttpPost("tenant-bypass-test")]
    public IActionResult TenantBypassTest()
    {
        // Only allow in Development
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        var currentTenantId = _tenantAccessor.TenantContext.TenantId;
        _logger.LogWarning("Tenant bypass test initiated by tenant: {TenantId}", currentTenantId);

        var results = new
        {
            current_tenant = currentTenantId,
            bypass_enabled_before = _tenantAccessor.TenantContext.IsBypassEnabled,
            message = "Bypass scope opened (this would allow cross-tenant queries)",
            warning = "This endpoint is only available in Development environment"
        };

        // Demonstrate bypass scope
        using (var bypass = _serviceProvider.GetRequiredService<TenantBypassScope>())
        {
            _logger.LogWarning("Bypass scope ACTIVE - tenant isolation is DISABLED");
            _logger.LogInformation("Bypass status: {Status}", _tenantAccessor.TenantContext.IsBypassEnabled);
        }

        _logger.LogInformation("Bypass scope CLOSED - tenant isolation RESTORED");

        return Ok(results);
    }
}
