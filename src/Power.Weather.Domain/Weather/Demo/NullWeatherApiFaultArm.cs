namespace Power.Weather.Domain.Weather.Demo;

public sealed class NullWeatherApiFaultArm : IWeatherApiFaultArm
{
    public static NullWeatherApiFaultArm Instance { get; } = new();

    private NullWeatherApiFaultArm()
    {
    }

    public void ArmNextRequest()
    {
    }

    public bool ConsumeArmed() => false;
}
