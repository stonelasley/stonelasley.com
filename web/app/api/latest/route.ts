import { getLatestPosts } from "@/lib/api";
import { findArea } from "@/lib/areas";

// The palette's empty state: the newest public Posts.
export async function GET(request: Request) {
  const areaSlug = new URL(request.url).searchParams.get("area");
  const area = areaSlug ? findArea(areaSlug) : undefined;
  if (areaSlug && !area) {
    return Response.json({ error: "unknown area" }, { status: 404 });
  }
  try {
    return Response.json(await getLatestPosts(area?.slug));
  } catch (error) {
    console.error(error);
    return Response.json({ error: "search unavailable" }, { status: 502 });
  }
}
