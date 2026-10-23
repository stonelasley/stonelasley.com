import Link from "next/link";
import Image from "next/image";
import { Container } from "@/components/Container";
import { PostCard } from "@/components/PostCard";
import { getAllPosts } from "@/lib/api";
import { contentAreas } from "@/lib/areas";
import { siteConfig } from "@/site.config";

export const revalidate = false; // Static generation

export default async function Home() {
  const recentPosts = (await getAllPosts()).slice(0, siteConfig.home.recentPosts);

  return (
    <>
      {/* Hero Section */}
      <section className="border-border border-b py-20 md:py-32">
        <Container>
          <div className="mx-auto max-w-3xl text-center">
            <h1 className="text-4xl font-bold tracking-tight sm:text-6xl">{siteConfig.name}</h1>
            <p className="text-muted-foreground mt-6 text-lg leading-8">{siteConfig.description}</p>
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
            <div className="flex flex-col gap-8 md:flex-row md:items-start">
              {siteConfig.author.avatar && (
                <div className="shrink-0">
                  <Image
                    src={siteConfig.author.avatar}
                    alt={siteConfig.author.name}
                    width={120}
                    height={120}
                    className="rounded-full"
                  />
                </div>
              )}
              <div>
                <h2 className="mb-4 text-2xl font-bold tracking-tight">About</h2>
                <div className="prose prose-zinc dark:prose-invert">
                  <p>
                    I&apos;m a software leader and builder with a passion for creating elegant
                    solutions to complex problems. With years of experience leading engineering
                    teams, I focus on clean architecture, scalable systems, and empowering teams to
                    do their best work.
                  </p>
                  <p>
                    When I&apos;m not coding or leading teams, you&apos;ll find me in the gym,
                    experimenting with meal prep recipes, or exploring the latest in software
                    engineering practices. This site is where I share my thoughts on technology,
                    leadership, fitness, and food.
                  </p>
                </div>
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
            <div className="grid gap-8 md:gap-12">
              {recentPosts.map((post) => (
                <PostCard key={post.id} post={post} headingLevel="h3" />
              ))}
            </div>
          </Container>
        </section>
      )}
    </>
  );
}
