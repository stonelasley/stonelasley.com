// Light, dark, or follow the system. The choice lives in localStorage; "system" is the absence of one.
export type Theme = "light" | "dark" | "system";

export const themeStorageKey = "theme";

/**
 * Runs inline in <head>, before first paint, so the page never flashes the wrong theme.
 * Kept dependency-free and wrapped in try: localStorage can throw in private modes. It repeats the rule
 * in applyTheme because it runs before any bundle loads; change both together.
 */
export const themeScript = `(function(){try{var t=localStorage.getItem("${themeStorageKey}");var d=t==="dark"||(t!=="light"&&matchMedia("(prefers-color-scheme: dark)").matches);var e=document.documentElement;e.classList.toggle("dark",d);e.style.colorScheme=d?"dark":"light"}catch(_){}})()`;

export function readTheme(): Theme {
  try {
    const stored = localStorage.getItem(themeStorageKey);
    return stored === "light" || stored === "dark" ? stored : "system";
  } catch {
    return "system";
  }
}

export function applyTheme(theme: Theme) {
  const dark =
    theme === "dark" ||
    (theme === "system" && window.matchMedia("(prefers-color-scheme: dark)").matches);
  document.documentElement.classList.toggle("dark", dark);
  document.documentElement.style.colorScheme = dark ? "dark" : "light";
}

export function saveTheme(theme: Theme) {
  try {
    if (theme === "system") {
      localStorage.removeItem(themeStorageKey);
    } else {
      localStorage.setItem(themeStorageKey, theme);
    }
  } catch {
    // Not persisted; the choice still applies to this page.
  }
}

// A tiny store for useSyncExternalStore: this tab's choices, plus choices made in other tabs.
const listeners = new Set<() => void>();

export function subscribeToTheme(listener: () => void) {
  listeners.add(listener);
  const onStorage = (event: StorageEvent) => {
    if (event.key === themeStorageKey || event.key === null) {
      applyTheme(readTheme());
      listener();
    }
  };
  window.addEventListener("storage", onStorage);
  return () => {
    listeners.delete(listener);
    window.removeEventListener("storage", onStorage);
  };
}

export function chooseTheme(theme: Theme) {
  saveTheme(theme);
  applyTheme(theme);
  listeners.forEach((listener) => listener());
}
