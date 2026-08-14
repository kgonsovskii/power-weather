namespace Power.Weather.Domain.Weather;

public sealed record DailyForecast(
    DateOnly Date,
    double MinTemperatureC,
    double MaxTemperatureC,
    WeatherCondition Condition);
