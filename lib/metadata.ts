import type { Metadata } from "next";
import { siteConfig } from "@/site.config";
import type { BlogPost, Recipe } from "./schemas";

export function constructMetadata({
  title = siteConfig.name,
  description = siteConfig.description,
  image = "/og-image.jpg",
  type = "website",
  noIndex = false,
}: {
  title?: string;
  description?: string;
  image?: string;
  type?: "website" | "article";
  noIndex?: boolean;
} = {}): Metadata {
  return {
    title: {
      default: title,
      template: `%s | ${siteConfig.name}`,
    },
    description,
    keywords: [
      "software engineering",
      "web development",
      "recipes",
      "meal prep",
      "fitness",
    ],
    authors: [{ name: siteConfig.author.name, url: siteConfig.url }],
    creator: siteConfig.author.name,
    openGraph: {
      type,
      locale: "en_US",
      url: siteConfig.url,
      title,
      description,
      siteName: siteConfig.name,
      images: [
        {
          url: image,
          width: 1200,
          height: 630,
          alt: title,
        },
      ],
    },
    twitter: {
      card: "summary_large_image",
      title,
      description,
      images: [image],
      creator: "@stonelasley",
    },
    icons: {
      icon: "/favicon.ico",
      shortcut: "/favicon-16x16.png",
      apple: "/apple-touch-icon.png",
    },
    metadataBase: new URL(siteConfig.url),
    ...(noIndex && {
      robots: {
        index: false,
        follow: false,
      },
    }),
  };
}

export function generateBlogPostJsonLd(post: BlogPost) {
  return {
    "@context": "https://schema.org",
    "@type": "BlogPosting",
    headline: post.title,
    description: post.excerpt,
    author: {
      "@type": "Person",
      name: post.author,
    },
    datePublished: post.date,
    dateModified: post.lastUpdated,
    image: `/og?title=${encodeURIComponent(post.title)}`,
    url: `${siteConfig.url}/blog/${post.slug}`,
    mainEntityOfPage: {
      "@type": "WebPage",
      "@id": `${siteConfig.url}/blog/${post.slug}`,
    },
    publisher: {
      "@type": "Person",
      name: siteConfig.author.name,
      url: siteConfig.url,
    },
  };
}

export function generateRecipeJsonLd(recipe: Recipe) {
  const totalTime = recipe.totalTime || 0;

  return {
    "@context": "https://schema.org",
    "@type": "Recipe",
    name: recipe.name,
    description: recipe.description || "",
    image: recipe.heroImg || `/og?title=${encodeURIComponent(recipe.name)}`,
    author: {
      "@type": "Person",
      name: siteConfig.author.name,
    },
    datePublished: recipe.lastUpdated,
    prepTime: recipe.prepTime ? `PT${recipe.prepTime}M` : undefined,
    cookTime: recipe.cookTime ? `PT${recipe.cookTime}M` : undefined,
    totalTime: totalTime > 0 ? `PT${totalTime}M` : undefined,
    recipeYield: recipe.servings ? `${recipe.servings} servings` : undefined,
    recipeCategory: recipe.category,
    recipeCuisine: "American",
    keywords: recipe.category,
    url: `${siteConfig.url}/recipes/${recipe.slug}`,
  };
}

export function generateBreadcrumbJsonLd(items: { name: string; url: string }[]) {
  return {
    "@context": "https://schema.org",
    "@type": "BreadcrumbList",
    itemListElement: items.map((item, index) => ({
      "@type": "ListItem",
      position: index + 1,
      name: item.name,
      item: `${siteConfig.url}${item.url}`,
    })),
  };
}
