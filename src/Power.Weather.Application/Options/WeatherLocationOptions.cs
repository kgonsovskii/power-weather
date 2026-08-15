namespace Power.Weather.Application.Options;

public sealed class WeatherLocationOptions
{
    public const string SectionName = "Location";

    public string CityName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>
    /// IANA-идентификатор часового пояса фиксированного города (например, Europe/Moscow). Предпочтительнее сырого смещения UTC.
    /// </summary>
    public string TimeZoneId { get; set; } = "Europe/Moscow";
}
