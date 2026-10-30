import type { Units } from "./recipe";

// Metric or imperial for Recipe weights. The choice lives in localStorage; metric is the absence of one.
export const unitsStorageKey = "units";

// This page's choice, for when localStorage throws (private modes).
let chosen: Units = "metric";

export function readUnits(): Units {
  try {
    return localStorage.getItem(unitsStorageKey) === "imperial" ? "imperial" : "metric";
  } catch {
    return chosen;
  }
}

// A tiny store for useSyncExternalStore, as in theme.ts: this tab's choices, plus choices made in other tabs.
const listeners = new Set<() => void>();

export function subscribeToUnits(listener: () => void) {
  listeners.add(listener);
  const onStorage = (event: StorageEvent) => {
    if (event.key === unitsStorageKey || event.key === null) listener();
  };
  window.addEventListener("storage", onStorage);
  return () => {
    listeners.delete(listener);
    window.removeEventListener("storage", onStorage);
  };
}

export function chooseUnits(units: Units) {
  chosen = units;
  try {
    if (units === "metric") {
      localStorage.removeItem(unitsStorageKey);
    } else {
      localStorage.setItem(unitsStorageKey, units);
    }
  } catch {
    // Not persisted; the choice still applies to this page.
  }
  listeners.forEach((listener) => listener());
}
