# Gigling Broadcast Deck

## Project
- Gigling Broadcast Deck is a Windows desktop broadcast and operator tool for Gigling Racing.
- The app helps creators, streamers, and hosts present race data clearly with desktop UI, local overlay output, and transparent source data.
- This project is read-only with respect to Gigaverse/Gigling Racing: fetch public data, transform it, and point users to official experiences when actions must happen elsewhere.

## Product Boundaries
- Build broadcast, transparency, and operator workflows only.
- Do not build gameplay automation, wallet custody, transaction signing, private-key handling, or profit/entry optimization.
- Do not frame the product as a betting tool, race-entry optimizer, or auto-player.

## Stack
- C# / .NET 10 LTS
- WPF desktop UI
- ASP.NET Core Minimal API for the local server
- Static HTML/CSS/JS for OBS overlays
- `HttpClient` for network access
- `System.Text.Json` only for JSON work

## Development Rules
- Build vertical slices end to end.
- Keep the MVP simple and demoable.
- Prefer public REST polling before realtime/WebSocket work.
- Normalize external API responses into internal models before rendering them.
- Use defensive JSON parsing because API shapes can change.
- Include loading, empty, error, stale-data, and unavailable-endpoint states in any user-facing data view.
- Favor simple WPF and standard .NET primitives over heavy UI frameworks or extra dependencies.
- Keep the desktop app and local server loosely coupled so overlay rendering can evolve independently.
- Treat all external input as untrusted.
- Never hardcode secrets, tokens, private keys, wallet phrases, or internal URLs.

## Source-of-Truth Notes
- Use `gigaverse-glhfers` for Gigling Racing API and protocol documentation.
- Use Microsoft Learn for WPF, .NET, and desktop app guidance.
- Use OpenAI docs when the task is specifically about Codex or OpenAI tooling.
- Prefer official docs and repository files over memory when behavior matters.

## Implementation Priorities
- Favor a clean operator workflow over a feature-rich but confusing UI.
- Keep the main surface focused on race selection, race cards, result cards, overlays, and source-data explanation.
- Make the overlay usable in OBS and easy to reason about on a livestream.
- Keep broadcast copy concise, legible, and friendly to non-technical viewers.
- Preserve transparent access to raw JSON or source data for verification.

## Testing And Verification
- Run `dotnet build`.
- Run `dotnet test`.
- Manually verify the app launches and the local overlay is reachable at `http://localhost:5050/overlay`.
- If port `5050` is already in use, fail gracefully with a user-friendly message that explains how to free the port or change it in `appsettings.json`.
- Verify loading, empty, error, and stale-data states before calling a slice done.

## Editing Expectations
- Prefer small, reviewable changes.
- Keep changes aligned with the existing architecture and docs.
- Update or add docs in `docs/` when behavior or workflow changes.
- Do not remove user work or unrelated files.
