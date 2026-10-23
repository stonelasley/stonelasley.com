"use client";

import { useEffect, useRef, useSyncExternalStore, type KeyboardEvent } from "react";
import { applyTheme, chooseTheme, readTheme, subscribeToTheme, type Theme } from "@/lib/theme";
import { cn } from "@/lib/utils";

const options: { value: Theme; label: string; iconPath: string }[] = [
  {
    value: "light",
    label: "Light",
    iconPath:
      "M12 3v2.25m6.364.386l-1.591 1.591M21 12h-2.25m-.386 6.364l-1.591-1.591M12 18.75V21m-4.773-4.227l-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0z",
  },
  {
    value: "dark",
    label: "Dark",
    iconPath:
      "M21.752 15.002A9.718 9.718 0 0118 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 003 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 009.002-5.998z",
  },
  {
    value: "system",
    label: "System",
    iconPath:
      "M9 17.25v1.007a3 3 0 01-.879 2.122L7.5 21h9l-.621-.621A3 3 0 0115 18.257V17.25m6-12V15a2.25 2.25 0 01-2.25 2.25H5.25A2.25 2.25 0 013 15V5.25m18 0A2.25 2.25 0 0018.75 3H5.25A2.25 2.25 0 003 5.25m18 0V12a2.25 2.25 0 01-2.25 2.25H5.25A2.25 2.25 0 013 12V5.25",
  },
];

export function ThemeToggle() {
  // The server can't see localStorage, so it renders "system"; the inline head script has already
  // painted the right colours, and this catches up on hydration.
  const theme = useSyncExternalStore(subscribeToTheme, readTheme, () => "system" as const);
  const buttons = useRef<(HTMLButtonElement | null)[]>([]);

  // While following the system, track changes to it.
  useEffect(() => {
    if (theme !== "system") return;
    const media = window.matchMedia("(prefers-color-scheme: dark)");
    const onChange = () => applyTheme("system");
    media.addEventListener("change", onChange);
    return () => media.removeEventListener("change", onChange);
  }, [theme]);

  // Radio-group keyboard pattern: one tab stop; arrow keys move and select.
  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const step = { ArrowRight: 1, ArrowDown: 1, ArrowLeft: -1, ArrowUp: -1 }[event.key];
    if (!step) return;
    event.preventDefault();
    const current = options.findIndex((option) => option.value === theme);
    const next = (current + step + options.length) % options.length;
    chooseTheme(options[next].value);
    buttons.current[next]?.focus();
  };

  return (
    <div
      onKeyDown={onKeyDown}
      role="radiogroup"
      aria-label="Theme"
      className="border-border flex rounded-md border p-0.5"
    >
      {options.map((option, index) => (
        <button
          key={option.value}
          ref={(button) => {
            buttons.current[index] = button;
          }}
          type="button"
          role="radio"
          aria-checked={theme === option.value}
          tabIndex={theme === option.value ? 0 : -1}
          title={option.label}
          onClick={() => chooseTheme(option.value)}
          className={cn(
            "h-8 w-8 rounded transition-colors",
            theme === option.value
              ? "bg-accent text-accent-foreground"
              : "text-muted-foreground hover:text-foreground"
          )}
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
            strokeWidth={1.5}
            stroke="currentColor"
            className="mx-auto h-4 w-4"
            aria-hidden="true"
          >
            <path strokeLinecap="round" strokeLinejoin="round" d={option.iconPath} />
          </svg>
          <span className="sr-only">{option.label}</span>
        </button>
      ))}
    </div>
  );
}
