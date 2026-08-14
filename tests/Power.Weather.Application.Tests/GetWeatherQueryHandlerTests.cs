using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Power.Weather.Application.Options;
using Power.Weather.Application.Weather;
using Power.Weather.Domain.Services;
using Power.Weather.Domain.Weather;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Power.Weather.Application.Tests;

public class GetWeatherQueryHandlerTests
{
    private const double Tolerance = 1e-6;

    [Fact]
    public async Task ItShouldRequestWeatherForConfiguredLocation()
    {
        var provider = Substitute.For<IWeatherProvider>();
        provider.GetAsync(Arg.Any<GeoLocation>(), Arg.Any<CancellationToken>())
            .Returns(EmptySnapshot());

        var options = MsOptions.Create(MoscowOptions());
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero));
        var handler = new GetWeatherQueryHandler(provider, options, timeProvider, new HourlyForecastSelector());

        await handler.Handle(new GetWeatherQuery(), CancellationToken.None);

        await provider.Received(1).GetAsync(
            Arg.Is<GeoLocation>(l =>
                l.CityName == "Москва" &&
                Math.Abs(l.Latitude - 55.7558) < Tolerance &&
                Math.Abs(l.Longitude - 37.6173) < Tolerance),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ItShouldFilterHourlyForecastInConfiguredTimeZone()
    {
        var offset = TimeSpan.FromHours(3);
        var condition = new WeatherCondition("Clear", "https://example/a.png", 1000);
        var provider = Substitute.For<IWeatherProvider>();
        provider.GetAsync(Arg.Any<GeoLocation>(), Arg.Any<CancellationToken>())
            .Returns(new WeatherSnapshot(
                new GeoLocation(55.7558, 37.6173, "Москва"),
                new CurrentWeather(new DateTimeOffset(2024, 1, 15, 15, 0, 0, offset), -5, -8, 10, 70, condition),
                [
                    new HourlyForecast(new DateTimeOffset(2024, 1, 15, 14, 0, 0, offset), -6, 0, condition),
                    new HourlyForecast(new DateTimeOffset(2024, 1, 15, 15, 0, 0, offset), -5, 0, condition),
                    new HourlyForecast(new DateTimeOffset(2024, 1, 16, 8, 0, 0, offset), -4, 0, condition),
                    new HourlyForecast(new DateTimeOffset(2024, 1, 17, 8, 0, 0, offset), -1, 0, condition)
                ],
                []));

        var options = MsOptions.Create(MoscowOptions());

        // 15:30 Moscow = 12:30 UTC
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 12, 30, 0, TimeSpan.Zero));
        var handler = new GetWeatherQueryHandler(provider, options, timeProvider, new HourlyForecastSelector());

        var result = await handler.Handle(new GetWeatherQuery(), CancellationToken.None);

        Assert.Equal(2, result.Hourly.Count);
        Assert.Equal(15, result.Hourly[0].Time.Hour);
        Assert.Equal(8, result.Hourly[1].Time.Hour);
        Assert.Equal(16, result.Hourly[1].Time.Day);
    }

    private static WeatherLocationOptions MoscowOptions()
        => new()
        {
            CityName = "Москва",
            Latitude = 55.7558,
            Longitude = 37.6173,
            TimeZoneId = "Europe/Moscow"
        };

    private static WeatherSnapshot EmptySnapshot()
    {
        var condition = new WeatherCondition("Clear", "https://example/a.png", 1000);
        return new WeatherSnapshot(
            new GeoLocation(55.7558, 37.6173, "Москва"),
            new CurrentWeather(DateTimeOffset.UnixEpoch, 0, 0, 0, 0, condition),
            [],
            []);
    }
}
