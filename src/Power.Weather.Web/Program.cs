using Power.Weather.Application;
using Power.Weather.Domain.Weather;
using Power.Weather.Infrastructure;
using Power.Weather.Web.Components;
using Power.Weather.Web.Loading;

namespace Power.Weather.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        builder.Services.AddScoped<WeatherLoadProgressTracker>();
        builder.Services.AddScoped<IWeatherLoadProgress>(sp => sp.GetRequiredService<WeatherLoadProgressTracker>());

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // HSTS не включаем: в проде сейчас self-signed сертификат
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
