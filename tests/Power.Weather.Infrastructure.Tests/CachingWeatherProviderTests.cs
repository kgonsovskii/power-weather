using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Power.Weather.Domain.Weather;
using Power.Weather.Infrastructure.Caching;

namespace Power.Weather.Infrastructure.Tests;

public class CachingWeatherProviderTests
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task ItShouldCallInnerProviderOnlyOnceWithinCacheDuration()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero));
        var (sut, inner, location, snapshot) = CreateSut(timeProvider);

        var first = await sut.GetAsync(location);
        timeProvider.Advance(TimeSpan.FromSeconds(9));
        var second = await sut.GetAsync(location);

        Assert.Same(snapshot, first);
        Assert.Same(snapshot, second);
        await inner.Received(1).GetAsync(location, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ItShouldCallInnerProviderAgainAfterCacheExpires()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero));
        var (sut, inner, location, firstSnapshot) = CreateSut(timeProvider);
        var secondSnapshot = EmptySnapshot(location, temperatureC: -3);
        inner.GetAsync(location, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(firstSnapshot, secondSnapshot);

        var first = await sut.GetAsync(location);
        timeProvider.Advance(CacheDuration + TimeSpan.FromMilliseconds(1));
        var second = await sut.GetAsync(location);

        Assert.Same(firstSnapshot, first);
        Assert.Same(secondSnapshot, second);
        await inner.Received(2).GetAsync(location, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ItShouldBypassCacheWhenForceRefresh()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero));
        var (sut, inner, location, firstSnapshot) = CreateSut(timeProvider);
        var secondSnapshot = EmptySnapshot(location, temperatureC: -3);
        inner.GetAsync(location, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(firstSnapshot, secondSnapshot);

        var first = await sut.GetAsync(location);
        var second = await sut.GetAsync(location, forceRefresh: true);

        Assert.Same(firstSnapshot, first);
        Assert.Same(secondSnapshot, second);
        await inner.Received(2).GetAsync(location, false, Arg.Any<CancellationToken>());
    }

    private static (
        CachingWeatherProvider Sut,
        IWeatherProvider Inner,
        GeoLocation Location,
        WeatherSnapshot Snapshot) CreateSut(FakeTimeProvider timeProvider)
    {
        var location = new GeoLocation(55.7558, 37.6173, "Москва");
        var snapshot = EmptySnapshot(location);
        var inner = Substitute.For<IWeatherProvider>();
        inner.GetAsync(location, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(snapshot);

        var cache = new MemoryCache(new MemoryCacheOptions
        {
            Clock = new FakeSystemClock(timeProvider),
            ExpirationScanFrequency = TimeSpan.FromMilliseconds(1)
        });
        var options = Options.Create(new WeatherCacheOptions { Duration = CacheDuration });
        var sut = new CachingWeatherProvider(inner, cache, options, NullLogger<CachingWeatherProvider>.Instance, NullWeatherLoadProgress.Instance);

        return (sut, inner, location, snapshot);
    }

    private static WeatherSnapshot EmptySnapshot(GeoLocation location, double temperatureC = 0)
    {
        var condition = new WeatherCondition("Clear", "https://example/a.png", 1000);
        return new WeatherSnapshot(
            location,
            new CurrentWeather(DateTimeOffset.UnixEpoch, temperatureC, 0, 0, 0, condition),
            [],
            []);
    }

    private sealed class FakeSystemClock(FakeTimeProvider timeProvider) : ISystemClock
    {
        public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
    }
}
