using System.Text.Json.Serialization;

namespace Power.Weather.Providers.WeatherDotCom.Contracts;

internal sealed class ForecastResponseDto
{
    [JsonPropertyName("location")]
    public LocationDto Location { get; set; } = new();

    [JsonPropertyName("current")]
    public CurrentDto Current { get; set; } = new();

    [JsonPropertyName("forecast")]
    public ForecastDto Forecast { get; set; } = new();
}

internal sealed class LocationDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }

    [JsonPropertyName("localtime_epoch")]
    public long LocaltimeEpoch { get; set; }

    [JsonPropertyName("tz_id")]
    public string TimeZoneId { get; set; } = string.Empty;
}

internal sealed class CurrentDto
{
    [JsonPropertyName("last_updated_epoch")]
    public long LastUpdatedEpoch { get; set; }

    [JsonPropertyName("temp_c")]
    public double TempC { get; set; }

    [JsonPropertyName("feelslike_c")]
    public double FeelsLikeC { get; set; }

    [JsonPropertyName("wind_kph")]
    public double WindKph { get; set; }

    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }

    [JsonPropertyName("condition")]
    public ConditionDto Condition { get; set; } = new();
}

internal sealed class ForecastDto
{
    [JsonPropertyName("forecastday")]
    public List<ForecastDayDto> ForecastDay { get; set; } = [];
}

internal sealed class ForecastDayDto
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("day")]
    public DayDto Day { get; set; } = new();

    [JsonPropertyName("hour")]
    public List<HourDto> Hour { get; set; } = [];
}

internal sealed class DayDto
{
    [JsonPropertyName("maxtemp_c")]
    public double MaxTempC { get; set; }

    [JsonPropertyName("mintemp_c")]
    public double MinTempC { get; set; }

    [JsonPropertyName("condition")]
    public ConditionDto Condition { get; set; } = new();
}

internal sealed class HourDto
{
    [JsonPropertyName("time_epoch")]
    public long TimeEpoch { get; set; }

    [JsonPropertyName("temp_c")]
    public double TempC { get; set; }

    [JsonPropertyName("chance_of_rain")]
    public int ChanceOfRain { get; set; }

    [JsonPropertyName("condition")]
    public ConditionDto Condition { get; set; } = new();
}

internal sealed class ConditionDto
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public int Code { get; set; }
}
