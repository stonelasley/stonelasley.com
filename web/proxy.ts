import { NextResponse, type NextRequest } from "next/server";
import { accessCookie } from "@/lib/access";
import { contentAreas } from "@/lib/areas";

const areas = new Set<string>(contentAreas.map((area) => area.slug));

// A visitor holding an Access cookie reads Posts through the dynamic /unlocked route, which forwards their
// session to the API. Everyone else gets the static pages. Private content is never pre-rendered or cached.
export function proxy(request: NextRequest) {
  const [area, slug, ...rest] = request.nextUrl.pathname.split("/").filter(Boolean);
  if (!request.cookies.has(accessCookie) || !areas.has(area) || !slug || rest.length > 0) {
    return NextResponse.next();
  }
  const url = request.nextUrl.clone();
  url.pathname = `/unlocked/${area}/${slug}`;
  const response = NextResponse.rewrite(url);
  response.headers.set("Cache-Control", "private, no-store");
  response.headers.set("X-Robots-Tag", "noindex");
  return response;
}

export const config = {
  matcher: ["/((?!_next|api|access|unlocked|search|feed|sitemap|robots|favicon).*)"],
};
