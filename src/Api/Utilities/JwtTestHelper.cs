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
        var claims = new List<Claim>
        {
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("user_id", userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            
            // Add all required policies for development
            new Claim("policy", "org.read"),
            new Claim("policy", "org.write"),
            new Claim("policy", "branch.read"),
            new Claim("policy", "branch.write"),
            new Claim("policy", "warehouse.read"),
            new Claim("policy", "warehouse.write"),
            new Claim("policy", "product.read"),
            new Claim("policy", "product.write"),
            new Claim("policy", "variant.read"),
            new Claim("policy", "variant.write"),
            new Claim("policy", "party.read"),
            new Claim("policy", "party.write"),
            new Claim("policy", "customer.read"),
            new Claim("policy", "customer.write"),
            new Claim("policy", "stock.read"),
            new Claim("policy", "stock.write"),
            new Claim("policy", "salesorder.read"),
            new Claim("policy", "salesorder.write"),
            new Claim("policy", "shipment.read"),
            new Claim("policy", "shipment.write"),
            new Claim("policy", "invoicing.write"),
            new Claim("policy", "invoice.read"),
            new Claim("policy", "invoice.write"),
            new Claim("policy", "payment.read"),
            new Claim("policy", "payment.write"),
            new Claim("policy", "order.read"),
            new Claim("policy", "order.write"),
            new Claim("policy", "pricelist.read"),
            new Claim("policy", "pricelist.write"),
            new Claim("policy", "pricing.read"),
            new Claim("policy", "purchaseorder.read"),
            new Claim("policy", "purchaseorder.write"),
            new Claim("policy", "goodsreceipt.read"),
            new Claim("policy", "goodsreceipt.write"),
            new Claim("policy", "cashbox.read"),
            new Claim("policy", "cashbox.write"),
            new Claim("policy", "bank.read"),
            new Claim("policy", "bank.write"),
            new Claim("policy", "cashledger.read"),
            new Claim("policy", "reports.read"),
            new Claim("policy", "admin")
        };

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("your-256-bit-secret-key-for-development-only-min-32-chars"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "ErpCloud",
            audience: "erp-cloud",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
