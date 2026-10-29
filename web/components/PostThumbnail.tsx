import { findArea, type AreaSlug } from "@/lib/areas";
import { resizedImage, resizedSrcSet } from "@/lib/images";
import { cn } from "@/lib/utils";

// Each variant's crop, the widths it offers the browser, and the stand-in's type. Compact (the palette's 44 px) and
// row (search's 64–96 px) are square; card (an area's grid, up to ~400 px wide) is 4:3.
const VARIANTS = {
  compact: { aspect: 1, widths: [64, 96, 128, 192, 288], reference: "text-[10px] leading-tight" },
  row: { aspect: 1, widths: [64, 96, 128, 192, 288], reference: "text-small md:text-body" },
  card: {
    aspect: 3 / 4,
    widths: [320, 480, 640, 960, 1280],
    reference: "text-heading tracking-tight",
  },
} as const;

interface PostThumbnailProps {
  hero: string | null;
  area: AreaSlug;
  watch: { make: string; reference: string } | null;
  /** The rendered size, for the browser's pick from the srcset, such as "(min-width: 768px) 96px, 64px". */
  sizes: string;
  /** Compact tiles drop the stand-in's second line and shorten the Content Area to its initial. */
  variant?: keyof typeof VARIANTS;
  className?: string;
}

/**
 * The image beside or above a Post in a list: its hero, or a stand-in until it has one. Decorative, since the title
 * is the link, so screen readers skip it.
 */
export function PostThumbnail({
  hero,
  area,
  watch,
  sizes,
  variant = "row",
  className,
}: PostThumbnailProps) {
  const { aspect, widths, reference } = VARIANTS[variant];
  const compact = variant === "compact";
  // cn() doesn't merge conflicting classes, so the caller sets size and rounding.
  const tile = cn("shrink-0 overflow-hidden", className);
  if (hero) {
    return (
      // eslint-disable-next-line @next/next/no-img-element -- Cloudflare resizes these (lib/images.ts); next/image would resize them again
      <img
        src={resizedImage(hero, widths[3], aspect)}
        srcSet={resizedSrcSet(hero, [...widths], aspect)}
        sizes={sizes}
        alt=""
        width={widths[1]}
        height={Math.round(widths[1] * aspect)}
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
      ? variant === "row" && watch.reference.length > 6
        ? "text-small"
        : reference
      : variant === "card"
        ? "text-small"
        : "text-small";
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
          watch ? "text-foreground tracking-tight tabular-nums" : "",
          textSize
        )}
      >
        {label}
      </span>
      {watch && !compact && (
        <span
          className={cn("text-small", variant === "card" ? "mt-1.5" : "mt-0.5 hidden md:block")}
        >
          {watch.make}
        </span>
      )}
    </div>
  );
}
