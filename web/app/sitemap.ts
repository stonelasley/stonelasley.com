import { MetadataRoute } from "next";
import { getAllPosts } from "@/lib/api";
import { contentAreas } from "@/lib/areas";
import { siteConfig } from "@/site.config";

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const posts = (await getAllPosts()).filter((post) => !post.draft); // Drafts never go in the sitemap

  return [
    {
      url: siteConfig.url,
      lastModified: new Date(),
      changeFrequency: "weekly",
      priority: 1,
    },
    ...contentAreas.map((area) => ({
      url: `${siteConfig.url}/${area.slug}`,
      lastModified: new Date(),
      changeFrequency: "weekly" as const,
      priority: 0.9,
    })),
    ...posts.map((post) => ({
      url: `${siteConfig.url}/${post.area}/${post.slug}`,
      lastModified: new Date(post.date),
      changeFrequency: "monthly" as const,
      priority: 0.6,
    })),
  ];
}
