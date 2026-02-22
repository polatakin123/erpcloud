using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ErpCloud.Api.Dev;

/// <summary>
/// Development JWT Token Generator - ONLY FOR TESTING
/// </summary>
public static class DevTokenGenerator
{
    public static string GenerateToken(string userId = "dev-user", string[]? roles = null, string[]? permissions = null)
    {
        var secretKey = "your-256-bit-secret-key-for-development-only-min-32-chars";
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("tenant_id", "default"),
            new Claim("preferred_username", "dev-admin"),
            new Claim("email", "dev@erpcloud.local")
        };

        // Add all permissions for testing
        var allPermissions = new[]
        {
            "stock.read", "stock.write",
            "salesorder.read", "salesorder.write",
            "shipment.read", "shipment.write",
            "invoicing.write",
            "order.read", "order.write",
            "org.read", "org.write",
            "branch.read", "branch.write",
            "warehouse.read", "warehouse.write",
            "party.read", "party.write",
            "product.read", "product.write",
            "variant.read", "variant.write",
            "pricelist.read", "pricelist.write",
            "pricing.read", "pricing.calculate",
            "purchaseorder.read", "purchaseorder.write",
            "goodsreceipt.read", "goodsreceipt.write",
            "cashbox.read", "cashbox.write",
            "bank.read", "bank.write",
            "cashledger.read",
            "payment.read", "payment.write",
            "reports.read",
            "admin"
        };

        foreach (var permission in permissions ?? allPermissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        foreach (var role in roles ?? new[] { "admin" })
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: "ErpCloud",
            audience: "erp-cloud",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
