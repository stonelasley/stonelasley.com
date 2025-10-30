import fs from "fs";
import path from "path";
import {
  blogPostSchema,
  recipeSchema,
  type BlogPost,
  type Recipe,
  type SearchIndexItem,
} from "./schemas";
import { ZodError } from "zod";

const contentDirectory = path.join(process.cwd(), "content");
const blogDirectory = path.join(contentDirectory, "blog");
const recipeDirectory = path.join(contentDirectory, "recipes");

// Blog Post Functions
export function getAllBlogPosts(): BlogPost[] {
  if (!fs.existsSync(blogDirectory)) {
    return [];
  }

  const fileNames = fs.readdirSync(blogDirectory);
  const posts = fileNames
    .filter((fileName) => fileName.endsWith(".json"))
    .map((fileName) => {
      const filePath = path.join(blogDirectory, fileName);
      const fileContents = fs.readFileSync(filePath, "utf8");
      const data = JSON.parse(fileContents);

      try {
        return blogPostSchema.parse(data);
      } catch (error) {
        // Provide helpful error message for validation failures
        const postTitle = data.title || fileName;
        console.error(`\n❌ Blog post validation failed for: ${postTitle} (${fileName})`);
        console.error(`   File path: ${filePath}`);

        if (error instanceof ZodError) {
          error.issues.forEach((issue) => {
            const field = issue.path.join(".");
            console.error(`   - Missing or invalid field: "${field}"`);
            console.error(`     ${issue.message}`);
          });
          console.error(`\n   Please ensure the blog post has all required fields.`);
          console.error(
            `   You may need to run "npm run fetch-content" to update posts from Notion.\n`
          );
        }

        throw error;
      }
    });

  // Sort by date, newest first
  return posts.sort((a, b) => {
    return new Date(b.date).getTime() - new Date(a.date).getTime();
  });
}

export function getBlogPostBySlug(slug: string): BlogPost | null {
  const posts = getAllBlogPosts();
  return posts.find((post) => post.slug === slug) || null;
}

export function getBlogPostsByTag(tag: string): BlogPost[] {
  const posts = getAllBlogPosts();
  return posts.filter((post) => post.tags?.includes(tag));
}

export function getAllBlogTags(): string[] {
  const posts = getAllBlogPosts();
  const tags = new Set<string>();
  posts.forEach((post) => {
    post.tags?.forEach((tag) => tags.add(tag));
  });
  return Array.from(tags).sort();
}

export function getFeaturedBlogPosts(limit?: number): BlogPost[] {
  const posts = getAllBlogPosts();
  const featured = posts.filter((post) => post.featured);
  return limit ? featured.slice(0, limit) : featured;
}

export function getRecentBlogPosts(limit: number): BlogPost[] {
  const posts = getAllBlogPosts();
  return posts.slice(0, limit);
}

// Recipe Functions
export function getAllRecipes(): Recipe[] {
  if (!fs.existsSync(recipeDirectory)) {
    return [];
  }

  const fileNames = fs.readdirSync(recipeDirectory);
  const recipes = fileNames
    .filter((fileName) => fileName.endsWith(".json"))
    .map((fileName) => {
      const filePath = path.join(recipeDirectory, fileName);
      const fileContents = fs.readFileSync(filePath, "utf8");
      const data = JSON.parse(fileContents);

      try {
        return recipeSchema.parse(data);
      } catch (error) {
        // Provide helpful error message for validation failures
        const recipeName = data.name || fileName;
        console.error(`\n❌ Recipe validation failed for: ${recipeName} (${fileName})`);
        console.error(`   File path: ${filePath}`);

        if (error instanceof ZodError) {
          error.issues.forEach((issue) => {
            const field = issue.path.join(".");
            console.error(`   - Missing or invalid field: "${field}"`);
            console.error(`     ${issue.message}`);
          });
          console.error(
            `\n   Please ensure the recipe has all required fields, including "content".`
          );
          console.error(
            `   You may need to run "npm run fetch-content" to update recipes from Notion.\n`
          );
        }

        throw error;
      }
    });

  // Sort by date, newest first (using lastUpdated)
  return recipes.sort((a, b) => {
    return new Date(b.lastUpdated).getTime() - new Date(a.lastUpdated).getTime();
  });
}

export function getRecipeBySlug(slug: string): Recipe | null {
  const recipes = getAllRecipes();
  return recipes.find((recipe) => recipe.slug === slug) || null;
}

export function getRecipesByCategory(category: string): Recipe[] {
  const recipes = getAllRecipes();
  return recipes.filter((recipe) => recipe.category === category);
}

export function getAllRecipeCategories(): string[] {
  const recipes = getAllRecipes();
  const categories = new Set<string>();
  recipes.forEach((recipe) => {
    if (recipe.category) {
      categories.add(recipe.category);
    }
  });
  return Array.from(categories).sort();
}

export function getRecentRecipes(limit: number): Recipe[] {
  const recipes = getAllRecipes();
  return recipes.slice(0, limit);
}

export function getLatestRecipe(): Recipe | null {
  const recipes = getAllRecipes();
  return recipes.length > 0 ? recipes[0] : null;
}

// Pagination Helpers
export interface PaginationResult<T> {
  items: T[];
  currentPage: number;
  totalPages: number;
  totalItems: number;
  hasNextPage: boolean;
  hasPrevPage: boolean;
}

export function paginateItems<T>(items: T[], page: number, perPage: number): PaginationResult<T> {
  const totalItems = items.length;
  const totalPages = Math.ceil(totalItems / perPage);
  const currentPage = Math.max(1, Math.min(page, totalPages || 1));
  const startIndex = (currentPage - 1) * perPage;
  const endIndex = startIndex + perPage;
  const paginatedItems = items.slice(startIndex, endIndex);

  return {
    items: paginatedItems,
    currentPage,
    totalPages,
    totalItems,
    hasNextPage: currentPage < totalPages,
    hasPrevPage: currentPage > 1,
  };
}

// Search Index Generation
export function generateSearchIndex(): SearchIndexItem[] {
  const posts = getAllBlogPosts();
  const recipes = getAllRecipes();

  const postItems: SearchIndexItem[] = posts.map((post) => ({
    title: post.title,
    slug: post.slug,
    excerpt: post.excerpt,
    tags: post.tags || [],
    type: "post" as const,
  }));

  const recipeItems: SearchIndexItem[] = recipes.map((recipe) => ({
    title: recipe.name,
    slug: recipe.slug,
    excerpt: recipe.description || "",
    tags: recipe.category ? [recipe.category] : [],
    type: "recipe" as const,
  }));

  return [...postItems, ...recipeItems];
}

// Calculate read time (words per minute)
export function calculateReadTime(content: string, wpm: number = 200): number {
  const words = content.trim().split(/\s+/).length;
  return Math.ceil(words / wpm);
}

// Meal Prep Page
export interface MealPrepPage {
  id: string;
  title: string;
  content: string;
  lastUpdated: string;
}

export function getMealPrepPage(): MealPrepPage | null {
  const filePath = path.join(contentDirectory, "meal-prep.json");

  if (!fs.existsSync(filePath)) {
    return null;
  }

  try {
    const fileContents = fs.readFileSync(filePath, "utf8");
    const data = JSON.parse(fileContents);
    return data as MealPrepPage;
  } catch (error) {
    console.error("Error reading meal prep page:", error);
    return null;
  }
}
