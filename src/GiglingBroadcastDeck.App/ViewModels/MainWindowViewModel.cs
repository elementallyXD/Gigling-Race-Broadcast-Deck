using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GiglingBroadcastDeck.App.Services;
using GiglingBroadcastDeck.Core.Models;
using GiglingBroadcastDeck.Core.Options;
using GiglingBroadcastDeck.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GiglingBroadcastDeck.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IRacePollingService _pollingService;
    private readonly IOverlayStateService _overlayStateService;
    private readonly IClipboardSummaryService _clipboardSummaryService;
    private readonly ILocalOverlayServer _overlayServer;
    private readonly PollingOptions _pollingOptions;
    private readonly OverlayOptions _overlayOptions;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly DispatcherTimer _recentRaceTimer;
    private readonly DispatcherTimer _selectedRaceTimer;

    private RaceSummary? _selectedRace;
    private RaceDetail? _selectedRaceDetail;
    private string _rawJson = "";
    private string _statusText = "Ready";
    private string _overlayServerStatus = "Overlay server has not started yet.";
    private string _phaseDescription = "Select a race to view phase details.";
    private bool _isLoading;
    private bool _isStale;
    private bool _hasError;
    private bool _isEmpty;
    private bool _isOverlayServerRunning;
    private bool _isRefreshing;
    private bool _isRefreshingSelected;

    public MainWindowViewModel(
        IRacePollingService pollingService,
        IOverlayStateService overlayStateService,
        IClipboardSummaryService clipboardSummaryService,
        ILocalOverlayServer overlayServer,
        IOptions<PollingOptions> pollingOptions,
        IOptions<OverlayOptions> overlayOptions,
        ILogger<MainWindowViewModel> logger)
    {
        _pollingService = pollingService;
        _overlayStateService = overlayStateService;
        _clipboardSummaryService = clipboardSummaryService;
        _overlayServer = overlayServer;
        _pollingOptions = pollingOptions.Value;
        _overlayOptions = overlayOptions.Value;
        _logger = logger;

        RefreshRacesCommand = new AsyncRelayCommand(RefreshRacesAsync);
        ShowRaceCardCommand = new RelayCommand(() => SetOverlayMode(OverlayMode.RaceCard), CanShowSelectedRaceOverlay);
        ShowResultCardCommand = new RelayCommand(() => SetOverlayMode(OverlayMode.ResultCard), CanShowResultOverlay);
        ShowTickerCommand = new RelayCommand(() => SetOverlayMode(OverlayMode.Ticker), CanShowSelectedRaceOverlay);
        HideOverlayCommand = new RelayCommand(HideOverlay);
        CopyDiscordSummaryCommand = new RelayCommand(CopyDiscordSummary, CanShowSelectedRaceOverlay);

        _recentRaceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(Math.Max(5, _pollingOptions.RecentRacesSeconds))
        };
        _recentRaceTimer.Tick += async (_, _) => await RefreshRacesAsync();

        _selectedRaceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(Math.Max(2, _pollingOptions.SelectedRaceSeconds))
        };
        _selectedRaceTimer.Tick += async (_, _) => await RefreshSelectedRaceAsync();
    }

    public ObservableCollection<RaceSummary> Races { get; } = [];

    public IAsyncRelayCommand RefreshRacesCommand { get; }
    public IRelayCommand ShowRaceCardCommand { get; }
    public IRelayCommand ShowResultCardCommand { get; }
    public IRelayCommand ShowTickerCommand { get; }
    public IRelayCommand HideOverlayCommand { get; }
    public IRelayCommand CopyDiscordSummaryCommand { get; }

    public string OverlayUrl => _overlayOptions.OverlayUrl;
    public string HealthUrl => _overlayServer.HealthUrl;

    public RaceSummary? SelectedRace
    {
        get => _selectedRace;
        set
        {
            if (SetProperty(ref _selectedRace, value) && value is not null)
            {
                _ = SelectRaceAsync(value);
            }
        }
    }

    public RaceDetail? SelectedRaceDetail
    {
        get => _selectedRaceDetail;
        private set => SetProperty(ref _selectedRaceDetail, value);
    }

    public string RawJson
    {
        get => _rawJson;
        private set => SetProperty(ref _rawJson, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string OverlayServerStatus
    {
        get => _overlayServerStatus;
        private set => SetProperty(ref _overlayServerStatus, value);
    }

    public string PhaseDescription
    {
        get => _phaseDescription;
        private set => SetProperty(ref _phaseDescription, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public bool IsStale
    {
        get => _isStale;
        private set => SetProperty(ref _isStale, value);
    }

    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    public bool IsOverlayServerRunning
    {
        get => _isOverlayServerRunning;
        private set => SetProperty(ref _isOverlayServerRunning, value);
    }

    public async Task StartAsync()
    {
        try
        {
            await _overlayServer.StartAsync(CancellationToken.None);
            IsOverlayServerRunning = true;
            OverlayServerStatus = $"Overlay server running at {OverlayUrl}";
        }
        catch (LocalOverlayServerException ex)
        {
            _logger.LogWarning(ex, "Local overlay server could not start.");
            IsOverlayServerRunning = false;
            OverlayServerStatus = ex.Message;
            MessageBox.Show(ex.Message, "Overlay server unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        await RefreshRacesAsync();
        _recentRaceTimer.Start();
        _selectedRaceTimer.Start();
    }

    public void Stop()
    {
        _recentRaceTimer.Stop();
        _selectedRaceTimer.Stop();
    }

    private async Task RefreshRacesAsync()
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;
        IsLoading = true;

        try
        {
            ApplySnapshot(await _pollingService.RefreshRecentRacesAsync(CancellationToken.None));
        }
        finally
        {
            _isRefreshing = false;
            IsLoading = false;
        }
    }

    private async Task SelectRaceAsync(RaceSummary race)
    {
        _selectedRaceTimer.Stop();
        IsLoading = true;

        try
        {
            ApplySnapshot(await _pollingService.SelectRaceAsync(race, CancellationToken.None));
            PushSelectedRaceToOverlay();
        }
        finally
        {
            IsLoading = false;
            _selectedRaceTimer.Start();
        }
    }

    private async Task RefreshSelectedRaceAsync()
    {
        if (SelectedRace is null || _isRefreshingSelected)
        {
            return;
        }

        _isRefreshingSelected = true;

        try
        {
            ApplySnapshot(await _pollingService.RefreshSelectedRaceAsync(CancellationToken.None));
            PushSelectedRaceToOverlay();
        }
        finally
        {
            _isRefreshingSelected = false;
        }
    }

    private void ApplySnapshot(RaceDataSnapshot snapshot)
    {
        Races.Clear();
        foreach (var race in snapshot.Races)
        {
            Races.Add(race);
        }

        SelectedRaceDetail = snapshot.SelectedRaceDetail;
        RawJson = snapshot.SelectedRaceDetail?.RawJson ?? SelectedRace?.RawJson ?? "";
        StatusText = FormatStatus(snapshot);
        IsStale = snapshot.IsStale;
        HasError = snapshot.Status == AppStatus.Error;
        IsEmpty = snapshot.Status == AppStatus.Empty;
        PhaseDescription = RacePhaseDescriptionService.Describe(snapshot.SelectedRaceDetail?.Phase ?? SelectedRace?.Phase);

        RefreshCommandStates();
    }

    private void SetOverlayMode(OverlayMode mode)
    {
        _overlayStateService.SetMode(mode, SelectedRace, SelectedRaceDetail);
        StatusText = $"Overlay mode set to {mode}.";
        RefreshCommandStates();
    }

    private void HideOverlay()
    {
        _overlayStateService.Hide();
        StatusText = "Overlay hidden.";
    }

    private void PushSelectedRaceToOverlay()
    {
        var currentMode = _overlayStateService.GetSnapshot().Mode;
        if (currentMode != OverlayMode.Hidden)
        {
            _overlayStateService.SetMode(currentMode, SelectedRace, SelectedRaceDetail);
        }
    }

    private void CopyDiscordSummary()
    {
        var summary = _clipboardSummaryService.CreateSummary(SelectedRace, SelectedRaceDetail);
        Clipboard.SetText(summary);
        StatusText = "Discord summary copied to clipboard.";
    }

    private bool CanShowSelectedRaceOverlay() => SelectedRace is not null || SelectedRaceDetail is not null;

    private bool CanShowResultOverlay() =>
        SelectedRaceDetail is not null &&
        string.Equals(SelectedRaceDetail.Phase, "RESOLVED", StringComparison.OrdinalIgnoreCase);

    private void RefreshCommandStates()
    {
        ShowRaceCardCommand.NotifyCanExecuteChanged();
        ShowResultCardCommand.NotifyCanExecuteChanged();
        ShowTickerCommand.NotifyCanExecuteChanged();
        CopyDiscordSummaryCommand.NotifyCanExecuteChanged();
    }

    private static string FormatStatus(RaceDataSnapshot snapshot)
    {
        var attempted = snapshot.LastAttemptedFetchAt?.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        return attempted is null ? snapshot.Message : $"{snapshot.Message} Last attempt: {attempted}.";
    }
}
