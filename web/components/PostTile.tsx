import Link from "next/link";
import { formatDate } from "@/lib/utils";
import type { PostSummary } from "@/lib/api";
import { DraftBadge } from "./DraftBadge";
import { PostThumbnail } from "./PostThumbnail";

interface PostTileProps {
  post: PostSummary;
}

/** A Post in an area's grid: its hero, then a Watch's make and reference (or the tags), title, excerpt and date. */
export function PostTile({ post }: PostTileProps) {
  const eyebrow = post.watch
    ? `${post.watch.make} · ${post.watch.reference}`
    : post.tags.slice(0, 3).join(" · ");
  return (
    <article className="group relative flex flex-col">
      <PostThumbnail
        hero={post.hero}
        area={post.area}
        watch={post.watch}
        variant="card"
        sizes="(min-width: 1024px) 400px, (min-width: 640px) 50vw, 100vw"
        className="mb-4 aspect-[4/3] w-full rounded-xl"
      />
      {eyebrow && (
        <div className="text-muted-foreground mb-1.5 text-xs font-medium tracking-wider uppercase">
          {eyebrow}
        </div>
      )}
      <h2 className="mb-1.5 text-xl font-semibold tracking-tight">
        {/* The link covers the card, so the image opens the Post too, with one link to announce. */}
        <Link
          href={`/${post.area}/${post.slug}`}
          className="group-hover:underline after:absolute after:inset-0"
        >
          {post.title}
        </Link>
        {post.draft && (
          <>
            {" "}
            <DraftBadge />
          </>
        )}
      </h2>
      {post.excerpt && <p className="text-muted-foreground line-clamp-2">{post.excerpt}</p>}
      <time dateTime={post.date} className="text-muted-foreground mt-2 text-sm">
        {formatDate(post.date)}
      </time>
    </article>
  );
}
