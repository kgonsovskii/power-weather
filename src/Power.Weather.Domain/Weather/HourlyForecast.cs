namespace Power.Weather.Domain.Weather;

public sealed record HourlyForecast(
    DateTimeOffset Time,
    double TemperatureC,
    int ChanceOfRainPercent,
    WeatherCondition Condition);
