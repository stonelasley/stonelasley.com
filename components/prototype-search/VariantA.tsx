"use client";

// PROTOTYPE — Variant A: search box lives in the header on every page, results
// drop down as you type, no results URL at all. Area filter is a select glued to the box.
import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { AREAS, type Area, postHref, search } from "./corpus";
import { Highlight } from "./shared";

export function VariantAHeader() {
  const [q, setQ] = useState("");
  const [area, setArea] = useState<Area | "">("");
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(0);
  const box = useRef<HTMLDivElement>(null);
  const results = search(q, area || null).slice(0, 8);

  useEffect(() => {
    const onClick = (e: MouseEvent) => {
      if (!box.current?.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", onClick);
    return () => document.removeEventListener("mousedown", onClick);
  }, []);

  return (
    <div ref={box} className="relative">
      <div className="border-input focus-within:ring-ring flex h-9 w-72 items-center overflow-hidden rounded-md border text-sm focus-within:ring-1 lg:w-96">
        <select
          value={area}
          onChange={(e) => setArea(e.target.value as Area | "")}
          className="bg-muted text-muted-foreground h-full border-r px-2 text-xs outline-none"
          aria-label="Content Area"
        >
          <option value="">All</option>
          {AREAS.map((a) => (
            <option key={a}>{a}</option>
          ))}
        </select>
        <input
          value={q}
          onChange={(e) => {
            setQ(e.target.value);
            setOpen(true);
            setActive(0);
          }}
          onFocus={() => setOpen(true)}
          onKeyDown={(e) => {
            if (e.key === "Escape") setOpen(false);
            if (e.key === "ArrowDown") setActive((i) => Math.min(i + 1, results.length - 1));
            if (e.key === "ArrowUp") setActive((i) => Math.max(i - 1, 0));
            if (e.key === "Enter" && results[active]) window.location.href = postHref(results[active].post);
          }}
          placeholder="Search posts…"
          className="h-full flex-1 bg-transparent px-3 outline-none"
        />
      </div>

      {open && q.trim() && (
        <div className="bg-popover border-border absolute right-0 mt-2 w-[28rem] rounded-md border shadow-lg">
          {results.length === 0 ? (
            <div className="p-4 text-sm">
              <p>
                Nothing matches <strong>&ldquo;{q}&rdquo;</strong>
                {area && <> in {area}</>}.
              </p>
              {area && (
                <button onClick={() => setArea("")} className="mt-2 text-xs underline">
                  Search all Content Areas instead
                </button>
              )}
            </div>
          ) : (
            <ul className="max-h-[70vh] overflow-auto py-1">
              {results.map((r, i) => (
                <li key={r.post.slug}>
                  <Link
                    href={postHref(r.post)}
                    onMouseEnter={() => setActive(i)}
                    className={`block px-4 py-2 ${i === active ? "bg-accent" : ""}`}
                  >
                    <div className="flex items-baseline justify-between gap-3">
                      <span className="truncate font-medium">
                        <Highlight text={r.post.title} terms={r.terms} />
                      </span>
                      <span className="text-muted-foreground shrink-0 text-[11px] uppercase tracking-wide">
                        {r.post.area}
                      </span>
                    </div>
                    <p className="text-muted-foreground line-clamp-1 text-xs">
                      <Highlight text={r.excerpt} terms={r.terms} />
                    </p>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
