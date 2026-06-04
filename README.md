# BEScanCV Backend

Backend API cho dự án BEScanCV — xây dựng bằng ASP.NET Core, PostgreSQL, JWT Authentication.

---

## Công nghệ sử dụng

- **.NET 10** — Framework chính
- **ASP.NET Core Web API** — REST API
- **Entity Framework Core** — ORM
- **PostgreSQL** — Database
- **JWT** — Authentication
- **BCrypt** — Hash password
- **FluentValidation** — Validate request
- **AutoMapper** — Map Entity ↔ DTO
- **MailKit** — Gửi email
- **Swagger** — API documentation

---

## Cấu trúc dự án

```
BEScanCV/
├── BEScanCV.API/            # Startup project — Controllers, Middlewares, Program.cs
├── BEScanCV.Application/    # Business logic — Services, DTOs, Interfaces, Validators
├── BEScanCV.Domain/         # Core — Entities, Enums, ValueObjects
├── BEScanCV.Infrastructure/ # Database — DbContext, Repositories, Migrations
└── BEScanCV.slnx
```

---

## Yêu cầu môi trường

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/)
- Visual Studio 2022 hoặc VS Code

---

## Cài đặt và chạy

### 1. Clone project

```bash
git clone https://github.com/your-username/BEScanCV.git
cd BEScanCV
```

### 2. Cấu hình database

Tạo file `appsettings.Development.json` trong thư mục `BEScanCV.API/` dựa theo file mẫu:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=BEScanCV;Username=postgres;Password=yourpassword"
  },
  "Jwt": {
    "SecretKey": "your-secret-key-minimum-32-characters",
    "Issuer": "BEScanCV",
    "Audience": "BEScanCV",
    "ExpiryMinutes": 60
  },
  "Email": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

### 3. Restore package

```bash
dotnet restore
```

### 4. Chạy migration

```bash
dotnet ef migrations add InitialCreate --project BEScanCV.Infrastructure --startup-project BEScanCV.API
dotnet ef database update --project BEScanCV.Infrastructure --startup-project BEScanCV.API
```

### 5. Chạy project

```bash
dotnet run --project BEScanCV.API
```

Hoặc bấm **F5** trong Visual Studio.

---

## API Documentation

Sau khi chạy, truy cập Swagger UI tại:

```
https://localhost:{port}/swagger
```

---

## Các tính năng chính

- Đăng ký tài khoản
- Đăng nhập bằng Email + Password
- Xác thực JWT
- Lấy lại mật khẩu qua Email
- CRUD người dùng

---

## Project References

```
API  →  Application  +  Infrastructure
Application  →  Domain
Infrastructure  →  Application  +  Domain
```
