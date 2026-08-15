using Power.Weather.Domain.Weather;

namespace Power.Weather.Web.Loading;

public sealed class WeatherLoadProgressTracker : IWeatherLoadProgress
{
    private readonly object _gate = new();
    private readonly List<WeatherLoadLogEntry> _lines = [];
    private DateTime _lastUiNotifyUtc = DateTime.MinValue;

    public WeatherLoadProgressUpdate Current { get; private set; } = new(
        WeatherLoadPhase.Idle,
        "Ожидание…",
        0,
        0,
        null);

    public IReadOnlyList<WeatherLoadLogEntry> Lines
    {
        get
        {
            lock (_gate)
            {
                return _lines.ToArray();
            }
        }
    }

    public event Action? Changed;

    public void Reset()
    {
        lock (_gate)
        {
            _lines.Clear();
            _lastUiNotifyUtc = DateTime.MinValue;
        }

        Report(new WeatherLoadProgressUpdate(
            WeatherLoadPhase.Idle,
            "Готовим загрузку…",
            0,
            0,
            null));
    }

    public void Report(WeatherLoadProgressUpdate update)
    {
        var notify = false;

        lock (_gate)
        {
            Current = update;

            if (_lines.Count == 0 ||
                _lines[^1].Phase != update.Phase ||
                !string.Equals(_lines[^1].Message, update.Message, StringComparison.Ordinal))
            {
                _lines.Add(new WeatherLoadLogEntry(
                    update.Phase,
                    update.Message,
                    update.BytesReceived,
                    update.TotalBytes));
                notify = true;
            }
            else
            {
                _lines[^1] = _lines[^1] with
                {
                    BytesReceived = update.BytesReceived,
                    TotalBytes = update.TotalBytes
                };

                // Частые отчёты скачивания не дёргаем UI на каждый Read.
                var now = DateTime.UtcNow;
                if ((now - _lastUiNotifyUtc).TotalMilliseconds >= 120 || update.Ratio >= 0.97)
                {
                    notify = true;
                }
            }

            if (notify)
            {
                _lastUiNotifyUtc = DateTime.UtcNow;
            }
        }

        if (notify)
        {
            Changed?.Invoke();
        }
    }
}

public sealed record WeatherLoadLogEntry(
    WeatherLoadPhase Phase,
    string Message,
    long BytesReceived,
    long? TotalBytes);
