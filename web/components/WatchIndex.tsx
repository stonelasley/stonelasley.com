"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import type { PostSummary, WatchFacets } from "@/lib/api";
import { PostList } from "./PostList";

interface WatchIndexProps {
  posts: PostSummary[];
  facets: WatchFacets;
}

const selectClass =
  "border-border bg-background rounded-md border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring";

/**
 * The Watches index with make, model name and decade filters. The list is pre-rendered; filtering happens
 * in the browser and lives in the URL, so a filtered view can be shared.
 */
export function WatchIndex({ posts, facets }: WatchIndexProps) {
  const router = useRouter();
  const pathname = usePathname();
  const params = useSearchParams();
  const make = params.get("make") ?? "";
  const model = params.get("model") ?? "";
  const decade = params.get("decade") ?? "";

  const setFilter = (name: string, value: string) => {
    const next = new URLSearchParams(params);
    if (value) next.set(name, value);
    else next.delete(name);
    const query = next.toString();
    router.replace(query ? `${pathname}?${query}` : pathname, { scroll: false });
  };

  const shown = posts.filter(
    ({ watch }) =>
      watch &&
      (!make || watch.make === make) &&
      (!model || watch.modelName === model) &&
      (!decade || watch.decades.includes(Number(decade)))
  );

  return (
    <>
      <div role="search" aria-label="Filter Watches" className="mb-12 flex flex-wrap gap-4">
        <label className="flex flex-col gap-1 text-sm font-medium">
          Make
          <select
            className={selectClass}
            value={make}
            onChange={(e) => setFilter("make", e.target.value)}
          >
            <option value="">All makes</option>
            {facets.makes.map((m) => (
              <option key={m}>{m}</option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1 text-sm font-medium">
          Model
          <select
            className={selectClass}
            value={model}
            onChange={(e) => setFilter("model", e.target.value)}
          >
            <option value="">All models</option>
            {facets.modelNames.map((m) => (
              <option key={m}>{m}</option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-1 text-sm font-medium">
          Decade
          <select
            className={selectClass}
            value={decade}
            onChange={(e) => setFilter("decade", e.target.value)}
          >
            <option value="">All decades</option>
            {facets.decades.map((d) => (
              <option key={d} value={d}>
                {d}s
              </option>
            ))}
          </select>
        </label>
      </div>
      <PostList posts={shown} />
    </>
  );
}
