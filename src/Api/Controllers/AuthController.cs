using ErpCloud.Api.Data;
using ErpCloud.Api.Helpers;
using ErpCloud.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ErpDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(ErpDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
            .Where(u => u.Username == request.Username && u.IsActive)
            .FirstOrDefaultAsync();

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı" });
        }

        // Load user permissions
        var userPermissions = await _context.UserPermissions
            .Where(up => up.UserId == user.Id)
            .Include(up => up.Permission)
            .Select(up => up.Permission.Code)
            .ToListAsync();

        // Add wildcard permission for Admin role
        if (user.Role == "Admin" && !userPermissions.Contains("*.*"))
        {
            userPermissions.Add("*.*");
        }

        var secretKey = _configuration["Jwt:SecretKey"] ?? "your-256-bit-secret-key-for-development-only-min-32-chars";
        var expiresAt = DateTime.UtcNow.AddDays(365);

        var token = JwtTestHelper.GenerateTestToken(
            tenantId: user.TenantId,
            userId: user.Id,
            email: user.Email ?? $"{user.Username}@erpcloud.local",
            roles: new[] { user.Role },
            permissions: userPermissions.ToArray(),
            secretKey: secretKey
        );

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Username = user.Username,
            Role = user.Role,
            Permissions = userPermissions.ToArray()
        });
    }
}
