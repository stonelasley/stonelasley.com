import { Container } from "@/components/Container";
import { PostList } from "@/components/PostList";
import { Pagination } from "@/components/Pagination";
import { Tag } from "@/components/Tag";
import {
  getAllBlogPosts,
  getBlogPostsByTag,
  getAllBlogTags,
  paginateItems,
} from "@/lib/content";
import { siteConfig } from "@/site.config";
import { constructMetadata } from "@/lib/metadata";

export const revalidate = false; // Static generation

export const metadata = constructMetadata({
  title: "Blog",
  description: "Thoughts on software engineering, leadership, and technology.",
});

interface BlogPageProps {
  searchParams: Promise<{ page?: string; tag?: string }>;
}

export default async function BlogPage({ searchParams }: BlogPageProps) {
  const params = await searchParams;
  const currentPage = Number(params.page) || 1;
  const selectedTag = params.tag;

  // Get posts, filtered by tag if provided
  const allPosts = selectedTag
    ? getBlogPostsByTag(selectedTag)
    : getAllBlogPosts();

  // Paginate
  const paginatedPosts = paginateItems(
    allPosts,
    currentPage,
    siteConfig.pagination.postsPerPage
  );

  // Get all tags for filter
  const allTags = getAllBlogTags();

  return (
    <Container className="py-16 md:py-24">
      <div className="mb-12">
        <h1 className="text-4xl font-bold tracking-tight mb-4">Blog</h1>
        <p className="text-lg text-muted-foreground">
          Thoughts on software engineering, leadership, and technology.
        </p>
      </div>

      {/* Tag Filter */}
      {allTags.length > 0 && (
        <div className="mb-12">
          <h2 className="text-sm font-medium mb-4">Filter by tag:</h2>
          <div className="flex flex-wrap gap-2">
            <Tag
              tag="All"
              active={!selectedTag}
              href="/blog"
            />
            {allTags.map((tag) => (
              <Tag
                key={tag}
                tag={tag}
                active={selectedTag === tag}
                href={`/blog?tag=${encodeURIComponent(tag)}`}
              />
            ))}
          </div>
        </div>
      )}

      {/* Results Info */}
      {selectedTag && (
        <div className="mb-8">
          <p className="text-sm text-muted-foreground">
            Showing {paginatedPosts.totalItems} {paginatedPosts.totalItems === 1 ? "post" : "posts"}{" "}
            tagged with <strong>{selectedTag}</strong>
          </p>
        </div>
      )}

      {/* Post List */}
      <PostList posts={paginatedPosts.items} />

      {/* Pagination */}
      <Pagination
        currentPage={paginatedPosts.currentPage}
        totalPages={paginatedPosts.totalPages}
        basePath="/blog"
        searchParams={selectedTag ? { tag: selectedTag } : {}}
      />
    </Container>
  );
}
