using BEScanCV.Application;
using BEScanCV.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Định nghĩa tên Policy cho CORS
const string allOriginsPolicy = "AllowAllOrigins";

// Add services to the container.

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
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 3. Kích hoạt CORS Middleware 
app.UseCors(allOriginsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();