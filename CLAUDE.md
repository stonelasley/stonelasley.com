# Claude.md - Project Context

## Project Overview

Personal website for Stone C. Lasley built with **Next.js 16**, **TypeScript**, and **Tailwind CSS 4**. Static-first architecture: public pages are pre-rendered at build time from a .NET API that stores Posts in Cloudflare D1.

## Key Technologies

- **Framework**: Next.js 16 App Router + React 19
- **Language**: TypeScript 5 (strict mode)
- **Styling**: Tailwind CSS 4, grayscale design system
- **Validation**: Zod schemas for type-safe content
- **Code Highlighting**: Shiki (server-side)
- **Testing**: Vitest (unit), Playwright (e2e)
- **Content**: Markdown Posts in a private content repo, ingested into the .NET API (`api/`)

## Architecture Principles

1. **Static Generation**: All pages pre-rendered at build time
2. **Type Safety**: Zod validation of every API response
3. **Performance**: Minimal client-side JS, server components by default
4. **Accessibility**: Semantic HTML, WCAG compliance

## Directory Structure

npm workspaces monorepo. Root scripts (`npm run dev`, `build`, `test`, …) delegate to the workspaces.

```
web/                     # Next.js site (Vercel Root Directory)
  app/                   # Next.js App Router pages
  components/            # React components (Header, Footer, Cards, etc.)
  lib/                   # Utilities & business logic
    ├── api.ts           # API client: Posts by area, Post by slug (Zod-validated)
    ├── areas.ts         # The four Content Areas
    ├── metadata.ts      # SEO helpers
    └── utils.ts         # Date formatting, URL helpers
  public/
    └── images/
      └── notion/        # Downloaded images from Notion (URLs expire after 1hr)
  scripts/
    ├── warm-api.mjs     # prebuild: wait for API_URL/health
    └── fetch-notion-content.js  # Retired from the build; kept for the Notion migration
  site.config.ts         # Central site configuration
  .env.local             # API_URL and site settings (read from web/, not the repo root)
worker/                  # Cloudflare Worker data proxy + D1 migrations (see worker/README.md)
api/                     # .NET API solution + Aspire AppHost (see api/README.md; not an npm workspace)
e2e/                     # Playwright tests (tests/) and config
```

Unless stated otherwise, file paths elsewhere in this document (`lib/`, `app/`, `.env.local`, `scripts/`) are relative to `web/`.

## Content

Posts are Markdown with YAML frontmatter in a private `content` repo (ADR 0002). Ingest pushes them to the .NET API, which stores them in D1 through the Worker. The site reads only the API (ADR 0001), pre-rendering public pages at build time.

```yaml
---
id: 01JBX5Q7Z9K3M8N2P4R6T0V1W2 # ULID; ingest matches on it, so the slug can change
title: Testing at the seams
slug: testing-at-the-seams
area: ai-development # recipes | watches | ai-development | training
date: 2026-10-01
tags: [testing]
audience: public # or clients, or one Client's ULID (a Private Post)
status: published # or draft
---
```

- Parsing and validation live in `api/src/Domain/Posts/MarkdownPost.cs`.
- Ingest is manifest → upload → commit (see `api/README.md`). `sl ingest <dir>` drives it.
- `npm run build` runs `web/scripts/warm-api.mjs` first: it waits for `API_URL/health` with retries and a per-attempt timeout, and fails the build if the API never answers.
- Fixture Posts for E2E live in `e2e/fixtures/content/`. Made-up content only: this repo is public. Clients are declared in the content repo's `_clients.yaml` (ULID and display name); the fixture one uses made-up names.
- Private Posts are never pre-rendered: `proxy.ts` sends visitors with the `sl_access` cookie to the dynamic `/unlocked/[area]/[slug]` route, which forwards the session to the API.
- The Notion fetch script (`web/scripts/fetch-notion-content.js`) is no longer part of the build. It stays until the Notion migration (#54).

## Key Patterns

### Content Loading

All content comes from the API via `web/lib/api.ts`:

```typescript
getPostsByArea(area); // Public Posts in one Content Area, newest first
getAllPosts(); // Across all areas, newest first
getPost(area, slug); // One Post with its Markdown body (and `recipe` in Recipes), or null
getAreaIntro(area); // The area's intro Markdown from the content repo, or null
findArea(slug); // A Content Area from lib/areas.ts, or undefined
```

### Routing

- `/{area}` lists a Content Area (`app/[area]/page.tsx`); the four areas are in `web/lib/areas.ts`
- `/{area}/{slug}` renders a Post (`app/[area]/[slug]/page.tsx`)
- Static params generated from the API at build time

### Theme System

- Client component: `ThemeToggle.tsx`, a Light / Dark / System radiogroup
- Inline `<head>` script (`lib/theme.ts`) sets the class before first paint
- CSS variables in `app/globals.css`; `dark:` follows the `.dark` class on the root
- Persists to localStorage (System = no stored value)

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
- Home page settings (number of recent Posts)

### Environment Variables

**Site Configuration:**

- Analytics: Vercel Web Analytics (`<Analytics />` in `app/layout.tsx`), no configuration
- `NEXT_PUBLIC_SITE_URL` - Site URL for absolute links
- `VERCEL_URL` - Auto-set by Vercel deployment

**API:**

- `API_URL` - The .NET API the build reads from (default `http://localhost:5180`)

## Development Commands

```bash
npm run dev           # Start dev server
npm run build         # Build for production (needs the API; prebuild warms it on /health)
npm run start         # Start production server
npm run lint          # Run ESLint
npm run lint:fix      # Fix ESLint violations
npm run format        # Format with Prettier
npm run type-check    # TypeScript validation
npm run test          # Unit tests (Vitest)
npm run test:e2e      # E2E tests (Playwright) against Worker + API + next start, seeded from e2e/fixtures/content
```

## Important Guidelines

### When Adding Content

Content lives in the private content repo, not here. For local testing, add a Markdown Post under `e2e/fixtures/content/` and run `sl ingest`.

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

- **Theme Toggle**: client component reading localStorage through `useSyncExternalStore` (server snapshot is "system"); the inline script in `lib/theme.ts` paints the right theme first, and repeats `applyTheme`'s rule, so change both together
- **Content Validation**: the API validates on ingest; `web/lib/api.ts` parses responses with Zod
- **Path Aliases**: Use `@/` prefix, configured in `tsconfig.json`
- **Build Time**: public content is loaded from the API at build, so the build needs the API running
- **No `fetch` for API reads**: `lib/api.ts` uses `node:http` because Next caches every `fetch` made while prerendering and reuses it in later builds (Vercel keeps it across deployments), which rendered stale content. Keep API reads in `lib/api.ts`; don't add `revalidate = false` to pages
- **Tailwind CSS 4**: Uses new CSS-based config, check syntax

## Git Workflow

- **Main branch**: `master`
- **Current branch**: `claude/personal-website-setup-011CUR2oMN3tgpHswv1aVRL9`
- Conventional commits preferred
- Run linters before committing
