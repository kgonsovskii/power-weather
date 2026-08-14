using Power.Weather.Domain.Weather;

namespace Power.Weather.Web.Loading;

public sealed class WeatherLoadProgressTracker : IWeatherLoadProgress
{
    private readonly object _gate = new();

    public WeatherLoadProgressUpdate Current { get; private set; } = new(
        WeatherLoadPhase.Idle,
        "Ожидание…",
        0,
        0,
        null);

    public event Action? Changed;

    public void Reset()
    {
        Report(new WeatherLoadProgressUpdate(
            WeatherLoadPhase.Idle,
            "Готовим загрузку…",
            0,
            0,
            null));
    }

    public void Report(WeatherLoadProgressUpdate update)
    {
        lock (_gate)
        {
            Current = update;
        }

        Changed?.Invoke();
    }
}
