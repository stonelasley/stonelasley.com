import { MetadataRoute } from "next";
import { getAllBlogPosts, getAllRecipes } from "@/lib/content";
import { siteConfig } from "@/site.config";

export default function sitemap(): MetadataRoute.Sitemap {
  const posts = getAllBlogPosts();
  const recipes = getAllRecipes();

  const postUrls = posts.map((post) => ({
    url: `${siteConfig.url}/blog/${post.slug}`,
    lastModified: new Date(post.lastUpdated),
    changeFrequency: "monthly" as const,
    priority: post.featured ? 0.8 : 0.6,
  }));

  const recipeUrls = recipes.map((recipe) => ({
    url: `${siteConfig.url}/recipes/${recipe.slug}`,
    lastModified: new Date(recipe.publishedAt || recipe["date:Date:start"] || new Date()),
    changeFrequency: "monthly" as const,
    priority: 0.6,
  }));

  return [
    {
      url: siteConfig.url,
      lastModified: new Date(),
      changeFrequency: "weekly",
      priority: 1,
    },
    {
      url: `${siteConfig.url}/blog`,
      lastModified: new Date(),
      changeFrequency: "weekly",
      priority: 0.9,
    },
    {
      url: `${siteConfig.url}/recipes`,
      lastModified: new Date(),
      changeFrequency: "weekly",
      priority: 0.9,
    },
    ...postUrls,
    ...recipeUrls,
  ];
}
