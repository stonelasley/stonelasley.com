"use client";

import { useRouter } from "next/navigation";
import { useEffect, useId, useRef, useState, type KeyboardEvent } from "react";
import { createPortal } from "react-dom";
import type { PostSummary, SearchResult } from "@/lib/api";
import { contentAreas, type AreaSlug } from "@/lib/areas";
import { cn } from "@/lib/utils";
import { HighlightedExcerpt } from "./SearchResults";

// Each keystroke-triggered query costs an embedding and a Worker call, so both are part of the spec.
export const DEBOUNCE_MS = 250;
export const MIN_QUERY_LENGTH = 2;

type Item = { key: string; label: string; href: string; render: React.ReactNode };

/** The header's search button and the palette it opens (⌘K, Ctrl+K, or / when not typing). */
export function SearchPalette() {
  const [open, setOpen] = useState(false);
  // Set once the shortcuts are listening, so tests (and only tests) can wait for hydration.
  const [ready, setReady] = useState(false);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect -- marks hydration, runs once
    setReady(true);
    const onKey = (event: globalThis.KeyboardEvent) => {
      const typing = (event.target as HTMLElement | null)?.closest(
        "input, textarea, select, [contenteditable]"
      );
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        setOpen((wasOpen) => !wasOpen);
      } else if (event.key === "/" && !typing) {
        event.preventDefault();
        setOpen(true);
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        aria-label="Search"
        aria-haspopup="dialog"
        data-ready={ready || undefined}
        className="border-border hover:bg-accent text-muted-foreground hover:text-foreground flex h-9 items-center gap-2 rounded-md border px-2.5 text-sm transition-colors"
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          strokeWidth={1.5}
          stroke="currentColor"
          className="h-4 w-4"
          aria-hidden="true"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z"
          />
        </svg>
        <kbd className="bg-muted hidden rounded px-1.5 font-mono text-[11px] sm:inline">⌘K</kbd>
      </button>
      {/* Portalled to <body>: the header's backdrop blur would otherwise trap a fixed overlay inside it. */}
      {open && createPortal(<Palette onClose={() => setOpen(false)} />, document.body)}
    </>
  );
}

function Palette({ onClose }: { onClose: () => void }) {
  const router = useRouter();
  const listId = useId();
  const input = useRef<HTMLInputElement>(null);
  const [query, setQuery] = useState("");
  const [area, setArea] = useState<AreaSlug | null>(null);
  const [active, setActive] = useState(0);
  const [latest, setLatest] = useState<PostSummary[]>([]);
  const [result, setResult] = useState<SearchResult | null>(null);

  const term = query.trim();
  const searching = term.length >= MIN_QUERY_LENGTH;

  useEffect(() => input.current?.focus(), []);

  // Latest Posts for the empty state, per chip.
  useEffect(() => {
    const controller = new AbortController();
    fetch(`/api/latest${area ? `?area=${area}` : ""}`, { signal: controller.signal })
      .then((response) => response.json())
      .then(setLatest)
      .catch(() => {});
    return () => controller.abort();
  }, [area]);

  // As-you-type search, debounced; a newer query cancels the one before it.
  useEffect(() => {
    if (!searching) return;
    const controller = new AbortController();
    const timer = setTimeout(() => {
      const params = new URLSearchParams({ q: term });
      if (area) params.set("area", area);
      fetch(`/api/search?${params}`, { signal: controller.signal })
        .then((response) => response.json())
        .then((data: SearchResult) => {
          setResult(data);
          setActive(0);
        })
        .catch(() => {});
    }, DEBOUNCE_MS);
    return () => {
      clearTimeout(timer);
      controller.abort();
    };
  }, [term, area, searching]);

  const allResultsHref = `/search?${new URLSearchParams({ q: term, ...(area ? { area } : {}) })}`;
  const hits = searching && result ? result.results : [];
  const groups = contentAreas
    .map((a) => ({ area: a, hits: hits.filter((hit) => hit.area === a.slug) }))
    .filter((group) => group.hits.length > 0);

  // One flat list in display order, so ↑/↓ and Enter follow what's on screen.
  const items: Item[] = searching
    ? [
        ...groups.flatMap((group) =>
          group.hits.map((hit) => ({
            key: hit.id,
            label: hit.title,
            href: `/${hit.area}/${hit.slug}`,
            render: (
              <>
                <div className="font-medium">{hit.title}</div>
                <div className="text-muted-foreground line-clamp-1 text-xs">
                  <HighlightedExcerpt parts={hit.excerpt} />
                </div>
              </>
            ),
          }))
        ),
        {
          key: "all",
          label: `All results for “${term}”`,
          href: allResultsHref,
          render: <span className="font-medium">All results for “{term}”</span>,
        },
      ]
    : latest.map((post) => ({
        key: post.id,
        label: post.title,
        href: `/${post.area}/${post.slug}`,
        render: (
          <>
            <span className="font-medium">{post.title}</span>
            <span className="text-muted-foreground ml-2 text-xs">
              {contentAreas.find((a) => a.slug === post.area)?.name}
            </span>
          </>
        ),
      }));

  const go = (item: Item | undefined) => {
    if (!item) return;
    onClose();
    router.push(item.href);
  };

  const chips: (AreaSlug | null)[] = [null, ...contentAreas.map((a) => a.slug)];
  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Escape") {
      onClose();
    } else if (event.key === "ArrowDown") {
      event.preventDefault();
      setActive((i) => Math.min(i + 1, items.length - 1));
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      setActive((i) => Math.max(i - 1, 0));
    } else if (event.key === "Enter") {
      event.preventDefault();
      go(items[active]);
    } else if (event.key === "Tab") {
      event.preventDefault();
      const i = chips.indexOf(area);
      setArea(chips[(i + (event.shiftKey ? -1 : 1) + chips.length) % chips.length]);
      setActive(0);
    }
  };

  const optionId = (i: number) => `${listId}-${i}`;
  const option = (item: Item) => {
    const i = items.indexOf(item);
    return (
      <li
        key={item.key}
        id={optionId(i)}
        role="option"
        aria-selected={i === active}
        aria-label={item.label}
        onMouseEnter={() => setActive(i)}
        onMouseDown={(event) => event.preventDefault()}
        onClick={() => go(item)}
        className={cn(
          "mx-2 cursor-pointer rounded-md px-3 py-2 text-sm",
          i === active && "bg-accent"
        )}
      >
        {item.render}
      </li>
    );
  };

  return (
    <div
      className="fixed inset-0 z-[90] flex items-start justify-center bg-black/40 sm:pt-[12vh]"
      onMouseDown={onClose}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Search"
        onMouseDown={(event) => event.stopPropagation()}
        className="bg-popover border-border flex h-full w-full flex-col overflow-hidden border shadow-2xl sm:h-auto sm:w-[min(40rem,92vw)] sm:rounded-xl"
      >
        <input
          ref={input}
          role="combobox"
          aria-label="Search Posts"
          aria-expanded="true"
          aria-controls={listId}
          aria-activedescendant={items.length ? optionId(active) : undefined}
          aria-autocomplete="list"
          value={query}
          onChange={(event) => {
            setQuery(event.target.value);
            setActive(0);
          }}
          onKeyDown={onKeyDown}
          placeholder="Search Posts…"
          className="border-border w-full border-b bg-transparent px-5 py-4 text-lg outline-none"
        />
        <div className="border-border flex flex-wrap gap-1.5 border-b px-4 py-2 text-xs">
          {chips.map((chip) => (
            <button
              key={chip ?? "all"}
              type="button"
              tabIndex={-1}
              aria-pressed={chip === area}
              onClick={() => {
                setArea(chip);
                setActive(0);
                input.current?.focus();
              }}
              className={cn(
                "rounded-full px-2.5 py-1",
                chip === area ? "bg-foreground text-background" : "bg-muted text-muted-foreground"
              )}
            >
              {chip ? contentAreas.find((a) => a.slug === chip)?.name : "All"}
            </button>
          ))}
        </div>

        <ul
          id={listId}
          role="listbox"
          aria-label="Results"
          className="flex-1 overflow-auto py-2 sm:max-h-[55vh]"
        >
          {!searching && (
            <li role="presentation">
              <div className="text-muted-foreground px-5 pt-2 pb-1 text-[11px] font-semibold tracking-wide uppercase">
                {area ? `Latest in ${contentAreas.find((a) => a.slug === area)?.name}` : "Latest"}
              </div>
              <ul role="group" aria-label="Latest">
                {items.map(option)}
              </ul>
            </li>
          )}

          {searching && result && hits.length === 0 && (
            <li role="presentation" className="px-5 py-6 text-center text-sm">
              <p>
                No Posts match “{term}”
                {area && <> in {contentAreas.find((a) => a.slug === area)?.name}</>}.
              </p>
              <p className="text-muted-foreground mt-1 text-xs">
                {area ? "Press Tab to widen the Content Area." : "Try fewer words."}
              </p>
            </li>
          )}

          {searching &&
            groups.map((group) => (
              <li key={group.area.slug} role="presentation">
                <div className="text-muted-foreground px-5 pt-2 pb-1 text-[11px] font-semibold tracking-wide uppercase">
                  {group.area.name}
                </div>
                <ul role="group" aria-label={group.area.name}>
                  {items
                    .filter((item) => group.hits.some((hit) => hit.id === item.key))
                    .map(option)}
                </ul>
              </li>
            ))}

          {searching && (
            <li role="presentation">
              <ul role="group" aria-label="More">
                {option(items[items.length - 1])}
              </ul>
            </li>
          )}
        </ul>

        <div className="border-border text-muted-foreground hidden gap-4 border-t px-4 py-2 text-[11px] sm:flex">
          <span>↑↓ move</span>
          <span>↵ open</span>
          <span>Tab Content Area</span>
          <span>Esc close</span>
        </div>
      </div>
    </div>
  );
}
