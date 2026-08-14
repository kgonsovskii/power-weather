using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Power.Weather.Domain.Weather;
using Power.Weather.Providers.WeatherDotCom.Contracts;

namespace Power.Weather.Providers.WeatherDotCom;

public sealed class WeatherDotComClient(
    HttpClient httpClient,
    IOptions<WeatherDotComOptions> options) : IWeatherProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClient;
    private readonly WeatherDotComOptions _options = options.Value;

    public async Task<WeatherSnapshot> GetAsync(GeoLocation location, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("WeatherDotCom:ApiKey is missing or empty in configuration.");
        }

        var forecastUri = BuildForecastUri(location);
        using var response = await _httpClient.GetAsync(forecastUri, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"WeatherDotCom forecast request failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
        }

        var dto = await response.Content.ReadFromJsonAsync<ForecastResponseDto>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (dto is null)
        {
            throw new InvalidOperationException("WeatherDotCom returned an empty forecast payload.");
        }

        return WeatherDotComMapper.ToSnapshot(dto, location);
    }

    private Uri BuildForecastUri(GeoLocation location)
    {
        var endpoint = _options.GetEndpointUri(WeatherDotComUrlKeys.Forecast);
        var query = $"key={Uri.EscapeDataString(_options.ApiKey)}" +
                    $"&q={location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                    $"{location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                    $"&days={_options.ForecastDays}";

        if (!string.IsNullOrWhiteSpace(_options.Language))
        {
            query += $"&lang={Uri.EscapeDataString(_options.Language)}";
        }

        var builder = new UriBuilder(endpoint) { Query = query };
        return builder.Uri;
    }
}
