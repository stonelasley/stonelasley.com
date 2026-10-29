import Link from "next/link";
import Image from "next/image";
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
                    className="size-[120px] rounded-full object-cover"
                  />
                </div>
              )}
              <div>
                <h2 className="mb-4 text-2xl font-bold tracking-tight">About</h2>
                <div className="prose prose-zinc dark:prose-invert flow-root">
                  <p>
                    I&apos;m an AI leader, software builder, and lifelong optimizer. I help teams
                    turn AI from an emerging technology into a practical advantage—adopting it
                    thoughtfully, applying AI-assisted development effectively, and building
                    scalable solutions that deliver meaningful results.
                  </p>
                  <figure className="not-prose mx-auto mb-6 w-full max-w-xs sm:float-right sm:mb-4 sm:ml-6 sm:w-48">
                    <Image
                      src="/images/about/stage.jpg"
                      alt="Stone Lasley on stage at the NPC Idaho Cup"
                      width={600}
                      height={900}
                      sizes="(min-width: 640px) 192px, 320px"
                      className="rounded-md"
                    />
                  </figure>
                  <p>
                    For me, AI isn&apos;t limited to work. I use it throughout my life to train
                    smarter, improve my coaching, optimize my health, and make better decisions.
                    I&apos;m constantly experimenting with ways to combine AI, data, and disciplined
                    habits to improve how I perform, create, and live.
                  </p>
                  <p>
                    This website is part of that experimentation. It&apos;s a live application where
                    I test AI-driven development concepts, workflows, and approaches—including the
                    idea of &ldquo;zero-touch coding.&rdquo; The application has been 100% coded by
                    AI agents. My role has been to provide the vision, requirements, direction, and
                    judgment while allowing the agents to handle the implementation. This approach
                    to development is progressing quickly, and I&apos;m still learning how to trust
                    it, optimize it, and come to terms with what it means for the future of building
                    software.
                  </p>
                  <p>
                    Most importantly, I&apos;m a husband of 20 years and the proud father of a
                    seven-year-old boy. When I&apos;m not building, training, or exploring new
                    ideas, I&apos;m spending time with my family, experimenting with meal-prep
                    recipes, or finding the next opportunity to improve. This site is where I share
                    what I&apos;m learning about AI leadership, personal optimization, fitness,
                    health, and food.
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
