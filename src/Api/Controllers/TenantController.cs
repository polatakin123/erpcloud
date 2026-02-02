using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TenantController : ControllerBase
{
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantController> _logger;

    public TenantController(ITenantContext tenantContext, ILogger<TenantController> logger)
    {
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Get current tenant information.
    /// </summary>
    [HttpGet("me")]
    public IActionResult GetMyTenant()
    {
        _logger.LogInformation("Tenant info requested: {TenantId}", _tenantContext.TenantId);

        return Ok(new
        {
            tenant_id = _tenantContext.TenantId,
            user_id = _tenantContext.UserId,
            is_bypass_enabled = _tenantContext.IsBypassEnabled
        });
    }
}
