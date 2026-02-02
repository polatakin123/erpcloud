using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ErpCloud.Api.Helpers;

/// <summary>
/// Helper for generating test JWT tokens (development only)
/// </summary>
public static class JwtTestHelper
{
    /// <summary>
    /// Generates a test JWT token with Keycloak-like structure
    /// </summary>
    public static string GenerateTestToken(
        Guid tenantId,
        Guid userId,
        string email,
        string[] roles,
        string[] permissions,
        string issuer = "ErpCloud",
        string audience = "erp-cloud",
        string secretKey = "your-256-bit-secret-key-for-development-only-min-32-chars")
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("user_id", userId.ToString()),
            new Claim("sub", userId.ToString()),
            new Claim("email", email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add Keycloak-style realm_access roles
        if (roles.Length > 0)
        {
            var realmAccess = new
            {
                roles = roles
            };
            claims.Add(new Claim("realm_access", JsonSerializer.Serialize(realmAccess)));
        }

        // Add permissions as JSON array
        if (permissions.Length > 0)
        {
            claims.Add(new Claim("permissions", JsonSerializer.Serialize(permissions)));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
