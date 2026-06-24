const pollMs = 1000;
const overlay = document.querySelector("#overlay");

const fields = {
  headline: document.querySelector('[data-field="headline"]'),
  phase: document.querySelector('[data-field="phase"]'),
  entrants: document.querySelector('[data-field="entrants"]'),
  pool: document.querySelector('[data-field="pool"]'),
  track: document.querySelector('[data-field="track"]'),
  weather: document.querySelector('[data-field="weather"]'),
  items: document.querySelector('[data-field="items"]'),
  lifecycle: document.querySelector('[data-field="lifecycle"]'),
  sourceNote: document.querySelector('[data-field="source-note"]'),
  resultHeadline: document.querySelector('[data-field="result-headline"]'),
  results: document.querySelector('[data-field="results"]'),
  tickerText: document.querySelector('[data-field="ticker-text"]')
};

let lastUpdatedAt = "";

async function poll() {
  try {
    const response = await fetch("/api/overlay-state", { cache: "no-store" });
    if (!response.ok) {
      throw new Error(`Overlay state returned ${response.status}`);
    }

    const state = await response.json();
    const updatedAt = read(state, "updatedAt", "UpdatedAt") ?? "";
    if (updatedAt !== lastUpdatedAt) {
      render(state);
      lastUpdatedAt = updatedAt;
    }
  } catch (error) {
    renderError(error);
  } finally {
    window.setTimeout(poll, pollMs);
  }
}

function render(state) {
  const mode = normalizeMode(read(state, "mode", "Mode"));
  const preset = normalizeClass(read(state, "preset", "Preset") ?? "Broadcast", "preset");
  const position = normalizeClass(read(state, "position", "Position") ?? "LowerLeft", "position");
  const race = read(state, "selectedRaceDetail", "SelectedRaceDetail", "selectedRace", "SelectedRace") ?? {};

  overlay.className = `overlay mode-${mode} ${preset} ${position}`;
  overlay.classList.toggle("hidden", mode === "hidden");
  if (mode === "hidden") {
    return;
  }

  const raceId = read(race, "raceId", "RaceId") ?? "unknown";
  const headline = read(state, "headline", "Headline") ?? `Race #${raceId}`;

  setText(fields.headline, headline);
  setText(fields.phase, read(race, "phase", "Phase") ?? "Unknown");
  setText(fields.entrants, formatEntrants(race));
  setText(fields.pool, formatPool(read(race, "pool", "Pool")));
  setText(fields.track, formatTrack(read(race, "trackLength", "TrackLength")));
  setText(fields.weather, read(race, "weather", "Weather") ?? "Unknown");
  setText(fields.items, read(race, "itemsMode", "ItemsMode") ?? "Unknown");
  setText(fields.lifecycle, read(state, "lifecycleText", "LifecycleText") ?? "");
  setText(fields.sourceNote, read(state, "sourceNote", "SourceNote") ?? "");
  setText(fields.resultHeadline, headline);
  setText(fields.tickerText, formatTicker(state));
  renderResults(read(race, "resultOrder", "ResultOrder") ?? []);
}

function renderResults(results) {
  fields.results.replaceChildren();
  const safeResults = Array.isArray(results) ? results.slice(0, 6) : [];

  if (safeResults.length === 0) {
    appendResult("Results pending");
    return;
  }

  for (const result of safeResults) {
    appendResult(String(result));
  }
}

function appendResult(text) {
  const item = document.createElement("li");
  item.textContent = text;
  fields.results.appendChild(item);
}

function renderError(error) {
  overlay.className = "overlay mode-ticker";
  setText(fields.tickerText, `Gigling Broadcast Deck overlay waiting for local server: ${error.message}`);
}

function read(source, ...names) {
  for (const name of names) {
    if (source && source[name] !== undefined && source[name] !== null) {
      return source[name];
    }
  }

  return undefined;
}

function setText(element, value) {
  if (element) {
    element.textContent = value;
  }
}

function normalizeMode(mode) {
  if (typeof mode === "number") {
    return ["hidden", "race-card", "result-card", "ticker"][mode] ?? "hidden";
  }

  return toKebab(mode ?? "Hidden");
}

function normalizeClass(value, prefix) {
  return `${prefix}-${toKebab(value)}`;
}

function toKebab(value) {
  return String(value)
    .replace(/([a-z])([A-Z])/g, "$1-$2")
    .toLowerCase();
}

function formatEntrants(race) {
  const current = read(race, "entrantCount", "EntrantCount") ?? "?";
  const max = read(race, "maxEntrants", "MaxEntrants") ?? "?";
  return `${current}/${max}`;
}

function formatPool(value) {
  if (value === null || value === undefined || value === "") {
    return "Unknown";
  }

  const number = Number(value);
  return Number.isFinite(number) ? `${number.toFixed(Math.min(4, decimals(number)))} ETH` : String(value);
}

function formatTrack(value) {
  return value === null || value === undefined || value === "" ? "Unknown" : `${value}m`;
}

function formatTicker(state) {
  const rundown = read(state, "rundownItems", "RundownItems") ?? [];
  const fallback = read(state, "tickerItems", "TickerItems") ?? [];
  const items = Array.isArray(rundown) && rundown.length > 0 ? rundown : fallback;

  if (Array.isArray(items) && items.length > 0) {
    return items.join("  |  ");
  }

  return read(state, "headline", "Headline") ?? "Gigling Racing";
}

function decimals(value) {
  const text = String(value);
  return text.includes(".") ? text.split(".")[1].length : 0;
}

poll();
