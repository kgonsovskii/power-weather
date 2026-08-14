using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Power.Weather.Application.Options;
using Power.Weather.Providers.WeatherDotCom;

namespace Power.Weather.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WeatherLocationOptions>(configuration.GetSection(WeatherLocationOptions.SectionName));
        services.AddWeatherDotComProvider(configuration);
        return services;
    }
}
