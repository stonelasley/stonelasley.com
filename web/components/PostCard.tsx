import Link from "next/link";
import { formatDate } from "@/lib/utils";
import type { PostSummary } from "@/lib/api";
import { DraftBadge } from "./DraftBadge";
import { Tag } from "./Tag";

interface PostCardProps {
  post: PostSummary;
  headingLevel?: "h2" | "h3";
}

export function PostCard({ post, headingLevel: Heading = "h2" }: PostCardProps) {
  return (
    <article className="group relative flex flex-col space-y-3">
      <Heading className="text-2xl font-semibold tracking-tight">
        <Link href={`/${post.area}/${post.slug}`} className="group-hover:underline">
          {post.title}
        </Link>
        {post.draft && (
          <>
            {" "}
            <DraftBadge />
          </>
        )}
      </Heading>
      {post.excerpt && <p className="text-muted-foreground">{post.excerpt}</p>}
      <div className="text-muted-foreground text-sm">
        <time dateTime={post.date}>{formatDate(post.date)}</time>
      </div>
      {post.tags.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {post.tags.map((tag) => (
            <Tag key={tag} tag={tag} />
          ))}
        </div>
      )}
    </article>
  );
}
