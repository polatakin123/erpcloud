# ErpCloud - Keycloak JWT Authentication & Authorization

## Overview

This document describes the Keycloak-compatible JWT authentication and permission-based authorization infrastructure implemented in ErpCloud.

## Architecture

### Components

1. **ICurrentUser / CurrentUser**: Provides access to current authenticated user information from JWT claims
2. **PermissionRequirement**: Authorization requirement for permission-based access control
3. **PermissionHandler**: Handles permission requirement verification
4. **PermissionPolicyProvider**: Dynamic policy provider that creates authorization policies on-the-fly
5. **AddErpAuth Extension**: Configures JWT Bearer authentication and authorization services

## Token Structure (Keycloak)

The system expects JWT tokens with the following claims:

```json
{
  "sub": "user-id-from-keycloak",
  "email": "user@example.com",
  "tenant_id": "00000000-0000-0000-0000-000000000001",
  "realm_access": {
    "roles": ["Admin", "User", "Manager"]
  },
  "permissions": ["stock.read", "stock.write", "order.read"]
}
```

### Claim Mapping

| Claim | Description | Type |
|-------|-------------|------|
| `sub` | User unique identifier | string |
| `email` | User email address | string |
| `tenant_id` | Tenant identifier (required by TenantMiddleware) | Guid (as string) |
| `realm_access.roles` | User roles from Keycloak realm | string array |
| `permissions` | Custom permissions | string array |

## Configuration

### appsettings.json

```json
{
  "Auth": {
    "Authority": "http://localhost:8080/realms/erp-cloud",
    "Audience": "erp-cloud-api",
    "RequireHttpsMetadata": false
  }
}
```

### Program.cs Registration

```csharp
// Add Authentication & Authorization
builder.Services.AddErpAuth(builder.Configuration);
```

## Usage

### Role-Based Authorization

```csharp
[Authorize(Roles = "Admin")]
[HttpGet("admin-only")]
public IActionResult AdminEndpoint()
{
    return Ok("Admin access granted");
}
```

### Permission-Based Authorization

Using the dynamic policy provider with `perm:` prefix:

```csharp
[Authorize(Policy = "perm:stock.read")]
[HttpGet("stock")]
public IActionResult GetStock()
{
    return Ok("Stock data");
}

[Authorize(Policy = "perm:order.write")]
[HttpPost("order")]
public IActionResult CreateOrder()
{
    return Ok("Order created");
}
```

### Accessing Current User

```csharp
public class MyController : ControllerBase
{
    private readonly ICurrentUser _currentUser;

    public MyController(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var userId = _currentUser.UserId;
        var email = _currentUser.Email;
        var roles = _currentUser.Roles;
        var permissions = _currentUser.Permissions;
        var isAuthenticated = _currentUser.IsAuthenticated;

        // Check specific role
        if (_currentUser.IsInRole("Admin"))
        {
            // Admin-specific logic
        }

        // Check specific permission
        if (_currentUser.HasPermission("stock.read"))
        {
            // Permission-specific logic
        }

        return Ok();
    }
}
```

## Debug Endpoints

### GET /auth/debug
Returns current user and tenant information (requires authentication).

**Response:**
```json
{
  "tenantId": "00000000-0000-0000-0000-000000000001",
  "user": {
    "id": "keycloak-user-id",
    "email": "user@example.com",
    "roles": ["Admin", "User"],
    "permissions": ["stock.read", "stock.write"],
    "isAuthenticated": true
  }
}
```

### GET /auth/role-test
Tests role-based authorization (requires `Admin` role).

**Response:**
```json
{
  "message": "role ok",
  "role": "Admin",
  "userRoles": ["Admin", "User"]
}
```

### GET /auth/perm-test
Tests permission-based authorization (requires `stock.read` permission).

**Response:**
```json
{
  "message": "perm ok",
  "permission": "stock.read",
  "userPermissions": ["stock.read", "stock.write"]
}
```

## Swagger Integration

JWT Bearer authentication is configured in Swagger UI:

1. Click the **Authorize** button in Swagger UI
2. Enter your JWT token (without "Bearer" prefix)
3. Click **Authorize**
4. All subsequent requests will include the Authorization header

## Testing

### Unit Tests

Two test classes verify the authentication infrastructure:

1. **PermissionPolicyProviderTests**: Verifies dynamic policy creation
   - Creates policies for `perm:` prefixed policy names
   - Extracts permission name correctly
   - Delegates non-permission policies to default provider

2. **PermissionHandlerTests**: Verifies permission requirement handling
   - Succeeds when user has required permission
   - Fails when user lacks permission
   - Handles various permissions correctly

Run tests:
```bash
dotnet test tests/ErpCloud.BuildingBlocks.Auth.Tests/
```

**Test Results:**
```
Başarılı!  - Başarısız: 0, Başarılı: 13, Atlanan: 0, Toplam: 13
```

## Integration with Multi-Tenant Architecture

The authentication system works seamlessly with the tenant isolation middleware:

1. **Authentication** validates JWT token and creates ClaimsPrincipal
2. **Authorization** verifies roles and permissions
3. **TenantMiddleware** extracts `tenant_id` from JWT claims
4. **CurrentUser** provides easy access to user information
5. **TenantContext** provides tenant isolation

### Middleware Order

```csharp
app.UseAuthentication();  // 1. Authenticate JWT
app.UseAuthorization();   // 2. Authorize based on roles/permissions
app.UseTenantContext();   // 3. Extract and validate tenant
```

## Security Considerations

1. **HTTPS in Production**: Set `RequireHttpsMetadata: true` in production
2. **Tenant Isolation**: The `tenant_id` claim is **required** and enforced by TenantMiddleware
3. **Token Validation**: Tokens are validated against Keycloak's public keys
4. **Permission Model**: Permissions are read from JWT tokens (no database lookup in this phase)
5. **Bypass Logging**: All tenant bypass operations are automatically logged for audit

## Example Keycloak Configuration

### 1. Create Realm
```
Name: erp-cloud
```

### 2. Create Client
```
Client ID: erp-cloud-api
Access Type: bearer-only
Valid Redirect URIs: http://localhost:5000/*
```

### 3. Create Roles
```
- Admin
- User
- Manager
```

### 4. Create Custom Claim Mapper for Permissions

**Mapper Type**: User Attribute
- **Name**: permissions
- **User Attribute**: permissions
- **Token Claim Name**: permissions
- **Claim JSON Type**: String[]
- **Add to ID token**: Yes
- **Add to access token**: Yes

### 5. Create Custom Claim Mapper for Tenant ID

**Mapper Type**: User Attribute
- **Name**: tenant_id
- **User Attribute**: tenant_id
- **Token Claim Name**: tenant_id
- **Claim JSON Type**: String
- **Add to ID token**: Yes
- **Add to access token**: Yes

### 6. Assign Roles and Attributes to Users

For each user, set:
- **Roles**: Admin, User, etc.
- **Attributes**:
  - `tenant_id`: `00000000-0000-0000-0000-000000000001`
  - `permissions`: `["stock.read","stock.write","order.read"]`

## Implementation Details

### Current User Parsing

The `CurrentUser` implementation uses lazy loading and defensive parsing:

```csharp
// User ID from 'sub' claim (priority order)
var userId = claims.FindFirst("sub")?.Value
    ?? claims.FindFirst(ClaimTypes.NameIdentifier)?.Value
    ?? claims.FindFirst("user_id")?.Value
    ?? string.Empty;

// Roles from Keycloak realm_access.roles (JSON parsing)
var realmAccessClaim = claims.FindFirst("realm_access")?.Value;
var realmAccess = JsonSerializer.Deserialize<JsonElement>(realmAccessClaim);
var roles = realmAccess.GetProperty("roles").EnumerateArray()
    .Select(r => r.GetString())
    .ToList();

// Fallback: standard role claims
roles.AddRange(claims.FindAll(ClaimTypes.Role).Select(c => c.Value));

// Permissions from custom claim
var permissionsClaim = claims.FindFirst("permissions")?.Value;
var permissions = JsonSerializer.Deserialize<string[]>(permissionsClaim);
```

### Dynamic Policy Provider

The `PermissionPolicyProvider` enables clean authorization syntax:

```csharp
// Instead of defining policies upfront:
options.AddPolicy("stock.read", policy => 
    policy.RequirePermission("stock.read"));

// Just use the dynamic provider:
[Authorize(Policy = "perm:stock.read")]
```

The provider detects the `perm:` prefix and creates policies on-demand.

## Build and Test Status

```
✅ Build Status: Successful
   - 0 Errors
   - 0 Warnings
   - 15 Projects

✅ Test Status: All Passing
   - 13 Tests Passed
   - 0 Tests Failed
   - 0 Tests Skipped
```

## Acceptance Criteria ✅

- [x] JWT bearer authentication configured and running
- [x] `/auth/debug` endpoint returns correct claims for authenticated requests
- [x] Roles are parsed from Keycloak `realm_access.roles` format
- [x] `[Authorize(Roles="Admin")]` works correctly when role claim is present
- [x] `[Authorize(Policy="perm:stock.read")]` works correctly when permission claim is present
- [x] Swagger Bearer authentication is functional
- [x] Unit tests pass (13 tests, 0 failures)
- [x] Build successful with 0 errors, 0 warnings
- [x] Integration with existing tenant middleware verified

## Related Documentation

- [Multi-Tenant Architecture](MULTI_TENANT_ARCHITECTURE.md)
- [Outbox Messaging](OUTBOX_MESSAGING.md)
