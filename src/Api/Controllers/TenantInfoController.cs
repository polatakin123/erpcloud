using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("me")]
[Authorize]
public class TenantInfoController : ControllerBase
{
    private readonly ITenantContext _tenantContext;

    public TenantInfoController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Get current tenant and user information
    /// </summary>
    [HttpGet("tenant")]
    public IActionResult GetTenantInfo()
    {
        return Ok(new
        {
            TenantId = _tenantContext.TenantId,
            UserId = _tenantContext.UserId,
            Timestamp = DateTime.UtcNow
        });
    }
}
