using System.IO;
using System.Net.Sockets;
using GiglingBroadcastDeck.Core.Options;
using GiglingBroadcastDeck.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GiglingBroadcastDeck.App.Services;

public sealed class LocalOverlayServer(
    IOptions<OverlayOptions> options,
    IOverlayStateService overlayState,
    ILogger<LocalOverlayServer> logger) : ILocalOverlayServer
{
    private readonly OverlayOptions _options = options.Value;
    private WebApplication? _app;

    public bool IsRunning => _app is not null;
    public string OverlayUrl => _options.OverlayUrl;
    public string HealthUrl => $"{_options.BaseAddress}/api/health";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_app is not null)
        {
            return;
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = AppContext.BaseDirectory,
            Args = []
        });

        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls(_options.BaseAddress);

        var app = builder.Build();
        ConfigureEndpoints(app);

        try
        {
            await app.StartAsync(cancellationToken).ConfigureAwait(false);
            _app = app;
            logger.LogInformation("Local overlay server started at {OverlayUrl}", OverlayUrl);
        }
        catch (Exception ex) when (IsPortConflict(ex))
        {
            await app.DisposeAsync().ConfigureAwait(false);
            throw new LocalOverlayServerException(
                $"Port {_options.Port} is already in use. Free the port or change Overlay:Port in appsettings.json.",
                ex);
        }
        catch
        {
            await app.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_app is null)
        {
            return;
        }

        await _app.StopAsync(cancellationToken).ConfigureAwait(false);
        await _app.DisposeAsync().ConfigureAwait(false);
        _app = null;
    }

    private void ConfigureEndpoints(WebApplication app)
    {
        var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        Directory.CreateDirectory(wwwroot);

        app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = new PhysicalFileProvider(wwwroot)
        });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(wwwroot)
        });

        app.MapGet("/", () => Results.Redirect("/overlay"));
        app.MapGet("/overlay", () => Results.File(Path.Combine(wwwroot, "overlay.html"), "text/html"));
        app.MapGet("/api/overlay-state", () => Results.Json(overlayState.GetSnapshot()));
        app.MapGet("/api/health", () => Results.Json(new
        {
            app = "Gigling Broadcast Deck",
            version = typeof(LocalOverlayServer).Assembly.GetName().Version?.ToString() ?? "dev",
            serverTime = DateTimeOffset.UtcNow,
            overlayMode = overlayState.GetSnapshot().Mode.ToString(),
            overlayUrl = OverlayUrl
        }));
    }

    private static bool IsPortConflict(Exception ex)
    {
        if (ex is IOException { InnerException: SocketException socket } &&
            socket.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            return true;
        }

        if (ex is SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse })
        {
            return true;
        }

        return ex.InnerException is not null && IsPortConflict(ex.InnerException);
    }
}
