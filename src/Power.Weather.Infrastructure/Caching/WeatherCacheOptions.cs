namespace Power.Weather.Infrastructure.Caching;

public sealed class WeatherCacheOptions
{
    public const string SectionName = "WeatherCache";

    /// <summary>
    /// How long a successful weather snapshot is reused before calling the provider again.
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(10);
}
