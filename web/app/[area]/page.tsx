import { notFound } from "next/navigation";
import { Container } from "@/components/Container";
import { PostList } from "@/components/PostList";
import { getAreaIntro, getPostsByArea } from "@/lib/api";
import { markdownToHtml } from "@/lib/markdown";
import { Prose } from "@/components/Prose";
import { contentAreas, findArea } from "@/lib/areas";
import { constructMetadata } from "@/lib/metadata";

export const dynamicParams = false; // The four Content Areas are a closed set

interface AreaPageProps {
  params: Promise<{ area: string }>;
}

export function generateStaticParams() {
  return contentAreas.map((area) => ({ area: area.slug }));
}

export async function generateMetadata({ params }: AreaPageProps) {
  const area = findArea((await params).area);
  return area ? constructMetadata({ title: area.name, description: area.description }) : {};
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
        <h1 className="mb-4 text-4xl font-bold tracking-tight">{area.name}</h1>
        {intro ? (
          <Prose className="text-muted-foreground text-lg">
            <div dangerouslySetInnerHTML={{ __html: await markdownToHtml(intro) }} />
          </Prose>
        ) : (
          <p className="text-muted-foreground text-lg">{area.description}</p>
        )}
      </div>

      <PostList posts={posts} />
    </Container>
  );
}
