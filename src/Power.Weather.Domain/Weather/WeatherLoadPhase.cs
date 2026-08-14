namespace Power.Weather.Domain.Weather;

public enum WeatherLoadPhase
{
    Idle = 0,
    Connecting = 1,
    Downloading = 2,
    Parsing = 3,
    Completed = 4
}
