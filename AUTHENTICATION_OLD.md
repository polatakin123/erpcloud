# ErpCloud - Keycloak JWT Authentication & Authorization

## 🎯 Tamamlanan İşler

### BuildingBlocks.Auth Altyapısı
Keycloak ile JWT authentication ve role/permission-based authorization altyapısı tamamlandı.

#### ✅ Oluşturulan Dosyalar

1. **ICurrentUser.cs** - Current user interface
   - UserId, Email, Roles, Permissions özellikleri
   - IsInRole(), HasPermission() metodları

2. **CurrentUser.cs** - HttpContext'ten claim parsing
   - Keycloak `realm_access.roles` JSON parsing
   - Custom `permissions` claim parsing
   - Lazy loading ile performans optimizasyonu
   - Multiple fallback claim names

3. **PermissionRequirement.cs** - Authorization requirement
   - Permission-based policy için requirement

4. **PermissionHandler.cs** - Authorization handler
   - ICurrentUser.HasPermission() ile yetki kontrolü
   - Scoped lifetime (DI düzeltmesi yapıldı)

5. **AuthExtensions.cs** - Service registration extension
   - AddAuth(IConfiguration) metodu
   - JWT Bearer authentication setup
   - Keycloak Authority/Audience configuration
   - RequirePermission() policy builder
   - AddPermissionPolicy() helper metodlar
   - Keycloak realm_access.roles → ClaimTypes.Role mapping

#### ✅ API Konfigürasyonu

1. **Program.cs** güncellemeleri:
   - `builder.Services.AddAuth(builder.Configuration)` eklendi
   - Permission policies registered:
     - stock.read, stock.write
     - order.read, order.write
     - admin

2. **appsettings.json** - JWT configuration eklendi:
```json
{
  "Jwt": {
    "Authority": "",
    "Audience": "erp-cloud",
    "ValidateIssuer": true,
    "RequireHttpsMetadata": false,
    "SecretKey": "your-256-bit-secret-key-for-development-only-min-32-chars",
    "Issuer": "ErpCloud"
  }
}
```

#### ✅ Test Infrastructure

1. **JwtTestHelper.cs** - Test token generator
   - GenerateTestToken() with Keycloak structure
   - realm_access.roles JSON claim
   - permissions JSON array claim

2. **TestTokenController.cs** - Token generation endpoints
   - `GET /test/token` - Custom token
   - `GET /test/token/admin` - Admin token
   - `GET /test/token/readonly` - Read-only token

3. **AuthDebugController.cs** - Authentication debugging
   - `GET /auth/debug` - Current user bilgileri
   - `GET /auth/test-role/{roleName}` - Role testi
   - `GET /auth/test-permission/{permission}` - Permission testi
   - `GET /auth/test-policy-stock` - Policy testi (stock.read gerektirir)

## 🚀 Kullanım

### 1. Test Token Alma

```bash
# Admin token
curl http://localhost:5039/test/token/admin

# Read-only token
curl http://localhost:5039/test/token/readonly

# Custom token
curl "http://localhost:5039/test/token?email=user@test.com&roles=user,manager&permissions=stock.read,order.write"
```

### 2. Authentication Test

Token aldıktan sonra Authorization header ile istek yapın:

```bash
# Token değişkenine kaydet
$TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Current user bilgilerini al
curl -H "Authorization: Bearer $TOKEN" http://localhost:5039/auth/debug

# Role testi
curl -H "Authorization: Bearer $TOKEN" http://localhost:5039/auth/test-role/admin

# Permission testi
curl -H "Authorization: Bearer $TOKEN" http://localhost:5039/auth/test-permission/stock.read

# Policy testi (stock.read permission gerektirir)
curl -H "Authorization: Bearer $TOKEN" http://localhost:5039/auth/test-policy-stock
```

### 3. Swagger ile Test

1. Swagger UI'ı açın: http://localhost:5039/swagger
2. `/test/token/admin` endpoint'ini çağırın
3. Response'tan `token` değerini kopyalayın
4. "Authorize" butonuna tıklayın
5. `Bearer {token}` formatında yapıştırın
6. `/auth/debug` endpoint'ini test edin

## 🔐 Keycloak Entegrasyonu

### Development Mode (Şu anki)
- Symmetric key ile JWT validation
- Test token generation endpoints
- Mock authentication

### Production Mode (Keycloak ile)

appsettings.json güncelleme:
```json
{
  "Jwt": {
    "Authority": "https://your-keycloak.com/realms/your-realm",
    "Audience": "erp-cloud",
    "ValidateIssuer": true,
    "RequireHttpsMetadata": true
  }
}
```

AddAuth() otomatik olarak:
- ✅ Authority'den OIDC discovery yapacak
- ✅ Public key ile token validation yapacak
- ✅ realm_access.roles claim'lerini parse edecek
- ✅ Permission claim'lerini parse edecek

## 📋 Claim Structure

### Keycloak Token Claims:
```json
{
  "tenant_id": "11111111-1111-1111-1111-111111111111",
  "user_id": "22222222-2222-2222-2222-222222222222",
  "sub": "22222222-2222-2222-2222-222222222222",
  "email": "user@example.com",
  "realm_access": {
    "roles": ["admin", "user"]
  },
  "permissions": ["stock.read", "stock.write", "order.read"]
}
```

## 🎨 Authorization Kullanımı

### Controller'da Role-Based:
```csharp
[Authorize(Roles = "admin")]
public IActionResult AdminOnly() { }
```

### Controller'da Permission-Based:
```csharp
[Authorize(Policy = "stock.read")]
public IActionResult ViewStock() { }
```

### Code'da Programmatic Check:
```csharp
public class MyService
{
    private readonly ICurrentUser _currentUser;

    public MyService(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public void DoSomething()
    {
        if (_currentUser.HasPermission("stock.write"))
        {
            // Stock yazma işlemi
        }

        if (_currentUser.IsInRole("admin"))
        {
            // Admin işlemi
        }
    }
}
```

## 🐛 Çözülen Sorunlar

1. ❌ **DI Lifetime Hatası**: `PermissionHandler` singleton idi, `ICurrentUser` scoped
   - ✅ Çözüm: `PermissionHandler`'ı scoped olarak kayıt ettik

2. ❌ **Package Version Conflicts**: .NET 10 paketleri vs .NET 8 framework
   - ✅ Çözüm: Tüm paketleri 8.0.x sürümüne düşürdük

3. ❌ **Missing Method Exception**: IAuthorizationPolicyProvider.get_AllowsCachingPolicies()
   - ✅ Çözüm: Microsoft.AspNetCore.Authorization 8.0.11 kullanıldı

## 📦 Yüklenen Paketler

### BuildingBlocks.Auth:
- Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11
- Microsoft.AspNetCore.Authorization 8.0.11
- Microsoft.AspNetCore.Http.Abstractions 2.3.9
- Microsoft.Extensions.Configuration.Abstractions 8.0.0
- Microsoft.Extensions.DependencyInjection.Abstractions 8.0.2

### Api:
- Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11
- Microsoft.AspNetCore.Authorization 8.0.11

## ✅ Build Status

```
dotnet build
✅ 0 Errors
✅ 0 Warnings
```

## 🌐 Çalışan Endpoints

- `GET /` - Application info
- `GET /health` - Health check
- `GET /swagger` - Swagger UI
- `GET /test/token` - Custom token generation
- `GET /test/token/admin` - Admin token
- `GET /test/token/readonly` - Read-only token
- `GET /auth/debug` 🔒 - Current user info (requires auth)
- `GET /auth/test-role/{role}` 🔒 - Test role
- `GET /auth/test-permission/{perm}` 🔒 - Test permission
- `GET /auth/test-policy-stock` 🔒 - Test stock.read policy

🔒 = Requires Authorization header

## 🎯 Sonraki Adımlar

1. ✅ Multi-tenant isolation (tenant_id claim ile)
2. ✅ JWT authentication infrastructure
3. ✅ Role-based authorization
4. ✅ Permission-based authorization
5. ⏭️ Gerçek Keycloak instance ile test
6. ⏭️ Refresh token flow
7. ⏭️ Token revocation
8. ⏭️ Audit logging (user actions)
