# Claude.md - Project Context

## Project Overview
Personal website for Stone C. Lasley built with **Next.js 16**, **TypeScript**, and **Tailwind CSS 4**. Static-first architecture with content compiled from JSON files at build time.

## Key Technologies
- **Framework**: Next.js 16 App Router + React 19
- **Language**: TypeScript 5 (strict mode)
- **Styling**: Tailwind CSS 4, grayscale design system
- **Validation**: Zod schemas for type-safe content
- **Code Highlighting**: Shiki (server-side)
- **Testing**: Vitest (unit), Playwright (e2e)
- **CMS**: Notion (headless CMS via Notion API)

## Architecture Principles
1. **Static Generation**: All pages pre-rendered at build time
2. **Type Safety**: Zod validation for all JSON content
3. **Performance**: Minimal client-side JS, server components by default
4. **Accessibility**: Semantic HTML, WCAG compliance

## Directory Structure
```
app/                    # Next.js App Router pages
components/             # React components (Header, Footer, Cards, etc.)
content/               # Static JSON files (blog/, recipes/)
  └── blog/            # Blog post JSON files (generated from Notion)
  └── recipes/         # Recipe JSON files (generated from Notion)
  └── metadata.json    # Build metadata (generated from Notion)
lib/                   # Utilities & business logic
  ├── content.ts       # Content loaders, pagination
  ├── schemas.ts       # Zod validation schemas
  ├── metadata.ts      # SEO helpers
  ├── types.ts         # TypeScript types for Notion content
  └── utils.ts         # Date formatting, URL helpers
public/
  └── images/
    └── notion/        # Downloaded images from Notion (URLs expire after 1hr)
scripts/
  └── fetch-notion-content.js  # Notion content fetching script
site.config.ts         # Central site configuration
```

## Content Management

### Blog Posts (`content/blog/*.json`)
```typescript
// Validated by blogPostSchema in lib/schemas.ts
{
  id, title, slug, date, excerpt,
  author, category, tags, featured,
  readTime, content, // Markdown string
  lastUpdated
}
```

### Recipes (`content/recipes/*.json`)
```typescript
// Validated by recipeSchema in lib/schemas.ts
{
  id, name, slug, description,
  prepTime, cookTime, totalTime,
  ovenTemp, category, difficulty,
  servings, tags, favorite,
  content, // Markdown string
  ingredients, // Optional structured ingredients
  heroImg, lastUpdated
}
```

## Notion Integration (Headless CMS)

### Overview
This site uses **Notion as a headless CMS**. Content is fetched from Notion databases at build time and converted to static JSON files. This approach provides:
- ✓ User-friendly content editing in Notion
- ✓ Zero runtime API calls (fully static)
- ✓ No Notion dependency in production
- ✓ Locally cached images (Notion URLs expire after 1 hour)

### Setup Instructions

#### 1. Create Notion Integration
1. Go to https://www.notion.so/my-integrations
2. Click "New integration"
3. Give it a name (e.g., "Personal Website")
4. Copy the **Internal Integration Secret** (starts with `secret_`)
5. Add this secret to `.env.local` as `NOTION_API_KEY`

#### 2. Set Up Notion Databases

**Required Databases:**
- **Blog Database** - For blog posts
- **Recipe Database** - For recipes

**Optional Databases:**
- **Ingredient Database** - For ingredient details
- **RecipeIngredient Database** - Junction table linking recipes to ingredients
- **Meal Prep Page** - Single page for meal prep content

#### 3. Blog Database Schema

Create a database in Notion with these properties:

| Property | Type | Description | Required |
|----------|------|-------------|----------|
| `Title` | Title | Blog post title | ✓ |
| `Status` | Select | Draft/Published/Archive | ✓ |
| `Date` | Date | Publication date | ✓ |
| `Tags` | Multi-select | Array of tag names | |
| `Category` | Select | Content category | |
| `Excerpt` | Text | Short description (auto-generated if empty) | |
| `Author` | Text | Author name | |
| `Slug` | Text | URL-friendly slug (auto-generated if empty) | |
| `Featured` | Checkbox | Featured post flag | |
| `ReadTime` | Number | Reading time in minutes (auto-calculated if empty) | |

**Important:** Only posts with `Status = Published` will be fetched.

#### 4. Recipe Database Schema

Create a database in Notion with these properties:

| Property | Type | Description | Required |
|----------|------|-------------|----------|
| `Name` | Title | Recipe name | ✓ |
| `Status` | Select | Draft/Published/Archive | ✓ |
| `Description` | Text | Recipe description (auto-generated from content if empty) | |
| `PrepTime` | Number | Preparation time in minutes | |
| `CookTime` | Number | Cooking time in minutes | |
| `Oven Temp (F)` | Number | Oven temperature in Fahrenheit | |
| `Category` | Select | Recipe category | |
| `Difficulty` | Select | Easy/Medium/Veteran | |
| `Servings` | Number | Number of servings | |
| `Tags` | Multi-select | Array of tag names | |
| `Favorite` | Checkbox | Favorite recipe flag | |
| `heroImg` | Files | Hero image URL | |
| `RecipeIngredient` | Relation | Links to RecipeIngredient junction table (optional) | |

**Important:** Only recipes with `Status = Published` and non-empty `Name` will be fetched.

#### 5. Share Databases with Integration
1. Open each database in Notion
2. Click the "..." menu in the top right
3. Click "Add connections"
4. Select your integration
5. Copy the database ID from the URL:
   - URL format: `notion.so/workspace/<DATABASE_ID>?v=...`
   - The database ID is a 32-character hex string
6. Add database IDs to `.env.local`:
   ```
   BLOG_DATABASE_ID=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
   RECIPE_DATABASE_ID=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
   ```

### Fetching Content

**Manual Fetch:**
```bash
npm run fetch-content
```

**Automatic Fetch (during build):**
```bash
npm run build  # Runs prebuild hook which fetches content
```

### How It Works

1. **Script Execution** (`scripts/fetch-notion-content.js`):
   - Connects to Notion API using `NOTION_API_KEY`
   - Queries each database for published content
   - Converts Notion blocks to Markdown using `notion-to-md`
   - Downloads images locally to `public/images/notion/`
   - Generates individual JSON files for each post/recipe
   - Creates `content/metadata.json` with build statistics

2. **Rate Limiting**:
   - Notion API allows 3 requests per second
   - Script includes 350ms delays between API calls
   - Image downloads include 100ms delays

3. **Content Processing**:
   - Notion blocks → Markdown conversion
   - Image URLs → Local file downloads
   - Auto-generated slugs (if not provided)
   - Auto-calculated reading time (if not provided)
   - Auto-generated excerpts (if not provided)

4. **Type Safety**:
   - All JSON validated against Zod schemas in `lib/schemas.ts`
   - TypeScript types in `lib/types.ts`
   - Build fails if content doesn't match schemas

### Advanced: Ingredient Relationships

For complex recipes with structured ingredients:

#### Ingredient Database Schema
| Property | Type | Description |
|----------|------|-------------|
| `Name` | Title | Ingredient name |
| `Description` | Text | Ingredient description |
| `Brand` | Select | Brand name |
| `In Pantry` | Checkbox | Pantry availability |

#### RecipeIngredient Junction Schema
| Property | Type | Description |
|----------|------|-------------|
| `Recipe` | Relation | Single relation to Recipe database |
| `Ingredient Database` | Relation | Single relation to Ingredient database |
| `Quantity` | Number | Amount of ingredient |
| `Unit` | Select | Measurement unit (cup, tbsp, tsp, etc.) |
| `Purpose` | Text | Purpose in recipe |
| `Instructions` | Text | Preparation instructions |
| `Optional` | Checkbox | Whether ingredient is optional |
| `Display` | Formula | Auto-formatted display string |

**Environment Variables:**
```
INGREDIENT_DATABASE_ID=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
RECIPE_INGREDIENT_DATABASE_ID=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

### Troubleshooting

**Error: "NOTION_API_KEY not set"**
- Ensure `.env.local` exists and contains `NOTION_API_KEY`
- Verify the API key starts with `secret_`

**Error: "Failed to download image"**
- Notion image URLs expire after 1 hour
- Re-run `npm run fetch-content` to download fresh URLs
- Check network connectivity

**Error: Zod validation failed**
- Check that JSON structure matches schemas in `lib/schemas.ts`
- Verify all required fields are present in Notion
- Check for typos in property names

**Rate limit errors**
- Notion API allows 3 requests per second
- Script includes delays, but very large databases may need adjustment
- Increase delays in `scripts/fetch-notion-content.js` if needed

### Content Workflow

1. **Edit content in Notion** - Use rich text, images, code blocks, etc.
2. **Set Status to "Published"** - Only published content is fetched
3. **Run fetch script** - `npm run fetch-content` (or happens automatically during build)
4. **Review generated JSON** - Check `content/blog/` and `content/recipes/`
5. **Build and deploy** - `npm run build` (includes fetch in prebuild hook)

## Key Patterns

### Content Loading
All content loaded at build time via `lib/content.ts`:
```typescript
getAllBlogPosts()      // Returns all published posts sorted by date
getBlogPostBySlug()    // Single post by slug
getAllRecipes()        // Returns all published recipes
getRecipeBySlug()      // Single recipe by slug
paginateItems()        // Generic pagination utility
```

### Routing
- Static params: `/blog/[slug]`, `/recipes/[slug]`
- Generate params at build time with `generateStaticParams()`
- Tag filtering via query params: `/blog?tag=typescript`

### Theme System
- Client component: `ThemeToggle.tsx`
- CSS variables in `app/globals.css`
- Dark mode class on document root
- Persists to localStorage

### Component Conventions
- Server components by default
- Client components marked with `'use client'`
- Wrap content in `<Container>` for responsive max-width
- Use `<Prose>` for typography styling on HTML content

## Configuration

### Site Config (`site.config.ts`)
Central configuration for:
- Site metadata (name, description, URL)
- Author information
- Navigation structure
- Social links
- Pagination settings (postsPerPage, recipesPerPage)
- Analytics (GTM container ID)

### Environment Variables

**Site Configuration:**
- `NEXT_PUBLIC_GTM_ID` - Google Tag Manager (optional)
- `NEXT_PUBLIC_SITE_URL` - Site URL for absolute links
- `VERCEL_URL` - Auto-set by Vercel deployment

**Notion Integration (Required for content fetching):**
- `NOTION_API_KEY` - Notion integration secret (starts with `secret_`)
- `BLOG_DATABASE_ID` - 32-character Notion database ID for blog posts
- `RECIPE_DATABASE_ID` - 32-character Notion database ID for recipes

**Notion Integration (Optional):**
- `INGREDIENT_DATABASE_ID` - For ingredient details
- `RECIPE_INGREDIENT_DATABASE_ID` - For recipe-ingredient relationships
- `MEALPREP_PAGE_ID` - For meal prep page content

## Development Commands
```bash
npm run dev           # Start dev server
npm run fetch-content # Fetch content from Notion
npm run build         # Build for production (includes fetch-content via prebuild hook)
npm run start         # Start production server
npm run lint          # Run ESLint
npm run lint:fix      # Fix ESLint violations
npm run format        # Format with Prettier
npm run type-check    # TypeScript validation
npm run test          # Unit tests (Vitest)
npm run test:e2e      # E2E tests (Playwright)
```

## Important Guidelines

### When Adding Content
1. Create JSON file in `content/blog/` or `content/recipes/`
2. Ensure schema compliance (run build to validate)
3. Use valid slug (lowercase, hyphens only)
4. HTML content must be sanitized and valid

### When Adding Components
1. Prefer server components unless client interactivity needed
2. Use TypeScript interfaces for props
3. Follow existing naming: `ComponentName.tsx`
4. Import from `@/components/ComponentName`

### When Modifying Styles
1. Use Tailwind utilities first
2. HSL CSS variables for colors (see `globals.css`)
3. Maintain light/dark mode support
4. Test both themes before committing

### When Adding Dependencies
1. Prefer zero-dependency solutions
2. Check bundle impact (`npm run build`)
3. Update this file if core dependency

## Testing Strategy
- **Unit Tests**: Component logic, utilities (Vitest + Testing Library)
- **E2E Tests**: Full user flows (Playwright)
- **Type Checking**: Pre-commit via TypeScript
- **Lint/Format**: Pre-commit via ESLint + Prettier

## SEO & Metadata
- Metadata helpers in `lib/metadata.ts`
- Dynamic OpenGraph images support
- JSON-LD structured data for articles
- Canonical URLs automatically set
- RSS/JSON feeds ready (using `feed` package)

## Performance Targets
- LCP < 2.0s (Largest Contentful Paint)
- CLS < 0.05 (Cumulative Layout Shift)
- TBT < 200ms (Total Blocking Time)
- Lighthouse score: 90+ across all categories

## Common Tasks

### Add a new blog post
1. Create `content/blog/my-slug.json`
2. Follow `blogPostSchema` structure
3. Run `npm run build` to validate
4. Post appears automatically

### Add a new recipe
1. Create `content/recipes/my-recipe.json`
2. Follow `recipeSchema` structure
3. Include nutrition info if available
4. Set Status: "Published"

### Add a new page
1. Create `app/my-page/page.tsx`
2. Export metadata and default component
3. Add to nav in `site.config.ts` if needed
4. Update sitemap generation if applicable

### Modify design system
1. Update CSS variables in `app/globals.css`
2. Test both light and dark modes
3. Check component inheritance
4. Verify accessibility (contrast ratios)

## Gotchas
- **Theme Toggle**: Must be client component, requires mounted check
- **Pagination**: Page params are strings, convert with `Number()`
- **Content Validation**: Zod throws on invalid JSON, catch in loader
- **Path Aliases**: Use `@/` prefix, configured in `tsconfig.json`
- **Build Time**: All content loaded at build, not runtime
- **Tailwind CSS 4**: Uses new CSS-based config, check syntax

## Git Workflow
- **Main branch**: `master`
- **Current branch**: `claude/personal-website-setup-011CUR2oMN3tgpHswv1aVRL9`
- Conventional commits preferred
- Run linters before committing
