# CLAUDE.md - Project Rules

## Tổng quan dự án

BEScanCV là backend API cho hệ thống quản lý và tìm kiếm CV, xây dựng bằng ASP.NET Core Web API, Entity Framework Core và PostgreSQL.

Code hiện tại tập trung vào:

- Lưu trữ thông tin CV đã được trích xuất.
- Lưu thông tin file CV, người upload và kỹ năng của ứng viên.
- Tìm kiếm CV bằng câu truy vấn tự nhiên thông qua AI Search Query Parser.
- Lấy danh sách CV có phân trang và lọc cơ bản.

Luôn ưu tiên giữ kiến trúc phân tầng hiện có: API -> Application -> Domain, Infrastructure -> Application + Domain.

---

## Stack công nghệ

- Backend: .NET 10, C#, ASP.NET Core Web API
- Database: PostgreSQL
- ORM: Entity Framework Core
- API documentation: Swagger / OpenAPI
- External AI service: HTTP API parse search query
- Main package hiện có: Npgsql.EntityFrameworkCore.PostgreSQL, Swashbuckle.AspNetCore, AutoMapper, FluentValidation, BCrypt.Net-Next

---

## Cấu trúc thư mục

```text
BEScanCV/
├── BEScanCV.API/                 # Startup project, Program.cs, Controllers, API response wrapper
│   ├── Common/
│   ├── Controllers/
│   ├── Properties/
│   ├── Program.cs
│   └── appsettings.json
├── BEScanCV.Application/         # DTOs, interfaces, business services, application exceptions
│   ├── DTOS/
│   ├── Exceptions/
│   ├── Interfaces/
│   │   └── Repositories/
│   ├── Services/
│   └── DependencyInjection.cs
├── BEScanCV.Domain/              # Core entities
│   └── Entities/
├── BEScanCV.Infrastructure/      # DbContext, repositories, migrations, external service clients
│   ├── Data/
│   ├── Migrations/
│   ├── Options/
│   ├── Repositories/
│   ├── Services/
│   └── DependencyInjection.cs
└── BEScanCV.slnx
```

---

## Quy tắc kiến trúc

### API layer

- Controller chỉ nhận request, validate đầu vào đơn giản, gọi service và trả response.
- Không đặt business logic, query database hoặc gọi external service trực tiếp trong Controller.
- Response thành công nên dùng `ApiResponse<T>`.
- Route hiện tại dùng prefix `api/v1`.
- Controller đặt trong `BEScanCV.API/Controllers`.

Ví dụ:

```csharp
[ApiController]
[Route("api/v1/cvs/search")]
public sealed class CvSearchController(ICvSearchService cvSearchService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CvSearchResponse>>> Search(
        [FromBody] CvSearchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await cvSearchService.SearchAsync(request, cancellationToken);
        return Ok(new ApiResponse<CvSearchResponse>(response));
    }
}
```

### Application layer

- Chứa business logic, DTO, interface service, interface repository.
- Service không phụ thuộc trực tiếp vào `BEScanCvDbContext`.
- Service làm việc thông qua repository interface hoặc interface external service.
- Không expose entity trực tiếp ra API, luôn map sang DTO.
- Logic phân trang, ranking, filtering thuộc Application layer.

### Domain layer

- Chỉ chứa entity và logic domain thuần nếu có.
- Không reference Application, Infrastructure hoặc API.
- Entity không nên chứa logic phụ thuộc database, HTTP, configuration hoặc framework web.

### Infrastructure layer

- Chứa EF Core `DbContext`, repository implementation, migration, options, HTTP client gọi external AI service.
- Infrastructure được phép phụ thuộc Application và Domain.
- Không đưa business rule phức tạp vào repository; repository chỉ truy xuất và lưu dữ liệu.
- External integration phải đi qua interface ở Application layer, ví dụ `ISearchQueryParser`.

---

## Dependency Injection

- Đăng ký Application service trong `BEScanCV.Application/DependencyInjection.cs`.
- Đăng ký DbContext, repositories, options và external clients trong `BEScanCV.Infrastructure/DependencyInjection.cs`.
- `Program.cs` chỉ gọi:

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

Khi thêm service mới:

1. Tạo interface trong `BEScanCV.Application/Interfaces`.
2. Tạo implementation trong `BEScanCV.Application/Services`.
3. Đăng ký trong `AddApplication`.

Khi thêm repository mới:

1. Tạo interface trong `BEScanCV.Application/Interfaces/Repositories`.
2. Tạo implementation trong `BEScanCV.Infrastructure/Repositories`.
3. Đăng ký trong `AddInfrastructure`.

---

## Quy tắc đặt tên

### File và class

- Controller: `CvSearchController.cs`, `CvGetAllController.cs`
- Service interface: `ICvSearchService.cs`
- Service implementation: `CvSearchService.cs`
- Repository interface: `ICvInfoRepository.cs`
- Repository implementation: `CvInfoRepository.cs`
- DTO request: `CvSearchRequest.cs`
- DTO response: `CvSearchResponse.cs`
- Options: `AiServiceOptions.cs`
- Exception: `AiParserException.cs`

### C# naming

```csharp
// Public method: PascalCase
public async Task<CvSearchResponse> SearchAsync(...) { }

// Local variable: camelCase
var searchTerm = Normalize(request.Search);

// Private field: _camelCase
private readonly AiServiceOptions _options;

// Constant: PascalCase hoặc UPPER_SNAKE_CASE, ưu tiên theo style file hiện tại
private const int PageSize = 10;
```

### Database naming

- Table: `snake_case`, số nhiều. Ví dụ: `users`, `cv_files`, `cv_infos`, `cv_skills`.
- Column: `snake_case`. Ví dụ: `full_name`, `created_at`, `cv_file_id`.
- Foreign key column: theo schema hiện tại. Lưu ý `cv_skills` đang dùng `cv_infos_id`, không phải `cv_info_id`.
- EF mapping phải đặt trong `BEScanCvDbContext.OnModelCreating`.

---

## Quy tắc code

### 1. Luôn dùng async/await cho I/O

```csharp
public async Task<IReadOnlyCollection<CvInfo>> GetWithSkillsAsync(
    CancellationToken cancellationToken = default)
{
    return await dbContext.CvInfos
        .AsNoTracking()
        .Include(cvInfo => cvInfo.CvSkills)
        .ToListAsync(cancellationToken);
}
```

Không dùng `.Result`, `.Wait()` hoặc truy vấn sync với database.

### 2. Luôn truyền CancellationToken

- Controller nhận `CancellationToken`.
- Service và repository truyền tiếp token xuống các call async.
- External HTTP call cũng phải dùng token.

### 3. Không expose entity trực tiếp

Đúng:

```csharp
return new CvSearchResultDto(
    cv.FullName,
    cv.Email,
    cv.CvFileId,
    candidateSkills,
    cv.CreatedAt,
    uploader);
```

Sai:

```csharp
return cvInfo;
```

### 4. Giữ response thống nhất

Response thành công:

```json
{
  "data": {},
  "message": "OK",
  "success": true,
  "statusCode": 200
}
```

Response phân trang:

```json
{
  "items": [],
  "meta": {
    "total": 0,
    "page": 1,
    "limit": 10,
    "totalPages": 0
  }
}
```

Khi thêm endpoint mới, ưu tiên trả về DTO có cấu trúc rõ ràng và bọc bằng `ApiResponse<T>`.

### 5. Validate input ở ranh giới API

- Validate request null, chuỗi rỗng, page nhỏ hơn 1, limit nhỏ hơn hoặc bằng 0.
- Chuẩn hóa page/limit trong Application service nếu cần fallback.
- Không tin dữ liệu từ client.

### 6. Không hardcode config

Đúng:

```csharp
configuration[$"{AiServiceOptions.SectionName}:BaseUrl"]
```

Sai:

```csharp
var baseUrl = "https://example.com";
```

Các giá trị như connection string, API key, base URL, path external service phải nằm trong configuration, user secrets hoặc environment variables.

### 7. Không commit secret thật

- Không đưa password database thật, JWT secret, API key hoặc token vào source code.
- Nếu cần ví dụ cấu hình, dùng placeholder.
- `appsettings.Development.json` nên dùng cho local và không chứa secret production.

### 8. Query database có chủ đích

- Dùng `AsNoTracking()` cho query chỉ đọc.
- Dùng `Include` khi cần navigation property.
- Không load toàn bộ bảng nếu có thể filter/paginate ở database.
- Với dữ liệu lớn, ưu tiên chuyển filter/pagination xuống EF query thay vì xử lý toàn bộ trong memory.

### 9. Transaction khi thao tác nhiều bảng

Khi một use case ghi nhiều bảng và cần atomicity, dùng transaction ở Infrastructure hoặc unit-of-work phù hợp.

```csharp
await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
try
{
    // Save multiple changes
    await transaction.CommitAsync(cancellationToken);
}
catch
{
    await transaction.RollbackAsync(cancellationToken);
    throw;
}
```

---

## Quy tắc DTO và JSON

- DTO đặt trong `BEScanCV.Application/DTOS`.
- Dùng `[JsonPropertyName]` khi API contract cần snake_case hoặc tên field cụ thể.
- Không đặt EF attributes hoặc database logic trong DTO.
- Request DTO nên có default hợp lý cho pagination:

```csharp
public int Page { get; set; } = 1;
public int Limit { get; set; } = 10;
```

---

## Schema DB hiện tại

```text
users
- id
- full_name
- email
- password_hash
- role
- status
- created_at
- updated_at

refresh_tokens
- id
- user_id
- token_hash
- expires_at
- revoked_at
- created_at

cv_files
- id
- uploaded_by
- original_file_name
- file_url
- file_type
- file_size
- ai_document_id
- created_at
- updated_at

cv_infos
- id
- cv_file_id
- full_name
- email
- phone
- position
- date_of_birth
- address
- summary
- educations
- certifications
- created_at
- updated_at
- status

cv_skills
- id
- cv_infos_id
- name
- years_of_experience
```

---

## AI Search Query Parser

`ISearchQueryParser` là abstraction ở Application layer.

Implementation hiện tại:

- `AiSearchQueryParserClient`: gọi external HTTP API.
- `FakeSearchQueryParser`: parser giả để test/thử nghiệm thủ công.

Quy tắc:

- Không gọi AI service trực tiếp trong controller hoặc application service.
- Nếu external service trả lỗi, ném `AiParserException` để API layer chuyển thành response lỗi phù hợp.
- Payload hiện tại gửi lên AI parser có dạng:

```json
{
  "text": "query của người dùng"
}
```

- Kết quả parser được map thành `CvSearchCriteriaDto`.
- Các field search được normalize trước khi match.

---

## Quy tắc migration

- Migration thuộc `BEScanCV.Infrastructure/Migrations`.
- Khi thay đổi entity hoặc mapping, tạo migration bằng startup project API:

```bash
dotnet ef migrations add MigrationName --project BEScanCV.Infrastructure --startup-project BEScanCV.API
dotnet ef database update --project BEScanCV.Infrastructure --startup-project BEScanCV.API
```

- Không sửa tay migration cũ đã apply ở môi trường chung, trừ khi team thống nhất reset database.
- Luôn kiểm tra snapshot sau khi thêm migration.

---

## Quy tắc bảo mật

- Không expose đường dẫn file thật nếu sau này thêm API download CV.
- Không tin MIME type từ client khi upload file; validate extension và magic bytes.
- File upload nên đổi tên bằng UUID hoặc tên sinh bởi server.
- Không log secret, password hash, token, API key.
- Nếu expose ID nội bộ ra public API, cân nhắc dùng UUID/public id riêng cho tài nguyên nhạy cảm.
- Không bật CORS allow-all ở production nếu frontend origin đã biết.

---

## Lệnh kiểm tra thường dùng

```bash
dotnet restore
dotnet build BEScanCV.slnx
dotnet run --project BEScanCV.API
```

Swagger local theo `launchSettings.json`:

```text
http://localhost:5226/swagger
https://localhost:7161/swagger
```

---

## Những điểm cần lưu ý trong code hiện tại

- README hiện không còn khớp hoàn toàn với code: README mô tả nhiều chức năng auth/user CRUD, còn code hiện tại chủ yếu là CV search và get-all.
- `appsettings.json` hiện có connection string local; tránh dùng secret thật trong repo.
- `CvSearchService` và `CvGetAllService` hiện đang load toàn bộ CV rồi filter/paginate trong memory. Nếu dữ liệu lớn, nên refactor sang query-level filtering/pagination.
- `CvGetAllService` có hàm `NormalizeFieldName` chưa được sử dụng.
- File `.http` hiện vẫn gọi `weatherforecast`, không khớp endpoint thật của dự án.

---

## Khi AI coding trong dự án này

Trước khi sửa code:

1. Đọc file liên quan trong đủ 4 layer nếu thay đổi chạm luồng API -> Service -> Repository -> DB.
2. Giữ đúng dependency direction hiện tại.
3. Không refactor ngoài phạm vi yêu cầu.
4. Không đổi schema database nếu không thật sự cần.
5. Không sửa migration hoặc config nhạy cảm khi chưa có lý do rõ ràng.
6. Sau khi sửa, chạy tối thiểu:

```bash
dotnet build BEScanCV.slnx
```

Nếu thêm logic quan trọng, nên thêm test project hoặc test phù hợp trước khi bàn giao.
