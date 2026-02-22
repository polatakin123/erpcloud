using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Dev;

public static class DatabaseSeeder
{
    public static async Task SeedDemoUsers(ErpDbContext context)
    {
        var demoTenantId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var adminUserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var kasiyerUserId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var demoUserId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        
        var allPermCodes = new[] {
            "*.*", // Wildcard - full access
            "POS.VIEW", "POS.SELL", "POS.REFUND", "POS.DISCOUNT_APPLY", "POS.PRICE_OVERRIDE",
            "STOCK.VIEW", "STOCK.ADJUST",
            "FINANCE.VIEW", "FINANCE.COLLECT",
            "ADMIN.USERS_MANAGE", "ADMIN.SETTINGS", "ADMIN.REPORTS_ALL"
        };
        
        foreach (var code in allPermCodes)
        {
            if (!await context.Permissions.AnyAsync(p => p.Code == code))
            {
                context.Permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Name = code.Replace(".", " "),
                    Category = code.Split('.')[0],
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await context.SaveChangesAsync();

        var permissions = await context.Permissions.ToListAsync();
        var permDict = permissions.ToDictionary(p => p.Code, p => p.Id);

        await SeedUser(context, adminUserId, demoTenantId, "admin", "Admin123!", "Admin User", "Admin", permDict.Values.ToArray());
        await SeedUser(context, kasiyerUserId, demoTenantId, "kasiyer", "Kasiyer123!", "Kasiyer User", "Dealer", 
            new[] { "POS.VIEW", "POS.SELL", "POS.REFUND", "STOCK.VIEW" }.Select(c => permDict[c]).ToArray());
        await SeedUser(context, demoUserId, demoTenantId, "demo", "Demo123!", "Demo User", "Dealer",
            new[] { "POS.VIEW", "POS.SELL" }.Select(c => permDict[c]).ToArray());
    }

    private static async Task SeedUser(ErpDbContext context, Guid userId, Guid tenantId, string username, string password, string fullName, string role, Guid[] permissionIds)
    {
        var existing = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Username == username);
        if (existing != null)
        {
            Console.WriteLine($"ℹ️ {username} already exists (UserId: {existing.Id}, TenantId: {existing.TenantId})");
            
            // User exists, update permissions
            var existingPermIds = await context.UserPermissions
                .IgnoreQueryFilters()
                .Where(up => up.UserId == userId)
                .Select(up => up.PermissionId)
                .ToListAsync();
            
            var missingPermIds = permissionIds.Except(existingPermIds).ToArray();
            Console.WriteLine($"  Existing perms: {existingPermIds.Count}, Missing perms: {missingPermIds.Length}");
            
            if (missingPermIds.Length > 0)
            {
                try
                {
                    foreach (var permId in missingPermIds)
                    {
                        Console.WriteLine($"  Adding permission {permId} to user {userId}");
                        context.UserPermissions.Add(new UserPermission
                        {
                            UserId = userId,
                            PermissionId = permId,
                            GrantedAt = DateTime.UtcNow,
                            GrantedBy = userId
                        });
                    }
                    await context.SaveChangesAsync();
                    Console.WriteLine($"✅ {username} updated with {missingPermIds.Length} new permissions");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error updating permissions for {username}: {ex.Message}");
                    context.ChangeTracker.Clear();
                }
            }
            else
            {
                Console.WriteLine($"ℹ️ {username} already has all {permissionIds.Length} permissions");
            }
            return;
        }
        
        try
        {
            var user = new User
            {
                Id = userId,
                TenantId = tenantId,
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 11),
                Email = $"{username}@erpcloud.local",
                FullName = fullName,
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            
            foreach (var permId in permissionIds)
            {
                context.UserPermissions.Add(new UserPermission
                {
                    UserId = userId,
                    PermissionId = permId,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = userId
                });
            }
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ {username} created with {permissionIds.Length} permissions");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ {username}: {ex.Message}");
            context.ChangeTracker.Clear();
        }
    }
}
