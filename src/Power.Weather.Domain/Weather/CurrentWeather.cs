namespace Power.Weather.Domain.Weather;

public sealed record CurrentWeather(
    DateTimeOffset ObservedAt,
    double TemperatureC,
    double FeelsLikeC,
    double WindKph,
    int HumidityPercent,
    WeatherCondition Condition);
