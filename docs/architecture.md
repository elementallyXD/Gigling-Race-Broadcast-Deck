# Architecture

Gigling Broadcast Deck is a small Windows desktop app with a local browser overlay server. The architecture is intentionally simple for hackathon demo reliability.

## Solution Structure

```text
GiglingBroadcastDeck.slnx
src/
  GiglingBroadcastDeck.Core/
  GiglingBroadcastDeck.App/
tests/
  GiglingBroadcastDeck.Tests/
```

## Project Responsibilities

`src/GiglingBroadcastDeck.Core`

- Domain models: `RaceSummary`, `RaceDetail`, `OverlayState`, `ExploreDataSnapshot`.
- Public API access: `IGigaverseRacingClient`, `GigaverseRacingClient`.
- JSON mapping: `IRaceMapper`, `RaceMapper`, `JsonElementExtensions`.
- App state services: `RacePollingService`, `ExploreDataService`, `OverlayStateService`.
- Text helpers: `RacePhaseExplainer`, `ClipboardSummaryService`.
- Options: `GigaverseOptions`, `PollingOptions`, `OverlayOptions`, `RealtimeOptions`.

`src/GiglingBroadcastDeck.App`

- WPF shell: `MainWindow.xaml`, `MainWindowViewModel`.
- Dependency injection and app startup: `App.xaml.cs`.
- Local server: `LocalOverlayServer`.
- Local preferences: `OperatorPreferencesService`.
- Overlay assets: `wwwroot/overlay.html`, `wwwroot/overlay.css`, `wwwroot/overlay.js`.

`tests/GiglingBroadcastDeck.Tests`

- Unit tests for mapping, polling failures, overlay state, Explore data, summaries, and phase explanation.

## Data Flow

```text
Gigaverse public API
  -> GigaverseRacingClient
  -> RaceMapper
  -> RacePollingService / ExploreDataService
  -> MainWindowViewModel
  -> OverlayStateService
  -> LocalOverlayServer
  -> /api/overlay-state
  -> overlay.js
  -> OBS Browser Source
```

## Main Services

- `GigaverseRacingClient`: performs read-only public `GET` requests and returns raw JSON or safe failure results.
- `RaceMapper`: normalizes changing JSON shapes into nullable app-owned models.
- `RacePollingService`: refreshes recent races and selected race detail, preserves last good data on failures.
- `ExploreDataService`: loads scheduled races, stats, and leaderboard data with partial failure tolerance.
- `OverlayStateService`: stores the current overlay mode, selected race, preset, position, and rundown items.
- `LocalOverlayServer`: hosts the local overlay page and JSON endpoints.
- `MainWindowViewModel`: coordinates UI commands, timers, selected race state, overlay state, and summaries.

## Local Overlay Server

Default base URL: `http://localhost:5050`.

Endpoints:

- `GET /`: redirects to `/overlay`.
- `GET /overlay`: serves the browser overlay HTML.
- `GET /api/overlay-state`: returns current `OverlayState` JSON.
- `GET /api/health`: returns app name, version, server time, current overlay mode, and overlay URL.

Static files are served from `src/GiglingBroadcastDeck.App/wwwroot` during development and from the published `wwwroot` folder in release output.

## Polling Strategy

- Recent races refresh every `Polling:RecentRacesSeconds` seconds, default `15`.
- Selected race details refresh every `Polling:SelectedRaceSeconds` seconds, default `5`.
- Overlapping polling calls are skipped by a lightweight gate.
- The overlay page polls `/api/overlay-state` every `Overlay:PollMs` milliseconds, default `1000`.
- Realtime support is disabled for the MVP.

## Error Handling

- HTTP failures become `ApiFetchResult<T>.Failure`.
- Invalid JSON is caught by polling and Explore services.
- Last known good race data is kept when refresh fails after a successful fetch.
- Empty race lists show an empty state instead of crashing.
- Missing fields map to `null`, `Unknown`, or empty lists.
- If `race/{raceId}` fails, `race-state?raceId={raceId}` is used as diagnostic fallback display data.
- Port conflicts are reported with a user-friendly message.

## Configuration

Settings file:

```text
src/GiglingBroadcastDeck.App/appsettings.json
```

Main settings:

- `Gigaverse:BaseUrl`
- `Gigaverse:RaceLimit`
- `Gigaverse:TimeoutSeconds`
- `Polling:RecentRacesSeconds`
- `Polling:SelectedRaceSeconds`
- `Polling:StaleAfterSeconds`
- `Overlay:Host`
- `Overlay:Port`
- `Overlay:PollMs`
- `Realtime:Enabled`

Local operator preferences are saved outside the repo in the user's local app data folder. They contain no secrets or race API cache.

## Safety Boundaries

The app is read-only with respect to Gigaverse and Gigling Racing.

Allowed:

- Public race data reads.
- Local overlay state.
- Local operator preferences.
- Clipboard summary generation.

Not allowed:

- Wallet custody.
- Private key or seed phrase handling.
- Transaction signing.
- Race joining.
- Reward claiming.
- Item usage.
- Auto-play or gameplay automation.
- Authenticated gameplay POST endpoints.
- Browser automation.
