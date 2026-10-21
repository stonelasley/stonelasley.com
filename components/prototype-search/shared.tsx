"use client";

// PROTOTYPE — throwaway. Variant plumbing and the floating switcher.
import { useRouter, usePathname, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";

export const VARIANTS = [
  { key: "A", name: "Header type-ahead, no results URL" },
  { key: "B", name: "Dedicated /search page, on submit" },
  { key: "C", name: "Command palette (⌘K / /)" },
] as const;
export type VariantKey = (typeof VARIANTS)[number]["key"];

const STORE = "prototype-search-variant";

// ?variant= wins; otherwise the last one picked (so plain nav links don't reset it).
export function useVariant(): VariantKey {
  const params = useSearchParams();
  const fromUrl = params.get("variant") as VariantKey | null;
  const [stored, setStored] = useState<VariantKey>("A");
  useEffect(() => {
    try {
      if (fromUrl) localStorage.setItem(STORE, fromUrl);
      // eslint-disable-next-line react-hooks/set-state-in-effect -- prototype
      else setStored((localStorage.getItem(STORE) as VariantKey) || "A");
    } catch {}
  }, [fromUrl]);
  return fromUrl && VARIANTS.some((v) => v.key === fromUrl) ? fromUrl : stored;
}

export function Highlight({ text, terms }: { text: string; terms: string[] }) {
  if (!terms.length) return <>{text}</>;
  const re = new RegExp(`(${terms.map((t) => t.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")).join("|")})`, "gi");
  return (
    <>
      {text.split(re).map((part, i) =>
        i % 2 === 1 ? (
          <mark key={i} className="bg-foreground/10 text-foreground rounded px-0.5">
            {part}
          </mark>
        ) : (
          <span key={i}>{part}</span>
        )
      )}
    </>
  );
}

export function PrototypeSwitcher() {
  const router = useRouter();
  const pathname = usePathname();
  const params = useSearchParams();
  const current = useVariant();
  const idx = VARIANTS.findIndex((v) => v.key === current);

  const go = useCallback(
    (delta: number) => {
      const next = VARIANTS[(idx + delta + VARIANTS.length) % VARIANTS.length].key;
      const p = new URLSearchParams(params.toString());
      p.set("variant", next);
      router.replace(`${pathname}?${p.toString()}`);
    },
    [idx, params, pathname, router]
  );

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const el = e.target as HTMLElement;
      if (el.closest("input, textarea, [contenteditable]")) return;
      if (e.key === "ArrowLeft") go(-1);
      if (e.key === "ArrowRight") go(1);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [go]);

  return (
    <div className="fixed bottom-4 left-1/2 z-[100] flex -translate-x-1/2 items-center gap-3 rounded-full bg-fuchsia-600 px-4 py-2 font-mono text-sm text-white shadow-2xl">
      <button onClick={() => go(-1)} aria-label="Previous variant" className="px-1 hover:opacity-70">
        ←
      </button>
      <span>
        PROTOTYPE {current} ({VARIANTS[idx].name})
      </span>
      <button onClick={() => go(1)} aria-label="Next variant" className="px-1 hover:opacity-70">
        →
      </button>
    </div>
  );
}
