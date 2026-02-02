using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ErpCloud.BuildingBlocks.Tenant;

/// <summary>
/// Extension methods for tenant services and middleware.
/// </summary>
public static class TenantExtensions
{
    public static IServiceCollection AddTenantContext(this IServiceCollection services)
    {
        services.AddScoped<TenantContextAccessor>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContextAccessor>().TenantContext);
        services.AddScoped<TenantBypassScope>(); // For DI in bypass scenarios

        return services;
    }

    public static IApplicationBuilder UseTenantContext(this IApplicationBuilder app)
    {
        app.UseMiddleware<TenantMiddleware>();
        return app;
    }
}
