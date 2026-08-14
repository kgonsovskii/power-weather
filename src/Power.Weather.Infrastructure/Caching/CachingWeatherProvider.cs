using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Power.Weather.Domain.Weather;

namespace Power.Weather.Infrastructure.Caching;

public sealed class CachingWeatherProvider : IWeatherProvider
{
    private readonly IWeatherProvider _inner;
    private readonly IMemoryCache _cache;
    private readonly IOptions<WeatherCacheOptions> _options;
    private readonly ILogger<CachingWeatherProvider> _logger;
    private readonly IWeatherLoadProgress _progress;

    public CachingWeatherProvider(
        IWeatherProvider inner,
        IMemoryCache cache,
        IOptions<WeatherCacheOptions> options,
        ILogger<CachingWeatherProvider> logger,
        IWeatherLoadProgress progress)
    {
        _inner = inner;
        _cache = cache;
        _options = options;
        _logger = logger;
        _progress = progress;
    }

    public async Task<WeatherSnapshot> GetAsync(
        GeoLocation location,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        var cacheKey = BuildCacheKey(location);
        if (!forceRefresh &&
            _cache.TryGetValue(cacheKey, out WeatherSnapshot? cached) &&
            cached is not null)
        {
            _logger.LogInformation(
                "Weather cache hit for {City} ({Lat}, {Lon})",
                location.CityName,
                location.Latitude,
                location.Longitude);

            _progress.Report(new WeatherLoadProgressUpdate(
                WeatherLoadPhase.Completed,
                "Прогноз из кэша",
                1,
                0,
                0));

            return cached;
        }

        if (forceRefresh)
        {
            _cache.Remove(cacheKey);
            _logger.LogInformation(
                "Weather cache bypass for {City} ({Lat}, {Lon})",
                location.CityName,
                location.Latitude,
                location.Longitude);
        }
        else
        {
            _logger.LogInformation(
                "Weather cache miss for {City} ({Lat}, {Lon}). Calling provider.",
                location.CityName,
                location.Latitude,
                location.Longitude);
        }

        var snapshot = await _inner.GetAsync(location, forceRefresh: false, cancellationToken).ConfigureAwait(false);
        var duration = _options.Value.Duration < TimeSpan.Zero ? TimeSpan.Zero : _options.Value.Duration;

        _cache.Set(cacheKey, snapshot, duration);

        _logger.LogInformation(
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
