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
        // Register TenantContext as scoped - shared instance throughout request
        services.AddScoped<TenantContext>();
        
        // Register ITenantContext interface pointing to the same TenantContext instance
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        
        // Register TenantContextAccessor to manipulate the same TenantContext
        services.AddScoped<TenantContextAccessor>(sp => 
        {
            var accessor = new TenantContextAccessor();
            // Wire up the scoped TenantContext
            var context = sp.GetRequiredService<TenantContext>();
            accessor.SetContext(context);
            return accessor;
        });
        
        services.AddScoped<TenantBypassScope>(); // For DI in bypass scenarios

        return services;
    }

    public static IApplicationBuilder UseTenantContext(this IApplicationBuilder app)
    {
        app.UseMiddleware<TenantMiddleware>();
        return app;
    }
}
