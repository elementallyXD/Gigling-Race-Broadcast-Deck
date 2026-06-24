# Testing And Demo

Use this checklist before recording or submitting the hackathon demo.

## Prerequisites

- Windows.
- .NET 10 SDK.
- Internet connection for public Gigling Racing API calls.
- Browser for testing `http://localhost:5050/overlay`.
- OBS Studio optional but recommended.

## Build Verification

```powershell
dotnet restore GiglingBroadcastDeck.slnx
dotnet build GiglingBroadcastDeck.slnx --configuration Release
dotnet test tests\GiglingBroadcastDeck.Tests\GiglingBroadcastDeck.Tests.csproj --configuration Release
```

## Run Instructions

Start the app:

```powershell
dotnet run --project .\src\GiglingBroadcastDeck.App\GiglingBroadcastDeck.App.csproj
```

Confirm the local overlay server:

- The app header should say the overlay server is running.
- Open `http://localhost:5050/api/health`.
- Open `http://localhost:5050/overlay`.

If the configured port changed, use the overlay URL shown in the app header.

## Manual Functional Test Checklist

- [ ] App starts without crashing.
- [ ] Overlay server starts.
- [ ] `/api/health` returns JSON.
- [ ] `/overlay` opens in a browser.
- [ ] Recent races load or a clear API error is shown.
- [ ] Selecting a race updates the selected race panel.
- [ ] Raw/source JSON is visible.
- [ ] `Show Race Card` updates the overlay.
- [ ] `Show Result Card` updates the overlay when the selected race is resolved.
- [ ] `Show Ticker` updates the overlay.
- [ ] `Hide Overlay` hides the overlay.
- [ ] `Pin Ticker Line` adds custom ticker copy.
- [ ] `Pin Selected Race` adds a race-based rundown line.
- [ ] `Clear Rundown` clears pinned rundown/ticker lines.
- [ ] `Copy Discord Summary` copies text.
- [ ] Explore tab loads scheduled races, stats, or leaderboard data when endpoints are available.
- [ ] API failure does not crash the app.
- [ ] Missing fields do not crash the app.
- [ ] Port conflict is handled or clearly reported.

## OBS Demo Setup

1. Open OBS Studio.
2. Add Source -> Browser.
3. URL: `http://localhost:5050/overlay` or the actual URL shown by the app.
4. Set width to `1920`.
5. Set height to `1080`.
6. Enable transparent background if needed.
7. Start Gigling Broadcast Deck.
8. Select a race.
9. Click overlay buttons in the app.
10. Confirm OBS overlay changes.

## 60-90 Second Demo Script

1. "Gigling Broadcast Deck is a read-only operator panel and OBS Browser Source overlay for presenting Gigling Racing data on stream."
2. Show the Windows operator panel.
3. Click `Refresh Races` and show recent races or the clear API state.
4. Select a race.
5. Point out race intelligence fields and the raw source JSON panel.
6. Click `Show Race Card`.
7. Show the overlay in a browser or OBS Browser Source.
8. Click `Show Ticker` or `Show Result Card`.
9. Click `Copy Discord Summary` and briefly show the generated text.
10. End with future potential: mock demo mode, public realtime updates, and more polished overlay themes.

## Known Demo Risks

- Gigling Racing API unavailable.
  - Mitigation: show the app's error/stale state and explain read-only public API dependency.
- No active races.
  - Mitigation: use scheduled races or Explore tab if available.
- Endpoint shape changed.
  - Mitigation: show raw JSON transparency and tolerant fallback behavior.
- OBS not installed.
  - Mitigation: demo `/overlay` in a browser.
- Local port occupied.
  - Mitigation: free port `5050` or change `Overlay:Port` in `appsettings.json`.
- Internet connection problem.
  - Mitigation: show app startup, overlay server, and documented fallback plan.
- Overlay not refreshing.
  - Mitigation: open `/api/overlay-state` and refresh the browser source.

## Fallback Demo Mode

The current code does not include mock/static race data mode.

TODO before final submission: add a simple local mock data mode or bundled sample JSON so the core demo can run even if the public API is unavailable.
