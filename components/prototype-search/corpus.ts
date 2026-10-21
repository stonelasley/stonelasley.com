// PROTOTYPE — throwaway. Fake public-Post corpus and a stand-in for the hybrid
// search API (#28). Only the shape of a result matters here, not the ranking.
import Fuse from "fuse.js";

export const AREAS = ["Recipes", "Watches", "AI & Development", "Training"] as const;
export type Area = (typeof AREAS)[number];

export const areaSlug: Record<Area, string> = {
  Recipes: "recipes",
  Watches: "watches",
  "AI & Development": "ai",
  Training: "training",
};

export interface FakePost {
  title: string;
  area: Area;
  slug: string;
  date: string;
  body: string;
}

export const POSTS: FakePost[] = [
  { title: "Buffed Up Protein Banana Bread", area: "Recipes", slug: "buffed-up-protein-banana-bread", date: "2024-03-02", body: "A dense, moist banana bread built on whey isolate and oat flour. Bake at 175 °C for 50 minutes; slices freeze well for a week of breakfasts." },
  { title: "High-Protein Coconut Cream Balls", area: "Recipes", slug: "high-protein-coconut-cream-balls", date: "2024-02-11", body: "No-bake coconut and orange bites rolled in shredded coconut. Vanilla whey, almond flour and coconut oil; makes 30." },
  { title: "Lemon Blueberry High-Protein Banana Bread", area: "Recipes", slug: "lemon-blueberry-high-protein-banana-brea", date: "2024-04-19", body: "The banana bread base with lemon zest and blueberries folded in. Keep the blueberries frozen so they don't bleed into the batter." },
  { title: "Mexican Muscle Cornbread", area: "Recipes", slug: "mexican-muscle-cornbread", date: "2024-05-08", body: "Cornbread with green chiles, egg whites and protein powder. Cast iron at 200 °C gives the best crust." },
  { title: "Rolex Submariner Date 16610", area: "Watches", slug: "rolex-16610", date: "2025-01-14", body: "The last aluminium-bezel Submariner Date. Calibre 3135, 40 mm Oystersteel case, 300 m water resistance, produced 1988–2010." },
  { title: "Omega Speedmaster Professional 3570.50", area: "Watches", slug: "omega-3570-50", date: "2025-02-03", body: "Hesalite crystal, manual-wind calibre 1861 chronograph, 42 mm case. The Moonwatch reference most people picture." },
  { title: "Seiko SKX007", area: "Watches", slug: "seiko-skx007", date: "2025-03-21", body: "An ISO-rated 200 m diver on the 7S26 automatic calibre. 42 mm steel case, Jubilee or rubber strap, endlessly modded." },
  { title: "Tudor Black Bay 58 79030N", area: "Watches", slug: "tudor-79030n", date: "2025-06-09", body: "39 mm vintage-proportioned diver with gilt accents and the in-house MT5402 calibre. 200 m water resistance." },
  { title: "Grand Seiko SBGA211 Snowflake", area: "Watches", slug: "grand-seiko-sbga211", date: "2025-08-30", body: "Spring Drive 9R65 in a 41 mm titanium case. The textured dial reads like wind-blown snow." },
  { title: "Hybrid search on Cloudflare: fusing FTS5 and Vectorize", area: "AI & Development", slug: "hybrid-search-cloudflare", date: "2026-09-12", body: "Keyword search in D1 FTS5 and embeddings in Vectorize, fused with reciprocal rank fusion at k=60. Why each leg misses what the other finds." },
  { title: "Evaluating search with nDCG@5 and a frozen snapshot", area: "AI & Development", slug: "search-eval-ndcg", date: "2026-10-01", body: "Forty graded queries, a Draft-inclusive snapshot and a regression guard against main. How a relative quality bar keeps ranking honest." },
  { title: "Chunking Markdown for embeddings", area: "AI & Development", slug: "chunking-markdown", date: "2026-09-25", body: "Heading-aware chunks, title prefixes, max-pooling chunk scores per Post, and a cosine floor so weak matches drop out." },
  { title: "Running two engineering jobs with Claude Code", area: "AI & Development", slug: "claude-code-two-jobs", date: "2026-07-18", body: "Skills, subagents and a wayfinding map on GitHub Issues. What an AI harness changes about planning work you can't finish in one sitting." },
  { title: "Why I rebuilt my split around five days", area: "Training", slug: "five-day-split", date: "2026-08-12", body: "Back and delts on Saturday, quads and glutes split across two days, and Bulgarian split squats doing more work than any machine." },
  { title: "Bulgarian split squats: dumbbell vs machine", area: "Training", slug: "bulgarian-split-squats", date: "2026-06-03", body: "Glute bias with a forward lean and dumbbells, quad bias upright on the Smith machine. Foot position matters more than load." },
  { title: "Hack squats after a knee break", area: "Training", slug: "hack-squats-return", date: "2026-08-20", body: "Ten months away, then back in with a slow eccentric, partial range, and a load ramp measured in weeks rather than sessions." },
  { title: "Weighing food to the gram without losing your mind", area: "Training", slug: "weighing-food", date: "2026-05-14", body: "Batch cooking on Sunday, a tare-everything habit, and why meal prep containers beat eyeballing every time." },
];

const fuse = new Fuse(POSTS, {
  keys: [
    { name: "title", weight: 3 },
    { name: "body", weight: 1 },
  ],
  threshold: 0.4,
  ignoreLocation: true,
  includeScore: true,
});

export interface Result {
  post: FakePost;
  excerpt: string;
  terms: string[];
}

function excerptFor(body: string, terms: string[], len = 140): string {
  const lower = body.toLowerCase();
  const hit = terms.map((t) => lower.indexOf(t)).filter((i) => i >= 0).sort((a, b) => a - b)[0] ?? 0;
  const start = Math.max(0, hit - 40);
  const out = body.slice(start, start + len);
  return (start > 0 ? "…" : "") + out + (start + len < body.length ? "…" : "");
}

export function search(q: string, area?: Area | null): Result[] {
  const query = q.trim();
  if (!query) return [];
  const terms = query.toLowerCase().split(/\s+/).filter((t) => t.length > 1);
  return fuse
    .search(query)
    .map((r) => r.item)
    .filter((p) => !area || p.area === area)
    .map((post) => ({ post, terms, excerpt: excerptFor(post.body, terms) }));
}

export function countsByArea(q: string): Record<Area, number> {
  const all = search(q);
  return Object.fromEntries(AREAS.map((a) => [a, all.filter((r) => r.post.area === a).length])) as Record<Area, number>;
}

export function postHref(p: FakePost): string {
  return `/${areaSlug[p.area]}/${p.slug}`;
}
