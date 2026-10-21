"use client";

// PROTOTYPE — Variant B: header only carries a link; search is its own page with a
// shareable URL (?q=&area=), submit-only, Content Areas as tabs with counts.
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { AREAS, type Area, areaSlug, countsByArea, postHref, search } from "./corpus";
import { Highlight } from "./shared";

const EXAMPLES = ["banana bread", "dive watch 200 m", "reciprocal rank fusion", "split squat"];

export function VariantBHeader() {
  return (
    <Link
      href="/prototype/search?variant=B"
      className="text-muted-foreground hover:text-foreground flex items-center gap-1.5 text-sm font-medium"
    >
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
        <circle cx="11" cy="11" r="7" />
        <path d="m20 20-3.5-3.5" />
      </svg>
      Search
    </Link>
  );
}

export function VariantBPage() {
  const router = useRouter();
  const params = useSearchParams();
  const q = params.get("q") ?? "";
  const areaParam = params.get("area");
  const area = (AREAS.find((a) => areaSlug[a] === areaParam) ?? null) as Area | null;
  const [draft, setDraft] = useState(q);

  const nav = (next: { q?: string; area?: Area | null }) => {
    const p = new URLSearchParams({ variant: "B" });
    const nq = next.q ?? q;
    const na = next.area === undefined ? area : next.area;
    if (nq) p.set("q", nq);
    if (na) p.set("area", areaSlug[na]);
    router.push(`/prototype/search?${p.toString()}`);
  };

  const results = search(q, area);
  const counts = countsByArea(q);
  const total = Object.values(counts).reduce((a, b) => a + b, 0);

  return (
    <div className="mx-auto max-w-3xl px-4 py-12">
      <h1 className="mb-6 text-3xl font-bold tracking-tight">Search</h1>
      <form
        onSubmit={(e) => {
          e.preventDefault();
          nav({ q: draft.trim() });
        }}
        className="flex gap-2"
      >
        <input
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          placeholder="Recipes, Watches, AI & Development, Training…"
          className="border-input focus:ring-ring h-12 flex-1 rounded-md border bg-transparent px-4 text-lg outline-none focus:ring-1"
          autoFocus
        />
        <button className="bg-primary text-primary-foreground h-12 rounded-md px-6 font-medium">Search</button>
      </form>

      {!q && (
        <div className="text-muted-foreground mt-8 text-sm">
          Try{" "}
          {EXAMPLES.map((ex, i) => (
            <span key={ex}>
              <button onClick={() => { setDraft(ex); nav({ q: ex }); }} className="text-foreground underline">
                {ex}
              </button>
              {i < EXAMPLES.length - 1 ? ", " : "."}
            </span>
          ))}
        </div>
      )}

      {q && (
        <>
          <nav className="border-border mt-6 flex gap-1 overflow-x-auto border-b text-sm" aria-label="Content Area">
            {[null, ...AREAS].map((a) => {
              const n = a ? counts[a] : total;
              const on = a === area;
              return (
                <button
                  key={a ?? "all"}
                  onClick={() => nav({ area: a })}
                  disabled={n === 0 && !on}
                  className={`-mb-px whitespace-nowrap border-b-2 px-3 py-2 ${
                    on ? "border-foreground font-medium" : "text-muted-foreground border-transparent"
                  } disabled:opacity-40`}
                >
                  {a ?? "All"} <span className="text-muted-foreground ml-1 text-xs">{n}</span>
                </button>
              );
            })}
          </nav>

          <p className="text-muted-foreground mt-4 text-sm">
            {results.length} result{results.length === 1 ? "" : "s"} for <strong className="text-foreground">&ldquo;{q}&rdquo;</strong>
            {area && <> in {area}</>}
          </p>

          {results.length === 0 ? (
            <div className="border-border mt-6 rounded-md border border-dashed p-8 text-center">
              <p className="font-medium">No Posts match &ldquo;{q}&rdquo;{area && <> in {area}</>}.</p>
              <p className="text-muted-foreground mt-2 text-sm">
                {area && total > 0 ? (
                  <button onClick={() => nav({ area: null })} className="underline">
                    {total} result{total === 1 ? "" : "s"} in other Content Areas
                  </button>
                ) : (
                  <>Try fewer or different words, or browse a Content Area:</>
                )}
              </p>
              {!(area && total > 0) && (
                <div className="mt-4 flex flex-wrap justify-center gap-2 text-sm">
                  {AREAS.map((a) => (
                    <Link key={a} href={`/${areaSlug[a]}`} className="border-border rounded-full border px-3 py-1">
                      {a}
                    </Link>
                  ))}
                </div>
              )}
            </div>
          ) : (
            <ol className="mt-6 space-y-8">
              {results.map((r) => (
                <li key={r.post.slug}>
                  <div className="text-muted-foreground text-xs">
                    {r.post.area} · {r.post.date}
                  </div>
                  <Link href={postHref(r.post)} className="text-xl font-semibold hover:underline">
                    <Highlight text={r.post.title} terms={r.terms} />
                  </Link>
                  <p className="text-muted-foreground mt-1 text-sm leading-relaxed">
                    <Highlight text={r.excerpt} terms={r.terms} />
                  </p>
                </li>
              ))}
            </ol>
          )}
        </>
      )}
    </div>
  );
}
