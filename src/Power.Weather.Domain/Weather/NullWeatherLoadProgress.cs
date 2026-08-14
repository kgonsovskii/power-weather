namespace Power.Weather.Domain.Weather;

public sealed class NullWeatherLoadProgress : IWeatherLoadProgress
{
    public static NullWeatherLoadProgress Instance { get; } = new();

    public void Report(WeatherLoadProgressUpdate update)
    {
    }
}
