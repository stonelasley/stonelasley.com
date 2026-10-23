import { z } from "zod";
import { contentAreas, type AreaSlug } from "./areas";

// The site reads content only from the .NET API (ADR 0001), at build time for public pages.

const postSummarySchema = z.object({
  id: z.string(),
  title: z.string(),
  slug: z.string(),
  area: z.enum(contentAreas.map((area) => area.slug)),
  date: z.string(), // YYYY-MM-DD
  tags: z.array(z.string()),
});

const postSchema = postSummarySchema.extend({
  body: z.string(), // Markdown
});

export type PostSummary = z.infer<typeof postSummarySchema>;
export type Post = z.infer<typeof postSchema>;

function apiUrl(): string {
  if (process.env.VERCEL && !process.env.API_URL) {
    throw new Error("API_URL is not set.");
  }
  return (process.env.API_URL ?? "http://localhost:5180").replace(/\/$/, "");
}

async function get<T>(path: string, schema: z.ZodType<T>): Promise<T | null> {
  const response = await fetch(`${apiUrl()}${path}`);
  if (response.status === 404) {
    return null;
  }
  if (!response.ok) {
    throw new Error(`GET ${path} failed: ${response.status} ${await response.text()}`);
  }
  return schema.parse(await response.json());
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
