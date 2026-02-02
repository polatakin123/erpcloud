using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ErpCloud.BuildingBlocks.Auth;

public static class AuthExtensions
{
    /// <summary>
    /// Adds ERP Cloud authentication and authorization with Keycloak JWT support
    /// </summary>
    public static IServiceCollection AddErpAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register current user accessor
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // JWT Authentication
        var authSection = configuration.GetSection("Auth");
        var authority = authSection["Authority"];
        var audience = authSection["Audience"];
        var requireHttpsMetadata = authSection.GetValue("RequireHttpsMetadata", false);
        
        // Development mode - use symmetric key
        var jwtSection = configuration.GetSection("Jwt");
        var secretKey = jwtSection["SecretKey"];
        var useDevToken = !string.IsNullOrEmpty(secretKey) && string.IsNullOrEmpty(authority);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            if (useDevToken)
            {
                // Development mode - symmetric key validation
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"] ?? "ErpCloud",
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"] ?? "erp-cloud",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey!)),
                    NameClaimType = "sub",
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            }
            else
            {
                // Production mode - Keycloak validation
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = requireHttpsMetadata;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    NameClaimType = "sub", // Keycloak user id
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
            }

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    // Map Keycloak realm_access.roles to standard role claims
                    MapKeycloakRolesToClaims(context.Principal!);
                    return Task.CompletedTask;
                }
            };
        });

        // Authorization with permission policies
        services.AddAuthorization();
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        return services;
    }

    /// <summary>
    /// Adds JWT authentication and permission-based authorization (legacy method)
    /// </summary>
    public static IServiceCollection AddAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Delegate to AddErpAuth for consistency
        return services.AddErpAuth(configuration);
    }

    /// <summary>
    /// Requires a specific permission for authorization
    /// </summary>
    public static AuthorizationPolicyBuilder RequirePermission(
        this AuthorizationPolicyBuilder builder,
        string permission)
    {
        builder.AddRequirements(new PermissionRequirement(permission));
        return builder;
    }

    /// <summary>
    /// Adds a permission-based authorization policy
    /// </summary>
    public static AuthorizationOptions AddPermissionPolicy(
        this AuthorizationOptions options,
        string policyName,
        string permission)
    {
        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequirePermission(permission);
        });
        return options;
    }

    /// <summary>
    /// Adds multiple permission-based authorization policies
    /// </summary>
    public static AuthorizationOptions AddPermissionPolicies(
        this AuthorizationOptions options,
        params (string PolicyName, string Permission)[] policies)
    {
        foreach (var (policyName, permission) in policies)
        {
            options.AddPermissionPolicy(policyName, permission);
        }
        return options;
    }

    private static void MapKeycloakRolesToClaims(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null) return;

        // Keycloak roles are in realm_access.roles JSON claim
        var realmAccessClaim = identity.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccessClaim)) return;

        try
        {
            var realmAccess = System.Text.Json.JsonDocument.Parse(realmAccessClaim);
            if (realmAccess.RootElement.TryGetProperty("roles", out var rolesElement))
            {
                foreach (var role in rolesElement.EnumerateArray())
                {
                    var roleValue = role.GetString();
                    if (!string.IsNullOrWhiteSpace(roleValue))
                    {
                        // Add as standard role claim
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                        identity.AddClaim(new Claim("role", roleValue));
                    }
                }
            }
        }
        catch
        {
            // Ignore JSON parsing errors
        }
    }
}
