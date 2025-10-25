import { notFound } from "next/navigation";
import Link from "next/link";
import { Container } from "@/components/Container";
import { Prose } from "@/components/Prose";
import { Tag } from "@/components/Tag";
import {
  getAllBlogPosts,
  getBlogPostBySlug,
} from "@/lib/content";
import { constructMetadata, generateBlogPostJsonLd } from "@/lib/metadata";
import { formatDate } from "@/lib/utils";
import { highlightCode } from "@/lib/syntax-highlighting";
import { markdownToHtml } from "@/lib/markdown";

export const revalidate = false; // Static generation

interface BlogPostPageProps {
  params: Promise<{ slug: string }>;
}

export async function generateStaticParams() {
  const posts = getAllBlogPosts();
  return posts.map((post) => ({
    slug: post.slug,
  }));
}

export async function generateMetadata({ params }: BlogPostPageProps) {
  const { slug } = await params;
  const post = getBlogPostBySlug(slug);

  if (!post) {
    return {};
  }

  return constructMetadata({
    title: post.title,
    description: post.excerpt,
    type: "article",
  });
}

export default async function BlogPostPage({ params }: BlogPostPageProps) {
  const { slug } = await params;
  const post = getBlogPostBySlug(slug);

  if (!post) {
    notFound();
  }

  // Convert Markdown to HTML, then highlight code blocks
  const htmlContent = await markdownToHtml(post.content);
  const highlightedContent = await highlightCode(htmlContent);

  // Generate JSON-LD
  const jsonLd = generateBlogPostJsonLd(post);

  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />
      <article>
        <Container className="py-16 md:py-24">
          {/* Back link */}
          <Link
            href="/blog"
            className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors mb-8"
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
            Back to Blog
          </Link>

          {/* Post Header */}
          <header className="mb-12 max-w-3xl">
            {post.featured && (
              <div className="mb-4">
                <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Featured
                </span>
              </div>
            )}
            <h1 className="text-4xl font-bold tracking-tight mb-4 md:text-5xl">
              {post.title}
            </h1>
            <p className="text-xl text-muted-foreground mb-6">{post.excerpt}</p>
            <div className="flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
              <div className="flex items-center gap-2">
                <span>By {post.author}</span>
              </div>
              <span>•</span>
              <time dateTime={post.date}>
                {formatDate(post.date)}
              </time>
              <span>•</span>
              <span>{post.readTime} min read</span>
            </div>
            {post.tags && post.tags.length > 0 && (
              <div className="flex flex-wrap gap-2 mt-6">
                {post.tags.map((tag) => (
                  <Tag key={tag} tag={tag} href={`/blog?tag=${encodeURIComponent(tag)}`} />
                ))}
              </div>
            )}
          </header>

          {/* Post Content */}
          <Prose className="max-w-3xl">
            <div dangerouslySetInnerHTML={{ __html: highlightedContent }} />
          </Prose>

          {/* Post Footer */}
          <footer className="mt-16 pt-8 border-t border-border max-w-3xl">
            <div className="text-sm text-muted-foreground">
              Last updated: {formatDate(post.lastUpdated)}
            </div>
          </footer>
        </Container>
      </article>
    </>
  );
}
