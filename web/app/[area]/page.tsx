import { notFound } from "next/navigation";
import { Container } from "@/components/Container";
import { PostList } from "@/components/PostList";
import { Suspense } from "react";
import { getAreaIntro, getPostsByArea, getWatchFacets } from "@/lib/api";
import { WatchIndex } from "@/components/WatchIndex";
import { markdownToHtml } from "@/lib/markdown";
import { Prose } from "@/components/Prose";
import { contentAreas, findArea } from "@/lib/areas";
import { constructMetadata } from "@/lib/metadata";

interface AreaPageProps {
  params: Promise<{ area: string }>;
}

export function generateStaticParams() {
  return contentAreas.map((area) => ({ area: area.slug }));
}

export async function generateMetadata({ params }: AreaPageProps) {
  const area = findArea((await params).area);
  return area
    ? constructMetadata({ title: area.name, description: area.description, path: `/${area.slug}` })
    : {};
}

export default async function AreaPage({ params }: AreaPageProps) {
  const area = findArea((await params).area);
  if (!area) {
    notFound();
  }

  const [posts, intro] = await Promise.all([getPostsByArea(area.slug), getAreaIntro(area.slug)]);

  return (
    <Container className="py-16 md:py-24">
      <div className="mb-12">
        <h1 className="text-title mb-4 font-bold tracking-tight">{area.name}</h1>
        {intro ? (
          <Prose className="text-muted-foreground text-body">
            <div dangerouslySetInnerHTML={{ __html: await markdownToHtml(intro) }} />
          </Prose>
        ) : (
          <p className="text-muted-foreground text-body">{area.description}</p>
        )}
      </div>

      {area.slug === "watches" ? (
        // The filters read the URL, so without JavaScript (or before hydration) the full list shows.
        <Suspense fallback={<PostList posts={posts} />}>
          <WatchIndex posts={posts} facets={await getWatchFacets()} />
        </Suspense>
      ) : (
        <PostList posts={posts} />
      )}
    </Container>
  );
}
