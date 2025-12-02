# UniMeet - Üniversite Etkinlik Yönetim Platformu

Üniversite kulüplerinin etkinliklerini yönetmesi ve öğrencilerin bu etkinliklere katılması için geliştirilmiş modern bir web uygulaması.

## 🚀 Özellikler

### Kullanıcı Özellikleri
- ✅ E-posta ile kayıt ve giriş sistemi (Doğuş Üniversitesi e-postası gerekli)
- ✅ E-posta doğrulama ve şifre sıfırlama
- ✅ Kulüplere üye olma
- ✅ Etkinlikleri görüntüleme ve katılım
- ✅ Favori etkinlikler
- ✅ Bildirim tercihleri yönetimi

### Kulüp Yöneticisi Özellikleri
- ✅ Etkinlik oluşturma, düzenleme ve iptal etme
- ✅ Kulüp profili yönetimi
- ✅ Üye yönetimi
- ✅ Otomatik e-posta bildirimleri

### Admin Özellikleri
- ✅ Kullanıcı yönetimi
- ✅ Kulüp yönetimi
- ✅ Bildirim logları ve istatistikleri

### Bildirim Sistemi
- 🔔 Yeni etkinlik oluşturulduğunda otomatik e-posta bildirimi
- 📧 HTML tabanlı profesyonel e-posta şablonları
- ⚙️ Kullanıcı bazlı bildirim tercihleri
- 📊 Bildirim loglama ve retry mekanizması

## 🛠️ Teknolojiler

### Backend
- **ASP.NET Core 9.0** - Web API
- **Entity Framework Core** - ORM
- **SQL Server** - Veritabanı
- **JWT** - Authentication
- **SMTP** - E-posta gönderimi

### Frontend
- **React** - UI Framework
- **Vite** - Build Tool
- **React Router** - Routing
- **Axios** - HTTP Client

## 📋 Gereksinimler

- .NET 9.0 SDK
- SQL Server (LocalDB veya SQL Server Express)
- Node.js 18+
- npm veya yarn

## 🔧 Kurulum

### 1. Repository'yi Klonlayın
```bash
git clone https://github.com/HmzT270/The-UniMeet.git
cd The-UniMeet
```

### 2. Backend Kurulumu

```bash
cd UniMeetApi

# appsettings.json dosyasını düzenleyin
# - ConnectionString'i güncelleyin
# - JWT Key'i değiştirin
# - SMTP ayarlarını yapın

# Migration'ları uygulayın
dotnet ef database update

# Backend'i başlatın
dotnet run
```

Backend: http://localhost:5062 adresinde çalışacak

### 3. Frontend Kurulumu

```bash
cd ../client

# Bağımlılıkları yükleyin
npm install

# Frontend'i başlatın
npm run dev
```

Frontend: http://localhost:5173 adresinde çalışacak

## ⚙️ Yapılandırma

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=UniMeetDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "AllowedEmailDomain": "dogus.edu.tr",
  "Jwt": {
    "Issuer": "UniMeet",
    "Audience": "UniMeetClient",
    "Key": "CHANGE-ME-TO-SECURE-KEY-AT-LEAST-32-CHARS"
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromAddress": "noreply@unimeet.local",
    "FromName": "UniMeet"
  }
}
```

## 📚 API Endpoints

### Authentication
```
POST /api/auth/request-verification  - E-posta doğrulama isteği
POST /api/auth/verify-email          - E-posta doğrulama
POST /api/auth/set-password          - Şifre belirleme
POST /api/auth/login                 - Giriş yapma
POST /api/auth/request-password-reset - Şifre sıfırlama isteği
POST /api/auth/reset-password        - Şifre sıfırlama
GET  /api/auth/notification-preferences - Bildirim tercihleri
PUT  /api/auth/notification-preferences - Bildirim tercihleri güncelleme
```

### Events
```
GET    /api/events           - Tüm etkinlikler
GET    /api/events/{id}      - Etkinlik detayı
POST   /api/events           - Yeni etkinlik (Manager)
PUT    /api/events/{id}      - Etkinlik güncelleme (Manager)
DELETE /api/events/{id}      - Etkinlik silme (Manager)
POST   /api/events/{id}/join - Etkinliğe katıl
DELETE /api/events/{id}/leave - Etkinlikten ayrıl
POST   /api/events/{id}/favorite - Favorilere ekle
DELETE /api/events/{id}/unfavorite - Favorilerden çıkar
```

### Clubs
```
GET    /api/clubs           - Tüm kulüpler
GET    /api/clubs/{id}      - Kulüp detayı
POST   /api/clubs           - Yeni kulüp (Admin)
PUT    /api/clubs/{id}      - Kulüp güncelleme (Manager/Admin)
DELETE /api/clubs/{id}      - Kulüp silme (Admin)
POST   /api/clubs/{id}/join - Kulübe katıl
DELETE /api/clubs/{id}/leave - Kulüpten ayrıl
```

### Admin
```
GET  /api/admin/users               - Tüm kullanıcılar
POST /api/admin/users/{id}/toggle   - Kullanıcı aktif/pasif
GET  /api/admin/notifications       - Bildirim logları
POST /api/admin/notifications/retry-failed - Başarısız bildirimleri tekrar dene
```

## 👤 Varsayılan Kullanıcılar

Sistem ilk çalıştırıldığında `SeedData.cs` dosyası ile örnek veriler oluşturulur.

## 🔒 Güvenlik

- JWT tabanlı authentication
- Şifre hashleme (HMACSHA256)
- E-posta doğrulama zorunluluğu
- Rol tabanlı yetkilendirme (Member, Manager, Admin)
- CORS yapılandırması

## 📝 Lisans

Bu proje eğitim amaçlı geliştirilmiştir.

## 📧 İletişim

Sorularınız için issue açabilirsiniz.
