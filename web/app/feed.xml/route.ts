import { Feed } from "feed";
import { getAllBlogPosts } from "@/lib/content";
import { siteConfig } from "@/site.config";

export const revalidate = false; // Static generation

export async function GET() {
  const posts = getAllBlogPosts();

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

  posts.forEach((post) => {
    feed.addItem({
      title: post.title,
      id: `${siteConfig.url}/blog/${post.slug}`,
      link: `${siteConfig.url}/blog/${post.slug}`,
      description: post.excerpt,
      content: post.content,
      author: [
        {
          name: post.author,
          email: siteConfig.author.email,
          link: siteConfig.url,
        },
      ],
      date: new Date(post.date),
      category: post.tags?.map((tag) => ({ name: tag })),
    });
  });

  return new Response(feed.rss2(), {
    headers: {
      "Content-Type": "application/xml; charset=utf-8",
    },
  });
}
