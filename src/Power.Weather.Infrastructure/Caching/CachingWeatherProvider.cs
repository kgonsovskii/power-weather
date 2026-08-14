using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Power.Weather.Domain.Weather;
using System.Globalization;

namespace Power.Weather.Infrastructure.Caching;

public sealed class CachingWeatherProvider(
    IWeatherProvider inner,
    IMemoryCache cache,
    IOptions<WeatherCacheOptions> options,
    ILogger<CachingWeatherProvider> logger) : IWeatherProvider
{
    public async Task<WeatherSnapshot> GetAsync(GeoLocation location, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        var cacheKey = BuildCacheKey(location);
        if (cache.TryGetValue(cacheKey, out WeatherSnapshot? cached) && cached is not null)
        {
            logger.LogInformation(
                "Weather cache hit for {City} ({Lat}, {Lon})",
                location.CityName,
                location.Latitude,
                location.Longitude);
            return cached;
        }

        logger.LogInformation(
            "Weather cache miss for {City} ({Lat}, {Lon}). Calling provider.",
            location.CityName,
            location.Latitude,
            location.Longitude);

        var snapshot = await inner.GetAsync(location, cancellationToken).ConfigureAwait(false);
        var duration = options.Value.Duration < TimeSpan.Zero ? TimeSpan.Zero : options.Value.Duration;

        cache.Set(cacheKey, snapshot, duration);

        logger.LogInformation(
            "Weather snapshot cached for {City} for {Duration}",
            location.CityName,
            duration);

        return snapshot;
    }

    private static string BuildCacheKey(GeoLocation location)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"weather:{location.Latitude:F4}:{location.Longitude:F4}");
}
