// Cloudflare resizes images served from this zone (Image Transformations is on for it), via
// https://img.stonelasley.com/cdn-cgi/image/<options>/<path>. Images from anywhere else, such as the local Worker in
// development and E2E, are left as they are.
const RESIZING_ZONE = "stonelasley.com";

/** A square crop of `src` at `size` pixels, in the best format the browser accepts. */
export function resizedImage(src: string, size: number): string {
  let url: URL;
  try {
    url = new URL(src);
  } catch {
    return src;
  }
  if (url.hostname !== RESIZING_ZONE && !url.hostname.endsWith(`.${RESIZING_ZONE}`)) {
    return src;
  }
  return `${url.origin}/cdn-cgi/image/width=${size},height=${size},fit=cover,format=auto${url.pathname}`;
}

/** A `srcset` of square crops, so the browser picks the size its layout and screen density need. */
export function resizedSrcSet(src: string, sizes: number[]): string | undefined {
  if (resizedImage(src, sizes[0]) === src) return undefined;
  return sizes.map((size) => `${resizedImage(src, size)} ${size}w`).join(", ");
}
