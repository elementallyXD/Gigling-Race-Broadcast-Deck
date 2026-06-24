# Gigling Broadcast Deck

Gigling Broadcast Deck is a Windows desktop broadcast and operator tool for Gigling Racing.
It reads public race data, helps an operator select and explain a race, and serves a local OBS Browser Source overlay.

## Problem

Gigling Racing race data is useful for creators, but it is difficult to turn raw public API responses into a clean livestream story quickly. This app gives a stream operator one read-only control surface for race selection, race explanation, OBS overlay output, pinned ticker copy, and transparent raw-source verification.

The app is intentionally read-only:

- No wallet custody.
- No transaction signing.
- No private keys or seed phrases.
- No auto-play, auto-join, or profit/entry optimization.

## Stack

- C# / .NET 10
- WPF desktop UI
- ASP.NET Core Minimal API local server
- Static HTML/CSS/JS overlay for OBS Browser Source
- `HttpClient` and `System.Text.Json`

## Features

- Recent race polling and selected-race live refresh.
- Race intelligence fields such as entry fee, pool, field progress, track length, weather, faction, items, payouts, and source notes when available.
- Operator rundown with pinned ticker lines, selected-race pins, and one-click clearing.
- Overlay modes: `Hidden`, `RaceCard`, `ResultCard`, and `Ticker`.
- Overlay presets: `Broadcast`, `Compact`, and `DataDesk`.
- Explore tab for scheduled races, global stats, and ELO leaderboard data.
- Raw JSON transparency panel for source verification.
- Stale, fallback, unavailable endpoint, and malformed JSON handling.

## Public Gigaverse Endpoints Used

All requests are read-only public `GET` requests under `Gigaverse:BaseUrl`.

- `GET races?limit={RaceLimit}` for recent races.
- `GET race/{raceId}` for selected race detail.
- `GET race-state?raceId={raceId}` as a diagnostic fallback display source.
- `GET scheduled` for scheduled race context.
- `GET stats` for global racing stats.
- `GET leaderboard/elo?limit=25&offset=0` for Explore leaderboard context.

The app does not call authenticated endpoints, gameplay POST endpoints, wallet endpoints, signing endpoints, or transaction endpoints.

## Run

```powershell
dotnet restore
dotnet build
dotnet run --project .\src\GiglingBroadcastDeck.App\GiglingBroadcastDeck.App.csproj
```

The local overlay server starts at:

- `http://localhost:5050/overlay`
- `http://localhost:5050/api/health`
- `http://localhost:5050/api/overlay-state`

## Test

```powershell
dotnet test
```

## Publish Demo Build

```powershell
.\scripts\publish-win-x64.ps1
```

The release output is created under:

```text
src\GiglingBroadcastDeck.App\bin\Release\net10.0-windows\win-x64\publish
```

## GitHub Actions

The repository includes a Windows GitHub Actions workflow at `.github/workflows/dotnet-desktop.yml`.

It validates pushes and pull requests to `main`:

- Restores `GiglingBroadcastDeck.slnx`.
- Builds the solution in `Debug` and `Release`.
- Runs the xUnit test project in both configurations.
- Uploads test results as workflow artifacts.

For pushes to `main` and manual workflow runs, it also:

- Publishes a self-contained `win-x64` demo build.
- Uploads the published app as a workflow artifact.

The workflow intentionally does not sign or create an MSIX package yet. The app currently has no Windows Application Packaging Project, signing certificate, or store-upload flow, so CI produces a simple demo-ready publish folder instead.

## Configuration

Runtime settings live in `src/GiglingBroadcastDeck.App/appsettings.json`.

Important defaults:

- `Gigaverse:BaseUrl`: `https://gigaverse.io/api/racing/`
- `Gigaverse:RaceLimit`: `50`
- `Gigaverse:TimeoutSeconds`: `10`
- `Polling:RecentRacesSeconds`: `15`
- `Polling:SelectedRaceSeconds`: `5`
- `Polling:StaleAfterSeconds`: `45`
- `Overlay:Host`: `localhost`
- `Overlay:Port`: `5050`
- `Overlay:PollMs`: `1000`
- `Realtime:Enabled`: `false`

If port `5050` is already in use, the app shows a friendly warning. Free the port or change `Overlay:Port`.

Operator preferences are stored locally under the current Windows user profile. They include overlay preset, overlay position, and rundown items.

`Clear Rundown` removes saved/pinned rundown lines and clears the pinned ticker fallback used by the overlay. If ticker mode is shown with no pinned rundown items, the overlay may still render generated race ticker text from the currently selected race.

## MVP Workflow

1. Start the desktop app.
2. Confirm the overlay server status in the header.
3. Refresh recent races or read the API error shown in the status bar.
4. Select a race to inspect normalized data and raw JSON.
5. Choose an overlay preset and position.
6. Pin selected races or custom ticker lines to the broadcast rundown.
7. Use the overlay controls to show `Race Card`, `Result Card`, `Ticker`, or `Hidden`.
8. Click `Clear Rundown` when pinned ticker lines should be removed from the overlay.
9. Add `http://localhost:5050/overlay` as an OBS Browser Source.
10. Use the Explore tab for scheduled races, stats, and leaderboard context.
11. Use `Copy Discord Summary` for a concise race update.

## Hackathon Demo Flow

1. Launch the app and show that the local overlay server is running.
2. Open `http://localhost:5050/overlay` in a browser or OBS Browser Source.
3. Refresh races and select a public race.
4. Show the Race Intelligence panel and raw JSON transparency panel.
5. Switch between `Race Card`, `Ticker`, and `Result Card` if the selected race is resolved.
6. Pin a custom rundown line, show the ticker, then clear the rundown to demonstrate operator control.
7. Open the Explore tab for scheduled races, stats, and leaderboard context.
8. Copy a Discord summary as the final creator utility moment.

## Known Limitations

- Public API response shapes may change; the app uses tolerant mapping and raw JSON preservation to reduce demo risk.
- Realtime updates are disabled by default; REST polling is the stable MVP transport.
- OBS WebSocket control, Stream Deck integration, wallets, signing, auto-join, auto-play, and gameplay automation are intentionally out of scope.
- The publish output is a self-contained folder, not a signed installer or MSIX package.

## Documentation

- Implementation plan: `docs/gigling-broadcast-deck-implementation-plan.md`
- Architecture notes: `docs/architecture.md`
- OBS setup: `docs/obs-setup.md`
- MVP runbook: `docs/mvp-runbook.md`
- Source research: `docs/gigling-broadcast-deck-resources.md`

## Verification Checklist

- `dotnet build` passes.
- `dotnet test` passes.
- Desktop app launches.
- `http://localhost:5050/api/health` returns JSON.
- `http://localhost:5050/overlay` loads.
- Race list shows data or a clear API error.
- Selecting a race updates selected details and raw JSON.
- Overlay buttons update the browser overlay without reload.
- `Clear Rundown` removes pinned rundown items from `/api/overlay-state` and from ticker rendering.
