namespace Power.Weather.Integrity.Tests;

public class HappyPathTests(PowerWeatherWebApplicationFactory factory)
    : IClassFixture<PowerWeatherWebApplicationFactory>
{
    [Fact]
    public async Task ItShouldReturnSuccessStatusCodeForHome()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
    }
}
