import { z } from "zod";

// Blog Post Schema (Notion format)
export const blogPostSchema = z.object({
  id: z.string(),
  title: z.string(),
  slug: z.string(),
  date: z.string(), // ISO 8601 date string
  excerpt: z.string(),
  author: z.string(),
  category: z.string(),
  tags: z.array(z.string()).default([]),
  featured: z.boolean(),
  readTime: z.number(),
  content: z.string().default(""),
  lastUpdated: z.string(),
});

export type BlogPost = z.infer<typeof blogPostSchema>;

// Recipe Category (flexible string instead of strict enum)
const recipeCategorySchema = z.string();

// Recipe Difficulty
const recipeDifficultySchema = z.enum(["Easy", "Medium", "Veteran"]);

// Recipe Brand (flexible string instead of strict enum)
const recipeBrandSchema = z.string().nullable();

// Recipe Unit (flexible string instead of strict enum)
const recipeUnitSchema = z.string().nullable();

// Ingredient Schema (Notion format)
export const ingredientSchema = z.object({
  id: z.string(),
  name: z.string(),
  description: z.string().optional(),
  brand: recipeBrandSchema.optional(),
  inPantry: z.boolean(),
});

export type Ingredient = z.infer<typeof ingredientSchema>;

// RecipeIngredient Display Schema (Notion format)
export const recipeIngredientDisplaySchema = z.object({
  id: z.string(),
  name: z.string(),
  quantity: z.number().nullable().optional(),
  unit: recipeUnitSchema.optional(),
  brand: recipeBrandSchema.optional(),
  description: z.string().optional(),
  instructions: z.string().optional(),
  purpose: z.string().optional(),
  optional: z.boolean(),
  inPantry: z.boolean(),
  display: z.string().optional(),
});

export type RecipeIngredientDisplay = z.infer<typeof recipeIngredientDisplaySchema>;

// Recipe Schema (Notion format)
export const recipeSchema = z.object({
  id: z.string(),
  name: z.string(),
  slug: z.string(),
  description: z.string(),
  prepTime: z.number(),
  cookTime: z.number(),
  totalTime: z.number(),
  ovenTemp: z.number().nullable().optional(),
  category: recipeCategorySchema,
  difficulty: recipeDifficultySchema,
  servings: z.number(),
  tags: z.array(z.string()).default([]),
  favorite: z.boolean(),
  content: z.string().default(""),
  ingredients: z.array(recipeIngredientDisplaySchema).optional(),
  heroImg: z.string().optional(),
  lastUpdated: z.string(),
});

export type Recipe = z.infer<typeof recipeSchema>;

// Search Index Item Schema
export const searchIndexItemSchema = z.object({
  title: z.string(),
  slug: z.string(),
  excerpt: z.string(),
  tags: z.array(z.string()),
  type: z.enum(["post", "recipe"]),
});

export type SearchIndexItem = z.infer<typeof searchIndexItemSchema>;
