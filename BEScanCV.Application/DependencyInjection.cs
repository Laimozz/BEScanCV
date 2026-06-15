using BEScanCV.Application.Interfaces;
using BEScanCV.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BEScanCV.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICvSearchService, CvSearchService>();
        services.AddScoped<ICvGetAllService, CvGetAllService>();

        return services;
    }
}
