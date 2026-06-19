using BEScanCV.Application;
using BEScanCV.Infrastructure;
using BEScanCV.Infrastructure.Data;
using BEScanCV.Infrastructure.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// 1. Định nghĩa tên Policy cho CORS
const string allOriginsPolicy = "AllowAllOrigins";

// 2. Thêm dịch vụ CORS vào DI Container
builder.Services.AddCors(options =>
{
    options.AddPolicy(allOriginsPolicy, policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BEScanCvDbContext>();
    dbContext.Database.Migrate();
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
const string localPdfFolder = @"D:\PDFLocal";
if (!Directory.Exists(localPdfFolder))
    Directory.CreateDirectory(localPdfFolder);

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
    }
});

app.UseAuthorization();

app.Map("/ws/upload-progress/{batchId}", async (
    HttpContext httpContext,
    string batchId,
    WebSocketUploadProgressNotifier notifier) =>
{
    await notifier.HandleClientAsync(httpContext, batchId, httpContext.RequestAborted);
});

app.MapControllers();

app.Run();
