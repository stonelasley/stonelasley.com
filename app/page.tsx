import Link from "next/link";
import Image from "next/image";
import { Container } from "@/components/Container";
import { PostCard } from "@/components/PostCard";
import { RecipeCard } from "@/components/RecipeCard";
import { getRecentBlogPosts, getLatestRecipe } from "@/lib/content";
import { siteConfig } from "@/site.config";

export const revalidate = false; // Static generation

export default function Home() {
  const recentPosts = getRecentBlogPosts(siteConfig.pagination.homeRecentPosts);
  const latestRecipe = getLatestRecipe();

  return (
    <>
      {/* Hero Section */}
      <section className="border-b border-border py-20 md:py-32">
        <Container>
          <div className="mx-auto max-w-3xl text-center">
            <h1 className="text-4xl font-bold tracking-tight sm:text-6xl">
              {siteConfig.name}
            </h1>
            <p className="mt-6 text-lg leading-8 text-muted-foreground">
              {siteConfig.description}
            </p>
            <div className="mt-10 flex items-center justify-center gap-4">
              <Link
                href="/blog"
                className="rounded-md bg-foreground px-6 py-3 text-sm font-semibold text-background shadow-sm hover:bg-foreground/90 transition-colors"
              >
                Read the Blog
              </Link>
              <Link
                href="/recipes"
                className="rounded-md border border-border bg-background px-6 py-3 text-sm font-semibold hover:bg-accent transition-colors"
              >
                View Recipes
              </Link>
            </div>
          </div>
        </Container>
      </section>

      {/* About Section */}
      <section className="border-b border-border py-16 md:py-24">
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
                <h2 className="text-2xl font-bold tracking-tight mb-4">About</h2>
                <div className="prose prose-zinc dark:prose-invert">
                  <p>
                    I'm a software leader and builder with a passion for creating
                    elegant solutions to complex problems. With years of experience
                    leading engineering teams, I focus on clean architecture,
                    scalable systems, and empowering teams to do their best work.
                  </p>
                  <p>
                    When I'm not coding or leading teams, you'll find me in the gym,
                    experimenting with meal prep recipes, or exploring the latest in
                    software engineering practices. This site is where I share my
                    thoughts on technology, leadership, fitness, and food.
                  </p>
                </div>
              </div>
            </div>
          </div>
        </Container>
      </section>

      {/* Recent Blog Posts */}
      {recentPosts.length > 0 && (
        <section className="border-b border-border py-16 md:py-24">
          <Container>
            <div className="flex items-center justify-between mb-12">
              <h2 className="text-3xl font-bold tracking-tight">Recent Posts</h2>
              <Link
                href="/blog"
                className="text-sm font-medium text-muted-foreground hover:text-foreground transition-colors"
              >
                View all posts →
              </Link>
            </div>
            <div className="grid gap-8 md:gap-12">
              {recentPosts.map((post) => (
                <PostCard key={post.id} post={post} />
              ))}
            </div>
          </Container>
        </section>
      )}

      {/* Latest Recipe */}
      {latestRecipe && (
        <section className="py-16 md:py-24">
          <Container>
            <div className="flex items-center justify-between mb-12">
              <h2 className="text-3xl font-bold tracking-tight">Latest Recipe</h2>
              <Link
                href="/recipes"
                className="text-sm font-medium text-muted-foreground hover:text-foreground transition-colors"
              >
                View all recipes →
              </Link>
            </div>
            <div className="mx-auto max-w-2xl">
              <RecipeCard recipe={latestRecipe} />
            </div>
          </Container>
        </section>
      )}
    </>
  );
}
