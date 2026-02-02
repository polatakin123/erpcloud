# Keycloak Token Alma Script'i
# Bu script Keycloak'tan JWT token alır

$keycloakUrl = "http://localhost:8080/realms/erp-cloud/protocol/openid-connect/token"
$clientId = "erp-cloud-api"
$username = "admin"  # Kullanıcı adınızı girin
$password = "admin"  # Şifrenizi girin

Write-Host "Keycloak'tan token alınıyor..." -ForegroundColor Cyan
Write-Host "URL: $keycloakUrl" -ForegroundColor Gray
Write-Host "Client ID: $clientId" -ForegroundColor Gray
Write-Host "Username: $username" -ForegroundColor Gray

try {
    $body = @{
        grant_type = "password"
        client_id = $clientId
        username = $username
        password = $password
    }

    $response = Invoke-RestMethod -Uri $keycloakUrl -Method Post -Body $body -ContentType "application/x-www-form-urlencoded"
    
    $token = $response.access_token
    
    Write-Host "`n.NET Token basariyla alindi!" -ForegroundColor Green
    Write-Host "`nToken (kopyalayin):" -ForegroundColor Yellow
    Write-Host $token -ForegroundColor White
    
    # Token'i clipboard'a kopyala
    Set-Clipboard -Value $token
    Write-Host "`nToken panoya kopyalandi!" -ForegroundColor Green
    
    # Token bilgilerini goster
    Write-Host "`nToken Bilgileri:" -ForegroundColor Cyan
    $minutes = [math]::Round($response.expires_in / 60, 2)
    Write-Host "Expires in: $($response.expires_in) saniye ($minutes dakika)" -ForegroundColor Gray
    Write-Host "Token type: $($response.token_type)" -ForegroundColor Gray
    
    # Token'i dosyaya da kaydet
    $tokenFile = Join-Path $PSScriptRoot "token.txt"
    $token | Out-File -FilePath $tokenFile -Encoding UTF8
    Write-Host "`nToken dosyaya kaydedildi: $tokenFile" -ForegroundColor Green
    
} catch {
    Write-Host "`nHATA: Token alinamadi!" -ForegroundColor Red
    Write-Host "Hata mesaji: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "`nDetay: $responseBody" -ForegroundColor Yellow
    }
    
    Write-Host "`nCozum onerileri:" -ForegroundColor Yellow
    Write-Host "1. Keycloak'in calistigini kontrol edin: http://localhost:8080" -ForegroundColor Gray
    Write-Host "2. Kullanici adi ve sifrenin dogru oldugunu kontrol edin" -ForegroundColor Gray
    Write-Host "3. Client ID'nin dogru oldugunu kontrol edin: $clientId" -ForegroundColor Gray
    Write-Host "4. Realm adinin dogru oldugunu kontrol edin: erp-cloud" -ForegroundColor Gray
}
