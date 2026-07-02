using BEScanCV.Application;
using BEScanCV.Infrastructure;
using BEScanCV.Infrastructure.Data;
using BEScanCV.Infrastructure.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using BEScanCV.Application.Interfaces;
using BEScanCV.API.Extensions;
using Resend;
using Microsoft.OpenApi;



var builder = WebApplication.CreateBuilder(args);

// 1. Định nghĩa tên Policy cho CORS
const string allOriginsPolicy = "AllowAllOrigins";

// 2. Thêm dịch vụ CORS vào DI Container
 builder.Services.AddCors(options =>
 {
     options.AddPolicy(allOriginsPolicy, policy =>
     {
         policy.SetIsOriginAllowed(_ => true)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
     });
 });

builder.Services.AddControllers();
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "BEScanCV API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'"
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", hostDocument: doc)] = new List<string>()
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BEScanCvDbContext>();
    var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDb");
    if (useInMemory)
    {
        dbContext.Database.EnsureCreated(); // InMemory: tạo schema trong RAM
    }
    else if (builder.Configuration.GetValue<bool>("Database:UseEnsureCreated"))
    {
        dbContext.Database.EnsureCreated();
    }
    else
    {
        dbContext.Database.Migrate();
    }

    if (builder.Configuration.GetValue<bool>("AdminSeed:Enabled"))
    {
        var email = builder.Configuration["AdminSeed:Email"];
        var password = builder.Configuration["AdminSeed:Password"];
        var fullName = builder.Configuration["AdminSeed:FullName"] ?? "System Admin";

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("AdminSeed:Email must be configured when AdminSeed is enabled.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("AdminSeed:Password must be configured when AdminSeed is enabled.");
        }

        await DatabaseSeeder.SeedAdminAsync(
            dbContext,
            email,
            password,
            fullName);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 4. Đọc forwarded headers từ ngrok (X-Forwarded-Host, X-Forwarded-Proto)
//    Giúp Request.Host và Request.Scheme phản ánh đúng URL ngrok
//    → pdf_url trả về sẽ là https://xxxx.ngrok-free.app/files/... thay vì localhost
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                     | ForwardedHeaders.XForwardedProto
                     | ForwardedHeaders.XForwardedHost,
    // Tin tưởng tất cả proxy (ngrok, load balancer...)
    KnownIPNetworks = { },
    KnownProxies = { }
});

app.UseHttpsRedirection();

// 3. Kích hoạt CORS Middleware
app.UseCors(allOriginsPolicy);
app.UseWebSockets();

// 4. Serve file PDF từ D:\PDFLocal dưới route /files
//    FE truy cập: http://<BE_IP>:<port>/files/<ten-file>.pdf
var localPdfFolder = app.Configuration["CvStorage:LocalPdfFolder"];
if (string.IsNullOrWhiteSpace(localPdfFolder))
{
    localPdfFolder = @"D:\PDFLocal";
}

if (!Directory.Exists(localPdfFolder))
    Directory.CreateDirectory(localPdfFolder);

var pdfStorageRoot = Path.GetFullPath(localPdfFolder)
    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(localPdfFolder),
    RequestPath = "/files",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append(
            "Access-Control-Allow-Origin",
            "*"
        );

        if (string.Equals(
                Path.GetExtension(ctx.File.Name),
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.ContentDisposition = "inline";
        }
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.Map("/ws/upload-progress/{batchId}", async (
    HttpContext httpContext,
    string batchId,
    WebSocketUploadProgressNotifier notifier) =>
{
    await notifier.HandleClientAsync(httpContext, batchId, httpContext.RequestAborted);
});

app.MapMethods("/files/{fileName}", ["GET", "HEAD"], IResult (string fileName) =>
{
    var safeFileName = Path.GetFileName(fileName);
    if (!string.Equals(fileName, safeFileName, StringComparison.Ordinal))
    {
        return Results.BadRequest();
    }

    var filePath = Path.GetFullPath(Path.Combine(pdfStorageRoot, safeFileName));
    if (!filePath.StartsWith(
            pdfStorageRoot + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest();
    }

    if (!System.IO.File.Exists(filePath))
    {
        return Results.NotFound();
    }

    var contentType = Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".doc" => "application/msword",
        _ => "application/octet-stream"
    };

    return Results.File(
        System.IO.File.OpenRead(filePath),
        contentType,
        enableRangeProcessing: true);
});

app.MapControllers();

app.Run();
