using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Power.Weather.Domain.Services;

namespace Power.Weather.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IHourlyForecastSelector, HourlyForecastSelector>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        return services;
    }
}
