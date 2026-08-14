using MediatR;
using Power.Weather.Domain.Weather;

namespace Power.Weather.Application.Weather;

public sealed record GetWeatherQuery(bool ForceRefresh = false) : IRequest<WeatherSnapshot>;
