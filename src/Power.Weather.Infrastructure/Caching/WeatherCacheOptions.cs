namespace Power.Weather.Infrastructure.Caching;

public sealed class WeatherCacheOptions
{
    public const string SectionName = "WeatherCache";

    /// <summary>
    /// Сколько хранить успешный снимок погоды в кэше до повторного обращения к провайдеру.
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(10);
}
