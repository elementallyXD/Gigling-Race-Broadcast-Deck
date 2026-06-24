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
- Weather, faction, items mode.
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

## GET `scheduled`

Purpose:

- Load scheduled race context for the Explore tab.

Used by:

- `GigaverseRacingClient.GetScheduledRacesRawAsync`
- `ExploreDataService.RefreshAsync`
- `RaceMapper.MapScheduledRaces`

Fields used when present:

- Same tolerant fields as recent race summaries.

Auth:

- Public read-only endpoint. No app auth is sent.

Failure behavior:

- The Explore tab can still show stats or leaderboard if this endpoint fails.

## GET `stats`

Purpose:

- Load global racing stats for the Explore tab.

Used by:

- `GigaverseRacingClient.GetGlobalStatsRawAsync`
- `ExploreDataService.RefreshAsync`
- `RaceMapper.MapGlobalStats`

Fields used when present:

- Simple scalar string, number, and boolean properties are displayed as label/value rows.

Auth:

- Public read-only endpoint. No app auth is sent.

Failure behavior:

- The Explore tab can still show scheduled races or leaderboard if this endpoint fails.

## GET `leaderboard/elo?limit=25&offset=0`

Purpose:

- Load public ELO leaderboard context for the Explore tab.

Used by:

- `GigaverseRacingClient.GetLeaderboardRawAsync`
- `ExploreDataService.RefreshAsync`
- `RaceMapper.MapLeaderboard`

Fields used when present:

- Rank.
- Name / pet name.
- Pet id.
- Owner.
- Faction.
- Rarity.
- ELO / rating / score.

Auth:

- Public read-only endpoint. No app auth is sent.

Failure behavior:

- The Explore tab can still show scheduled races or stats if this endpoint fails.

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
