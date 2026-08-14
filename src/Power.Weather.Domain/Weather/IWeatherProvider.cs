namespace Power.Weather.Domain.Weather;

public interface IWeatherProvider
{
    Task<WeatherSnapshot> GetAsync(GeoLocation location, CancellationToken cancellationToken = default);
}
