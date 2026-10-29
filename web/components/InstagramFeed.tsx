import { getInstagramMedia } from "@/lib/api";
import { siteConfig } from "@/site.config";

/** Recent Instagram photos, each linking to its post. Renders nothing until the API has cached some. */
export async function InstagramFeed() {
  const media = (await getInstagramMedia()).slice(0, 9);
  if (media.length === 0) return null;

  return (
    <section aria-labelledby="instagram" className="py-16 md:py-24">
      <div className="mb-8 flex items-center justify-between">
        <h2 id="instagram" className="text-heading font-bold tracking-tight">
          On Instagram
        </h2>
        <a
          href={siteConfig.social.instagram}
          className="text-muted-foreground hover:text-foreground text-small font-medium"
        >
          Follow →
        </a>
      </div>
      <ul className="grid grid-cols-3 gap-2">
        {media.map((item) => (
          <li key={item.id}>
            <a href={item.permalink} className="block aspect-square overflow-hidden rounded-md">
              {/* eslint-disable-next-line @next/next/no-img-element -- Instagram's CDN URLs; next/image would need every host allow-listed */}
              <img
                src={item.imageUrl}
                alt={item.caption ?? "Instagram post"}
                loading="lazy"
                className="h-full w-full object-cover transition-transform hover:scale-105"
              />
            </a>
          </li>
        ))}
      </ul>
    </section>
  );
}
