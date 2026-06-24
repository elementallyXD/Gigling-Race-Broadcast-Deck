# Hackathon Submission

## Project Name

Gigling Broadcast Deck

## One-Line Pitch

A read-only Windows operator panel and OBS Browser Source overlay for presenting Gigling Racing data on stream.

## Short Description

Gigling Broadcast Deck helps creators and race hosts turn public Gigling Racing data into livestream-ready race cards, result cards, ticker copy, and transparent source-data views. It focuses on broadcast clarity, operator control, and safe read-only data access.

## Problem Solved

Raw race data is hard to explain quickly during a live stream. The app gives stream operators a simple desktop workflow for selecting a race, showing useful context, and sending readable overlay graphics to OBS.

## Gigaverse / Gigling Racing Usage

The app reads public Gigling Racing REST endpoints for:

- Recent races.
- Selected race details.
- Race-state fallback diagnostics.

It normalizes API data into internal models and keeps raw JSON visible for transparency.

## MVP Features

- WPF operator panel.
- Local OBS Browser Source overlay.
- Race card, result card, ticker, and hidden overlay modes.
- Overlay presets and positions.
- Recent race polling and selected race refresh.
- Broadcast rundown with pinned ticker lines.
- Raw JSON transparency panel.
- Discord summary copy with resolved-race places and owner names/addresses when final order data is available.
- Stale/error/fallback states for public API failures.

## What Makes It Unique

- Combines creator workflow, race transparency, and OBS overlay output in one small tool.
- Keeps public data visible and explainable instead of hiding everything behind UI cards.
- Gives operators live control without adding wallet, signing, or gameplay automation risk.

## Safety Statement

Gigling Broadcast Deck is read-only and non-custodial. It does not store private keys, sign transactions, join races, claim rewards, use items, or call authenticated gameplay POST endpoints.

## Future Potential

- Mock demo mode for offline submissions.
- Public realtime race updates after REST polling remains stable.
- More overlay themes.
- Exportable overlay package.
- Stream Deck or OBS WebSocket integration as optional operator-control extensions.

## Demo Link

TODO: Add final demo video link.

## Repository Link

TODO: Add public repository link.

## Screenshots

TODO: Add operator panel and overlay screenshots.
