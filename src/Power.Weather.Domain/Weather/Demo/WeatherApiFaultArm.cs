namespace Power.Weather.Domain.Weather.Demo;

public sealed class WeatherApiFaultArm : IWeatherApiFaultArm
{
    private int _armed;

    public void ArmNextRequest() => Interlocked.Exchange(ref _armed, 1);

    public bool ConsumeArmed() => Interlocked.Exchange(ref _armed, 0) == 1;
}
