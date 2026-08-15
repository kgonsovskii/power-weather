namespace Power.Weather.Domain.Weather;

/// <summary>
/// Агрегат одного экрана погоды: текущая, почасовая и прогноз на 3 дня.
/// </summary>
public sealed record WeatherSnapshot(
    GeoLocation Location,
    CurrentWeather Current,
    IReadOnlyList<HourlyForecast> Hourly,
    IReadOnlyList<DailyForecast> Daily);
