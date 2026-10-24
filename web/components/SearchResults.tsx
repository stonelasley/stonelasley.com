import Link from "next/link";
import type { SearchHit } from "@/lib/api";
import { findArea } from "@/lib/areas";
import { formatDate } from "@/lib/utils";

interface SearchResultsProps {
  results: SearchHit[];
}

/** Search hits with Content Area, date, title and highlighted excerpt. Shared by /search and the palette. */
export function SearchResults({ results }: SearchResultsProps) {
  return (
    <div className="grid gap-8">
      {results.map((hit) => (
        <article key={hit.id} className="space-y-2">
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
            {hit.excerpt.map((part, index) =>
              part.match ? (
                <mark key={index} className="bg-accent text-foreground rounded px-0.5">
                  {part.text}
                </mark>
              ) : (
                <span key={index}>{part.text}</span>
              )
            )}
          </p>
        </article>
      ))}
    </div>
  );
}
