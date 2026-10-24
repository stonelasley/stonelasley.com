import { buildFeed } from "@/lib/feed";

export const revalidate = false; // Static generation

export async function GET() {
  const feed = await buildFeed();

  return new Response(feed.json1(), {
    headers: {
      "Content-Type": "application/json; charset=utf-8",
    },
  });
}
