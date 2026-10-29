"use client";

import { usePathname, useRouter, useSearchParams } from "next/navigation";
import type { PostSummary, WatchFacets } from "@/lib/api";
import { PostList } from "./PostList";

interface WatchIndexProps {
  posts: PostSummary[];
  facets: WatchFacets;
}

const selectClass =
  "border-border bg-background rounded-md border px-3 py-2 text-small focus:outline-none focus:ring-2 focus:ring-ring";

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

  // Models offered are the chosen make's, so make + model can't combine into an empty list.
  const models = make
    ? [
        ...new Set(posts.flatMap(({ watch }) => (watch?.make === make ? [watch.modelName] : []))),
      ].sort()
    : facets.modelNames;

  const setFilter = (name: string, value: string) => {
    const next = new URLSearchParams(params);
    if (value) next.set(name, value);
    else next.delete(name);
    if (name === "make" && value) {
      const kept = posts.some(({ watch }) => watch?.make === value && watch.modelName === model);
      if (!kept) next.delete("model");
    }
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
        <label className="text-small flex flex-col gap-1 font-medium">
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
        <label className="text-small flex flex-col gap-1 font-medium">
          Model
          <select
            className={selectClass}
            value={model}
            onChange={(e) => setFilter("model", e.target.value)}
          >
            <option value="">All models</option>
            {models.map((m) => (
              <option key={m}>{m}</option>
            ))}
          </select>
        </label>
        <label className="text-small flex flex-col gap-1 font-medium">
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
