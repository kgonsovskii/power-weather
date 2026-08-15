using System.Text.Json;
using Microsoft.Extensions.Options;
using Power.Weather.Domain.Weather;
using Power.Weather.Domain.Weather.Demo;
using Power.Weather.Providers.WeatherDotCom.Contracts;

namespace Power.Weather.Providers.WeatherDotCom;

public sealed class WeatherDotComClient(
    HttpClient httpClient,
    IOptions<WeatherDotComOptions> options,
    IWeatherApiFaultArm apiFaultArm,
    IWeatherLoadProgress? progress = null) : IWeatherProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient = httpClient;
    private readonly WeatherDotComOptions _options = options.Value;
    private readonly IWeatherLoadProgress _progress = progress ?? NullWeatherLoadProgress.Instance;
    private readonly IWeatherApiFaultArm _apiFaultArm = apiFaultArm;

    public async Task<WeatherSnapshot> GetAsync(
        GeoLocation location,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        _ = forceRefresh;
        ArgumentNullException.ThrowIfNull(location);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("WeatherDotCom:ApiKey is missing or empty in configuration.");
        }

        // Демо-сбой на один вызов: снимаем «взвод» и подменяем ключ только для этого HTTP-запроса.
        var forceApiError = _apiFaultArm.ConsumeArmed();
        var apiKey = forceApiError
            ? "pw-demo-forced-invalid-key"
            : _options.ApiKey;

        var forecastUri = BuildForecastUri(location, apiKey);

        _progress.Report(new WeatherLoadProgressUpdate(
            WeatherLoadPhase.Connecting,
            forceApiError
                ? "Демо: ломаем ключ API…"
                : "Подключаемся к сервису погоды…",
            0.05,
            0,
            null));

        using var response = await _httpClient
            .GetAsync(forecastUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"WeatherDotCom forecast request failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}",
                null,
                response.StatusCode);
        }

        var totalBytes = response.Content.Headers.ContentLength;
        _progress.Report(new WeatherLoadProgressUpdate(
            WeatherLoadPhase.Downloading,
            "Скачиваем прогноз…",
            0.12,
            0,
            totalBytes));

        await using var raw = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var tracked = new ProgressReportingStream(raw, totalBytes, _progress);

        var dto = await JsonSerializer
            .DeserializeAsync<ForecastResponseDto>(tracked, JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (dto is null)
        {
            throw new InvalidOperationException("WeatherDotCom returned an empty forecast payload.");
        }

        _progress.Report(new WeatherLoadProgressUpdate(
            WeatherLoadPhase.Parsing,
            "Собираем экран прогноза…",
            0.97,
            totalBytes ?? tracked.Position,
            totalBytes));

        var snapshot = WeatherDotComMapper.ToSnapshot(dto, location, _options.ForecastDays);

        _progress.Report(new WeatherLoadProgressUpdate(
            WeatherLoadPhase.Completed,
            "Готово",
            1,
            totalBytes ?? tracked.Position,
            totalBytes));

        return snapshot;
    }

    private Uri BuildForecastUri(GeoLocation location, string apiKey)
    {
        var endpoint = _options.GetEndpointUri(WeatherDotComUrlKeys.Forecast);
        var query = $"key={Uri.EscapeDataString(apiKey)}" +
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
