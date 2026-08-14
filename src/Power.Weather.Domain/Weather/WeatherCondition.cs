namespace Power.Weather.Domain.Weather;

public sealed record WeatherCondition(
    string Text,
    string IconUrl,
    int Code);
