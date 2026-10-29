// Cloudflare resizes images served from this zone (Image Transformations is on for it), via
// https://img.stonelasley.com/cdn-cgi/image/<options>/<path>. Images from anywhere else, such as the local Worker in
// development and E2E, are left as they are.
const RESIZING_ZONE = "stonelasley.com";

/** A crop of `src` at `width` pixels and `aspect` (height over width, square by default), in the best format the browser accepts. */
export function resizedImage(src: string, width: number, aspect = 1): string {
  let url: URL;
  try {
    url = new URL(src);
  } catch {
    return src;
  }
  if (url.hostname !== RESIZING_ZONE && !url.hostname.endsWith(`.${RESIZING_ZONE}`)) {
    return src;
  }
  const height = Math.round(width * aspect);
  return `${url.origin}/cdn-cgi/image/width=${width},height=${height},fit=cover,format=auto${url.pathname}`;
}

/** A `srcset` of crops, so the browser picks the width its layout and screen density need. */
export function resizedSrcSet(src: string, widths: number[], aspect = 1): string | undefined {
  if (resizedImage(src, widths[0]) === src) return undefined;
  return widths.map((width) => `${resizedImage(src, width, aspect)} ${width}w`).join(", ");
}
