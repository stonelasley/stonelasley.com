import { Feed } from "feed";
import { getAllPosts } from "./api";
import { siteConfig } from "@/site.config";

/** The site-wide feed of public, published Posts. */
export async function buildFeed(): Promise<Feed> {
  const feed = new Feed({
    title: siteConfig.name,
    description: siteConfig.description,
    id: siteConfig.url,
    link: siteConfig.url,
    language: "en",
    image: `${siteConfig.url}/og-image.jpg`,
    favicon: `${siteConfig.url}/favicon.ico`,
    copyright: `All rights reserved ${new Date().getFullYear()}, ${siteConfig.name}`,
    author: {
      name: siteConfig.author.name,
      email: siteConfig.author.email,
      link: siteConfig.url,
    },
  });

  for (const post of await getAllPosts()) {
    const url = `${siteConfig.url}/${post.area}/${post.slug}`;
    feed.addItem({
      title: post.title,
      id: url,
      link: url,
      date: new Date(post.date),
      category: post.tags.map((tag) => ({ name: tag })),
    });
  }

  return feed;
}
