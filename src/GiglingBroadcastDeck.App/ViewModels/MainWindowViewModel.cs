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

/// <summary>
/// Coordinates the operator UI, polling services, local overlay state, and rundown controls.
/// </summary>
/// <remarks>
/// The view model catches unexpected timer/command failures so WPF remains usable during API
/// outages or malformed public responses.
/// </remarks>
public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IRacePollingService _pollingService;
    private readonly IExploreDataService _exploreDataService;
    private readonly IOverlayStateService _overlayStateService;
    private readonly IRacePhaseExplainer _racePhaseExplainer;
    private readonly IClipboardSummaryService _clipboardSummaryService;
    private readonly ILocalOverlayServer _overlayServer;
    private readonly IOperatorPreferencesService _preferencesService;
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
    private bool _isUnavailable;
    private bool _isOverlayServerRunning;
    private bool _isRefreshing;
    private bool _isRefreshingSelected;
    private bool _isApplyingSnapshot;
    private bool _isRefreshingExplore;
    private bool _isExploreLoading;
    private string _newRundownText = "";
    private string _exploreStatusText = "Explore data has not loaded yet.";
    private OverlayPreset _selectedOverlayPreset = OverlayPreset.Broadcast;
    private OverlayPosition _selectedOverlayPosition = OverlayPosition.LowerLeft;

    public MainWindowViewModel(
        IRacePollingService pollingService,
        IExploreDataService exploreDataService,
        IOverlayStateService overlayStateService,
        IRacePhaseExplainer racePhaseExplainer,
        IClipboardSummaryService clipboardSummaryService,
        ILocalOverlayServer overlayServer,
        IOperatorPreferencesService preferencesService,
        IOptions<PollingOptions> pollingOptions,
        IOptions<OverlayOptions> overlayOptions,
        ILogger<MainWindowViewModel> logger)
    {
        _pollingService = pollingService;
        _exploreDataService = exploreDataService;
        _overlayStateService = overlayStateService;
        _racePhaseExplainer = racePhaseExplainer;
        _clipboardSummaryService = clipboardSummaryService;
        _overlayServer = overlayServer;
        _preferencesService = preferencesService;
        _pollingOptions = pollingOptions.Value;
        _overlayOptions = overlayOptions.Value;
        _logger = logger;

        RefreshRacesCommand = new AsyncRelayCommand(RefreshRacesAsync);
        RefreshExploreCommand = new AsyncRelayCommand(RefreshExploreAsync);
        ShowRaceCardCommand = new RelayCommand(() => SetOverlayMode(OverlayMode.RaceCard), CanShowSelectedRaceOverlay);
        ShowResultCardCommand = new RelayCommand(() => SetOverlayMode(OverlayMode.ResultCard), CanShowResultOverlay);
        ShowTickerCommand = new RelayCommand(() => SetOverlayMode(OverlayMode.Ticker), CanShowSelectedRaceOverlay);
        HideOverlayCommand = new RelayCommand(HideOverlay);
        CopyDiscordSummaryCommand = new RelayCommand(CopyDiscordSummary, CanShowSelectedRaceOverlay);
        AddSelectedRaceToRundownCommand = new RelayCommand(AddSelectedRaceToRundown, CanShowSelectedRaceOverlay);
        AddRundownLineCommand = new RelayCommand(AddRundownLine, () => !string.IsNullOrWhiteSpace(NewRundownText));
        ClearRundownCommand = new RelayCommand(ClearRundown);

        var preferences = _preferencesService.Load();
        _selectedOverlayPreset = preferences.OverlayPreset;
        _selectedOverlayPosition = preferences.OverlayPosition;
        foreach (var item in preferences.RundownItems.Take(5))
        {
            RundownItems.Add(item);
        }

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

    /// <summary>
    /// Recent public races displayed in the operator tab.
    /// </summary>
    public ObservableCollection<RaceSummary> Races { get; } = [];
    public ObservableCollection<RaceSummary> ScheduledRaces { get; } = [];
    public ObservableCollection<StatLine> GlobalStats { get; } = [];
    public ObservableCollection<LeaderboardEntry> Leaderboard { get; } = [];
    public ObservableCollection<string> RundownItems { get; } = [];
    public ObservableCollection<string> StatusChips { get; } = [];

    public IReadOnlyList<OverlayPreset> OverlayPresets { get; } = Enum.GetValues<OverlayPreset>();
    public IReadOnlyList<OverlayPosition> OverlayPositions { get; } = Enum.GetValues<OverlayPosition>();

    public IAsyncRelayCommand RefreshRacesCommand { get; }
    public IAsyncRelayCommand RefreshExploreCommand { get; }
    public IRelayCommand ShowRaceCardCommand { get; }
    public IRelayCommand ShowResultCardCommand { get; }
    public IRelayCommand ShowTickerCommand { get; }
    public IRelayCommand HideOverlayCommand { get; }
    public IRelayCommand CopyDiscordSummaryCommand { get; }
    public IRelayCommand AddSelectedRaceToRundownCommand { get; }
    public IRelayCommand AddRundownLineCommand { get; }
    public IRelayCommand ClearRundownCommand { get; }

    /// <summary>
    /// Local browser-source URL for OBS.
    /// </summary>
    public string OverlayUrl => _overlayOptions.OverlayUrl;

    /// <summary>
    /// Local health endpoint used for operator diagnostics.
    /// </summary>
    public string HealthUrl => _overlayServer.HealthUrl;

    public RaceSummary? SelectedRace
    {
        get => _selectedRace;
        set
        {
            if (_isApplyingSnapshot && value is null)
            {
                return;
            }

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

    public bool IsUnavailable
    {
        get => _isUnavailable;
        private set => SetProperty(ref _isUnavailable, value);
    }

    public bool IsOverlayServerRunning
    {
        get => _isOverlayServerRunning;
        private set => SetProperty(ref _isOverlayServerRunning, value);
    }

    public bool IsExploreLoading
    {
        get => _isExploreLoading;
        private set => SetProperty(ref _isExploreLoading, value);
    }

    public string ExploreStatusText
    {
        get => _exploreStatusText;
        private set => SetProperty(ref _exploreStatusText, value);
    }

    public string NewRundownText
    {
        get => _newRundownText;
        set
        {
            if (SetProperty(ref _newRundownText, value))
            {
                AddRundownLineCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public OverlayPreset SelectedOverlayPreset
    {
        get => _selectedOverlayPreset;
        set
        {
            if (SetProperty(ref _selectedOverlayPreset, value))
            {
                ApplyOverlayPreferences();
            }
        }
    }

    public OverlayPosition SelectedOverlayPosition
    {
        get => _selectedOverlayPosition;
        set
        {
            if (SetProperty(ref _selectedOverlayPosition, value))
            {
                ApplyOverlayPreferences();
            }
        }
    }

    /// <summary>
    /// Starts the local overlay server and initial public data refreshes.
    /// </summary>
    public async Task StartAsync()
    {
        try
        {
            await _overlayServer.StartAsync(CancellationToken.None);
            IsOverlayServerRunning = true;
            OverlayServerStatus = $"Overlay server running at {OverlayUrl}";
            ApplyOverlayPreferences();
        }
        catch (LocalOverlayServerException ex)
        {
            _logger.LogWarning(ex, "Local overlay server could not start.");
            IsOverlayServerRunning = false;
            OverlayServerStatus = ex.Message;
            MessageBox.Show(ex.Message, "Overlay server unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local overlay server failed unexpectedly.");
            IsOverlayServerRunning = false;
            OverlayServerStatus = $"Overlay server failed to start: {ex.Message}";
            MessageBox.Show(OverlayServerStatus, "Overlay server unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        await RefreshRacesAsync();
        await RefreshExploreAsync();
        _recentRaceTimer.Start();
        _selectedRaceTimer.Start();
    }

    /// <summary>
    /// Stops WPF polling timers before app shutdown.
    /// </summary>
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while refreshing recent races.");
            HasError = true;
            StatusText = $"Unexpected refresh error: {ex.Message}";
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while selecting race {RaceId}.", race.RaceId);
            HasError = true;
            StatusText = $"Unexpected race selection error: {ex.Message}";
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while refreshing selected race.");
            HasError = true;
            StatusText = $"Unexpected selected race refresh error: {ex.Message}";
        }
        finally
        {
            _isRefreshingSelected = false;
        }
    }

    private void ApplySnapshot(RaceDataSnapshot snapshot)
    {
        var selectedRaceId = SelectedRace?.RaceId ?? snapshot.SelectedRaceDetail?.RaceId;

        _isApplyingSnapshot = true;
        try
        {
            UpdateRaceCollection(snapshot.Races, selectedRaceId);
            RestoreSelectedRace(selectedRaceId);

            SelectedRaceDetail = snapshot.SelectedRaceDetail;
            RawJson = snapshot.SelectedRaceDetail?.RawJson ?? SelectedRace?.RawJson ?? "";
            StatusText = FormatStatus(snapshot);
            IsStale = snapshot.IsStale;
            HasError = snapshot.Status == AppStatus.Error;
            IsEmpty = snapshot.Status == AppStatus.Empty;
            IsUnavailable = snapshot.Status == AppStatus.Unavailable;
            PhaseDescription = _racePhaseExplainer.Explain(snapshot.SelectedRaceDetail?.Phase ?? SelectedRace?.Phase);
            UpdateStatusChips(snapshot);
        }
        finally
        {
            _isApplyingSnapshot = false;
        }

        RefreshCommandStates();
    }

    private void UpdateRaceCollection(IReadOnlyList<RaceSummary> races, string? selectedRaceId)
    {
        var desiredIds = races
            .Select(race => race.RaceId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var index = Races.Count - 1; index >= 0; index--)
        {
            if (!desiredIds.Contains(Races[index].RaceId))
            {
                Races.RemoveAt(index);
            }
        }

        for (var desiredIndex = 0; desiredIndex < races.Count; desiredIndex++)
        {
            var desiredRace = races[desiredIndex];
            var currentIndex = FindRaceIndex(desiredRace.RaceId);

            if (currentIndex < 0)
            {
                Races.Insert(desiredIndex, desiredRace);
                continue;
            }

            if (currentIndex != desiredIndex)
            {
                Races.Move(currentIndex, desiredIndex);
            }

            if (!IsSelectedRaceId(desiredRace.RaceId, selectedRaceId) && !Equals(Races[desiredIndex], desiredRace))
            {
                Races[desiredIndex] = desiredRace;
            }
        }
    }

    private void RestoreSelectedRace(string? selectedRaceId)
    {
        if (string.IsNullOrWhiteSpace(selectedRaceId))
        {
            return;
        }

        var refreshedSelection = Races.FirstOrDefault(race => string.Equals(race.RaceId, selectedRaceId, StringComparison.OrdinalIgnoreCase));
        if (refreshedSelection is not null && !ReferenceEquals(refreshedSelection, SelectedRace))
        {
            SetProperty(ref _selectedRace, refreshedSelection, nameof(SelectedRace));
        }
    }

    private int FindRaceIndex(string raceId)
    {
        for (var index = 0; index < Races.Count; index++)
        {
            if (string.Equals(Races[index].RaceId, raceId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsSelectedRaceId(string raceId, string? selectedRaceId) =>
        !string.IsNullOrWhiteSpace(selectedRaceId) &&
        string.Equals(raceId, selectedRaceId, StringComparison.OrdinalIgnoreCase);

    private async Task RefreshExploreAsync()
    {
        if (_isRefreshingExplore)
        {
            return;
        }

        _isRefreshingExplore = true;
        IsExploreLoading = true;

        try
        {
            var snapshot = await _exploreDataService.RefreshAsync(CancellationToken.None);
            ReplaceCollection(ScheduledRaces, snapshot.ScheduledRaces);
            ReplaceCollection(GlobalStats, snapshot.GlobalStats);
            ReplaceCollection(Leaderboard, snapshot.Leaderboard);
            ExploreStatusText = FormatExploreStatus(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while refreshing Explore data.");
            ExploreStatusText = $"Explore refresh failed: {ex.Message}";
        }
        finally
        {
            _isRefreshingExplore = false;
            IsExploreLoading = false;
        }
    }

    private void AddSelectedRaceToRundown()
    {
        if (SelectedRace is null && SelectedRaceDetail is null)
        {
            return;
        }

        var raceId = SelectedRaceDetail?.RaceId ?? SelectedRace?.RaceId ?? "unknown";
        var phase = SelectedRaceDetail?.Phase ?? SelectedRace?.Phase ?? "Unknown";
        var entrants = FormatEntrants(SelectedRaceDetail?.EntrantCount ?? SelectedRace?.EntrantCount, SelectedRaceDetail?.MaxEntrants ?? SelectedRace?.MaxEntrants);
        AddRundownItem($"Race #{raceId} - {phase} - {entrants}");
    }

    private void AddRundownLine()
    {
        AddRundownItem(NewRundownText.Trim());
        NewRundownText = "";
    }

    private void ClearRundown()
    {
        RundownItems.Clear();
        PushRundownToOverlay();
    }

    private void AddRundownItem(string item)
    {
        if (string.IsNullOrWhiteSpace(item))
        {
            return;
        }

        if (RundownItems.Count >= 5)
        {
            RundownItems.RemoveAt(0);
        }

        RundownItems.Add(item);
        PushRundownToOverlay();
    }

    private void ApplyOverlayPreferences()
    {
        _overlayStateService.SetPreset(SelectedOverlayPreset, SelectedOverlayPosition);
        PushRundownToOverlay();
        SavePreferences();
    }

    private void PushRundownToOverlay()
    {
        _overlayStateService.SetRundown(RundownItems.ToArray());
        SavePreferences();
    }

    private void SavePreferences()
    {
        try
        {
            _preferencesService.Save(new OperatorPreferences
            {
                OverlayPreset = SelectedOverlayPreset,
                OverlayPosition = SelectedOverlayPosition,
                RundownItems = RundownItems.ToArray()
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to save operator preferences.");
        }
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
        try
        {
            var summary = _clipboardSummaryService.CreateSummary(SelectedRace, SelectedRaceDetail);
            Clipboard.SetText(summary);
            StatusText = "Discord summary copied to clipboard.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to copy Discord summary.");
            HasError = true;
            StatusText = $"Unable to copy Discord summary: {ex.Message}";
        }
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
        AddSelectedRaceToRundownCommand.NotifyCanExecuteChanged();
    }

    private static string FormatStatus(RaceDataSnapshot snapshot)
    {
        var attempted = snapshot.LastAttemptedFetchAt?.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        return attempted is null ? snapshot.Message : $"{snapshot.Message} Last attempt: {attempted}.";
    }

    private void UpdateStatusChips(RaceDataSnapshot snapshot)
    {
        StatusChips.Clear();
        StatusChips.Add(IsOverlayServerRunning ? "Live" : "Offline");
        StatusChips.Add(IsLoading ? "Polling" : "Idle");

        if (snapshot.IsStale)
        {
            StatusChips.Add("Stale");
        }

        if (snapshot.Status == AppStatus.Unavailable)
        {
            StatusChips.Add("Fallback");
            StatusChips.Add("Endpoint unavailable");
        }

        if (snapshot.Status == AppStatus.Error)
        {
            StatusChips.Add("Endpoint unavailable");
        }
    }

    private static string FormatExploreStatus(ExploreDataSnapshot snapshot)
    {
        var fetched = snapshot.LastFetchedAt?.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        return fetched is null ? snapshot.Message : $"{snapshot.Message} Last fetch: {fetched}.";
    }

    private static string FormatEntrants(int? current, int? max) =>
        current is null && max is null ? "Entrants unknown" : $"Entrants {current?.ToString(CultureInfo.InvariantCulture) ?? "?"}/{max?.ToString(CultureInfo.InvariantCulture) ?? "?"}";

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
