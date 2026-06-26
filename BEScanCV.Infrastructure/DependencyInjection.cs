using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Interfaces.Repositories;
using BEScanCV.Infrastructure.Data;
using BEScanCV.Infrastructure.Options;
using BEScanCV.Infrastructure.Repositories;
using BEScanCV.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BEScanCV.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var useInMemory = configuration.GetValue<bool>("UseInMemoryDb");
        if (useInMemory)
        {
            services.AddDbContext<BEScanCvDbContext>(options =>
                options.UseInMemoryDatabase("BEScanCvDev"));
        }
        else
        {
            services.AddDbContext<BEScanCvDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        }

        var aiServiceOptions = new AiServiceOptions
        {
            BaseUrl = configuration[$"{AiServiceOptions.SectionName}:BaseUrl"] ?? string.Empty,
            ParseSearchQueryPath = configuration[$"{AiServiceOptions.SectionName}:ParseSearchQueryPath"] ?? "/parse-search-query",
            ProcessCvPath = configuration[$"{AiServiceOptions.SectionName}:ProcessCvPath"] ?? "/api/v1/cv/index",
            SemanticSearchPath = configuration[$"{AiServiceOptions.SectionName}:SemanticSearchPath"] ?? "/api/v1/search",
            ApiKey = configuration[$"{AiServiceOptions.SectionName}:ApiKey"]
        };

        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(aiServiceOptions));

        services.Configure<RedisQueueOptions>(
            configuration.GetSection(RedisQueueOptions.SectionName));

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Redis must be configured for the CV upload queue.");
        }

        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiServiceOptions>>().Value;
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(150);
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl);
            }

            return client;
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ICvFileRepository, CvFileRepository>();
        services.AddScoped<ICvInfoRepository, CvInfoRepository>();
        services.AddScoped<ICvSkillRepository, CvSkillRepository>();
        services.AddScoped<ICvUploadBatchRepository, CvUploadBatchRepository>();
        services.AddScoped<ICvUploadBatchItemRepository, CvUploadBatchItemRepository>();

        services.AddScoped<ISearchQueryParser, AiSearchQueryParserClient>();
        services.AddScoped<ISemanticSearchClient, AiSemanticSearchClient>();
        services.AddSingleton<IHasher, BcryptHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<ICvFileStorageService, LocalCvFileStorageService>();
        services.AddScoped<ICvProcessingClient, AiCvProcessingClient>();
        services.AddScoped<ICvCleanupService, CvCleanupService>();
        services.AddSingleton<ICvUploadJobQueue, RedisCvUploadJobQueue>();
        services.AddSingleton<WebSocketUploadProgressNotifier>();
        services.AddSingleton<IUploadProgressNotifier>(
            serviceProvider => serviceProvider.GetRequiredService<WebSocketUploadProgressNotifier>());
        services.AddHostedService<CvUploadBackgroundWorker>();

        return services;
    }
}
