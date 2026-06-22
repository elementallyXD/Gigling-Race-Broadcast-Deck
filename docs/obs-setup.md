# OBS Setup

Use this guide to connect Gigling Broadcast Deck to OBS as a Browser Source.

## Prerequisites

- Windows desktop environment.
- .NET 10 SDK/runtime installed.
- Gigling Broadcast Deck builds successfully with `dotnet build`.
- The desktop app is running.

## Add The Overlay

1. Open OBS.
2. In a scene, add a new `Browser` source.
3. Set the URL to `http://localhost:5050/overlay`.
4. Set width to `1920`.
5. Set height to `1080`.
6. Enable transparency if OBS offers the option.
7. Click `OK`.

## Operator Workflow

1. Start Gigling Broadcast Deck.
2. Confirm the header says the overlay server is running.
3. Select a race from the recent race list.
4. Use `Show Race Card`, `Show Result Card`, `Show Ticker`, or `Hide Overlay`.
5. Pin custom ticker lines or selected races in the rundown when you want operator-controlled ticker copy.
6. Click `Clear Rundown` when pinned ticker lines should disappear from the overlay.
7. Verify the OBS Browser Source updates without reloading.

## Health Check

Open this URL in a browser:

```text
http://localhost:5050/api/health
```

Expected result:

- JSON response with app name, version, server time, current overlay mode, and overlay URL.

## Troubleshooting

### Port 5050 Is In Use

If the app warns that port `5050` is already in use, close the process using the port or change `Overlay:Port` in:

```text
src/GiglingBroadcastDeck.App/appsettings.json
```

Then restart the desktop app and update the OBS Browser Source URL.

### Overlay Is Blank

Blank is expected when the mode is `Hidden`.

Check:

- A race is selected in the desktop app.
- The operator selected `Show Race Card`, `Show Result Card`, or `Show Ticker`.
- `http://localhost:5050/api/overlay-state` returns current state JSON.

### Old Ticker Copy Still Appears

Check whether the text is pinned rundown copy or generated race copy:

- `rundownItems` and `tickerItems` should be empty in `/api/overlay-state` immediately after `Clear Rundown`.
- If a race is selected and ticker mode is shown, generated race ticker text can still appear when no pinned rundown items exist.
- Pin a new rundown line to override generated race ticker text.

### Race Data Does Not Load

The app is read-only and depends on the public Gigling Racing REST API. If the API is unavailable or the response shape changes, the desktop app should show a clear error or stale-data status instead of crashing.

## MVP Boundaries

This overlay does not control OBS, sign transactions, join races, automate gameplay, or handle private keys. It only renders public race data selected by the operator.
