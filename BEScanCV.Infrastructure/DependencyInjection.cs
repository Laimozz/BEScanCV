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
            ApiKey = configuration[$"{AiServiceOptions.SectionName}:ApiKey"]
        };

        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(aiServiceOptions));
        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiServiceOptions>>().Value;
            var client = new HttpClient();
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

        services.AddScoped<ISearchQueryParser, AiSearchQueryParserClient>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        return services;
    }
}
