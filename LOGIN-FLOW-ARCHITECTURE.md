# 🎯 FINAL LOGIN FLOW ARCHITECTURE

## A) Complete Login Flow (Step-by-Step)

```
1. USER OPENS APP
   → Frontend checks localStorage["authToken"]
   → If exists: Use it (no auto-validate)
   → If missing: Redirect to /login

2. USER ENTERS CREDENTIALS
   → POST /api/auth/login { username, password }
   → Backend validates against Users table
   → Returns: { token: "JWT...", expiresAt: "ISO-date" }

3. FRONTEND SAVES TOKEN
   → localStorage.setItem("authToken", token)
   → Navigate to /dashboard (or intended route)

4. ALL API CALLS
   → Axios interceptor adds: Authorization: Bearer {token}
   → Backend validates JWT → extracts tenant_id/user_id
   → If 401: Clear localStorage → Redirect /login

5. USER CLICKS LOGOUT
   → localStorage.removeItem("authToken")
   → Navigate to /login
   → (Backend stateless, no server logout needed)
```

---

## B) Backend Responsibilities

### 1. Login Endpoint (`/api/auth/login`)

```csharp
RESPONSIBILITIES:
✅ Validate username + password (bcrypt hash check)
✅ Fetch user's tenant_id from Users table
✅ Generate JWT with claims:
   - tenant_id (GUID)
   - user_id (GUID)
   - username (string)
   - policies (array - örn: ["admin", "stock.write"])
✅ Return JSON: { token, expiresAt }
❌ Session management YAPMA (stateless)
❌ Refresh token YAPMA (şimdilik - phase 2)
```

### 2. Demo User Strategy

```sql
-- Migration veya Seed içinde:
INSERT INTO users (id, tenant_id, username, password_hash, email)
VALUES (
  '00000000-0000-0000-0000-000000000001',
  'a1b2c3d4-e5f6-7890-abcd-ef1234567890', -- demo tenant
  'demo',
  '$2a$11$...', -- BCrypt hash of "demo123"
  'demo@erpcloud.local'
);

-- Tenant da seed'lenmiş olmalı:
INSERT INTO tenants (id, name, code)
VALUES (
  'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
  'Demo Şirketi',
  'DEMO'
);
```

**Demo User Kuralları:**
- Gerçek user gibi DB'de
- Password BCrypt ile hash'lenmiş
- Bypass/fake kontrol YOK
- Production'da aynı kod çalışır, sadece demo user silinir

### 3. JWT Generation

```csharp
// JwtHelper.GenerateToken(User user)
var claims = new[]
{
    new Claim("tenant_id", user.TenantId.ToString()),
    new Claim("user_id", user.Id.ToString()),
    new Claim("username", user.Username),
    // Policies user roles'den gelir
    new Claim("policies", JsonSerializer.Serialize(user.Policies))
};

var token = new JwtSecurityToken(
    issuer: "ErpCloud",
    audience: "erp-cloud",
    claims: claims,
    expires: DateTime.UtcNow.AddDays(7), // 7 gün (production için)
    signingCredentials: creds
);
```

---

## C) Frontend Responsibilities

### 1. Auth State Management (SINGLE SOURCE OF TRUTH)

**Context: `AuthContext.tsx`**

```typescript
RESPONSIBILITIES:
✅ Read token from localStorage on mount
✅ Provide isAuthenticated boolean
✅ Provide login(token) function
✅ Provide logout() function
❌ Token validation YAPMA (backend'e bırak)
❌ User data parse YAPME (lazımsa backend'den /me endpoint)

RULE: Token varsa authenticated, yoksa değil. Başka kontrol YOK.
```

### 2. Route Guard (`PrivateRoute.tsx`)

```typescript
LOGIC:
- Check: localStorage.getItem("authToken")
- If exists: Render children
- If missing: <Navigate to="/login" replace />

NO API CALL, NO VALIDATION, INSTANT DECISION.
```

### 3. API Interceptor (`api-client.ts`)

```typescript
REQUEST INTERCEPTOR:
✅ Token varsa ekle: Authorization: Bearer {token}
✅ Token yoksa ekleme (public endpoints için)

RESPONSE INTERCEPTOR:
✅ 401 gelirse:
   - localStorage.removeItem("authToken")
   - window.location.href = "/login" (veya router.navigate)
✅ Diğer hatalar: Normal error handling
❌ Retry logic YAPMA (infinite loop riski)
```

### 4. Login Page Flow

```typescript
ON SUBMIT:
1. POST /api/auth/login { username, password }
2. Success:
   - localStorage.setItem("authToken", response.token)
   - navigate("/dashboard")
3. Error (401):
   - Show: "Kullanıcı adı veya şifre hatalı"
4. Error (500):
   - Show: "Sunucu hatası, tekrar deneyin"
```

---

## D) En Sık Yapılan 5 Hata ve Önleme

### ❌ HATA 1: Token'ı validate etmeye çalışmak (frontend'de)

**Neden kötü:** JWT decode edip expiry check yapmak gereksiz komplekslik.  
**Doğrusu:** Backend 401 dönene kadar token geçerlidir. Frontend sadece saklar/gönderir.

### ❌ HATA 2: Her route değişiminde API'ye "am I logged in?" sorusu

**Neden kötü:** Gereksiz network, yavaş UX.  
**Doğrusu:** Token localStorage'da mı? Evet → logged in. Hayır → not logged in.

### ❌ HATA 3: 401 gelince retry yapmak

**Neden kötü:** Infinite loop, token hâlâ invalid.  
**Doğrusu:** 401 = logout + redirect. Retry YOK.

### ❌ HATA 4: Login success'te navigate ÖNCE, token kaydetme SONRA

**Neden kötü:** Dashboard yüklenirken token yok, guard tekrar login'e atar.  
**Doğrusu:** 1) Save token, 2) Navigate. Sıra kritik.

### ❌ HATA 5: Demo user için bypass logic yazmak

**Neden kötü:** Production'da unutulur, güvenlik açığı.  
**Doğrusu:** Demo user = gerçek user. DB'de seed'le, login flow aynı.

---

## 🚀 IMPLEMENTATION CHECKLIST

### Backend:
- [ ] `POST /api/auth/login` endpoint (username/password → JWT)
- [ ] BCrypt password hashing
- [ ] Demo user seed (migration or DbInitializer)
- [ ] JWT claims: tenant_id, user_id, username, policies

### Frontend:
- [ ] `AuthContext` (token storage + isAuthenticated)
- [ ] `PrivateRoute` (localStorage check only)
- [ ] `LoginPage` (form → API → save token → navigate)
- [ ] Axios request interceptor (add Authorization header)
- [ ] Axios response interceptor (401 → logout)
- [ ] Logout button (clear localStorage + navigate)

### Testing:
- [ ] Demo user ile login → dashboard görünür
- [ ] Logout → login'e atar
- [ ] Token sil (DevTools) → refresh → login'e atar
- [ ] 401 dön (backend stop et) → logout tetiklenir
- [ ] Demo user şifre yanlış → error message

---

## 💡 PRO TIP: "10 Saniye Kuralı"

Satışçı uygulamayı açtı:
1. Login ekranı görünür (< 1 saniye)
2. "demo" / "demo123" yazar (3 saniye)
3. Enter → JWT alındı, localStorage'a yazıldı (< 500ms)
4. Dashboard yüklendi, ürün ara/sat (< 2 saniye)

**TOPLAM: 7 saniye.** Hedef bu. Bypass/dev shortcut bundan DAHA YAVAŞ.

---

## 🔥 NEXT STEPS

Şimdi istersen:
1. **Backend login endpoint kodunu yazayım**
2. **Frontend AuthContext + interceptor kodunu yazayım**
3. **Demo user seed script'ini oluşturayım**

Hangisinden başlamak istersin?
