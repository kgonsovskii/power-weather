namespace Power.Weather.Domain.Weather;

public interface IWeatherLoadProgress
{
    void Report(WeatherLoadProgressUpdate update);
}
