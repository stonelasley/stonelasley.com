import http from "node:http";
import https from "node:https";
import { z } from "zod";
import { contentAreas, type AreaSlug } from "./areas";

// The site reads content only from the .NET API (ADR 0001), at build time for public pages.

// What the Watches index shows and filters on.
const watchSummarySchema = z.object({
  make: z.string(),
  reference: z.string(),
  modelName: z.string(),
  decades: z.array(z.number()), // 1980 for the 1980s
});

const postSummarySchema = z.object({
  id: z.string(),
  title: z.string(),
  slug: z.string(),
  area: z.enum(contentAreas.map((area) => area.slug)),
  date: z.string(), // YYYY-MM-DD
  tags: z.array(z.string()),
  excerpt: z.string(), // opening prose as plain text
  watch: watchSummarySchema.nullable(),
});

const ingredientSchema = z.object({
  quantity: z.number().nullable(),
  unit: z.string().nullable(),
  name: z.string(),
  note: z.string().nullable(),
  optional: z.boolean(),
});

// The Recipe typed core, with total time and °F derived by the API.
const recipeSchema = z.object({
  prepTimeMin: z.number(),
  cookTimeMin: z.number(),
  totalTimeMin: z.number(),
  servings: z.number(),
  difficulty: z.enum(["Easy", "Medium", "Veteran"]),
  ovenTempC: z.number().nullable(),
  ovenTempF: z.number().nullable(),
  ingredients: z.array(ingredientSchema),
});

// The Watch typed core, metric-only, with feet and "in production" derived by the API.
const watchSchema = z.object({
  make: z.string(),
  reference: z.string(),
  modelName: z.string(),
  variants: z.array(z.string()),
  productionStart: z.number().nullable(),
  productionEnd: z.number().nullable(),
  inProduction: z.boolean(),
  caseDiameterMm: z.number().nullable(),
  calibre: z.string().nullable(),
  caseMaterial: z.string().nullable(),
  waterResistanceM: z.number().nullable(),
  waterResistanceFt: z.number().nullable(),
});

const postSchema = postSummarySchema.omit({ watch: true }).extend({
  body: z.string(), // Markdown
  recipe: recipeSchema.nullable(),
  watch: watchSchema.nullable(),
});

const watchFacetsSchema = z.object({
  makes: z.array(z.string()),
  modelNames: z.array(z.string()),
  decades: z.array(z.number()),
});

const areaSchema = z.object({
  slug: z.string(),
  intro: z.string().nullable(), // Markdown
});

export type PostSummary = z.infer<typeof postSummarySchema>;
export type Post = z.infer<typeof postSchema>;
export type Recipe = z.infer<typeof recipeSchema>;
export type Ingredient = z.infer<typeof ingredientSchema>;
export type Watch = z.infer<typeof watchSchema>;
export type WatchFacets = z.infer<typeof watchFacetsSchema>;

function apiUrl(): string {
  if (process.env.VERCEL && !process.env.API_URL) {
    throw new Error("API_URL is not set.");
  }
  return (process.env.API_URL ?? "http://localhost:5180").replace(/\/$/, "");
}

// Plain node:http, not fetch. Next caches every fetch made while prerendering a static page and reuses it
// in later builds (and Vercel keeps that cache across deployments), so a rebuild after an ingest would
// render the old content. Pages stay static either way; revalidation re-renders them from fresh API data.
function request(url: string): Promise<{ status: number; body: string }> {
  const client = url.startsWith("https:") ? https : http;
  return new Promise((resolve, reject) => {
    const req = client.get(
      url,
      { headers: { accept: "application/json" }, timeout: 30_000 },
      (res) => {
        let body = "";
        res.setEncoding("utf8");
        res.on("data", (chunk) => (body += chunk));
        res.on("end", () => resolve({ status: res.statusCode ?? 0, body }));
      }
    );
    req.on("timeout", () => req.destroy(new Error(`GET ${url} timed out`)));
    req.on("error", reject);
  });
}

async function get<T>(path: string, schema: z.ZodType<T>): Promise<T | null> {
  const response = await request(`${apiUrl()}${path}`);
  if (response.status === 404) {
    return null;
  }
  if (response.status < 200 || response.status >= 300) {
    throw new Error(`GET ${path} failed: ${response.status} ${response.body}`);
  }
  try {
    return schema.parse(JSON.parse(response.body));
  } catch (error) {
    throw new Error(`GET ${path} returned an unexpected body`, { cause: error });
  }
}

/** Public Posts in one Content Area, newest first. */
export async function getPostsByArea(area: AreaSlug): Promise<PostSummary[]> {
  const posts = await get(`/areas/${area}/posts`, z.array(postSummarySchema));
  if (posts === null) {
    throw new Error(`The API does not know the Content Area '${area}'.`);
  }
  return posts;
}

/** Public Posts across every Content Area, newest first. */
export async function getAllPosts(): Promise<PostSummary[]> {
  const byArea = await Promise.all(contentAreas.map((area) => getPostsByArea(area.slug)));
  return byArea.flat().sort((a, b) => b.date.localeCompare(a.date));
}

export async function getPost(area: AreaSlug, slug: string): Promise<Post | null> {
  return get(`/areas/${area}/posts/${encodeURIComponent(slug)}`, postSchema);
}

/** A Content Area's intro from the content repo, if it has one. */
export async function getAreaIntro(area: AreaSlug): Promise<string | null> {
  const found = await get(`/areas/${area}`, areaSchema);
  return found?.intro ?? null;
}

/** Every make, model name and production decade the Watches index can filter by. */
export async function getWatchFacets(): Promise<WatchFacets> {
  const facets = await get("/areas/watches/facets", watchFacetsSchema);
  if (facets === null) {
    throw new Error("The API has no Watch facets.");
  }
  return facets;
}

const searchResultSchema = z.object({
  query: z.string(),
  area: z.string().nullable(),
  results: z.array(
    z.object({
      id: z.string(),
      title: z.string(),
      slug: z.string(),
      area: z.enum(contentAreas.map((area) => area.slug)),
      date: z.string(),
      // Matched words are flagged, so the page can highlight them without rendering HTML from the API.
      excerpt: z.array(z.object({ text: z.string(), match: z.boolean() })),
    })
  ),
  counts: z.record(z.string(), z.number()),
});

export type SearchResult = z.infer<typeof searchResultSchema>;
export type SearchHit = SearchResult["results"][number];

/** Keyword search over public Posts, optionally within one Content Area. */
export async function searchPosts(query: string, area?: AreaSlug): Promise<SearchResult> {
  const params = new URLSearchParams({ q: query });
  if (area) params.set("area", area);
  const result = await get(`/search?${params}`, searchResultSchema);
  if (result === null) {
    throw new Error(`The API does not know the Content Area '${area}'.`);
  }
  return result;
}

/** The newest public Posts, across every area or within one. */
export async function getLatestPosts(area?: AreaSlug, limit = 5): Promise<PostSummary[]> {
  const params = new URLSearchParams({ limit: String(limit) });
  if (area) params.set("area", area);
  const posts = await get(`/posts/latest?${params}`, z.array(postSummarySchema));
  if (posts === null) {
    throw new Error(`The API does not know the Content Area '${area}'.`);
  }
  return posts;
}
