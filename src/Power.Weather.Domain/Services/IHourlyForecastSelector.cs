using Power.Weather.Domain.Weather;

namespace Power.Weather.Domain.Services;

public interface IHourlyForecastSelector
{
    IReadOnlyList<HourlyForecast> TakeRemainingTodayAndAllTomorrow(
        IReadOnlyList<HourlyForecast> hours,
        DateTimeOffset localNow);
}
