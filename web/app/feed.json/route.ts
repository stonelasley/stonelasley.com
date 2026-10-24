import { buildFeed } from "@/lib/feed";

export const dynamic = "force-static"; // Built once from API data; revalidation rebuilds it

export async function GET() {
  const feed = await buildFeed();

  return new Response(feed.json1(), {
    headers: {
      "Content-Type": "application/json; charset=utf-8",
    },
  });
}
