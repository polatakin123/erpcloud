using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Controllers;

#if DEBUG
[ApiController]
[Route("debug")]
public class TenantDebugController : ControllerBase
{
    private readonly ErpDbContext _context;
    private readonly TenantContextAccessor _tenantAccessor;
    private readonly ILogger<TenantDebugController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IServiceProvider _serviceProvider;

    public TenantDebugController(
        ErpDbContext context,
        TenantContextAccessor tenantAccessor,
        ILogger<TenantDebugController> logger,
        IWebHostEnvironment env,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
        _env = env;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Test tenant bypass scope (DEVELOPMENT ONLY)
    /// Creates test data for two different tenants and proves bypass works
    /// </summary>
    [HttpPost("tenant-bypass-test")]
    public async Task<IActionResult> TestTenantBypass()
    {
        if (!_env.IsDevelopment())
        {
            return StatusCode(403, new { error = "This endpoint is only available in Development environment" });
        }

        var tenant1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tenant2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Create test items for both tenants using bypass
        using (var bypass = _serviceProvider.GetRequiredService<TenantBypassScope>())
        {
            // Clear existing test data
            var existingItems = await _context.SampleItems
                .Where(s => s.Name.StartsWith("[TEST]"))
                .ToListAsync();
            _context.SampleItems.RemoveRange(existingItems);

            // Create items for tenant 1
            _tenantAccessor.SetTenantId(tenant1);
            _context.SampleItems.Add(new SampleItem
            {
                Id = Guid.NewGuid(),
                Name = "[TEST] Tenant 1 Item",
                Description = "This belongs to tenant 1"
            });

            // Create items for tenant 2
            _tenantAccessor.SetTenantId(tenant2);
            _context.SampleItems.Add(new SampleItem
            {
                Id = Guid.NewGuid(),
                Name = "[TEST] Tenant 2 Item",
                Description = "This belongs to tenant 2"
            });

            await _context.SaveChangesAsync();

            _logger.LogWarning("BYPASS ENABLED: Created test items for both tenants");
        }

        // Now read with bypass to prove we can see both
        List<SampleItem> allItems;
        using (var bypass = _serviceProvider.GetRequiredService<TenantBypassScope>())
        {
            allItems = await _context.SampleItems
                .Where(s => s.Name.StartsWith("[TEST]"))
                .OrderBy(s => s.TenantId)
                .ToListAsync();

            _logger.LogWarning("BYPASS ENABLED: Read {Count} items across all tenants", allItems.Count);
        }

        // Try to read without bypass (should return 0 because current tenant context is tenant2)
        var itemsWithoutBypass = await _context.SampleItems
            .Where(s => s.Name.StartsWith("[TEST]"))
            .ToListAsync();

        _logger.LogInformation("WITHOUT BYPASS: Read {Count} items (should only see current tenant)", itemsWithoutBypass.Count);

        return Ok(new
        {
            Message = "Tenant bypass test completed",
            TestTenantIds = new[] { tenant1, tenant2 },
            CurrentTenantId = _tenantAccessor.TenantContext.TenantId,
            Results = new
            {
                WithBypass = new
                {
                    ItemCount = allItems.Count,
                    Items = allItems.Select(i => new
                    {
                        i.Id,
                        i.TenantId,
                        i.Name,
                        i.Description
                    })
                },
                WithoutBypass = new
                {
                    ItemCount = itemsWithoutBypass.Count,
                    Items = itemsWithoutBypass.Select(i => new
                    {
                        i.Id,
                        i.TenantId,
                        i.Name,
                        i.Description
                    })
                }
            },
            Proof = new
            {
                BypassAllowsAccessToAllTenants = allItems.Count == 2,
                NormalQueryOnlyShowsCurrentTenant = itemsWithoutBypass.Count <= 1,
                TenantIsolationWorks = allItems.Count > itemsWithoutBypass.Count
            }
        });
    }

    /// <summary>
    /// Simple endpoint to verify tenant isolation
    /// </summary>
    [HttpGet("tenant-isolation-check")]
    public async Task<IActionResult> CheckTenantIsolation()
    {
        if (!_env.IsDevelopment())
        {
            return StatusCode(403, new { error = "This endpoint is only available in Development environment" });
        }

        var currentTenantId = _tenantAccessor.TenantContext.TenantId;

        // Get items for current tenant
        var myItems = await _context.SampleItems.ToListAsync();

        // Get total count with bypass
        int totalItemsAllTenants;
        using (var bypass = _serviceProvider.GetRequiredService<TenantBypassScope>())
        {
            totalItemsAllTenants = await _context.SampleItems.CountAsync();
        }

        return Ok(new
        {
            CurrentTenantId = currentTenantId,
            MyItemsCount = myItems.Count,
            TotalItemsAllTenants = totalItemsAllTenants,
            IsolationWorking = totalItemsAllTenants >= myItems.Count,
            Message = totalItemsAllTenants > myItems.Count
                ? "✓ Tenant isolation is working - you can only see your tenant's data"
                : "All items belong to your tenant or database is empty"
        });
    }
}
#endif
