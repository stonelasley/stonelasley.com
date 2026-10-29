import { findArea, type AreaSlug } from "@/lib/areas";
import { resizedImage, resizedSrcSet } from "@/lib/images";
import { cn } from "@/lib/utils";

// Covers 44 px (palette) to 96 px (search page) at 1×–3× density.
const SIZES = [64, 96, 128, 192, 288];

interface PostThumbnailProps {
  hero: string | null;
  area: AreaSlug;
  watch: { make: string; reference: string } | null;
  /** The rendered size, for the browser's pick from the srcset, such as "(min-width: 768px) 96px, 64px". */
  sizes: string;
  /** Small tiles drop the stand-in's second line. */
  compact?: boolean;
  className?: string;
}

/**
 * The square image beside a Post in a list: its hero, or a stand-in until it has one. Decorative, since the title
 * beside it is the link, so screen readers skip it.
 */
export function PostThumbnail({
  hero,
  area,
  watch,
  sizes,
  compact,
  className,
}: PostThumbnailProps) {
  // cn() doesn't merge conflicting classes, so the caller sets size and rounding.
  const tile = cn("shrink-0 overflow-hidden", className);
  if (hero) {
    return (
      // eslint-disable-next-line @next/next/no-img-element -- Cloudflare resizes these (lib/images.ts); next/image would resize them again
      <img
        src={resizedImage(hero, 192)}
        srcSet={resizedSrcSet(hero, SIZES)}
        sizes={sizes}
        alt=""
        width={96}
        height={96}
        loading="lazy"
        decoding="async"
        className={cn(tile, "bg-muted object-cover")}
        data-testid="post-thumbnail"
      />
    );
  }

  // A Watch shows its reference, anything else its Content Area: in full, or the initial on a small tile.
  const areaName = findArea(area)?.name ?? "";
  const label = watch?.reference ?? (compact ? areaName.charAt(0) : areaName);
  const textSize = compact
    ? "text-[10px] leading-tight"
    : watch
      ? watch.reference.length > 6
        ? "text-xs md:text-sm"
        : "text-sm md:text-lg"
      : "text-[10px] tracking-wider md:text-xs";
  return (
    <div
      aria-hidden="true"
      className={cn(
        tile,
        "bg-muted text-muted-foreground flex flex-col items-center justify-center p-1 text-center"
      )}
      data-testid="post-thumbnail-standin"
    >
      <span
        className={cn(
          "max-w-full font-semibold break-all",
          watch ? "text-foreground font-mono tracking-tight" : "uppercase",
          textSize
        )}
      >
        {label}
      </span>
      {watch && !compact && (
        <span className="mt-0.5 text-[9px] tracking-widest uppercase md:text-[10px]">
          {watch.make}
        </span>
      )}
    </div>
  );
}
