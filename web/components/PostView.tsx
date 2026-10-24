import Link from "next/link";
import { Container } from "@/components/Container";
import { DraftBadge } from "@/components/DraftBadge";
import { Prose } from "@/components/Prose";
import { Tag } from "@/components/Tag";
import { IngredientList, RecipeFacts } from "@/components/RecipeDetails";
import { WatchSpecs } from "@/components/WatchSpecs";
import type { Post } from "@/lib/api";
import type { ContentArea } from "@/lib/areas";
import { generateBlogPostingJsonLd, generateRecipeJsonLd } from "@/lib/metadata";
import { formatDate } from "@/lib/utils";
import { highlightCode } from "@/lib/syntax-highlighting";
import { markdownToHtml } from "@/lib/markdown";

/** One Post's page body: shared by the static route and the unlocked (Private Post) route. */
export async function PostView({
  area,
  post,
  structuredData = true,
}: {
  area: ContentArea;
  post: Post;
  /** JSON-LD for crawlers; off for Private Posts, so their description never leaks. */
  structuredData?: boolean;
}) {
  // Convert Markdown to HTML, then highlight code blocks
  const html = await highlightCode(await markdownToHtml(post.body));

  const { recipe } = post;

  return (
    <article>
      {structuredData && (
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{
            __html: JSON.stringify(
              recipe ? generateRecipeJsonLd(post, recipe) : generateBlogPostingJsonLd(post)
            ),
          }}
        />
      )}
      <Container className="py-16 md:py-24 print:py-0">
        <Link
          href={`/${area.slug}`}
          className="text-muted-foreground hover:text-foreground no-print mb-8 inline-flex items-center gap-2 text-sm transition-colors"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 20 20"
            fill="currentColor"
            className="h-4 w-4"
          >
            <path
              fillRule="evenodd"
              d="M17 10a.75.75 0 01-.75.75H5.612l4.158 3.96a.75.75 0 11-1.04 1.08l-5.5-5.25a.75.75 0 010-1.08l5.5-5.25a.75.75 0 111.04 1.08L5.612 9.25H16.25A.75.75 0 0117 10z"
              clipRule="evenodd"
            />
          </svg>
          Back to {area.name}
        </Link>

        <header className="mb-12 max-w-3xl">
          <h1 className="mb-4 text-4xl font-bold tracking-tight md:text-5xl">{post.title}</h1>
          {post.draft && (
            <p className="mb-4">
              <DraftBadge />
            </p>
          )}
          <div className="text-muted-foreground text-sm">
            <time dateTime={post.date}>{formatDate(post.date)}</time>
          </div>
          {post.tags.length > 0 && (
            <div className="mt-6 flex flex-wrap gap-2">
              {post.tags.map((tag) => (
                <Tag key={tag} tag={tag} />
              ))}
            </div>
          )}
        </header>

        {post.watch && <WatchSpecs watch={post.watch} />}

        {recipe ? (
          <div className="grid gap-8 md:grid-cols-3 print:flex print:flex-col">
            <div className="md:col-span-2">
              <IngredientList recipe={recipe} />
              <Prose>
                <div dangerouslySetInnerHTML={{ __html: html }} />
              </Prose>
            </div>
            <aside className="md:col-span-1 print:order-first">
              <div className="sticky top-24 print:static">
                <RecipeFacts recipe={recipe} />
              </div>
            </aside>
          </div>
        ) : (
          <Prose className="max-w-3xl">
            <div dangerouslySetInnerHTML={{ __html: html }} />
          </Prose>
        )}
      </Container>
    </article>
  );
}
