import type { NextConfig } from "next";

// Preview is never indexed. SITE_NOINDEX lets E2E check the header outside Vercel.
const noIndex = process.env.VERCEL_ENV === "preview" || process.env.SITE_NOINDEX === "true";

const nextConfig: NextConfig = {
  async headers() {
    return noIndex
      ? [{ source: "/:path*", headers: [{ key: "X-Robots-Tag", value: "noindex" }] }]
      : [];
  },
};

export default nextConfig;
