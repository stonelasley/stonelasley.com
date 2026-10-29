"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useId, useRef, useState, type KeyboardEvent } from "react";
import { createPortal } from "react-dom";
import type { PostSummary, SearchResult } from "@/lib/api";
import { contentAreas, findArea, type AreaSlug } from "@/lib/areas";
import { cn } from "@/lib/utils";
import { PostThumbnail } from "./PostThumbnail";
import { HighlightedExcerpt } from "./SearchResults";

// Each keystroke-triggered query costs an embedding and a Worker call, so both are part of the spec.
export const DEBOUNCE_MS = 250;
export const MIN_QUERY_LENGTH = 2;

type Item = { key: string; label: string; href: string; render: React.ReactNode };

/** The header's search button and the palette it opens (⌘K, Ctrl+K, or / when not typing). */
export function SearchPalette() {
  const [open, setOpen] = useState(false);
  const trigger = useRef<HTMLAnchorElement>(null);
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
      } else if (
        event.key === "/" &&
        !typing &&
        !event.metaKey &&
        !event.ctrlKey &&
        !event.altKey
      ) {
        event.preventDefault();
        setOpen(true);
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  return (
    <>
      {/* A link, so search still works without JavaScript; with it, the link opens the palette. */}
      <Link
        ref={trigger}
        href="/search"
        onClick={(event) => {
          event.preventDefault();
          setOpen(true);
        }}
        aria-label="Search"
        aria-haspopup="dialog"
        data-ready={ready || undefined}
        className="bg-muted hover:bg-accent text-muted-foreground hover:text-foreground text-small flex h-9 items-center gap-2 rounded-md px-2.5 transition-colors"
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
        <kbd className="text-small hidden font-sans sm:inline">⌘K</kbd>
      </Link>
      {/* Portalled to <body>, so no ancestor's transform or filter can trap the fixed overlay. */}
      {open &&
        createPortal(
          <Palette
            onClose={() => {
              setOpen(false);
              trigger.current?.focus();
            }}
          />,
          document.body
        )}
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
  // Each response remembers what it answered, so nothing from an older query or chip is ever shown.
  const [latest, setLatest] = useState<{ area: AreaSlug | null; posts: PostSummary[] } | null>(
    null
  );
  const [result, setResult] = useState<{
    term: string;
    area: AreaSlug | null;
    data: SearchResult;
  } | null>(null);
  const [failed, setFailed] = useState<{ term: string; area: AreaSlug | null } | null>(null);

  const term = query.trim();
  const searching = term.length >= MIN_QUERY_LENGTH;
  const current = result && result.term === term && result.area === area ? result.data : null;
  const unavailable =
    failed !== null && failed.term === (searching ? term : "") && failed.area === area;

  useEffect(() => input.current?.focus(), []);

  // Modal: the page behind doesn't scroll.
  useEffect(() => {
    const previous = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = previous;
    };
  }, []);

  // Latest Posts for the empty state, per chip.
  useEffect(() => {
    const controller = new AbortController();
    fetch(`/api/latest${area ? `?area=${area}` : ""}`, { signal: controller.signal })
      .then(json<PostSummary[]>)
      .then((posts) => setLatest({ area, posts }))
      .catch((error) => onFailure(error, () => setFailed({ term: "", area })));
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
        .then(json<SearchResult>)
        .then((data) => {
          setResult({ term, area, data });
          setActive(0);
        })
        .catch((error) => onFailure(error, () => setFailed({ term, area })));
    }, DEBOUNCE_MS);
    return () => {
      clearTimeout(timer);
      controller.abort();
    };
  }, [term, area, searching]);

  const allResultsHref = `/search?${new URLSearchParams({ q: term, ...(area ? { area } : {}) })}`;
  const hits = searching && current ? current.results : [];
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
              <div className="flex items-center gap-3">
                <PostThumbnail
                  hero={hit.hero}
                  area={hit.area}
                  watch={hit.watch}
                  sizes="44px"
                  variant="compact"
                  className="size-11 rounded-md"
                />
                <div className="min-w-0">
                  <div className="font-medium">{hit.title}</div>
                  <div className="text-muted-foreground text-small line-clamp-1">
                    <HighlightedExcerpt parts={hit.excerpt} />
                  </div>
                </div>
              </div>
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
    : (latest?.area === area ? latest.posts : []).map((post) => ({
        key: post.id,
        label: post.title,
        href: `/${post.area}/${post.slug}`,
        render: (
          <div className="flex items-center gap-3">
            <PostThumbnail
              hero={post.hero}
              area={post.area}
              watch={post.watch}
              sizes="44px"
              variant="compact"
              className="size-11 rounded-md"
            />
            <div className="min-w-0">
              <span className="font-medium">{post.title}</span>
              <span className="text-muted-foreground text-small ml-2">
                {findArea(post.area)?.name}
              </span>
            </div>
          </div>
        ),
      }));

  const activeIndex = Math.min(active, items.length - 1);

  // Keep the highlighted row on screen as ↑/↓ move through a long list.
  useEffect(() => {
    document.getElementById(`${listId}-${activeIndex}`)?.scrollIntoView({ block: "nearest" });
  }, [activeIndex, listId]);

  const go = (item: Item | undefined) => {
    if (!item) return;
    onClose();
    router.push(item.href);
  };

  const chips: (AreaSlug | null)[] = [null, ...contentAreas.map((a) => a.slug)];
  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.nativeEvent.isComposing) return; // an IME is still composing; Enter confirms the text
    if (event.key === "ArrowDown") {
      event.preventDefault();
      setActive((i) => Math.min(i + 1, items.length - 1));
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      setActive((i) => Math.max(i - 1, 0));
    } else if (event.key === "Enter") {
      event.preventDefault();
      go(items[activeIndex]);
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
        aria-selected={i === activeIndex}
        onMouseEnter={() => setActive(i)}
        onMouseDown={(event) => event.preventDefault()}
        onClick={() => go(item)}
        className={cn(
          "text-small mx-2 cursor-pointer rounded-md px-3 py-2",
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
        onKeyDown={(event) => {
          if (event.key === "Escape") {
            event.preventDefault();
            onClose();
          } else if (event.key === "Tab" && event.target !== input.current) {
            // Focus stays in the dialog: Tab from anywhere else returns to the search box.
            event.preventDefault();
            input.current?.focus();
          }
        }}
        className="bg-popover border-border flex h-full w-full flex-col overflow-hidden border shadow-2xl sm:h-auto sm:w-[min(40rem,92vw)] sm:rounded-xl"
      >
        <input
          ref={input}
          role="combobox"
          aria-label="Search Posts"
          aria-expanded="true"
          aria-controls={listId}
          aria-activedescendant={items.length ? optionId(activeIndex) : undefined}
          aria-autocomplete="list"
          value={query}
          onChange={(event) => {
            setQuery(event.target.value);
            setActive(0);
          }}
          onKeyDown={onKeyDown}
          placeholder="Search Posts…"
          className="border-border text-body w-full border-b bg-transparent px-5 py-4 outline-none"
        />
        <div className="border-border text-small flex flex-wrap gap-1.5 border-b px-4 py-2">
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
              {chip ? findArea(chip)?.name : "All"}
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
              <div className="text-muted-foreground text-small px-5 pt-2 pb-1 font-semibold">
                {area ? `Latest in ${findArea(area)?.name}` : "Latest"}
              </div>
              <ul role="group" aria-label="Latest">
                {items.map(option)}
              </ul>
            </li>
          )}

          {unavailable && (
            <li role="presentation" className="text-small px-5 py-6 text-center">
              Search is unavailable right now.
            </li>
          )}

          {searching && current && hits.length === 0 && (
            <li role="presentation" className="text-small px-5 py-6 text-center">
              <p>
                No Posts match “{term}”{area && <> in {findArea(area)?.name}</>}.
              </p>
              <p className="text-muted-foreground text-small mt-1">
                {area ? "Press Tab to widen the Content Area." : "Try fewer words."}
              </p>
            </li>
          )}

          {searching &&
            groups.map((group) => (
              <li key={group.area.slug} role="presentation">
                <div className="text-muted-foreground text-small px-5 pt-2 pb-1 font-semibold">
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

        <div className="border-border text-muted-foreground text-small hidden gap-4 border-t px-4 py-2 sm:flex">
          <span>↑↓ move</span>
          <span>↵ open</span>
          <span>Tab Content Area</span>
          <span>Esc close</span>
        </div>
      </div>
    </div>
  );
}

async function json<T>(response: Response): Promise<T> {
  if (!response.ok) throw new Error(`${response.url} answered ${response.status}`);
  return response.json();
}

// An aborted request was replaced by a newer one, so it isn't a failure.
function onFailure(error: unknown, record: () => void) {
  if (error instanceof DOMException && error.name === "AbortError") return;
  record();
}
