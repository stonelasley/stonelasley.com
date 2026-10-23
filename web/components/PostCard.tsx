import Link from "next/link";
import { formatDate } from "@/lib/utils";
import type { BlogPost } from "@/lib/schemas";
import { Tag } from "./Tag";

interface PostCardProps {
  post: BlogPost;
}

export function PostCard({ post }: PostCardProps) {
  return (
    <article className="group relative flex flex-col space-y-3">
      <Link href={`/blog/${post.slug}`} className="absolute inset-0 z-10">
        <span className="sr-only">Read {post.title}</span>
      </Link>
      <div className="space-y-2">
        {post.featured && (
          <div className="inline-block">
            <span className="text-muted-foreground text-xs font-medium tracking-wider uppercase">
              Featured
            </span>
          </div>
        )}
        <h3 className="text-2xl font-semibold tracking-tight group-hover:underline">
          {post.title}
        </h3>
        <p className="text-muted-foreground">{post.excerpt}</p>
      </div>
      <div className="text-muted-foreground flex items-center justify-between text-sm">
        <div className="flex items-center gap-3">
          <time dateTime={post.date}>{formatDate(post.date)}</time>
          <span>•</span>
          <span>{post.readTime} min read</span>
        </div>
      </div>
      {post.tags && post.tags.length > 0 && (
        <div className="relative z-20 flex flex-wrap gap-2">
          {post.tags.map((tag) => (
            <Tag key={tag} tag={tag} />
          ))}
        </div>
      )}
    </article>
  );
}
