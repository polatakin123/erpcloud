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
        "/api/test"
    };

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContextAccessor tenantAccessor)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // DEBUG: Log all claims
        Console.WriteLine($"[TenantMiddleware] Path: {path}, IsAuthenticated: {context.User.Identity?.IsAuthenticated}");
        Console.WriteLine($"[TenantMiddleware] Total Claims: {context.User.Claims.Count()}");
        foreach (var claim in context.User.Claims)
        {
            Console.WriteLine($"[TenantMiddleware]   - {claim.Type}: {claim.Value}");
        }

        // Skip tenant requirement for public paths and swagger
        if (IsPublicPath(path))
        {
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
