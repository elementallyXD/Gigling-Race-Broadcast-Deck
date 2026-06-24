# API Notes

The app only uses public read-only `GET` endpoints from `Gigaverse:BaseUrl`, which defaults to:

```text
https://gigaverse.io/api/racing/
```

The API client is implemented in:

```text
src/GiglingBroadcastDeck.Core/Services/GigaverseRacingClient.cs
```

Mapping is implemented in:

```text
src/GiglingBroadcastDeck.Core/Mapping/RaceMapper.cs
```

## GET `races?limit={RaceLimit}`

Purpose:

- Load the recent race list for the Operator tab.

Used by:

- `GigaverseRacingClient.GetRecentRacesRawAsync`
- `RacePollingService.RefreshRecentRacesAsync`
- `RaceMapper.MapRecentRaces`

Fields used when present:

- Race id.
- Phase/status.
- Pool.
- Entry fee.
- Entrant count.
- Max entrants / field size.
- Track length.
- Creator.
- Privacy flag.
- Start/finish time.
- Weather / track condition.
- Race type when exposed through race type, source, or join-hook policy fields.
- Payout fields.
- Source.

Auth:

- Public read-only endpoint. No app auth is sent.

Failure behavior:

- HTTP failures become an error/stale state.
- Invalid JSON becomes an error/stale state.
- Last good race list is kept after a previous successful fetch.

## GET `race/{raceId}`

Purpose:

- Load selected race detail for the Operator tab and overlay.

Used by:

- `GigaverseRacingClient.GetRaceDetailRawAsync`
- `RacePollingService.SelectRaceAsync`
- `RacePollingService.RefreshSelectedRaceAsync`
- `RaceMapper.MapRaceDetail`

Fields used when present:

- All race summary fields.
- Result order / final ranking.
- Entrant/player owner names, usernames, nicknames, and wallet addresses when exposed on result rows or related entrant/player rows.
- Entry rows, slot, join time, juiced state, finish times, and public owner profile summaries when available.
- Raw JSON for transparency.

Auth:

- Public read-only endpoint. No app auth is sent.

Failure behavior:

- If this endpoint fails, the app tries `race-state?raceId={raceId}` as diagnostic fallback display data.
- Invalid JSON marks the selected race data stale if prior detail data exists.

## GET `race-state?raceId={raceId}`

Purpose:

- Diagnostic fallback display source when selected race detail is unavailable.

Used by:

- `GigaverseRacingClient.GetRaceStateRawAsync`
- `RacePollingService.TryLoadRaceStateFallbackAsync`
- `RaceMapper.MapRaceDetail`

Fields used when present:

- Same tolerant race fields as `race/{raceId}`.

Auth:

- Public read-only endpoint. No app auth is sent.

Failure behavior:

- If both selected race detail and race-state fail, the app shows a user-safe error/stale state.

## GET `/api/frontend/noob-summary?wallet={ownerAddress}`

Purpose:

- Resolve public account usernames for race owner wallet addresses so Discord summaries can show owner nicknames when available.

Used by:

- `GigaverseRacingClient.GetNoobSummaryRawAsync`
- `RacePollingService.RefreshSelectedRaceAsync`

Fields used when present:

- `summary.username`.
- Public owner account fields such as noob id, energy, pet count, and top racing Gigling metadata when available.

Auth:

- Public read-only endpoint. No app auth is sent.

Failure behavior:

- Username lookup failures do not make race data fail. The app keeps owner wallet addresses as the fallback display value.

## Local App Endpoints

These are served by the desktop app on localhost and are not Gigaverse endpoints.

Default base URL:

```text
http://localhost:5050
```

- `GET /`: redirects to `/overlay`.
- `GET /overlay`: serves the static OBS overlay page.
- `GET /api/overlay-state`: returns current overlay state JSON.
- `GET /api/health`: returns local server health information.
