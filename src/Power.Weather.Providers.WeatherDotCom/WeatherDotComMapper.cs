using Power.Weather.Domain.Weather;
using Power.Weather.Providers.WeatherDotCom.Contracts;

namespace Power.Weather.Providers.WeatherDotCom;

internal static class WeatherDotComMapper
{
    public static WeatherSnapshot ToSnapshot(ForecastResponseDto response, GeoLocation requestedLocation)
    {
        var timeZone = ResolveTimeZone(response.Location.TimeZoneId);

        var location = new GeoLocation(
            response.Location.Lat,
            response.Location.Lon,
            string.IsNullOrWhiteSpace(requestedLocation.CityName) ? response.Location.Name : requestedLocation.CityName);

        var current = new CurrentWeather(
            DateTimeOffset.FromUnixTimeSeconds(response.Current.LastUpdatedEpoch)
                .ToOffset(timeZone.GetUtcOffset(DateTimeOffset.FromUnixTimeSeconds(response.Current.LastUpdatedEpoch))),
            response.Current.TempC,
            response.Current.FeelsLikeC,
            response.Current.WindKph,
            response.Current.Humidity,
            ToCondition(response.Current.Condition));

        // Full hourly series; Application applies "remaining today + all tomorrow" in city time zone.
        var hourly = response.Forecast.ForecastDay
            .SelectMany(day => day.Hour)
            .Select(hour => ToHourly(hour, timeZone))
            .OrderBy(hour => hour.Time)
            .ToList();

        var daily = response.Forecast.ForecastDay
            .Take(3)
            .Select(day => new DailyForecast(
                DateOnly.Parse(day.Date),
                day.Day.MinTempC,
                day.Day.MaxTempC,
                ToCondition(day.Day.Condition)))
            .ToList();

        return new WeatherSnapshot(location, current, hourly, daily);
    }

    private static HourlyForecast ToHourly(HourDto hour, TimeZoneInfo timeZone)
    {
        var time = DateTimeOffset.FromUnixTimeSeconds(hour.TimeEpoch)
            .ToOffset(timeZone.GetUtcOffset(DateTimeOffset.FromUnixTimeSeconds(hour.TimeEpoch)));

        return new HourlyForecast(
            time,
            hour.TempC,
            hour.ChanceOfRain,
            ToCondition(hour.Condition));
    }

    private static WeatherCondition ToCondition(ConditionDto condition)
        => new(condition.Text, NormalizeIconUrl(condition.Icon), condition.Code);

    private static string NormalizeIconUrl(string icon)
    {
        if (string.IsNullOrWhiteSpace(icon))
        {
            return string.Empty;
        }

        if (icon.StartsWith("//", StringComparison.Ordinal))
        {
            return "https:" + icon;
        }

        return icon;
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
