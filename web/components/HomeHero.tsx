"use client";

import Link from "next/link";
import { useSyncExternalStore } from "react";

export interface HeroPhoto {
  href: string;
  title: string;
  /** A wide crop for md and up, and a 4:3 crop for phones. */
  wide: { src: string; srcSet?: string };
  narrow: { src: string; srcSet?: string };
}

// One draw per page load. The server has none, so it renders the newest photo and the client swaps in its pick
// after hydration; the frame has a fixed height, so the swap doesn't shift the layout.
const seed = typeof window === "undefined" ? null : Math.random();
const subscribe = () => () => {};

/** The homepage's opening photograph: a different Post's hero on each visit. */
export function HomeHero({ photos }: { photos: HeroPhoto[] }) {
  const draw = useSyncExternalStore(
    subscribe,
    () => seed,
    () => null
  );
  const photo = photos[draw == null ? 0 : Math.floor(draw * photos.length)];

  return (
    // The photo is the link to its Post; its alt text (the Post's title) names the link.
    <Link href={photo.href} className="rise group block overflow-hidden [--d:160ms]">
      <picture>
        <source media="(min-width: 768px)" srcSet={photo.wide.srcSet ?? photo.wide.src} />
        <img
          src={photo.narrow.src}
          srcSet={photo.narrow.srcSet}
          sizes="100vw"
          alt={photo.title}
          fetchPriority="high"
          className="bg-muted h-[44svh] w-full object-cover transition-transform duration-700 group-hover:scale-[1.02] md:h-[52svh] dark:brightness-[.85]"
          data-testid="home-hero"
        />
      </picture>
    </Link>
  );
}
