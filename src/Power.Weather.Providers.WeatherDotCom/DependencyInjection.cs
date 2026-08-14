using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Power.Weather.Providers.WeatherDotCom;

public static class DependencyInjection
{
    public static IServiceCollection AddWeatherDotComProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<WeatherDotComOptions>(configuration.GetSection(WeatherDotComOptions.SectionName));
        services.AddHttpClient<WeatherDotComClient>();
        return services;
    }
}
