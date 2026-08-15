using Power.Weather.Domain.Weather.Demo;

namespace Power.Weather.Domain.Tests;

public class WeatherApiFaultArmTests
{
    [Fact]
    public void ItShouldArmOnceAndResetOnConsume()
    {
        var arm = new WeatherApiFaultArm();

        Assert.False(arm.ConsumeArmed());

        arm.ArmNextRequest();
        Assert.True(arm.ConsumeArmed());
        Assert.False(arm.ConsumeArmed());
    }
}
