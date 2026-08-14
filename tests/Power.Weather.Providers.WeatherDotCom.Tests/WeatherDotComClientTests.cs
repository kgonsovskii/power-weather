using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Power.Weather.Domain.Weather;

namespace Power.Weather.Providers.WeatherDotCom.Tests;

public class WeatherDotComClientTests
{
    [Fact]
    public async Task ItShouldMapForecastResponseToWeatherSnapshot()
    {
        var json = await File.ReadAllTextAsync(TestDataPath("WeatherDotCom", "forecast-response.json"));

        var handler = new StubHttpMessageHandler(json);
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new WeatherDotComOptions
        {
            ApiKey = "test-key",
            ForecastDays = 3,
            Language = "ru",
            Urls = new Dictionary<string, string>
            {
                [WeatherDotComUrlKeys.Base] = "https://api.weatherapi.com/v1/",
                [WeatherDotComUrlKeys.Current] = "current.json",
                [WeatherDotComUrlKeys.Forecast] = "forecast.json"
            }
        });

        var client = new WeatherDotComClient(httpClient, options);
        var location = new GeoLocation(55.7558, 37.6173, "Москва");

        var snapshot = await client.GetAsync(location);

        Assert.Equal("Moscow", snapshot.Location.CityName);
        Assert.Equal(-5.0, snapshot.Current.TemperatureC);
        Assert.Equal("https://cdn.weatherapi.com/a.png", snapshot.Current.Condition.IconUrl);
        Assert.Equal(3, snapshot.Daily.Count);
        Assert.Equal(5, snapshot.Hourly.Count);
        Assert.Contains("forecast.json", handler.LastRequestUri!.AbsolutePath);
        Assert.Contains("key=test-key", handler.LastRequestUri.Query);
        Assert.Contains("days=3", handler.LastRequestUri.Query);
        Assert.Contains("lang=ru", handler.LastRequestUri.Query);
    }

    [Fact]
    public void ItShouldBuildEndpointFromUrlsDictionary()
    {
        var options = new WeatherDotComOptions
        {
            Urls = new Dictionary<string, string>
            {
                [WeatherDotComUrlKeys.Base] = "https://api.weatherapi.com/v1/",
                [WeatherDotComUrlKeys.Forecast] = "forecast.json"
            }
        };

        var uri = options.GetEndpointUri(WeatherDotComUrlKeys.Forecast);

        Assert.Equal("https://api.weatherapi.com/v1/forecast.json", uri.ToString());
    }

    private static string TestDataPath(string provider, string fileName)
        => Path.Combine(AppContext.BaseDirectory, "TestData", provider, fileName);

    private sealed class StubHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
