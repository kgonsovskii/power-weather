namespace Power.Weather.Domain.Weather.Demo;

/// <summary>
/// Демо-переключатель на один вызов: вооружается синхронно, срабатывает на следующем запросе провайдера и сам сбрасывается.
/// </summary>
public interface IWeatherApiFaultArm
{
    void ArmNextRequest();

    bool ConsumeArmed();
}
