#!/usr/bin/env node

/* eslint-disable @typescript-eslint/no-require-imports */

/**
 * Fetch content from Notion databases at build time
 *
 * This script:
 * - Fetches blog posts and recipes from Notion
 * - Converts Notion blocks to Markdown
 * - Downloads and caches images locally (Notion URLs expire after 1 hour)
 * - Generates type-safe JSON files for static site generation
 * - Handles complex relationships (recipes with ingredients)
 */

const { Client } = require("@notionhq/client");
const { NotionToMarkdown } = require("notion-to-md");
const readingTime = require("reading-time");
const fs = require("fs");
const path = require("path");
const https = require("https");
const http = require("http");
const { URL } = require("url");

// Load environment variables from .env.local (for local development)
// In CI/production, environment variables are already set
const dotenvPath = path.resolve(process.cwd(), ".env.local");
if (fs.existsSync(dotenvPath)) {
  require("dotenv").config({ path: dotenvPath });
}

// Configuration
const NOTION_API_KEY = process.env.NOTION_API_KEY;
const BLOG_DATABASE_ID = process.env.BLOG_DATABASE_ID;
const RECIPE_DATABASE_ID = process.env.RECIPE_DATABASE_ID;
const INGREDIENT_DATABASE_ID = process.env.INGREDIENT_DATABASE_ID;
const RECIPE_INGREDIENT_DATABASE_ID = process.env.RECIPE_INGREDIENT_DATABASE_ID;
const MEALPREP_PAGE_ID = process.env.MEALPREP_PAGE_ID;

// Output directories
const CONTENT_DIR = path.join(process.cwd(), "content");
const BLOG_DIR = path.join(CONTENT_DIR, "blog");
const RECIPE_DIR = path.join(CONTENT_DIR, "recipes");
const IMAGES_DIR = path.join(process.cwd(), "public", "images", "notion");

// Initialize Notion clients
const notion = new Client({ auth: NOTION_API_KEY });
const n2m = new NotionToMarkdown({ notionClient: notion });

// Helper: Delay for rate limiting
const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

// Helper: Extract plain text from Notion rich text
function extractPlainText(richText) {
  if (!richText || !Array.isArray(richText)) return "";
  return richText.map((rt) => rt.plain_text).join("");
}

// Helper: Generate URL-friendly slug
function generateSlug(title) {
  return title
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

// Helper: Parse Notion multi-select relations
function parseNotionRelation(relationField) {
  if (!relationField || !Array.isArray(relationField)) return [];
  return relationField.map((item) => item.id);
}

// Helper: Parse Notion single-select relation
function parseNotionSingleRelation(relationField) {
  if (!relationField || !Array.isArray(relationField) || relationField.length === 0) {
    return null;
  }
  return relationField[0].id || null;
}

// Helper: Extract page title (works for any title property)
function extractPageTitle(page, fallback = "") {
  if (!page || !page.properties) return fallback;

  for (const property of Object.values(page.properties)) {
    if (property?.type === "title" && Array.isArray(property.title)) {
      const titleText = extractPlainText(property.title);
      if (titleText) return titleText;
    }
  }

  return fallback;
}

// Helper: Generate excerpt from markdown
function generateExcerptFromMarkdown(markdown, length = 160) {
  if (!markdown) return "";

  const plainText = markdown
    .replace(/```[\s\S]*?```/g, "") // Remove code blocks
    .replace(/`[^`]*`/g, "") // Remove inline code
    .replace(/!\[[^\]]*\]\([^)]*\)/g, "") // Remove images
    .replace(/\[(.*?)\]\([^)]*\)/g, "$1") // Replace links with text
    .replace(/[*_>#\-]+/g, " ") // Remove markdown characters
    .replace(/\s+/g, " ") // Collapse whitespace
    .trim();

  return plainText.slice(0, length).trim();
}

// Helper: Download image from Notion
async function downloadImage(url, filename) {
  return new Promise((resolve, reject) => {
    const parsedUrl = new URL(url);
    const protocol = parsedUrl.protocol === "https:" ? https : http;

    const filepath = path.join(IMAGES_DIR, filename);
    const file = fs.createWriteStream(filepath);

    protocol
      .get(url, (response) => {
        if (response.statusCode === 200) {
          response.pipe(file);
          file.on("finish", () => {
            file.close();
            resolve(`/images/notion/${filename}`);
          });
        } else {
          file.close();
          fs.unlink(filepath, () => {});
          reject(new Error(`Failed to download image: ${response.statusCode}`));
        }
      })
      .on("error", (err) => {
        file.close();
        fs.unlink(filepath, () => {});
        reject(err);
      });
  });
}

// Helper: Process page content (convert to markdown and download images)
async function processPageContent(pageId, slug) {
  try {
    // Get markdown blocks from Notion
    const mdblocks = await n2m.pageToMarkdown(pageId);
    let markdown = n2m.toMarkdownString(mdblocks).parent;

    // Find and download images
    const imageRegex = /!\[.*?\]\((https:\/\/.*?)\)/g;
    let match;
    let imageIndex = 0;
    const imageMap = {};

    while ((match = imageRegex.exec(markdown)) !== null) {
      const imageUrl = match[1];
      const imageExt = path.extname(new URL(imageUrl).pathname) || ".png";
      const filename = `${slug}-${imageIndex}${imageExt}`;

      try {
        const localPath = await downloadImage(imageUrl, filename);
        imageMap[imageUrl] = localPath;
        markdown = markdown.replace(imageUrl, localPath);
        imageIndex++;
        await delay(100); // Rate limiting for image downloads
      } catch (err) {
        console.error(`  ⚠ Failed to download image: ${imageUrl}`, err.message);
      }
    }

    return { markdown, imageMap };
  } catch (error) {
    console.error(`  ✗ Error processing content for page ${pageId}:`, error.message);
    return { markdown: "", imageMap: {} };
  }
}

// Fetch all ingredients (if database exists)
async function fetchAllIngredients() {
  if (!INGREDIENT_DATABASE_ID) {
    console.log("⊘ Ingredient database not configured, skipping...");
    return new Map();
  }

  try {
    console.log("Fetching ingredients...");
    const response = await notion.databases.query({
      database_id: INGREDIENT_DATABASE_ID,
    });

    const ingredientsMap = new Map();
    for (const page of response.results) {
      ingredientsMap.set(page.id, {
        id: page.id,
        name: extractPlainText(page.properties.Name?.title),
        description: extractPlainText(page.properties.Description?.rich_text),
        brand: page.properties.Brand?.select?.name || null,
        inPantry: page.properties["In Pantry"]?.checkbox || false,
      });
    }

    console.log(`✓ Fetched ${ingredientsMap.size} ingredients`);
    await delay(350); // Rate limiting
    return ingredientsMap;
  } catch (error) {
    console.error("✗ Error fetching ingredients:", error.message);
    return new Map();
  }
}

// Fetch all recipe-ingredients (if database exists)
async function fetchAllRecipeIngredients() {
  if (!RECIPE_INGREDIENT_DATABASE_ID) {
    console.log("⊘ RecipeIngredient database not configured, skipping...");
    return new Map();
  }

  try {
    console.log("Fetching recipe-ingredients...");
    const response = await notion.databases.query({
      database_id: RECIPE_INGREDIENT_DATABASE_ID,
    });

    const recipeIngredientsMap = new Map();
    for (const page of response.results) {
      recipeIngredientsMap.set(page.id, {
        id: page.id,
        recipeId: parseNotionSingleRelation(page.properties.Recipe?.relation),
        ingredientId: parseNotionSingleRelation(page.properties["Ingredient Database"]?.relation),
        quantity: page.properties.Quantity?.number || null,
        unit: page.properties.Unit?.select?.name || null,
        purpose: extractPlainText(page.properties.Purpose?.rich_text),
        instructions: extractPlainText(page.properties.Instructions?.rich_text),
        optional: page.properties.Optional?.checkbox || false,
        display: page.properties.Display?.formula?.string || null,
      });
    }

    console.log(`✓ Fetched ${recipeIngredientsMap.size} recipe-ingredients`);
    await delay(350); // Rate limiting
    return recipeIngredientsMap;
  } catch (error) {
    console.error("✗ Error fetching recipe-ingredients:", error.message);
    return new Map();
  }
}

// Build recipe ingredients from local data
function buildRecipeIngredients(recipeIngredientIds, recipeIngredientsMap, ingredientsMap) {
  if (!recipeIngredientIds || recipeIngredientIds.length === 0) return [];

  const ingredientsWithDetails = [];

  for (const riPageId of recipeIngredientIds) {
    if (!riPageId) continue;

    const riData = recipeIngredientsMap.get(riPageId);
    if (!riData) continue;

    const ingredientData = riData.ingredientId ? ingredientsMap.get(riData.ingredientId) : null;

    ingredientsWithDetails.push({
      id: riData.id,
      name: ingredientData?.name || "Unknown Ingredient",
      quantity: riData.quantity,
      unit: riData.unit,
      brand: ingredientData?.brand || null,
      description: ingredientData?.description || "",
      instructions: riData.instructions,
      purpose: riData.purpose,
      optional: riData.optional,
      inPantry: ingredientData?.inPantry || false,
      display: riData.display,
    });
  }

  return ingredientsWithDetails;
}

// Fetch blog posts
async function fetchBlogPosts() {
  try {
    console.log("\nFetching blog posts...");

    const response = await notion.databases.query({
      database_id: BLOG_DATABASE_ID,
      filter: {
        property: "Status",
        select: {
          equals: "Published",
        },
      },
      sorts: [
        {
          property: "Date",
          direction: "descending",
        },
      ],
    });

    await delay(350); // Rate limiting

    const blogPosts = [];

    for (const page of response.results) {
      const title = extractPlainText(page.properties.Title?.title);
      const slug = page.properties.Slug?.rich_text?.[0]?.plain_text || generateSlug(title);

      console.log(`  Processing: ${title}`);

      // Process content
      const { markdown } = await processPageContent(page.id, slug);
      await delay(350); // Rate limiting

      // Calculate reading time
      const calculatedReadTime =
        markdown && markdown.trim() ? Math.ceil(readingTime(markdown).minutes) : 1;
      const readTimeFromNotion = page.properties.ReadTime?.number;

      // Extract or generate excerpt
      const excerptFromNotion = extractPlainText(page.properties.Excerpt?.rich_text);
      const excerpt = excerptFromNotion || generateExcerptFromMarkdown(markdown);

      const blogPost = {
        id: page.id,
        title,
        slug,
        date: page.properties.Date?.date?.start || new Date().toISOString().split("T")[0],
        excerpt,
        author: extractPlainText(page.properties.Author?.rich_text) || "Anonymous",
        category: page.properties.Category?.select?.name || "Uncategorized",
        tags: page.properties.Tags?.multi_select?.map((tag) => tag.name) || [],
        featured: page.properties.Featured?.checkbox || false,
        readTime: readTimeFromNotion || calculatedReadTime,
        content: markdown,
        lastUpdated: page.last_edited_time,
      };

      // Write individual JSON file
      fs.writeFileSync(path.join(BLOG_DIR, `${slug}.json`), JSON.stringify(blogPost, null, 2));

      blogPosts.push(blogPost);
      console.log(`  ✓ Saved: ${slug}.json`);
    }

    console.log(`✓ Fetched ${blogPosts.length} blog posts`);
    return blogPosts;
  } catch (error) {
    console.error("✗ Error fetching blog posts:", error);
    throw error;
  }
}

// Fetch recipes
async function fetchRecipes(recipeIngredientsMap, ingredientsMap) {
  try {
    console.log("\nFetching recipes...");

    const response = await notion.databases.query({
      database_id: RECIPE_DATABASE_ID,
      filter: {
        and: [
          {
            property: "Status",
            select: {
              equals: "Published",
            },
          },
          {
            property: "Name",
            title: {
              is_not_empty: true,
            },
          },
        ],
      },
      sorts: [
        {
          property: "Name",
          direction: "ascending",
        },
      ],
    });

    await delay(350); // Rate limiting

    const recipes = [];

    for (const page of response.results) {
      const name = extractPlainText(page.properties.Name?.title);
      const slug = generateSlug(name);

      console.log(`  Processing: ${name}`);

      // Process content
      const { markdown } = await processPageContent(page.id, slug);
      await delay(350); // Rate limiting

      // Extract hero image if available
      let heroImg = null;
      if (page.properties.heroImg?.files && page.properties.heroImg.files.length > 0) {
        const imageFile = page.properties.heroImg.files[0];
        const imageUrl = imageFile.file?.url || imageFile.external?.url;
        if (imageUrl) {
          try {
            const imageExt = path.extname(new URL(imageUrl).pathname) || ".png";
            const filename = `${slug}-hero${imageExt}`;
            heroImg = await downloadImage(imageUrl, filename);
            await delay(100);
          } catch (err) {
            console.error(`  ⚠ Failed to download hero image:`, err.message);
          }
        }
      }

      // Build ingredient relationships
      const recipeIngredientIds = parseNotionRelation(page.properties.RecipeIngredient?.relation);
      const ingredients = buildRecipeIngredients(
        recipeIngredientIds,
        recipeIngredientsMap,
        ingredientsMap
      );

      // Extract times
      const prepTime = page.properties.PrepTime?.number || 0;
      const cookTime = page.properties.CookTime?.number || 0;

      // Generate excerpt from description or markdown
      const descriptionFromNotion = extractPlainText(page.properties.Description?.rich_text);
      const description = descriptionFromNotion || generateExcerptFromMarkdown(markdown, 200);

      const recipe = {
        id: page.id,
        name,
        slug,
        description,
        prepTime,
        cookTime,
        totalTime: prepTime + cookTime,
        ovenTemp: page.properties["Oven Temp (F)"]?.number || null,
        category: page.properties.Category?.select?.name || "Other",
        difficulty: page.properties.Difficulty?.select?.name || "Medium",
        servings: page.properties.Servings?.number || 1,
        tags: page.properties.Tags?.multi_select?.map((tag) => tag.name) || [],
        favorite: page.properties.Favorite?.checkbox || false,
        content: markdown,
        ingredients: ingredients.length > 0 ? ingredients : undefined,
        heroImg: heroImg || undefined,
        lastUpdated: page.last_edited_time,
      };

      // Write individual JSON file
      fs.writeFileSync(path.join(RECIPE_DIR, `${slug}.json`), JSON.stringify(recipe, null, 2));

      recipes.push(recipe);
      console.log(`  ✓ Saved: ${slug}.json`);
    }

    console.log(`✓ Fetched ${recipes.length} recipes`);
    return recipes;
  } catch (error) {
    console.error("✗ Error fetching recipes:", error);
    throw error;
  }
}

// Fetch meal prep page (optional)
async function fetchMealPrepPage() {
  if (!MEALPREP_PAGE_ID) {
    console.log("\n⊘ Meal prep page not configured, skipping...");
    return null;
  }

  try {
    console.log("\nFetching meal prep page...");

    const page = await notion.pages.retrieve({ page_id: MEALPREP_PAGE_ID });
    await delay(350);

    const title = extractPageTitle(page, "Meal Prep");
    const slug = generateSlug(title);

    const { markdown } = await processPageContent(MEALPREP_PAGE_ID, slug);
    await delay(350);

    const mealPrepPage = {
      id: page.id,
      title,
      content: markdown,
      lastUpdated: page.last_edited_time,
    };

    // Write JSON file
    fs.writeFileSync(
      path.join(CONTENT_DIR, "meal-prep.json"),
      JSON.stringify(mealPrepPage, null, 2)
    );

    console.log(`✓ Fetched meal prep page`);
    return mealPrepPage;
  } catch (error) {
    console.error("✗ Error fetching meal prep page:", error.message);
    return null;
  }
}

// Main execution
async function main() {
  console.log("========================================");
  console.log("Starting Notion content fetch...");
  console.log("========================================\n");

  // Validate environment variables
  if (!NOTION_API_KEY) {
    console.error("✗ ERROR: NOTION_API_KEY environment variable is not set");
    console.error("   Set it in .env.local locally or as a secret in CI");
    process.exit(1);
  }

  if (!BLOG_DATABASE_ID || !RECIPE_DATABASE_ID) {
    console.error(
      "✗ ERROR: BLOG_DATABASE_ID or RECIPE_DATABASE_ID environment variables are not set"
    );
    console.error("   Set them in .env.local locally or as secrets in CI");
    process.exit(1);
  }

  // Create output directories
  if (!fs.existsSync(BLOG_DIR)) {
    fs.mkdirSync(BLOG_DIR, { recursive: true });
    console.log(`✓ Created directory: ${BLOG_DIR}`);
  }
  if (!fs.existsSync(RECIPE_DIR)) {
    fs.mkdirSync(RECIPE_DIR, { recursive: true });
    console.log(`✓ Created directory: ${RECIPE_DIR}`);
  }
  if (!fs.existsSync(IMAGES_DIR)) {
    fs.mkdirSync(IMAGES_DIR, { recursive: true });
    console.log(`✓ Created directory: ${IMAGES_DIR}`);
  }

  try {
    // Fetch supporting data first (if available)
    const ingredientsMap = await fetchAllIngredients();
    const recipeIngredientsMap = await fetchAllRecipeIngredients();

    // Fetch main content
    const blogPosts = await fetchBlogPosts();
    const recipes = await fetchRecipes(recipeIngredientsMap, ingredientsMap);
    const mealPrepPage = await fetchMealPrepPage();

    // Create metadata
    const metadata = {
      lastFetched: new Date().toISOString(),
      blogPostCount: blogPosts.length,
      recipeCount: recipes.length,
      ingredientCount: ingredientsMap.size,
      recipeIngredientCount: recipeIngredientsMap.size,
      totalItems: blogPosts.length + recipes.length,
      mealPrepPageAvailable: Boolean(mealPrepPage && mealPrepPage.content),
    };

    // Write metadata file
    fs.writeFileSync(path.join(CONTENT_DIR, "metadata.json"), JSON.stringify(metadata, null, 2));

    console.log("\n========================================");
    console.log("✓ Content fetch completed successfully!");
    console.log("========================================");
    console.log(`\n📊 Summary:`);
    console.log(`  Blog posts: ${metadata.blogPostCount}`);
    console.log(`  Recipes: ${metadata.recipeCount}`);
    console.log(`  Ingredients: ${metadata.ingredientCount}`);
    console.log(`  Recipe-Ingredients: ${metadata.recipeIngredientCount}`);
    console.log(`  Last fetched: ${metadata.lastFetched}`);
    console.log("");
  } catch (error) {
    console.error("\n========================================");
    console.error("✗ Fatal error:", error);
    console.error("========================================\n");
    process.exit(1);
  }
}

// Run the script
main();
