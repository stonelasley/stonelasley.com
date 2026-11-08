import Link from "next/link";
import { getPostsByArea, type Watch } from "@/lib/api";
import { makePostFor, moreFromMake } from "@/lib/watches";
import { PostList } from "./PostList";

interface MakeLinksProps {
  slug: string;
  watch: Watch;
}

/**
 * The end of a Watch page: a Reference links to its make's Make Post, and every Watch lists the make's other
 * References. Built from the public Watches list, so a Private Post never appears, and each ingest's revalidation
 * keeps it current.
 */
export async function MakeLinks({ slug, watch }: MakeLinksProps) {
  const posts = await getPostsByArea("watches");
  const makePost = watch.reference !== null ? makePostFor(posts, watch.make) : undefined;
  const others = moreFromMake(posts, { slug, make: watch.make });
  if (!makePost && others.length === 0) return null;

  return (
    <footer className="no-print mt-16 border-t pt-12">
      {makePost && (
        <p className="mb-10">
          <Link
            href={`/${makePost.area}/${makePost.slug}`}
            className="text-heading font-medium hover:underline"
          >
            About {watch.make}
          </Link>
        </p>
      )}
      {others.length > 0 && (
        <section aria-labelledby="more-from-make">
          <h2 id="more-from-make" className="text-heading mb-8 font-semibold tracking-tight">
            More from {watch.make}
          </h2>
          <PostList posts={others} headingLevel="h3" />
        </section>
      )}
    </footer>
  );
}
