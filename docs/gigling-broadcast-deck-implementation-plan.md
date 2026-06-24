# Gigling Broadcast Deck Implementation Plan

## 1. Project Understanding
Gigling Broadcast Deck is a Windows-native, read-only broadcast/operator app for Gigling Racing. The MVP fetches public race data, lets an operator select a race, shows race details and raw JSON, and serves an OBS Browser Source overlay at `http://localhost:5050/overlay`.

MVP scope:
- WPF desktop operator panel.
- Public Gigling Racing REST polling.
- Local ASP.NET Core Minimal API server.
- Static HTML/CSS/JS overlay.
- Overlay modes: `Hidden`, `RaceCard`, `ResultCard`, `Ticker`.
- Copy Discord Summary.
- Loading, empty, stale-data, unavailable-endpoint, and API-error states.

Out of scope for MVP:
- Wallets, signing, private keys, auto-play, auto-join, WebView2 game client, OBS WebSocket, Stream Deck plugin, GigaSocket realtime, profit/win recommendations, advanced themes.

Reference project findings:
- `Animated-Lower-Thirds`: useful operator-panel and browser-source split; avoid its jQuery/BroadcastChannel complexity for MVP.
- `websocket-overlays`: useful server-owned state model; implement this via `/api/overlay-state` polling instead of WebSockets.
- `static-browser-overlays`: useful simple local static overlay packaging and CSS organization.
- `obs-browser`: confirms Browser Source is the right integration target.
- `obs-websocket` and `obs-websocket-dotnet`: useful optional stretch only; avoid for MVP due auth/control complexity.
- `stream-overlay`: useful inspiration for desktop overlay later, but out of MVP.

## 2. Architecture Proposal
Use a small two-project .NET solution plus tests.

Production projects:
- `src/GiglingBroadcastDeck.Core`: domain models, API client, tolerant mapping, overlay state, summary formatting, testable logic.
- `src/GiglingBroadcastDeck.App`: WPF UI, DI composition, polling orchestration, local Kestrel overlay server, static overlay assets.

Test project:
- `tests/GiglingBroadcastDeck.Tests`: xUnit tests for mapping, overlay state, stale/error handling, and summary generation.

Primary runtime flow:
- WPF starts DI container.
- `LocalOverlayServer` starts Kestrel on configured localhost port, default `5050`.
- `RacePollingService` fetches `/races?limit=50`.
- Selecting a race fetches `/race/{raceId}`, with `/race-state?raceId={raceId}` as diagnostic/fallback display data only.
- `RaceMapper` preserves raw JSON and maps only required fields.
- Operator buttons update `OverlayStateService`.
- `/api/overlay-state` returns the latest server-owned overlay state.
- `overlay.js` polls `/api/overlay-state` every `750-1000ms` and renders by mode.

Public interfaces to define:
- Models: `RaceSummary`, `RaceDetail`, `OverlayMode`, `OverlayState`, `ApiFetchResult<T>`, `AppStatus`.
- Services: `IGigaverseRacingClient`, `IRaceMapper`, `IOverlayStateService`, `IClipboardSummaryService`, `ILocalOverlayServer`, `IRacePollingService`.
- Local endpoints: `GET /overlay`, `GET /api/overlay-state`, `GET /api/health`.

Configuration:
- `appsettings.json`: `Gigaverse:BaseUrl`, `Gigaverse:RaceLimit`, `Polling:RecentRacesSeconds`, `Polling:SelectedRaceSeconds`, `Overlay:Host`, `Overlay:Port`, `Overlay:PollMs`.
- Defaults: base URL `https://gigaverse.io/api/racing`, port `5050`, recent races `15s`, selected race `5s`, overlay poll `1000ms`.

## 3. Implementation Phases
- Phase 0: Create the implementation-plan document file.
- Phase 1: Solution skeleton and buildable baseline.
- Phase 2: Local overlay server with health and static overlay.
- Phase 3: Core domain models and tolerant JSON mapping.
- Phase 4: Public race API client and polling service.
- Phase 5: WPF operator shell and race list.
- Phase 6: Selected race detail, raw JSON, and stale/error states.
- Phase 7: Overlay state controls and browser overlay rendering.
- Phase 8: Discord summary, docs, and MVP polish.
- Phase 9: Optional features only after MVP approval.

Approval gates:
- After Phase 2: user can open `/overlay` and `/api/health`.
- After Phase 5: user can see races or a clear API error.
- After Phase 7: user can control OBS overlay modes from WPF.
- After Phase 8: MVP is demo-ready.

## 4. Detailed Step-by-Step Task List

### Step 0. Create plan file
Goal: Persist this implementation plan.
Files/modules: `docs/gigling-broadcast-deck-implementation-plan.md`.
Implementation details: Write the accepted plan in Markdown.
Tests/checks: Verify file exists and headings match requested format.
Expected result: Project has a single authoritative implementation plan.
Dependencies: none.

### Step 1. Create solution skeleton
Goal: Establish a buildable .NET workspace.
Files/modules: `GiglingBroadcastDeck.sln`, `src/GiglingBroadcastDeck.Core`, `src/GiglingBroadcastDeck.App`, `tests/GiglingBroadcastDeck.Tests`.
Implementation details: Create class library, WPF app, xUnit project; reference Core from App and Tests.
Tests/checks: `dotnet build`, `dotnet test`.
Expected result: Empty solution builds and tests pass.
Dependencies: Step 0.

### Step 2. Add baseline packages and project settings
Goal: Add only MVP dependencies.
Files/modules: project files.
Implementation details: Add `CommunityToolkit.Mvvm`, ASP.NET Core framework reference as needed, `Microsoft.Extensions.*`, xUnit packages. Target `net10.0-windows`; use `net8.0-windows` only if local SDK prevents .NET 10 build.
Tests/checks: `dotnet restore`, `dotnet build`.
Expected result: Package graph is minimal and buildable.
Dependencies: Step 1.

### Step 3. Add configuration model
Goal: Centralize runtime settings.
Files/modules: `appsettings.json`, `Core/Options`, `App/Composition`.
Implementation details: Define options for Gigaverse, polling, overlay host/port, stale thresholds. Bind via `Microsoft.Extensions.Configuration`.
Tests/checks: Unit test default option validation.
Expected result: App can read defaults without hardcoded scattered constants.
Dependencies: Step 2.

### Step 4. Add logging and DI composition
Goal: Make services composable and observable.
Files/modules: `App.xaml.cs`, `ServiceRegistration` classes.
Implementation details: Use `Host.CreateDefaultBuilder` or equivalent WPF host bootstrap; register logging, options, app services, view models.
Tests/checks: App starts; `dotnet build`.
Expected result: WPF has a working service provider.
Dependencies: Step 3.

### Step 5. Implement local overlay server baseline
Goal: Start Kestrel inside the WPF process.
Files/modules: `LocalOverlayServer`, `wwwroot/overlay.html`, `wwwroot/overlay.css`, `wwwroot/overlay.js`.
Implementation details: Bind to `http://localhost:5050`; map `/api/health`; serve `/overlay`; serve static assets.
Tests/checks: Run app; visit `/api/health` and `/overlay`.
Expected result: Browser opens health JSON and blank transparent overlay.
Dependencies: Step 4.

### Step 6. Handle port conflict gracefully
Goal: Prevent confusing startup failure.
Files/modules: `LocalOverlayServer`, WPF startup status/dialog.
Implementation details: Catch bind failure; log it; show a user-friendly message saying port `5050` is in use and can be changed in `appsettings.json`.
Tests/checks: Occupy port manually; run app.
Expected result: App shows useful error and does not crash.
Dependencies: Step 5.

### Step 7. Define core domain models
Goal: Lock minimal internal data contracts.
Files/modules: `Core/Models`.
Implementation details: Add `RaceSummary`, `RaceDetail`, `OverlayMode`, `OverlayState`, fetch/status result types. Use nullable fields for uncertain API data; preserve `RawJson`.
Tests/checks: Build.
Expected result: Stable app-owned model layer.
Dependencies: Step 2.

### Step 8. Implement tolerant JSON helpers
Goal: Safely extract changing API fields.
Files/modules: `Core/Mapping/JsonElementExtensions`.
Implementation details: Add helpers for string, decimal, int, arrays, nested paths, case-insensitive property lookup, and ETH/wei best-effort formatting only when source shape is clear.
Tests/checks: Unit tests with missing, renamed, null, wrong-type fields.
Expected result: Bad or unexpected JSON cannot crash mapping.
Dependencies: Step 7.

### Step 9. Implement `RaceMapper`
Goal: Convert raw race JSON into app models.
Files/modules: `Core/Mapping/RaceMapper`.
Implementation details: Map race id, phase, pool, entry fee, entrant count, max entrants, result order, last fetched time, raw JSON. Unknown values become `"Unknown"`.
Tests/checks: Unit tests with sample race-list and race-detail JSON.
Expected result: UI can rely on stable models.
Dependencies: Step 8.

### Step 10. Implement `GigaverseRacingClient`
Goal: Fetch public Gigling Racing endpoints.
Files/modules: `Core/Services/GigaverseRacingClient`.
Implementation details: Use `HttpClient`; implement `GetRecentRacesAsync`, `GetRaceDetailAsync`, `GetRaceStateAsync`; support cancellation; never call auth or POST gameplay endpoints.
Tests/checks: Unit test with fake `HttpMessageHandler`; optional manual live endpoint smoke test.
Expected result: API fetch layer returns raw JSON or structured errors.
Dependencies: Step 9.

### Step 11. Add resilient fetch behavior
Goal: Keep last good data during transient failures.
Files/modules: `RacePollingService`, result/cache models.
Implementation details: Track last success, last attempt, stale flag, error message; prevent overlapping requests with a lightweight gate.
Tests/checks: Unit test failure after success keeps cached data and marks stale.
Expected result: API failures are visible but non-fatal.
Dependencies: Step 10.

### Step 12. Build WPF main layout
Goal: Create the operator panel shell.
Files/modules: `MainWindow.xaml`, `MainWindowViewModel`.
Implementation details: Three top columns: recent races, selected race, overlay controls; bottom status/error bar and raw JSON panel. Use simple dark theme and WPF bindings.
Tests/checks: Run app; resize window; verify no layout overlap.
Expected result: Usable empty operator shell.
Dependencies: Step 4.

### Step 13. Add app state view model
Goal: Bind UI to race data and app status.
Files/modules: `ViewModels/MainWindowViewModel`.
Implementation details: Use CommunityToolkit commands/properties. Include `RefreshRacesCommand`, selected race, race list, loading, empty, error, stale, and status text.
Tests/checks: ViewModel unit tests where practical; manual UI check.
Expected result: UI can show state transitions.
Dependencies: Step 12.

### Step 14. Wire recent race polling into UI
Goal: Load recent races into the desktop app.
Files/modules: `RacePollingService`, `MainWindowViewModel`.
Implementation details: Start polling on app load; refresh every `15s`; allow manual refresh; update UI on dispatcher.
Tests/checks: Live API or mocked failure run; verify loading, empty, error.
Expected result: Races appear or clear error appears.
Dependencies: Step 13.

### Step 15. Implement race selection flow
Goal: Load selected race details.
Files/modules: `MainWindowViewModel`, `RacePollingService`.
Implementation details: Selecting a race triggers detail fetch and starts selected-race polling every `5s`; update raw JSON and status.
Tests/checks: Select a race; verify detail fields update.
Expected result: Operator can inspect a race.
Dependencies: Step 14.

### Step 16. Add raw JSON and transparency panel
Goal: Make source data visible and explainable.
Files/modules: `MainWindow.xaml`, `RacePhaseExplainer` or helper.
Implementation details: Show raw JSON text; add lifecycle explanation for phases `IDLE`, `OPEN`, `RESOLVING`, `RESOLVED`, `CANCELLED`, `Unknown`.
Tests/checks: Select races in different phases or mock phase values.
Expected result: Judges/viewers can verify source data.
Dependencies: Step 15.

### Step 17. Implement overlay state service
Goal: Store current overlay state server-side.
Files/modules: `Core/Services/OverlayStateService`.
Implementation details: Thread-safe in-memory state with `SetMode`, `SetSelectedRace`, `SetTicker`, `Hide`, `GetSnapshot`.
Tests/checks: Unit tests for mode changes and snapshot immutability.
Expected result: WPF and local API share consistent overlay state.
Dependencies: Step 16.

### Step 18. Expose `/api/overlay-state`
Goal: Let the browser overlay read current state.
Files/modules: `LocalOverlayServer`.
Implementation details: Map endpoint to `OverlayStateService.GetSnapshot`; JSON serialize with camelCase; include `updatedAt`.
Tests/checks: Change mode in app; refresh endpoint manually.
Expected result: Endpoint returns current overlay state.
Dependencies: Step 17.

### Step 19. Add overlay control buttons
Goal: Operator can set overlay mode.
Files/modules: `MainWindow.xaml`, `MainWindowViewModel`.
Implementation details: Add commands for Race Card, Result Card, Ticker, Hide. Disable result mode if no selected race; still allow hidden.
Tests/checks: Click each button; verify `/api/overlay-state`.
Expected result: WPF controls overlay state.
Dependencies: Step 18.

### Step 20. Implement browser overlay rendering
Goal: Render modes in OBS Browser Source.
Files/modules: `overlay.html`, `overlay.css`, `overlay.js`.
Implementation details: Poll `/api/overlay-state` every `750-1000ms`; render hidden/race/result/ticker modes; use CSS classes for transitions; no frameworks.
Tests/checks: Open `/overlay`; click WPF buttons; verify live updates without reload.
Expected result: Functional OBS-compatible overlay.
Dependencies: Step 19.

### Step 21. Style overlay for 1920x1080
Goal: Make demo visually strong and legible.
Files/modules: `overlay.css`.
Implementation details: Transparent background, broadcast-safe typography, high-contrast race card, result card, bottom ticker. Avoid visual clutter.
Tests/checks: Browser at 1920x1080 and 1280x720; verify text fits.
Expected result: Overlay looks stream-ready.
Dependencies: Step 20.

### Step 22. Implement Discord summary
Goal: Copy a concise race summary.
Files/modules: `ClipboardSummaryService`, `MainWindowViewModel`.
Implementation details: Generate text from selected race/detail: race id, phase, entrants, pool, updated time, and source note. Use WPF Clipboard.
Tests/checks: Select race; click Copy; paste into text editor.
Expected result: Summary is copied and status confirms it.
Dependencies: Step 16.

### Step 23. Add health/status diagnostics
Goal: Make local debugging easier.
Files/modules: `LocalOverlayServer`, `MainWindowViewModel`.
Implementation details: `/api/health` returns app name, version, server time, overlay mode. UI shows local overlay URL and server status.
Tests/checks: Visit health endpoint; verify UI status.
Expected result: Operator knows server is running.
Dependencies: Step 18.

### Step 24. Add documentation
Goal: Make demo setup repeatable.
Files/modules: `README.md`, `docs/obs-setup.md`, update existing docs if needed.
Implementation details: Include install/run steps, OBS Browser Source setup, port config, MVP boundaries, troubleshooting.
Tests/checks: Follow README from clean terminal.
Expected result: One developer can run and demo the app.
Dependencies: Step 23.

### Step 25. Final MVP hardening
Goal: Stabilize the hackathon demo.
Files/modules: app-wide.
Implementation details: Audit cancellation, UI thread usage, unhandled exceptions, null displays, stale data labels, timeout behavior, logging.
Tests/checks: `dotnet build`, `dotnet test`, manual API failure test, overlay test, port conflict test.
Expected result: MVP is reliable enough to demo.
Dependencies: Step 24.

## Optional Features After MVP Approval
- GigaSocket realtime updates after REST polling works.
- OBS WebSocket integration using `obs-websocket-dotnet`, with explicit password/auth handling.
- Tray icon via `H.NotifyIcon.Wpf`.
- Pet watchlist.
- Theming presets.
- Exportable overlay package.
- Desktop always-on-top preview overlay.
- Stream Deck plugin.

## 5. Testing Plan
Core unit tests:
- `RaceMapper` handles missing fields, nulls, wrong types, unknown phases, and preserves raw JSON.
- `OverlayStateService` changes modes correctly and returns safe snapshots.
- `ClipboardSummaryService` formats useful summary text without null crashes.
- `RacePollingService` keeps last good data and marks stale on failure.

Integration/manual checks after each approval gate:
- Phase 2: app starts, `/api/health` returns JSON, `/overlay` opens.
- Phase 5: race list loads or shows a clear API error.
- Phase 6: selected race detail and raw JSON update.
- Phase 7: buttons update `/api/overlay-state` and overlay changes without reload.
- Phase 8: OBS Browser Source can use `http://localhost:5050/overlay`.

Reliability/security checks:
- No wallet, signing, private key, auth-token, or gameplay POST logic exists.
- Port conflict shows a friendly message.
- Network failures do not crash the UI.
- External JSON is treated as untrusted.
- `dotnet build` and `dotnet test` pass before each approval checkpoint.

## Assumptions
- Use `.NET 10 LTS` and `net10.0-windows`; fall back to `.NET 8` only if the local SDK/tooling blocks .NET 10.
- The initial implementation does not persist app state beyond configuration files; in-memory state is enough for MVP.
- API response shapes may differ from docs, so tolerant mapping and raw JSON preservation are mandatory.
- The implementation plan file should be created at `docs/gigling-broadcast-deck-implementation-plan.md` when execution mode is available.
