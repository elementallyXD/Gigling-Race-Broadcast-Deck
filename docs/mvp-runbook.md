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

## Discord Summary Approval Gate

1. Select a race.
2. Click `Copy Discord Summary`.
3. Paste into a text editor or Discord draft.

Expected result:

- Summary includes race id, phase, entrants, pool, updated time, and public-data source note.

## Reliability Checks

- Port conflict shows a friendly warning.
- Network failure shows error or stale-data status.
- No wallet, signing, private key, auth-token, gameplay POST, auto-play, or auto-join code exists.
- External JSON is parsed through tolerant mapping and raw JSON is preserved for transparency.
