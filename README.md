# Gigling Broadcast Deck

Gigling Broadcast Deck is a Windows desktop broadcast and operator tool for Gigling Racing.
It reads public race data, helps an operator select and explain a race, and serves a local OBS Browser Source overlay.

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

## Documentation

- Implementation plan: `docs/gigling-broadcast-deck-implementation-plan.md`
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
