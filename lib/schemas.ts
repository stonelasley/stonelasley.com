import { z } from "zod";

// Blog Post Schema
export const blogPostSchema = z.object({
  id: z.string(),
  title: z.string(),
  slug: z.string(),
  excerpt: z.string(),
  author: z.string(),
  category: z.string(),
  tags: z.array(z.string()).optional(),
  featured: z.boolean(),
  readTime: z.number(),
  content: z.string(),
  publishedAt: z.string(),
  lastUpdated: z.string(),
});

export type BlogPost = z.infer<typeof blogPostSchema>;

// Recipe Category
const recipeCategorySchema = z.enum([
  "Meal Prep",
  "Smoothies",
  "Vegetables",
  "Meats",
  "Baking",
  "Snacks",
]);

// Recipe Difficulty
const recipeDifficultySchema = z.enum(["Easy", "Medium", "Veteran"]);

// Recipe Status
const recipeStatusSchema = z.enum(["Draft", "Published", "Archived"]);

// Recipe Brand
const recipeBrandSchema = z.enum([
  "Kirkland",
  "Great Value",
  "Dole",
  "Mortons",
  "Jell-O",
  "Impact",
]);

// Recipe Unit
const recipeUnitSchema = z.enum([
  "cup",
  "tablespoon",
  "teaspoon",
  "ounce",
  "pound",
  "gram",
  "kilogram",
  "milliliter",
  "liter",
  "pinch",
  "each",
]);

// Ingredient Schema
export const ingredientSchema = z.object({
  url: z.string(),
  Name: z.string(),
  Description: z.string().optional(),
  Brand: recipeBrandSchema.optional(),
  "In Pantry": z.enum(["__YES__", "__NO__"]).optional(),
  RecipeIngredient: z.string().optional(),
});

export type Ingredient = z.infer<typeof ingredientSchema>;

// RecipeIngredient Junction Schema
export const recipeIngredientSchema = z.object({
  url: z.string(),
  "userDefined:Id": z.string(),
  Recipe: z.string().optional(),
  "Ingredient Database": z.string().optional(),
  Quantity: z.number().optional(),
  Unit: recipeUnitSchema.optional(),
  Purpose: z.string().optional(),
  Instructions: z.string().optional(),
  Optional: z.enum(["__YES__", "__NO__"]).optional(),
  Display: z.string().optional(),
});

export type RecipeIngredient = z.infer<typeof recipeIngredientSchema>;

// RecipeIngredient with Details
export interface RecipeIngredientWithDetails extends RecipeIngredient {
  ingredient?: Ingredient;
}

// Recipe Schema
export const recipeSchema = z.object({
  url: z.string(),
  Name: z.string(),
  slug: z.string(),
  Description: z.string().optional(),
  Category: recipeCategorySchema.optional(),
  PrepTime: z.number().optional(),
  CookTime: z.number().optional(),
  "OvenTemp (F)": z.number().optional(),
  Servings: z.number().optional(),
  "date:Date:start": z.string().optional(),
  "date:Date:end": z.string().optional(),
  "date:Date:is_datetime": z.union([z.literal(0), z.literal(1)]).optional(),
  Difficulty: recipeDifficultySchema.optional(),
  Status: recipeStatusSchema.optional(),
  RecipeIngredient: z.string().optional(),
  IngredientList: z.string().optional(),
  heroImg: z.string().optional(),
  content: z.string().optional(), // Recipe steps/instructions
  publishedAt: z.string().optional(),
});

export type Recipe = z.infer<typeof recipeSchema>;

// Recipe with Ingredients
export interface RecipeWithIngredients extends Recipe {
  ingredients?: RecipeIngredientWithDetails[];
}

// Search Index Item Schema
export const searchIndexItemSchema = z.object({
  title: z.string(),
  slug: z.string(),
  excerpt: z.string(),
  tags: z.array(z.string()),
  type: z.enum(["post", "recipe"]),
});

export type SearchIndexItem = z.infer<typeof searchIndexItemSchema>;
