# 🚀 ERP CLOUD MASAÜSTÜ UYGULAMASI - DEPLOYMENT REHBERİ

## 📋 İçindekiler
1. [Gereksinimler](#gereksinimler)
2. [Build İşlemi](#build-i̇şlemi)
3. [Installer Dağıtımı](#installer-dağıtımı)
4. [İlk Kurulum ve Yapılandırma](#i̇lk-kurulum-ve-yapılandırma)
5. [Sorun Giderme](#sorun-giderme)
6. [Güvenlik Notları](#güvenlik-notları)
7. [Güncellemeler](#güncellemeler)

---

## ⚙️ Gereksinimler

### Build Makinesi İçin

**Zorunlu:**
- Windows 10/11 (64-bit)
- [Node.js 18+](https://nodejs.org/) (LTS önerilir)
- [Rust](https://rustup.rs/)
- [Visual Studio Build Tools 2022](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022)

**Visual Studio Build Tools Kurulumu:**
```powershell
# Build Tools indirin ve şu bileşenleri seçin:
# - Desktop development with C++
# - MSVC v143 - VS 2022 C++ x64/x86 build tools
# - Windows 10/11 SDK
```

**Rust Kurulumu:**
```powershell
# PowerShell'de:
winget install --id Rustlang.Rustup

# veya https://rustup.rs/ adresinden indirin
# Kurulumdan sonra:
rustup default stable
```

**Kontrol:**
```powershell
node --version    # v18.0.0+
npm --version     # 9.0.0+
rustc --version   # 1.70.0+
cargo --version   # 1.70.0+
```

---

## 🏗️ Build İşlemi

### Adım 1: Proje Hazırlığı

```powershell
# Proje klasörüne gidin
cd C:\xampp\htdocs\projeler\ErpCloud\apps\admin-desktop

# Dependencies yükleyin
npm install
```

### Adım 2: Environment Variables Yapılandırması

`.env.production` dosyasını düzenleyin:

```bash
# Varsayılan API adresi (kullanıcı değiştirebilir)
VITE_API_BASE_URL=http://localhost:5039
```

> **NOT:** Bu sadece ilk açılış için varsayılan değerdir. Kullanıcı Ayarlar ekranından değiştirebilir.

### Adım 3: Versiyon Güncelleme

`package.json` ve `src-tauri/tauri.conf.json` dosyalarında versiyonu güncelleyin:

```json
{
  "version": "1.0.0"  // Semantic versioning: MAJOR.MINOR.PATCH
}
```

### Adım 4: Build Komutu

**Otomatik Build (Önerilen):**
```powershell
.\build-desktop.ps1
```

**Manuel Build:**
```powershell
# Frontend build
npm run build

# Tauri installer oluştur
npm run tauri build
```

### Adım 5: Build Çıktıları

Build tamamlandığında installer dosyaları şu konumda oluşur:

```
src-tauri/target/release/bundle/
├── msi/
│   └── ERP Cloud Yönetim_1.0.0_x64_tr-TR.msi     # MSI Installer
└── nsis/
    └── ERP Cloud Yönetim_1.0.0_x64-setup.exe     # NSIS Installer
```

**Dosya Boyutları:**
- MSI: ~15-25 MB
- NSIS: ~15-25 MB

---

## 📦 Installer Dağıtımı

### İki Installer Türü

**MSI (Önerilen):**
- ✅ Windows Installer standardı
- ✅ Group Policy ile dağıtılabilir
- ✅ Sessiz kurulum desteği: `msiexec /i installer.msi /quiet`
- ✅ Kurumsal ortamlar için ideal

**NSIS:**
- ✅ Daha küçük dosya boyutu
- ✅ Daha hızlı kurulum
- ✅ Özelleştirilebilir kurulum deneyimi

### Dağıtım Yöntemleri

**1. Manuel Dağıtım:**
```powershell
# Installer dosyasını paylaşılan ağ klasörüne kopyalayın
Copy-Item "src-tauri\target\release\bundle\msi\*.msi" -Destination "\\server\share\erpcloud\"
```

**2. Email ile Dağıtım:**
- Installer dosyasını ZIP içine koyun
- Kullanıcılara kurulum talimatlarıyla gönderin

**3. Web İndirme:**
- Installer dosyasını web sunucusuna yükleyin
- Kullanıcılara indirme linki verin

---

## 🎯 İlk Kurulum ve Yapılandırma

### Son Kullanıcı İçin Adımlar

**1. Installer Çalıştırma:**
```
Sağ tık → "Yönetici olarak çalıştır"
```

**2. Kurulum Yeri:**
```
Varsayılan: C:\Program Files\ERP Cloud Yönetim\
```

**3. İlk Açılış:**
Uygulama açıldığında varsayılan API adresi:
```
http://localhost:5039
```

**4. API Ayarlarını Yapılandırma:**

a) Ayarlar sayfasına gidin:
```
Sol menü → Ayarlar veya /settings
```

b) API Sunucu Adresini girin:
```
Örnek: http://192.168.1.100:5039
Örnek: https://api.sirket.com
```

c) "Bağlantıyı Test Et" butonuna tıklayın

d) Başarılı ise "Kaydet" butonuna tıklayın

**5. Giriş Yapma:**
```
Kullanıcı adı ve şifre ile sisteme giriş yapın
```

---

## 🔧 Sorun Giderme

### Problem: "API sunucusuna bağlanılamıyor"

**Çözüm:**
1. API sunucusunun çalıştığından emin olun:
   ```powershell
   # Backend sunucuda:
   curl http://localhost:5039/health
   ```

2. Firewall kurallarını kontrol edin:
   ```powershell
   # Windows Firewall
   New-NetFirewallRule -DisplayName "ERP Cloud API" -Direction Inbound -LocalPort 5039 -Protocol TCP -Action Allow
   ```

3. API adresinin doğru olduğunu kontrol edin:
   - Protokol dahil: `http://` veya `https://`
   - Port numarası: `:5039`
   - Örnek: `http://192.168.1.100:5039`

### Problem: "Uygulama açılmıyor"

**Çözüm:**
1. Windows Defender veya Antivirus'ü kontrol edin
2. Uygulamayı Yönetici olarak çalıştırın
3. Visual C++ Redistributables yükleyin:
   ```
   https://aka.ms/vs/17/release/vc_redist.x64.exe
   ```

### Problem: "Installer güvenlik uyarısı veriyor"

**Çözüm:**
- Normal bir durumdur (kod imzası yoksa)
- "Daha fazla bilgi" → "Yine de çalıştır" seçeneğini kullanın
- **Kurumsal:** Code Signing sertifikası alın ve installer'ı imzalayın

### Problem: "Ayarlar kaydedilmiyor"

**Çözüm:**
1. Uygulama klasörüne yazma izni olduğundan emin olun
2. Store dosyasını kontrol edin:
   ```
   %APPDATA%\com.erpcloud.admin\settings.json
   ```

---

## 🔒 Güvenlik Notları

### API Bağlantısı Güvenliği

**Üretim Ortamı İçin:**
- ✅ **HTTPS kullanın:** `https://api.sirket.com`
- ✅ **Geçerli SSL sertifikası:** Let's Encrypt veya ticari sertifika
- ❌ **HTTP kullanmayın:** Şifreler açık metin olarak gider!

### JWT Token Güvenliği

- Tokenlar Tauri Store'da şifrelenmiş olarak saklanır
- LocalStorage yerine Tauri Store kullanılır (daha güvenli)
- Otomatik logout: Token süresi dolunca

### Veri Güvenliği

- API üzerinden gelen tüm veriler encrypted channel'dan geçer (HTTPS)
- Yerel veritabanı yoktur (tüm veri backend'de)
- Krediler masaüstü uygulamada saklanmaz

---

## 🔄 Güncellemeler

### Manuel Güncelleme

**Kullanıcılar için:**
1. Yeni installer dosyasını indirin
2. Eski versiyonu kaldırmadan yeni installer'ı çalıştırın
3. Kurulum otomatik olarak eski versiyonu güncelleyecektir
4. Ayarlar korunur

### Otomatik Güncelleme (Gelecek Özellik)

Tauri Updater kullanılarak otomatik güncelleme eklenebilir:

```json
// tauri.conf.json
{
  "plugins": {
    "updater": {
      "active": true,
      "endpoints": [
        "https://updates.sirket.com/erpcloud/{{target}}/{{current_version}}"
      ],
      "dialog": true,
      "pubkey": "YOUR_PUBLIC_KEY"
    }
  }
}
```

---

## 📝 Deployment Checklist

### Build Öncesi

- [ ] Versiyon numarası güncellendi
- [ ] Varsayılan API adresi ayarlandı
- [ ] Tüm testler geçti
- [ ] Build makinesi gereksinimleri karşılandı
- [ ] Icon dosyaları mevcut

### Build Sonrası

- [ ] MSI installer oluştu
- [ ] NSIS installer oluştu
- [ ] Dosya boyutları makul (15-30 MB)
- [ ] Test kurulumu yapıldı

### Test Kurulumu

- [ ] Temiz makinede kurulum başarılı
- [ ] Uygulama açılıyor
- [ ] API bağlantısı yapılabiliyor
- [ ] Ayarlar kaydediliyor
- [ ] Tezgâh ekranı çalışıyor
- [ ] Login/logout çalışıyor

### Dağıtım

- [ ] Kullanıcı dokumentasyonu hazırlandı
- [ ] Kurulum talimatları yazıldı
- [ ] Destek kanalı belirlendi
- [ ] Rollback planı hazırlandı

---

## 📞 Destek

**Teknik Sorunlar:**
- Email: destek@erpcloud.com
- Tel: +90 XXX XXX XX XX

**Kurulum Yardımı:**
- Uzaktan destek: TeamViewer / AnyDesk
- Telefon desteği: Çalışma saatleri içinde

---

## 📄 Lisans

Copyright © 2026 ERP Cloud. Tüm hakları saklıdır.

---

**Son Güncelleme:** 6 Şubat 2026  
**Doküman Versiyonu:** 1.0.0
