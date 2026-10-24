import type { Metadata } from "next";
import { siteConfig } from "@/site.config";
import type { Post, Recipe } from "./api";
import { ingredientText } from "./recipe";

export function constructMetadata({
  title = siteConfig.name,
  description = siteConfig.description,
  image = "/og-image.jpg",
  type = "website",
  noIndex = false,
  path,
}: {
  title?: string;
  description?: string;
  image?: string;
  type?: "website" | "article";
  noIndex?: boolean;
  /** The page's own path, such as "/recipes/oats"; sets the canonical URL and og:url. */
  path?: string;
} = {}): Metadata {
  const url = path ? `${siteConfig.url}${path}` : siteConfig.url;
  return {
    ...(path && { alternates: { canonical: url } }),
    title: {
      default: title,
      template: `%s | ${siteConfig.name}`,
    },
    description,
    keywords: ["software engineering", "web development", "recipes", "meal prep", "fitness"],
    authors: [{ name: siteConfig.author.name, url: siteConfig.url }],
    creator: siteConfig.author.name,
    openGraph: {
      type,
      locale: "en_US",
      url,
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

export function generateBlogPostingJsonLd(post: Post) {
  const url = `${siteConfig.url}/${post.area}/${post.slug}`;
  return {
    "@context": "https://schema.org",
    "@type": "BlogPosting",
    headline: post.title,
    description: post.excerpt,
    author: { "@type": "Person", name: siteConfig.author.name, url: siteConfig.url },
    datePublished: post.date,
    keywords: post.tags.join(", "),
    url,
    mainEntityOfPage: { "@type": "WebPage", "@id": url },
  };
}

export function generateRecipeJsonLd(post: Post, recipe: Recipe) {
  const url = `${siteConfig.url}/${post.area}/${post.slug}`;
  return {
    "@context": "https://schema.org",
    "@type": "Recipe",
    name: post.title,
    description: post.excerpt,
    author: { "@type": "Person", name: siteConfig.author.name },
    datePublished: post.date,
    prepTime: `PT${recipe.prepTimeMin}M`,
    cookTime: `PT${recipe.cookTimeMin}M`,
    totalTime: `PT${recipe.totalTimeMin}M`,
    recipeYield: `${recipe.servings} servings`,
    recipeIngredient: recipe.ingredients.map((ingredient) =>
      ingredientText(ingredient, { markOptional: true })
    ),
    keywords: post.tags.join(", "),
    url,
    mainEntityOfPage: { "@type": "WebPage", "@id": url },
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
