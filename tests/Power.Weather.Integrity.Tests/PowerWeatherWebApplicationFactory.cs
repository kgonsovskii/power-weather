using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Testing;
using Power.Weather.Web;

namespace Power.Weather.Integrity.Tests;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Создаётся не явно")]
public sealed class PowerWeatherWebApplicationFactory : WebApplicationFactory<Program>;
