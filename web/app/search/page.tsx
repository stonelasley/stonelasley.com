import Link from "next/link";
import { notFound } from "next/navigation";
import { Container } from "@/components/Container";
import { SearchResults } from "@/components/SearchResults";
import { searchPosts } from "@/lib/api";
import { contentAreas, findArea, type ContentArea } from "@/lib/areas";
import { constructMetadata } from "@/lib/metadata";
import { cn } from "@/lib/utils";

export const metadata = constructMetadata({ title: "Search", path: "/search", noIndex: true });

interface SearchPageProps {
  searchParams: Promise<{ q?: string; area?: string }>;
}

function searchHref(query: string, area?: string) {
  const params = new URLSearchParams({ q: query });
  if (area) params.set("area", area);
  return `/search?${params}`;
}

// A linkable, server-rendered page: the URL is the state, so back and share just work.
export default async function SearchPage({ searchParams }: SearchPageProps) {
  const { q = "", area: areaSlug } = await searchParams;
  const query = q.trim();
  let area: ContentArea | undefined;
  if (areaSlug) {
    area = findArea(areaSlug);
    if (!area) notFound();
  }

  const result = query ? await searchPosts(query, area?.slug) : null;
  const total = result ? Object.values(result.counts).reduce((sum, n) => sum + n, 0) : 0;
  const elsewhere = result
    ? contentAreas.filter((a) => a.slug !== area?.slug && (result.counts[a.slug] ?? 0) > 0)
    : [];

  return (
    <Container className="py-16 md:py-24">
      <h1 className="text-title mb-8 font-bold tracking-tight">Search</h1>

      <form action="/search" method="get" role="search" className="mb-8 flex max-w-2xl gap-2">
        <label htmlFor="q" className="sr-only">
          Search
        </label>
        <input
          id="q"
          name="q"
          type="search"
          defaultValue={query}
          key={query}
          // Otherwise the browser restores what was typed on back, and the box disagrees with the URL.
          autoComplete="off"
          className="border-border bg-background focus:ring-ring flex-1 rounded-md border px-3 py-2 focus:ring-2 focus:outline-none"
        />
        {area && <input type="hidden" name="area" value={area.slug} />}
        <button
          type="submit"
          className="bg-foreground text-background hover:bg-foreground/90 text-small rounded-md px-4 py-2 font-semibold"
        >
          Search
        </button>
      </form>

      {result && (
        <>
          <nav aria-label="Filter by Content Area" className="mb-10 flex flex-wrap gap-2">
            {[
              { slug: undefined, name: "All", count: total },
              ...contentAreas.map((a) => ({ ...a, count: result.counts[a.slug] ?? 0 })),
            ].map((tab) => {
              const current = tab.slug === area?.slug;
              return (
                <Link
                  key={tab.name}
                  href={searchHref(query, tab.slug)}
                  aria-current={current ? "page" : undefined}
                  className={cn(
                    "text-small rounded-full px-3 py-1 font-medium",
                    current
                      ? "bg-foreground text-background"
                      : "bg-secondary text-secondary-foreground"
                  )}
                >
                  {tab.name} {tab.count}
                </Link>
              );
            })}
          </nav>

          {result.results.length > 0 ? (
            <SearchResults results={result.results} />
          ) : area ? (
            <div className="space-y-3">
              <p>
                Nothing matched “{query}” in {area.name}.
              </p>
              {elsewhere.length > 0 && (
                <p>
                  Matches in other areas:{" "}
                  {elsewhere.map((a, index) => (
                    <span key={a.slug}>
                      {index > 0 && ", "}
                      <Link href={searchHref(query, a.slug)} className="underline">
                        {a.name} ({result.counts[a.slug]})
                      </Link>
                    </span>
                  ))}
                </p>
              )}
            </div>
          ) : (
            <div className="space-y-3">
              <p>Nothing matched “{query}”. Try fewer words, or browse:</p>
              <ul className="flex flex-wrap gap-4">
                {contentAreas.map((a) => (
                  <li key={a.slug}>
                    <Link href={`/${a.slug}`} className="underline">
                      {a.name}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </>
      )}
    </Container>
  );
}
