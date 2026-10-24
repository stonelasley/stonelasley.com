import { searchPosts } from "@/lib/api";
import { findArea } from "@/lib/areas";

// The palette searches from the browser through here, so the API stays server-side (ADR 0001).
export async function GET(request: Request) {
  const params = new URL(request.url).searchParams;
  const query = (params.get("q") ?? "").slice(0, 200);
  const areaSlug = params.get("area");
  const area = areaSlug ? findArea(areaSlug) : undefined;
  if (areaSlug && !area) {
    return Response.json({ error: "unknown area" }, { status: 404 });
  }
  return Response.json(await searchPosts(query, area?.slug));
}
