# Demo Tenant için JWT Token Oluştur
# TenantId: a1b2c3d4-e5f6-7890-abcd-ef1234567890

$body = @{
    tenantId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
    userId = "00000000-0000-0000-0000-000000000001"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5039/api/test/generate-token" `
    -Method Get `
    -ContentType "application/json" `
    -Body @{
        tenantId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
        userId = "00000000-0000-0000-0000-000000000001"
    }

Write-Host "=== DEMO TENANT TOKEN ===" -ForegroundColor Green
Write-Host ""
Write-Host "Tenant ID: $($response.tenant_id)" -ForegroundColor Cyan
Write-Host "User ID: $($response.user_id)" -ForegroundColor Cyan
Write-Host ""
Write-Host "TOKEN:" -ForegroundColor Yellow
Write-Host $response.token
Write-Host ""
Write-Host "Login sayfasına bu token'ı yapıştırın:" -ForegroundColor Green
Write-Host "http://localhost:1420/login" -ForegroundColor Cyan
