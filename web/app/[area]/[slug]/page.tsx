import { notFound } from "next/navigation";
import { getPost, getPostsByArea } from "@/lib/api";
import { contentAreas, findArea } from "@/lib/areas";
import { constructMetadata } from "@/lib/metadata";
import { PostView } from "@/components/PostView";

interface PostPageProps {
  params: Promise<{ area: string; slug: string }>;
}

export async function generateStaticParams() {
  const byArea = await Promise.all(
    contentAreas.map(async (area) =>
      (await getPostsByArea(area.slug)).map((post) => ({ area: area.slug, slug: post.slug }))
    )
  );
  return byArea.flat();
}

async function loadPost(params: PostPageProps["params"]) {
  const { area: areaSlug, slug } = await params;
  const area = findArea(areaSlug);
  const post = area ? await getPost(area.slug, slug) : null;
  return area && post ? { area, post } : null;
}

export async function generateMetadata({ params }: PostPageProps) {
  const found = await loadPost(params);
  if (!found) return {};
  const { post } = found;
  return constructMetadata({
    title: post.title,
    description: post.excerpt || undefined,
    type: "article",
    path: `/${post.area}/${post.slug}`,
  });
}

export default async function PostPage({ params }: PostPageProps) {
  const found = await loadPost(params);
  if (!found) {
    notFound();
  }
  return <PostView area={found.area} post={found.post} />;
}
