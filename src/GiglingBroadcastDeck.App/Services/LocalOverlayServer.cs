using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using GiglingBroadcastDeck.Core.Models;
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

/// <summary>
/// Hosts the local Minimal API and static browser overlay used by OBS Browser Source.
/// </summary>
/// <remarks>
/// The server binds only to the configured local host/port and exposes local app state.
/// It does not proxy authenticated Gigaverse requests or control OBS.
/// </remarks>
public sealed class LocalOverlayServer(
    IOptions<OverlayOptions> options,
    IOverlayStateService overlayState,
    ILogger<LocalOverlayServer> logger) : ILocalOverlayServer
{
    private readonly OverlayOptions _options = options.Value;
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private WebApplication? _app;

    /// <inheritdoc />
    public bool IsRunning => _app is not null;

    /// <inheritdoc />
    public string OverlayUrl => _options.OverlayUrl;

    /// <inheritdoc />
    public string HealthUrl => $"{_options.BaseAddress}/api/health";

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_app is null)
        {
            return;
        }

        await _app.StopAsync(cancellationToken).ConfigureAwait(false);
        await _app.DisposeAsync().ConfigureAwait(false);
        _app = null;
        logger.LogInformation("Local overlay server stopped.");
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
        app.MapGet("/api/overlay-state", () => Results.Json(overlayState.GetSnapshot(), JsonOptions));
        app.MapGet("/api/health", () => Results.Json(new
        {
            app = "Gigling Broadcast Deck",
            version = typeof(LocalOverlayServer).Assembly.GetName().Version?.ToString() ?? "dev",
            serverTime = DateTimeOffset.UtcNow,
            overlayMode = overlayState.GetSnapshot().Mode.ToString(),
            overlayUrl = OverlayUrl
        }, JsonOptions));
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        jsonOptions.Converters.Add(new JsonStringEnumConverter<OverlayMode>());
        jsonOptions.Converters.Add(new JsonStringEnumConverter<OverlayPreset>());
        jsonOptions.Converters.Add(new JsonStringEnumConverter<OverlayPosition>());
        return jsonOptions;
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
