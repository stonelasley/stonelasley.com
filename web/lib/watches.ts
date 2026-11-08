import type { PostSummary } from "./api";

// A Make Post is a Watch with a make and no Reference; everything else in Watches is a Reference Post.

type WatchSummary = NonNullable<PostSummary["watch"]>;

export interface WatchFilters {
  make: string;
  model: string;
  decade: string;
}

/** What a Watch is labelled by: "Seiko" for a Make Post, "Seiko · 6105" for a Reference. */
export function watchLabel(watch: { make: string; reference: string | null }): string {
  return watch.reference ? `${watch.make} · ${watch.reference}` : watch.make;
}

/** Whether a Watch passes the index filters. A Make Post has no model or decade, so either filter drops it. */
export function matchesWatchFilters(
  watch: WatchSummary | null,
  { make, model, decade }: WatchFilters
): boolean {
  return (
    watch !== null &&
    (!make || watch.make === make) &&
    (!model || watch.modelName === model) &&
    (!decade || watch.decades.includes(Number(decade)))
  );
}

/** The model names of one make's References, sorted. */
export function modelNamesFor(posts: PostSummary[], make: string): string[] {
  return [
    ...new Set(
      posts.flatMap(({ watch }) =>
        watch?.make === make && watch.modelName ? [watch.modelName] : []
      )
    ),
  ].sort();
}

/** The make's Reference Posts other than the one being read, newest first. */
export function moreFromMake(
  posts: PostSummary[],
  current: { slug: string; make: string }
): PostSummary[] {
  return posts
    .filter(
      ({ slug, watch }) =>
        slug !== current.slug && watch?.make === current.make && watch.reference !== null
    )
    .sort((a, b) => b.date.localeCompare(a.date));
}

/** The make's Make Post, if it has one. */
export function makePostFor(posts: PostSummary[], make: string): PostSummary | undefined {
  return posts.find(({ watch }) => watch?.make === make && watch.reference === null);
}
