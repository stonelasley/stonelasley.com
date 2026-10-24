import { buildFeed } from "@/lib/feed";

export const revalidate = false; // Static generation

export async function GET() {
  const feed = await buildFeed();

  return new Response(feed.rss2(), {
    headers: {
      "Content-Type": "application/xml; charset=utf-8",
    },
  });
}
