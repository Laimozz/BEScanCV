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
        dbContext.Database.EnsureCreated(); // InMemory: tạo schema trong RAM
    else
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

app.UseAuthentication();
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
