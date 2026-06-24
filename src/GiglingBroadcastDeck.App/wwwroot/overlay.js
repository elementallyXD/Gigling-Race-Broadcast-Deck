const pollMs = 1000;
const overlay = document.querySelector("#overlay");

const fields = {
  headline: document.querySelector('[data-field="headline"]'),
  phase: document.querySelector('[data-field="phase"]'),
  entrants: document.querySelector('[data-field="entrants"]'),
  pool: document.querySelector('[data-field="pool"]'),
  entryFee: document.querySelector('[data-field="entry-fee"]'),
  raceType: document.querySelector('[data-field="race-type"]'),
  track: document.querySelector('[data-field="track"]'),
  weather: document.querySelector('[data-field="weather"]'),
  creator: document.querySelector('[data-field="creator"]'),
  raceStart: document.querySelector('[data-field="race-start"]'),
  access: document.querySelector('[data-field="access"]'),
  source: document.querySelector('[data-field="source"]'),
  lifecycle: document.querySelector('[data-field="lifecycle"]'),
  sourceNote: document.querySelector('[data-field="source-note"]'),
  resultHeadline: document.querySelector('[data-field="result-headline"]'),
  results: document.querySelector('[data-field="results"]'),
  positionsHeadline: document.querySelector('[data-field="positions-headline"]'),
  positions: document.querySelector('[data-field="positions"]'),
  positionsNote: document.querySelector('[data-field="positions-note"]'),
  tickerText: document.querySelector('[data-field="ticker-text"]')
};

const surfaces = {
  raceCard: document.querySelector("#race-card"),
  resultCard: document.querySelector("#result-card"),
  positionsCard: document.querySelector("#positions-card"),
  ticker: document.querySelector("#ticker")
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
  applyModeVisibility(mode);
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
  setText(fields.entryFee, formatPool(read(race, "entryFee", "EntryFee")));
  setText(fields.raceType, read(race, "raceType", "RaceType") ?? "Unknown");
  setText(fields.track, formatTrack(read(race, "trackLength", "TrackLength")));
  setText(fields.weather, read(race, "weather", "Weather") ?? "Unknown");
  setText(fields.creator, shortenAddress(read(race, "creator", "Creator") ?? "Unknown"));
  setText(fields.raceStart, formatDateTime(read(race, "raceStart", "RaceStart")));
  setText(fields.access, read(race, "isPrivate", "IsPrivate") === true ? "Private" : "Public");
  setText(fields.source, read(race, "source", "Source") ?? "Public API");
  setText(fields.lifecycle, read(state, "lifecycleText", "LifecycleText") ?? "");
  setText(fields.sourceNote, read(state, "sourceNote", "SourceNote") ?? "");
  setText(fields.resultHeadline, headline);
  setText(fields.positionsHeadline, headline);
  setText(fields.positionsNote, "Shows public live position data when the selected race payload exposes it.");
  setText(fields.tickerText, formatTicker(state));
  renderEntrantList(fields.results, read(race, "resultEntrants", "ResultEntrants") ?? read(race, "resultOrder", "ResultOrder") ?? [], "Results pending");
  renderEntrantList(fields.positions, read(race, "livePositions", "LivePositions") ?? [], "Live positions unavailable");
}

function renderEntrantList(target, entrants, emptyText) {
  if (!target) {
    return;
  }

  target.replaceChildren();
  const safeEntrants = Array.isArray(entrants) ? entrants.slice(0, 8) : [];

  if (safeEntrants.length === 0) {
    appendEntrant(target, emptyText);
    return;
  }

  for (const entrant of safeEntrants) {
    appendEntrant(target, formatEntrant(entrant));
  }
}

function appendEntrant(target, text) {
  const item = document.createElement("li");

  if (typeof text === "object" && text !== null) {
    const title = document.createElement("strong");
    title.textContent = text.title;
    item.appendChild(title);

    for (const line of text.lines) {
      const detail = document.createElement("span");
      detail.textContent = line;
      item.appendChild(detail);
    }

    return target.appendChild(item);
  }

  item.textContent = text;
  target.appendChild(item);
}

function renderError(error) {
  overlay.className = "overlay mode-ticker";
  applyModeVisibility("ticker");
  setText(fields.tickerText, `Gigling Broadcast Deck overlay waiting for local server: ${error.message}`);
}

function applyModeVisibility(mode) {
  setSurfaceDisplay(surfaces.raceCard, mode === "race-card", "block");
  setSurfaceDisplay(surfaces.resultCard, mode === "result-card", "block");
  setSurfaceDisplay(surfaces.positionsCard, mode === "positions", "block");
  setSurfaceDisplay(surfaces.ticker, mode === "ticker", "grid");
}

function setSurfaceDisplay(element, isVisible, displayValue) {
  if (element) {
    element.style.display = isVisible ? displayValue : "none";
  }
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
    return ["hidden", "race-card", "result-card", "race-card", "ticker"][mode] ?? "hidden";
  }

  const normalized = toKebab(mode ?? "Hidden");
  return normalized === "position" || normalized === "positions" || normalized === "live-positions"
    ? "race-card"
    : normalized;
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

function formatEntrant(entrant) {
  if (entrant === null || entrant === undefined) {
    return "Unknown entrant";
  }

  if (typeof entrant !== "object") {
    return String(entrant);
  }

  const name = read(entrant, "displayName", "DisplayName", "petId", "PetId") ?? "Unknown entrant";
  const owner = read(entrant, "ownerName", "OwnerName", "ownerAddress", "OwnerAddress");
  const position = read(entrant, "position", "Position");
  const progress = read(entrant, "progress", "Progress");
  const lines = [];
  const ownerText = owner ? `Owner: ${shortenOwner(owner)}` : "";
  if (ownerText) {
    lines.push(ownerText);
  }

  const petFacts = [
    formatValue("Rarity", read(entrant, "petRarity", "PetRarity")),
    formatValue("Gender", read(entrant, "petGender", "PetGender")),
    formatNumberValue("ELO", read(entrant, "petElo", "PetElo")),
    formatValue("Slot", read(entrant, "slot", "Slot")),
    formatBoolValue("Juiced", read(entrant, "isJuiced", "IsJuiced")),
    formatFinishTime(read(entrant, "finishTimeMs", "FinishTimeMs"))
  ].filter(Boolean);
  if (petFacts.length > 0) {
    lines.push(petFacts.join(" | "));
  }

  const ownerFacts = [
    formatValue("Noob", read(entrant, "ownerNoobId", "OwnerNoobId")),
    formatValue("Stable", formatPetCount(read(entrant, "ownerPetCount", "OwnerPetCount"))),
    formatEnergy(read(entrant, "ownerEnergy", "OwnerEnergy"), read(entrant, "ownerMaxEnergy", "OwnerMaxEnergy"))
  ].filter(Boolean);
  if (ownerFacts.length > 0) {
    lines.push(ownerFacts.join(" | "));
  }

  const topPet = formatTopPet(entrant);
  if (topPet) {
    lines.push(topPet);
  }

  const positionText = formatPosition(position, progress).replace(/^ - /, "");
  if (positionText) {
    lines.push(positionText);
  }

  return { title: name, lines };
}

function formatValue(label, value) {
  return value === null || value === undefined || value === "" ? "" : `${label}: ${value}`;
}

function formatNumberValue(label, value) {
  if (value === null || value === undefined || value === "") {
    return "";
  }

  const number = Number(value);
  return Number.isFinite(number) ? `${label}: ${number.toFixed(0)}` : `${label}: ${value}`;
}

function formatBoolValue(label, value) {
  if (value === null || value === undefined || value === "") {
    return "";
  }

  return `${label}: ${value === true ? "Yes" : value === false ? "No" : value}`;
}

function formatFinishTime(value) {
  if (value === null || value === undefined || value === "") {
    return "";
  }

  const number = Number(value);
  return Number.isFinite(number) ? `Finish: ${(number / 1000).toFixed(2)}s` : `Finish: ${value}`;
}

function formatPetCount(value) {
  if (value === null || value === undefined || value === "") {
    return "";
  }

  return `${value} pets`;
}

function formatEnergy(current, max) {
  if ((current === null || current === undefined || current === "") &&
      (max === null || max === undefined || max === "")) {
    return "";
  }

  return `Energy: ${current ?? "?"}/${max ?? "?"}`;
}

function formatTopPet(entrant) {
  const id = read(entrant, "ownerTopPetId", "OwnerTopPetId");
  const name = read(entrant, "ownerTopPetName", "OwnerTopPetName");
  const rarity = read(entrant, "ownerTopPetRarity", "OwnerTopPetRarity");
  const gender = read(entrant, "ownerTopPetGender", "OwnerTopPetGender");
  const elo = read(entrant, "ownerTopPetElo", "OwnerTopPetElo");

  if (!id && !name && !rarity && !gender && !elo) {
    return "";
  }

  return [
    `Top: ${name || id}`,
    rarity,
    gender,
    elo ? `ELO ${Number(elo).toFixed(0)}` : ""
  ].filter(Boolean).join(" | ");
}

function formatPosition(position, progress) {
  if (progress !== null && progress !== undefined && progress !== "") {
    const numericProgress = Number(progress);
    return Number.isFinite(numericProgress) ? ` - ${numericProgress.toFixed(1)}%` : ` - ${progress}`;
  }

  if (position !== null && position !== undefined && position !== "") {
    const numericPosition = Number(position);
    return Number.isFinite(numericPosition) ? ` - ${numericPosition.toFixed(0)}m` : ` - ${position}`;
  }

  return "";
}

function shortenOwner(owner) {
  const text = String(owner);
  return shortenAddress(text);
}

function shortenAddress(value) {
  const text = String(value);
  return text.length > 16 && text.startsWith("0x") ? `${text.slice(0, 6)}...${text.slice(-4)}` : text;
}

function formatDateTime(value) {
  if (!value) {
    return "Unknown";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return String(value);
  }

  return date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
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
