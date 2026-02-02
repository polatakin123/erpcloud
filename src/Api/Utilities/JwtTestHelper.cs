using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace ErpCloud.Api.Utilities;

/// <summary>
/// Helper class to generate mock JWT tokens for testing.
/// </summary>
public static class JwtTestHelper
{
    /// <summary>
    /// Generates a test JWT token with tenant_id and user_id claims.
    /// </summary>
    public static string GenerateTestToken(Guid tenantId, Guid userId)
    {
        var claims = new[]
        {
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("user_id", userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("test-secret-key-for-development-only-32chars"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "https://keycloak.example.com/realms/erpcloud",
            audience: "erp-api",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
