namespace Power.Weather.Domain.Weather;

public interface IWeatherProvider
{
    Task<WeatherSnapshot> GetAsync(
        GeoLocation location,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);
}
