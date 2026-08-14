namespace Power.Weather.Providers.WeatherDotCom;

public sealed class WeatherDotComOptions
{
    public const string SectionName = "Providers:WeatherDotCom";

    public string ApiKey { get; init; } = string.Empty;

    public int ForecastDays { get; init; } = 3;

    /// <summary>
    /// Language code for condition text (WeatherAPI <c>lang</c>), e.g. ru.
    /// </summary>
    public string Language { get; init; } = "ru";

    /// <summary>
    /// Named URLs from configuration, e.g. Base / Current / Forecast.
    /// </summary>
    public Dictionary<string, string> Urls { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    private string GetRequiredUrl(string key)
    {
        if (!Urls.TryGetValue(key, out var url) || string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException(
                $"WeatherDotCom:Urls['{key}'] is missing or empty in configuration.");
        }

        return url.Trim();
    }

    public Uri GetEndpointUri(string endpointKey)
    {
        var baseUrl = GetRequiredUrl(WeatherDotComUrlKeys.Base);
        var endpoint = GetRequiredUrl(endpointKey);

        return Uri.TryCreate(endpoint, UriKind.Absolute, out var absolute) ? absolute 
            : new Uri(new Uri(EnsureTrailingSlash(baseUrl), UriKind.Absolute), endpoint);
    }

    private static string EnsureTrailingSlash(string url)
        => url.EndsWith('/') ? url : url + "/";
}
