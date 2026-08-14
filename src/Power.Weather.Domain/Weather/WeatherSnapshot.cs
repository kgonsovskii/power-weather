namespace Power.Weather.Domain.Weather;

/// <summary>
/// Aggregate for the single weather screen: current, hourly, and 3-day forecast.
/// </summary>
public sealed record WeatherSnapshot(
    GeoLocation Location,
    CurrentWeather Current,
    IReadOnlyList<HourlyForecast> Hourly,
    IReadOnlyList<DailyForecast> Daily);
