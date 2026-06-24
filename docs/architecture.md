# Architecture

Gigling Broadcast Deck is a small two-project .NET desktop application plus tests. The architecture is intentionally simple for hackathon reliability and demo readability.

## Projects

- `src/GiglingBroadcastDeck.Core`: domain models, tolerant JSON mapping, public API client abstractions, polling state, overlay state, race phase explanation, and summary formatting.
- `src/GiglingBroadcastDeck.App`: WPF operator UI, dependency injection, local Kestrel overlay server, local operator preferences, and static overlay assets.
- `tests/GiglingBroadcastDeck.Tests`: xUnit tests for mapping, polling failure handling, overlay state, Explore data, summary formatting, and race phase explanation.

## Runtime Flow

1. WPF starts the dependency-injection container from `App.xaml.cs`.
2. `LocalOverlayServer` starts a localhost Kestrel server.
3. `RacePollingService` polls public Gigling Racing REST endpoints through `IGigaverseRacingClient`.
4. `RaceMapper` normalizes changing JSON shapes into app-owned nullable models while preserving raw JSON.
5. The operator selects a race and controls overlay mode, preset, position, and rundown lines.
6. `OverlayStateService` stores a thread-safe in-memory snapshot.
7. `overlay.js` polls `/api/overlay-state` and renders the OBS Browser Source overlay.

## Safety Boundary

The application is read-only with respect to Gigaverse and Gigling Racing.

- Allowed: public `GET` requests for race, scheduled, stats, leaderboard, and diagnostic race-state data.
- Disallowed: wallet custody, private keys, transaction signing, authenticated gameplay endpoints, race joining, reward claiming, item use, browser automation, and auto-play.
- Local preferences store only overlay/rundown choices and contain no secrets or cached race API data.

## Error Handling

- HTTP failures become `ApiFetchResult<T>.Failure` values.
- Malformed JSON is caught by polling and Explore services, then shown as user-safe error/stale state.
- Last known good race data remains visible after transient failures.
- The overlay server reports port conflicts with a user-friendly message.
- Unknown/missing fields render as fallback text rather than crashing the UI or overlay.

## Extension Points

- `IRacePhaseExplainer`: keeps lifecycle wording consistent across WPF and overlay state.
- `IRaceMapper`: isolates API-shape tolerance from UI code.
- `IOverlayStateService`: isolates browser-source state from WPF controls.
- `IRealtimeRaceFeed`: reserved for future public realtime updates; disabled for MVP.

Keep future extensions small, read-only, and demoable before adding new integrations.
