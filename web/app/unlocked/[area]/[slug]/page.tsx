import { cookies } from "next/headers";
import { notFound } from "next/navigation";
import { PostView } from "@/components/PostView";
import { getPost } from "@/lib/api";
import { accessCookie } from "@/lib/access";
import { findArea } from "@/lib/areas";
import { constructMetadata } from "@/lib/metadata";

// Reached only through proxy.ts, for visitors with an Access cookie. Rendered per request, never cached.
export const dynamic = "force-dynamic";

interface UnlockedPostPageProps {
  params: Promise<{ area: string; slug: string }>;
}

async function load(params: UnlockedPostPageProps["params"]) {
  const { area: areaSlug, slug } = await params;
  const area = findArea(areaSlug);
  const session = (await cookies()).get(accessCookie)?.value;
  const post = area ? await getPost(area.slug, slug, session) : null;
  return area && post ? { area, post } : null;
}

export async function generateMetadata({ params }: UnlockedPostPageProps) {
  const found = await load(params);
  return found
    ? found.post.audiencePublic
      ? constructMetadata({
          title: found.post.title,
          description: found.post.excerpt || undefined,
          type: "article",
          path: `/${found.area.slug}/${found.post.slug}`,
        })
      : constructMetadata({ title: found.post.title, type: "article", privatePost: true })
    : {};
}

export default async function UnlockedPostPage({ params }: UnlockedPostPageProps) {
  const found = await load(params);
  if (!found) {
    notFound();
  }
  return (
    <PostView area={found.area} post={found.post} structuredData={found.post.audiencePublic} />
  );
}
