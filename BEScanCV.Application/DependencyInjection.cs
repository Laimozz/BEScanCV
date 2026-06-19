using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Options;
using BEScanCV.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BEScanCV.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        services.AddScoped<ICvSearchService, CvSearchService>();
        services.AddScoped<ICvGetAllService, CvGetAllService>();
        services.AddScoped<ICvDetailService, CvDetailService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICvService, CvService>();

        return services;
    }
}
