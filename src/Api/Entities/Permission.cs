namespace ErpCloud.Api.Entities;

public class Permission
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}

public class UserPermission
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    
    public DateTime GrantedAt { get; set; }
    public Guid? GrantedBy { get; set; }
}
