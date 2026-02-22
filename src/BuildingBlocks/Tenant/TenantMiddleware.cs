using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;

namespace ErpCloud.BuildingBlocks.Tenant;

/// <summary>
/// Middleware to extract tenant and user context from JWT claims.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _publicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/swagger",
        "/api/test",
        "/api/auth/login",
        "/api/debug/check-demo-user",
        "/api/debug/update-demo-password",
        "/api/debug/update-admin-password"
    };

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContextAccessor tenantAccessor)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip tenant requirement for public paths and swagger
        if (IsPublicPath(path))
        {
            await _next(context);
            return;
        }

        // 🔧 DEVELOPMENT MODE BYPASS - Authentication olmadan geliştirme yapabilmek için
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        var bypassAuth = Environment.GetEnvironmentVariable("BYPASS_AUTH") == "true";
        
        if (isDevelopment && bypassAuth)
        {
            // Development modunda hard-coded tenant ve user kullan
            var devTenantId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
            var devUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            
            var devContext = new TenantContext
            {
                TenantId = devTenantId,
                UserId = devUserId
            };
            tenantAccessor.SetContext(devContext);
            
            Console.WriteLine($"[TenantMiddleware] 🔧 DEVELOPMENT BYPASS ACTIVE - Tenant: {devTenantId}, User: {devUserId}");
            
            await _next(context);
            return;
        }

        // Extract tenant_id from claims
        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var errorJson = JsonSerializer.Serialize(new
            {
                error = "Tenant.Missing",
                message = "tenant_id claim is required"
            });
            await context.Response.WriteAsync(errorJson);
            return;
        }

        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var errorJson = JsonSerializer.Serialize(new
            {
                error = "Tenant.Invalid",
                message = "tenant_id must be a valid GUID"
            });
            await context.Response.WriteAsync(errorJson);
            return;
        }

        tenantAccessor.SetTenantId(tenantId);

        Console.WriteLine($"[TenantMiddleware] Set TenantId: {tenantId}, Path: {path}");

        // Extract user_id if present (for audit)
        var userIdClaim = context.User.FindFirst("user_id")?.Value
                         ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
        {
            tenantAccessor.SetUserId(userId);
        }

        await _next(context);
    }

    private bool IsPublicPath(string path)
    {
        // Exact match or starts with pattern (for swagger paths like /swagger/v1/...)
        return _publicPaths.Any(p => 
            path.Equals(p, StringComparison.OrdinalIgnoreCase) || 
            path.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase));
    }
}
