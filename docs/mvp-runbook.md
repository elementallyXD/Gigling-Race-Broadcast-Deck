# MVP Runbook

This runbook is the approval checklist for the current Gigling Broadcast Deck MVP.

## Build And Test

```powershell
dotnet restore
dotnet build
dotnet test
```

Expected result:

- Restore succeeds.
- Build succeeds with no errors.
- Unit tests pass.

## Launch

```powershell
dotnet run --project .\src\GiglingBroadcastDeck.App\GiglingBroadcastDeck.App.csproj
```

Expected result:

- WPF operator window opens.
- Header shows the local overlay server status.
- Overlay URL is `http://localhost:5050/overlay` unless configured otherwise.

## Local Server Approval Gate

Open:

```text
http://localhost:5050/api/health
```

Expected result:

- JSON response includes app name, version, server time, overlay mode, and overlay URL.

Open:

```text
http://localhost:5050/overlay
```

Expected result:

- Transparent overlay page loads.
- Page may look blank until a non-hidden overlay mode is selected.

## Race Data Approval Gate

In the desktop app:

1. Click `Refresh Races`.
2. Wait for recent races or an API error.

Expected result:

- If API data is available, recent races appear in the left panel.
- If API data is unavailable, the status bar shows a clear error.
- The app does not crash.

## Selected Race Approval Gate

1. Select a race.
2. Confirm selected race details appear.
3. Confirm raw JSON appears in the bottom panel.

Expected result:

- Race id, phase, entrants, field size, pool, lifecycle explanation, and raw JSON update.
- Unknown/missing API fields show safe fallback text instead of crashing.
- If the race-detail endpoint is unavailable but race-state works, the status bar shows `Unavailable: True` and displays race-state fallback data.

## Overlay Control Approval Gate

1. Open `http://localhost:5050/overlay` in a browser.
2. Select a race in the desktop app.
3. Click `Show Race Card`.
4. Click `Show Ticker`.
5. If the race is resolved, click `Show Result Card`.
6. Click `Hide Overlay`.

Expected result:

- Browser overlay changes without reload.
- `/api/overlay-state` reflects the selected mode and race data.
- Overlay preset, position, lifecycle text, source note, and rundown items are visible in `/api/overlay-state`.

## Explore Approval Gate

1. Open the `Explore` tab.
2. Click `Refresh Explore`.

Expected result:

- Scheduled races, global stats, and ELO leaderboard load when the public endpoints are available.
- If one endpoint is unavailable, the tab still shows partial data from the other endpoints.
- The status text clearly names unavailable or malformed endpoint data.

## Rundown Approval Gate

1. Select a race.
2. Click `Pin Selected Race`.
3. Add a custom ticker line.
4. Click `Show Ticker`.
5. Click `Clear Rundown`.

Expected result:

- Pinned items appear in the rundown list.
- The ticker overlay prefers rundown items over generated race ticker text.
- Clearing the rundown removes pinned overlay ticker items.
- If ticker mode is shown after clearing and a race is still selected, generated race ticker text may still appear; this is separate from pinned rundown copy.

## Discord Summary Approval Gate

1. Select a race.
2. Click `Copy Discord Summary`.
3. Paste into a text editor or Discord draft.

Expected result:

- Summary includes race id, phase, entrants, pool, updated time, and public-data source note.

## Reliability Checks

- Port conflict shows a friendly warning.
- Network failure shows error or stale-data status.
- Malformed API JSON shows error or stale-data status instead of crashing.
- No wallet, signing, private key, auth-token, gameplay POST, auto-play, or auto-join code exists.
- External JSON is parsed through tolerant mapping and raw JSON is preserved for transparency.
- No authenticated REST endpoints, gameplay POST endpoints, wallet logic, transaction signing, or OBS WebSocket control exists.

## Demo Publish

```powershell
.\scripts\publish-win-x64.ps1
```

Expected result:

- A self-contained Windows publish folder is created under `src\GiglingBroadcastDeck.App\bin\Release\net10.0-windows\win-x64\publish`.
- `wwwroot` and `appsettings.json` are included so the overlay and runtime configuration remain inspectable.
