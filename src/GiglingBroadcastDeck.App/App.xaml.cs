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

/// <summary>
/// WPF application entry point and dependency-injection composition root.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"Unexpected app error: {args.Exception.Message}",
                "Unexpected error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            _services = ConfigureServices();
            _services.GetRequiredService<ILogger<App>>().LogInformation("Gigling Broadcast Deck starting.");
            var window = _services.GetRequiredService<MainWindow>();
            window.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Gigling Broadcast Deck could not start: {ex.Message}",
                "Startup failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_services is not null)
        {
            _services.GetRequiredService<ILogger<App>>().LogInformation("Gigling Broadcast Deck shutting down.");
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
        services.Configure<RealtimeOptions>(configuration.GetSection("Realtime"));
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<GigaverseOptions>>().Value);

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<GigaverseOptions>>().Value;
            return new HttpClient
            {
                BaseAddress = new Uri(EnsureTrailingSlash(options.BaseUrl), UriKind.Absolute),
                Timeout = TimeSpan.FromSeconds(Math.Max(3, options.TimeoutSeconds))
            };
        });

        services.AddSingleton<IRaceMapper, RaceMapper>();
        services.AddSingleton<IGigaverseRacingClient, GigaverseRacingClient>();
        services.AddSingleton<IRacePollingService, RacePollingService>();
        services.AddSingleton<IExploreDataService, ExploreDataService>();
        services.AddSingleton<IRacePhaseExplainer, RacePhaseExplainer>();
        services.AddSingleton<IOverlayStateService, OverlayStateService>();
        services.AddSingleton<IClipboardSummaryService, ClipboardSummaryService>();
        services.AddSingleton<IRealtimeRaceFeed, DisabledRealtimeRaceFeed>();
        services.AddSingleton<ILocalOverlayServer, LocalOverlayServer>();
        services.AddSingleton<IOperatorPreferencesService, OperatorPreferencesService>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static string EnsureTrailingSlash(string value) =>
        value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";
}
