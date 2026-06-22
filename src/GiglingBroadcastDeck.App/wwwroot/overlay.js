const pollMs = 1000;
const overlay = document.querySelector("#overlay");

const fields = {
  headline: document.querySelector('[data-field="headline"]'),
  phase: document.querySelector('[data-field="phase"]'),
  entrants: document.querySelector('[data-field="entrants"]'),
  pool: document.querySelector('[data-field="pool"]'),
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
    if ((state.updatedAt ?? state.UpdatedAt) !== lastUpdatedAt) {
      render(state);
      lastUpdatedAt = state.updatedAt ?? state.UpdatedAt ?? "";
    }
  } catch (error) {
    renderError(error);
  } finally {
    window.setTimeout(poll, pollMs);
  }
}

function render(state) {
  const mode = normalizeMode(state.mode ?? state.Mode);
  const race = state.selectedRaceDetail ?? state.SelectedRaceDetail ?? state.selectedRace ?? state.SelectedRace ?? {};

  overlay.className = `overlay mode-${mode}`;
  if (mode === "hidden") {
    overlay.classList.add("hidden");
    return;
  }

  overlay.classList.remove("hidden");

  const raceId = race.raceId ?? race.RaceId ?? "unknown";
  const phase = race.phase ?? race.Phase ?? "Unknown";
  const entrants = formatEntrants(race);
  const pool = formatPool(race.pool ?? race.Pool);
  const headline = state.headline ?? state.Headline ?? `Race #${raceId}`;

  fields.headline.textContent = headline;
  fields.phase.textContent = phase;
  fields.entrants.textContent = entrants;
  fields.pool.textContent = pool;
  fields.resultHeadline.textContent = headline;
  fields.tickerText.textContent = formatTicker(state);
  renderResults(race.resultOrder ?? race.ResultOrder ?? []);
}

function renderResults(results) {
  fields.results.replaceChildren();
  if (!Array.isArray(results) || results.length === 0) {
    const item = document.createElement("li");
    item.textContent = "Results pending";
    fields.results.appendChild(item);
    return;
  }

  for (const result of results.slice(0, 6)) {
    const item = document.createElement("li");
    item.textContent = String(result);
    fields.results.appendChild(item);
  }
}

function renderError(error) {
  overlay.className = "overlay mode-ticker";
  fields.tickerText.textContent = `Gigling Broadcast Deck overlay waiting for local server: ${error.message}`;
}

function normalizeMode(mode) {
  if (typeof mode === "number") {
    return ["hidden", "race-card", "result-card", "ticker"][mode] ?? "hidden";
  }

  return String(mode ?? "Hidden")
    .replace(/([a-z])([A-Z])/g, "$1-$2")
    .toLowerCase();
}

function formatEntrants(race) {
  const current = race.entrantCount ?? race.EntrantCount ?? "?";
  const max = race.maxEntrants ?? race.MaxEntrants ?? "?";
  return `${current}/${max}`;
}

function formatPool(value) {
  if (value === null || value === undefined || value === "") {
    return "Unknown";
  }

  const number = Number(value);
  return Number.isFinite(number) ? `${number.toFixed(Math.min(4, decimals(number)))} ETH` : String(value);
}

function formatTicker(state) {
  const items = state.tickerItems ?? state.TickerItems ?? [];
  return Array.isArray(items) && items.length > 0 ? items.join("  •  ") : (state.headline ?? state.Headline ?? "Gigling Racing");
}

function decimals(value) {
  const text = String(value);
  return text.includes(".") ? text.split(".")[1].length : 0;
}

poll();
