// TypeScript types for Notion-fetched content

export interface BlogPost {
  id: string; // Notion page ID
  title: string; // Post title
  slug: string; // URL-friendly slug
  date: string; // ISO 8601 date string
  excerpt: string; // Short description
  author: string; // Author name
  category: string; // Category name
  tags: string[]; // Array of tag names
  featured: boolean; // Featured flag
  readTime: number; // Reading time in minutes
  content: string; // Markdown content
  lastUpdated: string; // ISO 8601 timestamp
}

export interface RecipeDisplay {
  id: string; // Notion page ID
  name: string; // Recipe name
  slug: string; // URL-friendly slug
  description: string; // Description
  prepTime: number; // Prep time in minutes
  cookTime: number; // Cook time in minutes
  totalTime: number; // Total time (prep + cook)
  ovenTemp?: number; // Oven temperature in Fahrenheit
  category: string; // Category name
  difficulty: string; // Difficulty level
  servings: number; // Number of servings
  tags: string[]; // Array of tag names
  favorite: boolean; // Favorite flag
  content: string; // Markdown content
  ingredients?: RecipeIngredientDisplay[]; // Structured ingredients (optional)
  heroImg?: string; // Hero image URL
  lastUpdated: string; // ISO 8601 timestamp
}

export interface RecipeIngredientDisplay {
  id: string; // Junction entry ID
  name: string; // Ingredient name
  quantity?: number; // Amount
  unit?: string; // Measurement unit
  brand?: string; // Brand name
  description?: string; // Ingredient description
  instructions?: string; // Preparation instructions
  purpose?: string; // Purpose in recipe
  optional: boolean; // Optional flag
  inPantry: boolean; // Pantry availability
  display?: string; // Formatted display string
}

export interface Ingredient {
  id: string; // Notion page ID
  name: string; // Ingredient name
  description?: string; // Ingredient description
  brand?: string; // Brand name
  inPantry: boolean; // Pantry availability flag
}

export interface RecipeIngredient {
  id: string; // Junction entry ID
  recipeId: string | null; // Recipe relation ID
  ingredientId: string | null; // Ingredient relation ID
  quantity?: number; // Amount
  unit?: string; // Measurement unit
  purpose?: string; // Purpose in recipe
  instructions?: string; // Preparation instructions
  optional: boolean; // Optional flag
  display?: string; // Formatted display string
}

export interface Metadata {
  lastFetched: string; // ISO 8601 timestamp
  blogPostCount: number; // Number of blog posts
  recipeCount: number; // Number of recipes
  ingredientCount: number; // Number of ingredients
  recipeIngredientCount: number; // Number of recipe-ingredients
  totalItems: number; // Total content items
  mealPrepPageAvailable: boolean; // Meal prep page availability
}

export interface MealPrepPage {
  id: string; // Notion page ID
  title: string; // Page title
  content: string; // Markdown content
  lastUpdated: string; // ISO 8601 timestamp
}
