using System.Net.Http;
using System.Windows;
using GiglingBroadcastDeck.App.Services;
using GiglingBroadcastDeck.App.ViewModels;
using GiglingBroadcastDeck.Core.Mapping;
using GiglingBroadcastDeck.Core.Options;
using GiglingBroadcastDeck.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GiglingBroadcastDeck.App;

public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _services = ConfigureServices();
        var window = _services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_services is not null)
        {
            var overlayServer = _services.GetRequiredService<ILocalOverlayServer>();
            await overlayServer.StopAsync(CancellationToken.None);
            await _services.DisposeAsync();
        }

        base.OnExit(e);
    }

    private static ServiceProvider ConfigureServices()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddSimpleConsole();
        });

        services.Configure<GigaverseOptions>(configuration.GetSection("Gigaverse"));
        services.Configure<PollingOptions>(configuration.GetSection("Polling"));
        services.Configure<OverlayOptions>(configuration.GetSection("Overlay"));
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<GigaverseOptions>>().Value);

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<GigaverseOptions>>().Value;
            return new HttpClient
            {
                BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute),
                Timeout = TimeSpan.FromSeconds(10)
            };
        });

        services.AddSingleton<IRaceMapper, RaceMapper>();
        services.AddSingleton<IGigaverseRacingClient, GigaverseRacingClient>();
        services.AddSingleton<IRacePollingService, RacePollingService>();
        services.AddSingleton<IOverlayStateService, OverlayStateService>();
        services.AddSingleton<IClipboardSummaryService, ClipboardSummaryService>();
        services.AddSingleton<ILocalOverlayServer, LocalOverlayServer>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider(validateScopes: true);
    }
}
