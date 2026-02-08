# ================================================
# ERP CLOUD MASAÜSTÜ UYGULAMASI BUILD SCRİPTİ
# ================================================
# Bu script Windows için Tauri installer oluşturur
# Gereksinimler: Node.js, Rust, Visual Studio Build Tools
#
# Kullanım:
#   .\build-desktop.ps1
#
# Çıktı:
#   src-tauri\target\release\bundle\msi\
#   src-tauri\target\release\bundle\nsis\
# ================================================

Write-Host "`n=== ERP CLOUD MASAÜSTÜ UYGULAMASI BUILD ===" -ForegroundColor Cyan
Write-Host "Başlangıç: $(Get-Date -Format 'HH:mm:ss')`n" -ForegroundColor Gray

# 1. Gerekli araçları kontrol et
Write-Host "[1/5] Gerekli araçlar kontrol ediliyor..." -ForegroundColor Yellow

# Node.js kontrolü
try {
    $nodeVersion = node --version
    Write-Host "  ✓ Node.js: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "  ✗ Node.js bulunamadı! Lütfen Node.js yükleyin." -ForegroundColor Red
    Write-Host "  İndirme: https://nodejs.org/" -ForegroundColor Yellow
    exit 1
}

# Rust kontrolü
try {
    $rustVersion = rustc --version
    Write-Host "  ✓ Rust: $rustVersion" -ForegroundColor Green
} catch {
    Write-Host "  ✗ Rust bulunamadı! Lütfen Rust yükleyin." -ForegroundColor Red
    Write-Host "  İndirme: https://rustup.rs/" -ForegroundColor Yellow
    exit 1
}

# 2. Dependencies kontrol
Write-Host "`n[2/5] Bağımlılıklar kontrol ediliyor..." -ForegroundColor Yellow

if (-not (Test-Path "node_modules")) {
    Write-Host "  → npm install çalıştırılıyor..." -ForegroundColor Gray
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ✗ npm install başarısız!" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "  ✓ node_modules mevcut" -ForegroundColor Green
}

# 3. Frontend build
Write-Host "`n[3/5] Frontend build yapılıyor..." -ForegroundColor Yellow
Write-Host "  → npm run build" -ForegroundColor Gray

npm run build

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Frontend build başarısız!" -ForegroundColor Red
    exit 1
}

Write-Host "  ✓ Frontend build tamamlandı" -ForegroundColor Green

# 4. Tauri build
Write-Host "`n[4/5] Tauri installer oluşturuluyor..." -ForegroundColor Yellow
Write-Host "  → npm run tauri build" -ForegroundColor Gray
Write-Host "  Bu işlem birkaç dakika sürebilir..." -ForegroundColor Gray

npm run tauri build

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Tauri build başarısız!" -ForegroundColor Red
    exit 1
}

Write-Host "  ✓ Tauri build tamamlandı" -ForegroundColor Green

# 5. Çıktı dosyalarını göster
Write-Host "`n[5/5] Build çıktıları:" -ForegroundColor Yellow

$bundlePath = "src-tauri\target\release\bundle"

if (Test-Path "$bundlePath\msi") {
    Write-Host "`n  📦 MSI Installer:" -ForegroundColor Cyan
    Get-ChildItem "$bundlePath\msi\*.msi" | ForEach-Object {
        $size = [math]::Round($_.Length / 1MB, 2)
        Write-Host "     → $($_.Name) ($size MB)" -ForegroundColor White
        Write-Host "       Konum: $($_.FullName)" -ForegroundColor Gray
    }
}

if (Test-Path "$bundlePath\nsis") {
    Write-Host "`n  📦 NSIS Installer:" -ForegroundColor Cyan
    Get-ChildItem "$bundlePath\nsis\*.exe" | ForEach-Object {
        $size = [math]::Round($_.Length / 1MB, 2)
        Write-Host "     → $($_.Name) ($size MB)" -ForegroundColor White
        Write-Host "       Konum: $($_.FullName)" -ForegroundColor Gray
    }
}

Write-Host "`n=== BUILD TAMAMLANDI ===" -ForegroundColor Green
Write-Host "Bitiş: $(Get-Date -Format 'HH:mm:ss')`n" -ForegroundColor Gray

Write-Host "Sonraki Adımlar:" -ForegroundColor Yellow
Write-Host "  1. Installer dosyalarını test edin" -ForegroundColor White
Write-Host "  2. Farklı makinelerde kurulum yapın" -ForegroundColor White
Write-Host "  3. API sunucu bağlantısını test edin" -ForegroundColor White
Write-Host "  4. Kullanıcı dokumentasyonunu hazırlayın`n" -ForegroundColor White
