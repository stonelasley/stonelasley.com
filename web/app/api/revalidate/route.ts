import { createHash, timingSafeEqual } from "node:crypto";
import { revalidatePath } from "next/cache";

// Called by the API after an ingest commits, so pre-rendered pages rebuild from fresh API data.
export async function POST(request: Request) {
  const secret = process.env.REVALIDATE_SECRET;
  const presented = request.headers.get("authorization") ?? "";
  if (!secret || !matches(presented, `Bearer ${secret}`)) {
    return Response.json({ error: "unauthorized" }, { status: 401 });
  }

  // Every page reads the API, and an ingest can touch any of them, so revalidate the whole site.
  revalidatePath("/", "layout");
  return Response.json({ revalidated: true });
}

// Compares digests so the check takes the same time whatever the input length.
function matches(a: string, b: string): boolean {
  const digest = (value: string) => createHash("sha256").update(value).digest();
  return timingSafeEqual(digest(a), digest(b));
}
