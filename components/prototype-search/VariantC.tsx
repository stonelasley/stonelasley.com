"use client";

// PROTOTYPE — Variant C: command palette opened from anywhere (⌘K, Ctrl+K or "/"),
// as you type, results grouped by Content Area, filter chips cycled with Tab. No URL.
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { AREAS, type Area, POSTS, postHref, search } from "./corpus";
import { Highlight } from "./shared";

export function VariantCHeader() {
  const [open, setOpen] = useState(false);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const typing = (e.target as HTMLElement).closest("input, textarea, [contenteditable]");
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        setOpen((o) => !o);
      } else if (e.key === "/" && !typing) {
        e.preventDefault();
        setOpen(true);
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="border-input text-muted-foreground hover:text-foreground flex h-9 w-48 items-center justify-between rounded-md border px-3 text-sm"
      >
        Search…
        <kbd className="bg-muted rounded px-1.5 font-mono text-[11px]">⌘K</kbd>
      </button>
      {open && <Palette onClose={() => setOpen(false)} />}
    </>
  );
}

function Palette({ onClose }: { onClose: () => void }) {
  const router = useRouter();
  const [q, setQ] = useState("");
  const [area, setArea] = useState<Area | null>(null);
  const [active, setActive] = useState(0);
  const input = useRef<HTMLInputElement>(null);
  useEffect(() => input.current?.focus(), []);

  const results = q.trim() ? search(q, area) : [];
  const suggested = POSTS.filter((p) => !area || p.area === area)
    .sort((a, b) => b.date.localeCompare(a.date))
    .slice(0, 5);
  const flat = q.trim() ? results.map((r) => r.post) : suggested;
  const groups = AREAS.map((a) => ({ area: a, items: results.filter((r) => r.post.area === a) })).filter(
    (g) => g.items.length
  );
  const chips: (Area | null)[] = [null, ...AREAS];

  const openPost = (i: number) => {
    const p = flat[i];
    if (!p) return;
    onClose();
    router.push(postHref(p));
  };

  return (
    <div className="fixed inset-0 z-[90] flex items-start justify-center bg-black/40 pt-[12vh]" onMouseDown={onClose}>
      <div
        className="bg-popover border-border w-[min(40rem,92vw)] overflow-hidden rounded-xl border shadow-2xl"
        onMouseDown={(e) => e.stopPropagation()}
      >
        <input
          ref={input}
          value={q}
          onChange={(e) => {
            setQ(e.target.value);
            setActive(0);
          }}
          onKeyDown={(e) => {
            if (e.key === "Escape") onClose();
            if (e.key === "ArrowDown") { e.preventDefault(); setActive((i) => Math.min(i + 1, flat.length - 1)); }
            if (e.key === "ArrowUp") { e.preventDefault(); setActive((i) => Math.max(i - 1, 0)); }
            if (e.key === "Enter") openPost(active);
            if (e.key === "Tab") {
              e.preventDefault();
              const i = chips.indexOf(area);
              setArea(chips[(i + (e.shiftKey ? -1 : 1) + chips.length) % chips.length]);
              setActive(0);
            }
          }}
          placeholder="Search Posts…"
          className="border-border w-full border-b bg-transparent px-5 py-4 text-lg outline-none"
        />
        <div className="border-border flex gap-1.5 border-b px-4 py-2 text-xs">
          {chips.map((c) => (
            <button
              key={c ?? "all"}
              onClick={() => { setArea(c); input.current?.focus(); }}
              className={`rounded-full px-2.5 py-1 ${c === area ? "bg-primary text-primary-foreground" : "bg-muted text-muted-foreground"}`}
            >
              {c ?? "All"}
            </button>
          ))}
        </div>

        <div className="max-h-[55vh] overflow-auto py-2">
          {!q.trim() && (
            <Section label={area ? `Latest in ${area}` : "Latest"}>
              {suggested.map((p, i) => (
                <Row key={p.slug} active={i === active} onHover={() => setActive(i)} onPick={() => openPost(i)}>
                  <span className="font-medium">{p.title}</span>
                  <span className="text-muted-foreground ml-2 text-xs">{p.area}</span>
                </Row>
              ))}
            </Section>
          )}

          {q.trim() && results.length === 0 && (
            <div className="px-5 py-8 text-center text-sm">
              <p>No Posts match &ldquo;{q}&rdquo;{area && <> in {area}</>}.</p>
              <p className="text-muted-foreground mt-1 text-xs">
                {area ? "Press Tab to widen the Content Area." : "Try fewer words."}
              </p>
            </div>
          )}

          {groups.map((g) => (
            <Section key={g.area} label={g.area}>
              {g.items.map((r) => {
                const i = flat.indexOf(r.post);
                return (
                  <Row key={r.post.slug} active={i === active} onHover={() => setActive(i)} onPick={() => openPost(i)}>
                    <div className="font-medium">
                      <Highlight text={r.post.title} terms={r.terms} />
                    </div>
                    <div className="text-muted-foreground line-clamp-1 text-xs">
                      <Highlight text={r.excerpt} terms={r.terms} />
                    </div>
                  </Row>
                );
              })}
            </Section>
          ))}
        </div>

        <div className="border-border text-muted-foreground flex gap-4 border-t px-4 py-2 text-[11px]">
          <span>↑↓ move</span>
          <span>↵ open</span>
          <span>Tab Content Area</span>
          <span>Esc close</span>
        </div>
      </div>
    </div>
  );
}

function Section({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="mb-1">
      <div className="text-muted-foreground px-5 pb-1 pt-2 text-[11px] font-semibold uppercase tracking-wide">{label}</div>
      <ul>{children}</ul>
    </div>
  );
}

function Row(props: { active: boolean; onHover: () => void; onPick: () => void; children: React.ReactNode }) {
  return (
    <li
      onMouseEnter={props.onHover}
      onClick={props.onPick}
      className={`mx-2 cursor-pointer rounded-md px-3 py-2 text-sm ${props.active ? "bg-accent" : ""}`}
    >
      {props.children}
    </li>
  );
}
