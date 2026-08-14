namespace Power.Weather.Domain.Weather;

public sealed record WeatherLoadProgressUpdate(
    WeatherLoadPhase Phase,
    string Message,
    double Ratio,
    long BytesReceived,
    long? TotalBytes);
