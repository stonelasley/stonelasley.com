import type { NextConfig } from "next";

// Preview is never indexed. SITE_NOINDEX lets E2E check the header outside Vercel.
const noIndex = process.env.VERCEL_ENV === "preview" || process.env.SITE_NOINDEX === "true";

// Old URLs from the v1 site. A static list, with no redirect table in the API.
const legacyRedirects = [
  { source: "/blog", destination: "/ai-development" },
  { source: "/blog/:slug*", destination: "/ai-development" },
  { source: "/meal-prep", destination: "/recipes" },
  // The v1 slug was cut off; the Post now has the full one.
  {
    source: "/recipes/lemon-blueberry-high-protein-banana-brea",
    destination: "/recipes/lemon-blueberry-high-protein-banana-bread",
  },
];

const nextConfig: NextConfig = {
  async redirects() {
    // statusCode 301 rather than `permanent: true`, which sends 308.
    return legacyRedirects.map((redirect) => ({ ...redirect, statusCode: 301 }));
  },
  async headers() {
    return noIndex
      ? [{ source: "/:path*", headers: [{ key: "X-Robots-Tag", value: "noindex" }] }]
      : [];
  },
};

export default nextConfig;
