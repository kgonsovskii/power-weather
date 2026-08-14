using MediatR;
using Microsoft.Extensions.Options;
using Power.Weather.Application.Options;
using Power.Weather.Domain.Services;
using Power.Weather.Domain.Weather;

namespace Power.Weather.Application.Weather;

public sealed class GetWeatherQueryHandler(
    IWeatherProvider weatherProvider,
    IOptions<WeatherLocationOptions> locationOptions,
    TimeProvider timeProvider,
    IHourlyForecastSelector hourlyForecastSelector)
    : IRequestHandler<GetWeatherQuery, WeatherSnapshot>
{
    public async Task<WeatherSnapshot> Handle(GetWeatherQuery request, CancellationToken cancellationToken)
    {
        var options = locationOptions.Value;
        var location = new GeoLocation(options.Latitude, options.Longitude, options.CityName);
        var snapshot = await weatherProvider
            .GetAsync(location, request.ForceRefresh, cancellationToken)
            .ConfigureAwait(false);

        var timeZone = ResolveTimeZone(options.TimeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);
        var hourly = hourlyForecastSelector.TakeRemainingTodayAndAllTomorrow(snapshot.Hourly, localNow);

        return snapshot with { Hourly = hourly };
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
        }

        return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }
}
