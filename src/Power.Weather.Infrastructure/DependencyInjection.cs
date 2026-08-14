using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Power.Weather.Application.Options;
using Power.Weather.Domain.Weather;
using Power.Weather.Infrastructure.Caching;
using Power.Weather.Providers.WeatherDotCom;

namespace Power.Weather.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WeatherLocationOptions>(configuration.GetSection(WeatherLocationOptions.SectionName));
        services.Configure<WeatherCacheOptions>(configuration.GetSection(WeatherCacheOptions.SectionName));

        services.AddMemoryCache();
        services.AddScoped<IWeatherLoadProgress, NullWeatherLoadProgress>();
        services.AddWeatherDotComProvider(configuration);

        services.AddTransient<IWeatherProvider>(sp =>
        {
            var inner = sp.GetRequiredService<WeatherDotComClient>();
            return ActivatorUtilities.CreateInstance<CachingWeatherProvider>(sp, inner);
        });

        return services;
    }
}
