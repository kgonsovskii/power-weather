using Power.Weather.Domain.Weather;

namespace Power.Weather.Domain.Services;

/// <summary>
/// TZ requirement: remaining hours of the current local day + all hours of the next local day.
/// </summary>
public sealed class HourlyForecastSelector : IHourlyForecastSelector
{
    public IReadOnlyList<HourlyForecast> TakeRemainingTodayAndAllTomorrow(
        IReadOnlyList<HourlyForecast> hours,
        DateTimeOffset localNow)
    {
        ArgumentNullException.ThrowIfNull(hours);

        var today = DateOnly.FromDateTime(localNow.DateTime);
        var tomorrow = today.AddDays(1);
        var startOfCurrentHour = new DateTimeOffset(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            localNow.Hour,
            0,
            0,
            localNow.Offset);

        return hours
            .Where(hour =>
            {
                var day = DateOnly.FromDateTime(hour.Time.DateTime);
                if (day == today)
                {
                    return hour.Time >= startOfCurrentHour;
                }

                return day == tomorrow;
            })
            .OrderBy(hour => hour.Time)
            .ToList();
    }
}
