import Link from "next/link";
import { Container } from "@/components/Container";
import { PostList } from "@/components/PostList";
import { InstagramFeed } from "@/components/InstagramFeed";
import { getAllPosts } from "@/lib/api";
import { contentAreas } from "@/lib/areas";
import { siteConfig } from "@/site.config";
import { constructMetadata } from "@/lib/metadata";

export const metadata = constructMetadata({ path: "/" });

export default async function Home() {
  const recentPosts = (await getAllPosts()).slice(0, siteConfig.home.recentPosts);

  return (
    <>
      {/* Hero Section */}
      <section className="border-border border-b py-20 md:py-32">
        <Container>
          <div className="mx-auto max-w-3xl text-center">
            <h1 className="text-4xl font-bold tracking-tight sm:text-6xl">{siteConfig.name}</h1>
            <p className="text-muted-foreground mt-6 text-lg leading-8">{siteConfig.intro}</p>
            <div className="mt-10 flex flex-wrap items-center justify-center gap-4">
              {contentAreas.map((area) => (
                <Link
                  key={area.slug}
                  href={`/${area.slug}`}
                  className="border-border bg-background hover:bg-accent rounded-md border px-6 py-3 text-sm font-semibold transition-colors"
                >
                  {area.name}
                </Link>
              ))}
            </div>
          </div>
        </Container>
      </section>

      {/* About Section */}
      <section className="border-border border-b py-16 md:py-24">
        <Container>
          <div className="mx-auto max-w-3xl">
            <div>
              <h2 className="mb-4 text-2xl font-bold tracking-tight">About</h2>
              <div className="prose prose-zinc dark:prose-invert">
                {siteConfig.about.map((paragraph) => (
                  <p key={paragraph}>{paragraph}</p>
                ))}
              </div>
            </div>
          </div>
        </Container>
      </section>

      {/* Recent Posts */}
      {recentPosts.length > 0 && (
        <section className="border-border border-b py-16 md:py-24">
          <Container>
            <div className="mb-12 flex items-center justify-between">
              <h2 className="text-3xl font-bold tracking-tight">Recent Posts</h2>
            </div>
            <PostList posts={recentPosts} headingLevel="h3" />
          </Container>
        </section>
      )}
      <Container>
        <InstagramFeed />
      </Container>
    </>
  );
}
