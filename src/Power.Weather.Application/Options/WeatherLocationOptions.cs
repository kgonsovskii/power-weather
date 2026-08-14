namespace Power.Weather.Application.Options;

public sealed class WeatherLocationOptions
{
    public const string SectionName = "Location";

    public string CityName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>
    /// IANA time zone id for the fixed city (e.g. Europe/Moscow). Prefer this over a raw UTC offset.
    /// </summary>
    public string TimeZoneId { get; set; } = "Europe/Moscow";
}
