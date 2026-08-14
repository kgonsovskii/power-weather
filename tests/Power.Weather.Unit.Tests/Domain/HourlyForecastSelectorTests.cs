using Microsoft.Extensions.Time.Testing;
using Power.Weather.Domain.Services;
using Power.Weather.Domain.Weather;

namespace Power.Weather.Unit.Tests.Domain;

public class HourlyForecastSelectorTests
{
    private static readonly WeatherCondition Clear = new("Clear", "https://example/a.png", 1000);
    private static readonly TimeSpan MoscowOffset = TimeSpan.FromHours(3);
    private readonly IHourlyForecastSelector _selector = new HourlyForecastSelector();

    [Fact]
    public void ItShouldKeepRemainingHoursOfTodayAndAllHoursOfTomorrow()
    {
        // 15:20 Moscow = 12:20 UTC
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 12, 20, 0, TimeSpan.Zero));
        var localNow = ToMoscowLocal(timeProvider);

        var hours = new[]
        {
            Hour(2024, 1, 15, 14, -6),
            Hour(2024, 1, 15, 15, -5),
            Hour(2024, 1, 15, 16, -4),
            Hour(2024, 1, 16, 0, -7),
            Hour(2024, 1, 16, 23, -3),
            Hour(2024, 1, 17, 0, 0)
        };

        var selected = _selector.TakeRemainingTodayAndAllTomorrow(hours, localNow);

        Assert.Equal(4, selected.Count);
        Assert.Equal([15, 16, 0, 23], selected.Select(h => h.Time.Hour));
        Assert.DoesNotContain(selected, h => h.Time.Day == 17);
        Assert.DoesNotContain(selected, h => h.Time is { Day: 15, Hour: 14 });
    }

    [Fact]
    public void ItShouldIncludeCurrentHourEvenWhenLocalNowIsPastTheHourStart()
    {
        // 10:59 Moscow = 07:59 UTC
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 15, 7, 59, 0, TimeSpan.Zero));
        var localNow = ToMoscowLocal(timeProvider);

        var hours = new[]
        {
            Hour(2024, 1, 15, 10, 1),
            Hour(2024, 1, 15, 11, 2)
        };

        var selected = _selector.TakeRemainingTodayAndAllTomorrow(hours, localNow);

        Assert.Equal(2, selected.Count);
        Assert.Equal(10, selected[0].Time.Hour);
    }

    private static DateTimeOffset ToMoscowLocal(TimeProvider timeProvider)
        => timeProvider.GetUtcNow().ToOffset(MoscowOffset);

    private static HourlyForecast Hour(int year, int month, int day, int hour, double tempC)
        => new(
            new DateTimeOffset(year, month, day, hour, 0, 0, MoscowOffset),
            tempC,
            0,
            Clear);
}
