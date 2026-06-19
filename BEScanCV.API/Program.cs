using BEScanCV.Application;
using BEScanCV.Infrastructure;
using BEScanCV.Infrastructure.Data;
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
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add email service (Resend)

var apiKey = builder.Configuration.GetSection("Resend")["ApiKey"];

Console.WriteLine(apiKey);

builder.Services.Configure<ResendClientOptions>(o =>
{
    o.ApiToken = apiKey;
});
builder.Services.AddHttpClient<IResend, ResendClient>();
builder.Services.AddScoped<IEmailService, ResendEmailService>();


// Add email service(Postmark)
// builder.Services.Configure<PostmarkSettings>(
// builder.Configuration.GetSection("Postmark"));
// builder.Services.AddScoped<IEmailService, PostmarkEmailService>();
builder.Services.AddControllers();
builder.Services.AddApplication();
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

// Problem with Resend: can only send mails to API key owner (trannguyenphuc1902@gmail.com)
// Problem with Postmark: have to verify domain

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

// 4. Serve file PDF từ D:\PDFLocal dưới route /files
//    FE truy cập: http://<BE_IP>:<port>/files/<ten-file>.pdf
const string localPdfFolder = @"D:\PDFLocal";
if (!Directory.Exists(localPdfFolder))
    Directory.CreateDirectory(localPdfFolder);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(localPdfFolder),
    RequestPath = "/files"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
