import { Fragment } from "react";
import Image from "next/image";
import Link from "next/link";
import { Container } from "@/components/Container";
import { HomeHero, type HeroPhoto } from "@/components/HomeHero";
import { InstagramFeed } from "@/components/InstagramFeed";
import { getAllPosts, type PostSummary } from "@/lib/api";
import { resizedImage, resizedSrcSet } from "@/lib/images";
import { formatDate } from "@/lib/utils";
import { siteConfig } from "@/site.config";
import { constructMetadata } from "@/lib/metadata";

export const metadata = constructMetadata({ path: "/" });

// How many recent Posts with a photo the opening draws from.
const HERO_POOL = 6;
const WIDE = [960, 1440, 1920, 2560];
const NARROW = [480, 768, 1080];

type WithHero = PostSummary & { hero: string };

function heroPhoto(post: WithHero): HeroPhoto {
  return {
    href: `/${post.area}/${post.slug}`,
    title: post.title,
    wide: {
      src: resizedImage(post.hero, 1920, 1 / 2),
      srcSet: resizedSrcSet(post.hero, WIDE, 1 / 2),
    },
    narrow: {
      src: resizedImage(post.hero, 768, 3 / 4),
      srcSet: resizedSrcSet(post.hero, NARROW, 3 / 4),
    },
  };
}

export default async function Home() {
  const withHero = (await getAllPosts()).filter((post): post is WithHero => post.hero != null);
  const exhibits = withHero.slice(0, siteConfig.home.recentPosts);

  return (
    <>
      <section className="pt-2 pb-24 md:pb-40">
        <Container>
          {withHero.length > 0 && <HomeHero photos={withHero.slice(0, HERO_POOL).map(heroPhoto)} />}
          <h1 className="rise text-display mt-4 font-extralight md:mt-2">{siteConfig.name}</h1>
          <div className="mt-6 grid gap-6 md:mt-8 md:grid-cols-12 md:items-start">
            <p className="rise md:text-heading leading-snug [--d:80ms] md:col-span-7 md:leading-snug">
              {siteConfig.intro}
            </p>
            <Link
              href="/watches"
              className="rise bg-action text-action-foreground inline-flex h-12 items-center justify-self-start px-6 font-semibold transition [--d:120ms] hover:brightness-110 active:scale-[0.98] md:col-span-5 md:justify-self-end"
            >
              Read the watch notes
            </Link>
          </div>
        </Container>
      </section>

      {exhibits.length > 0 && (
        <section aria-labelledby="recent" className="pb-24 md:pb-40">
          <Container>
            <h2 id="recent" className="reveal text-title mb-12 font-light md:mb-20">
              Recent notes
            </h2>
            <div className="space-y-24 md:space-y-40">
              {exhibits.map((post, index) => (
                <Exhibit key={post.id} post={post} flip={index % 2 === 1} />
              ))}
            </div>
          </Container>
        </section>
      )}

      <section aria-labelledby="about" className="pb-24 md:pb-40">
        <Container>
          <div className="reveal grid gap-6 md:grid-cols-12">
            <div className="md:col-span-4">
              <h2 id="about" className="text-title font-light">
                About
              </h2>
              <Image
                src={siteConfig.author.avatar}
                alt={siteConfig.author.name}
                width={120}
                height={120}
                className="mt-6 size-[120px] rounded-full object-cover"
              />
            </div>
            <div className="flow-root space-y-4 md:col-span-7 md:col-start-6 md:pt-3">
              {siteConfig.about.map((paragraph, index) => (
                <Fragment key={paragraph}>
                  {/* The stage shot sits beside the training paragraph. */}
                  {index === 1 && (
                    <figure className="mx-auto mb-6 w-full max-w-xs sm:float-right sm:mb-4 sm:ml-6 sm:w-48">
                      <Image
                        src="/images/about/stage.jpg"
                        alt="Stone Lasley on stage at the NPC Idaho Cup"
                        width={600}
                        height={900}
                        sizes="(min-width: 640px) 192px, 320px"
                        className="rounded-md"
                      />
                    </figure>
                  )}
                  <p>{paragraph}</p>
                </Fragment>
              ))}
            </div>
          </div>
        </Container>
      </section>

      <Container>
        <InstagramFeed />
      </Container>
    </>
  );
}

/**
 * A recent Post as a lit exhibit: a large photograph, and beside it the Watch's reference at size, then the title.
 * The photograph links too, but out of the tab order, so the title is the one link announced.
 */
function Exhibit({ post, flip }: { post: WithHero; flip: boolean }) {
  const href = `/${post.area}/${post.slug}`;
  return (
    <article className="group reveal grid gap-6 md:grid-cols-12 md:items-end md:gap-12">
      <Link
        href={href}
        tabIndex={-1}
        aria-hidden="true"
        className={
          flip
            ? "block overflow-hidden md:col-span-8 md:col-start-5"
            : "block overflow-hidden md:col-span-8"
        }
      >
        {/* eslint-disable-next-line @next/next/no-img-element -- Cloudflare resizes these (lib/images.ts) */}
        <img
          src={resizedImage(post.hero, 1280, 3 / 4)}
          srcSet={resizedSrcSet(post.hero, [640, 960, 1280, 1920], 3 / 4)}
          sizes="(min-width: 768px) 66vw, 100vw"
          alt=""
          loading="lazy"
          decoding="async"
          className="bg-muted aspect-[4/3] w-full object-cover transition-transform duration-700 group-hover:scale-[1.03]"
          data-testid="post-thumbnail"
        />
      </Link>
      <div className={flip ? "md:col-span-4 md:col-start-1 md:row-start-1" : "md:col-span-4"}>
        {post.watch && (
          <p className="text-title font-extralight tabular-nums" aria-hidden="true">
            {post.watch.reference}
          </p>
        )}
        <h3 className="text-heading mt-3 font-medium">
          <Link href={href} className="group-hover:underline">
            {post.title}
          </Link>
        </h3>
        {post.excerpt && <p className="mt-3 line-clamp-3">{post.excerpt}</p>}
        <time dateTime={post.date} className="text-muted-foreground text-small mt-3 block">
          {formatDate(post.date)}
        </time>
      </div>
    </article>
  );
}
