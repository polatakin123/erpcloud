using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/debug")]
[Authorize]
public class DebugController : ControllerBase
{
    private readonly ErpDbContext _context;
    private readonly TenantContextAccessor _tenantAccessor;
    private readonly ILogger<DebugController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IServiceProvider _serviceProvider;

    public DebugController(
        ErpDbContext context,
        TenantContextAccessor tenantAccessor,
        ILogger<DebugController> logger,
        IWebHostEnvironment env,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
        _env = env;
        _serviceProvider = serviceProvider;
    }

    [HttpGet("check-demo-user")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckDemoUser()
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Username == "demo")
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.PasswordHash,
                u.IsActive,
                u.Email,
                u.Role,
                HashLength = u.PasswordHash.Length,
                HashPrefix = u.PasswordHash.Substring(0, 10)
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound(new { message = "Demo user not found" });
        }

        var testPassword = "Demo123!";
        var verifyResult = BCrypt.Net.BCrypt.Verify(testPassword, user.PasswordHash);

        return Ok(new
        {
            user,
            testPassword,
            verifyResult,
            verifyMessage = verifyResult ? "✅ Password matches!" : "❌ Password does NOT match"
        });
    }

    [HttpPost("update-demo-password")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateDemoPassword()
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Username == "demo");
        if (user == null)
        {
            return NotFound(new { message = "Demo user not found" });
        }

        var newHash = BCrypt.Net.BCrypt.HashPassword("Demo123!", 11);
        user.PasswordHash = newHash;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "✅ Password updated successfully",
            newHash,
            verified = BCrypt.Net.BCrypt.Verify("Demo123!", newHash)
        });
    }

    [HttpPost("update-admin-password")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateAdminPassword()
    {
        var demoTenantId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Username == "admin");
            
        var newHash = BCrypt.Net.BCrypt.HashPassword("Admin123!", 11);
        
        if (user == null)
        {
            // Create admin user if not exists
            user = new User
            {
                Id = adminUserId,
                TenantId = demoTenantId,
                Username = "admin",
                PasswordHash = newHash,
                Email = "admin@erpcloud.local",
                FullName = "Admin User",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = adminUserId
            };
            _context.Users.Add(user);
        }
        else
        {
            // Update existing user password
            user.PasswordHash = newHash;
            user.UpdatedAt = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "✅ Admin password updated successfully",
            username = user.Username,
            role = user.Role,
            newHash,
            verified = BCrypt.Net.BCrypt.Verify("Admin123!", newHash)
        });
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
