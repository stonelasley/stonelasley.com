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
  └── blog/            # Blog post JSON files
  └── recipes/         # Recipe JSON files
lib/                   # Utilities & business logic
  ├── content.ts       # Content loaders, pagination
  ├── schemas.ts       # Zod validation schemas
  ├── metadata.ts      # SEO helpers
  └── utils.ts         # Date formatting, URL helpers
public/                # Static assets
site.config.ts         # Central site configuration
```

## Content Management

### Blog Posts (`content/blog/*.json`)
```typescript
// Validated by blogPostSchema in lib/schemas.ts
{
  id, title, slug, excerpt, author,
  category, tags, featured, readTime,
  content, // HTML string
  publishedAt, lastUpdated
}
```

### Recipes (`content/recipes/*.json`)
```typescript
// Validated by recipeSchema in lib/schemas.ts
{
  Name, Description, Category,
  PrepTime, CookTime, ServingsCount,
  Difficulty, Status, // "Draft" | "Published" | "Archived"
  IngredientList, // Notion-style ingredient objects
  Instructions, // HTML string
  NutritionInfo
}
```

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
- `NEXT_PUBLIC_GTM_ID` - Google Tag Manager (optional)
- `NEXT_PUBLIC_SITE_URL` - Site URL for absolute links
- `VERCEL_URL` - Auto-set by Vercel deployment

## Development Commands
```bash
npm run dev           # Start dev server
npm run build         # Build for production
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
