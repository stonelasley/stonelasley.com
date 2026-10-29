import Link from "next/link";
import type { SearchHit } from "@/lib/api";
import { findArea } from "@/lib/areas";
import { formatDate } from "@/lib/utils";
import { PostThumbnail } from "./PostThumbnail";

interface SearchResultsProps {
  results: SearchHit[];
}

/** Search hits with thumbnail, Content Area, date, title and highlighted excerpt, for /search. */
export function SearchResults({ results }: SearchResultsProps) {
  return (
    <div className="grid gap-8">
      {results.map((hit) => (
        <article key={hit.id} className="flex items-start gap-4 md:gap-5">
          <PostThumbnail
            hero={hit.hero}
            area={hit.area}
            watch={hit.watch}
            sizes="(min-width: 768px) 96px, 64px"
            className="size-16 rounded-lg md:size-24"
          />
          <div className="min-w-0 space-y-2">
            <div className="text-muted-foreground flex gap-3 text-xs font-medium tracking-wider uppercase">
              <span>{findArea(hit.area)?.name}</span>
              <time dateTime={hit.date}>{formatDate(hit.date)}</time>
            </div>
            <h2 className="text-xl font-semibold tracking-tight">
              <Link href={`/${hit.area}/${hit.slug}`} className="hover:underline">
                {hit.title}
              </Link>
            </h2>
            <p className="text-muted-foreground">
              <HighlightedExcerpt parts={hit.excerpt} />
            </p>
          </div>
        </article>
      ))}
    </div>
  );
}

/** An excerpt with its matched words marked. */
export function HighlightedExcerpt({ parts }: { parts: SearchHit["excerpt"] }) {
  return (
    <>
      {parts.map((part, index) =>
        part.match ? (
          <mark key={index} className="bg-accent text-foreground rounded px-0.5">
            {part.text}
          </mark>
        ) : (
          <span key={index}>{part.text}</span>
        )
      )}
    </>
  );
}
