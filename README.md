# Stone C. Lasley - Personal Website

A modern, minimal, and fast personal website built with Next.js 16, featuring a blog and recipe collection. Content is compiled at build time from static JSON files for optimal performance.

## Tech Stack

- **Framework**: Next.js 16 (App Router)
- **Language**: TypeScript
- **Styling**: Tailwind CSS 4 with CSS variables for theming
- **Content**: Static JSON files with Zod validation
- **Icons**: Simple Icons
- **Analytics**: Google Tag Manager (optional)
- **Hosting**: Vercel

## Features

- ✨ **Modern Design**: Minimal, grayscale aesthetic with generous whitespace
- 🌙 **Dark Mode**: Auto-respects `prefers-color-scheme` with manual toggle
- 📝 **Blog**: Paginated posts with tag filtering and code syntax highlighting
- 🍳 **Recipes**: Meal prep and macro-friendly recipes with nutrition info
- 🚀 **Performance**: Fully static generation, LCP < 2.0s
- ♿ **Accessible**: WCAG 2.2 AA compliant with keyboard navigation
- 📱 **Responsive**: Mobile-first design that works on all devices
- 🔍 **SEO Optimized**: OpenGraph, Twitter cards, JSON-LD, sitemap, RSS/JSON feeds

## Project Structure

```
/
├── app/                    # Next.js App Router pages
│   ├── blog/              # Blog pages
│   ├── recipes/           # Recipe pages
│   ├── feed.xml/          # RSS feed
│   ├── feed.json/         # JSON feed
│   ├── robots.ts          # Robots.txt
│   └── sitemap.ts         # Sitemap
├── components/            # React components
│   ├── Header.tsx         # Site header with navigation
│   ├── Footer.tsx         # Site footer
│   ├── PostCard.tsx       # Blog post card
│   ├── RecipeCard.tsx     # Recipe card
│   └── ...
├── content/               # Static JSON content
│   ├── blog/*.json        # Blog posts
│   └── recipes/*.json     # Recipes
├── lib/                   # Utilities and helpers
│   ├── content.ts         # Content loading and pagination
│   ├── metadata.ts        # SEO helpers and JSON-LD generators
│   ├── schemas.ts         # Zod schemas
│   └── utils.ts           # General utilities
├── styles/                # Additional styles
│   └── prose.css          # Typography styles
└── site.config.ts         # Site configuration
```

## Getting Started

### Prerequisites

- Node.js 18+
- npm or yarn

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/stonelasley/stonelasley.com.git
   cd stonelasley.com
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Create environment file (optional):
   ```bash
   cp .env.example .env.local
   ```

4. Run the development server:
   ```bash
   npm run dev
   ```

5. Open [http://localhost:3000](http://localhost:3000) in your browser.

## Content Management

### Adding Blog Posts

Create a new JSON file in `/content/blog/`:

```json
{
  "id": "my-post",
  "slug": "my-post",
  "title": "My Blog Post",
  "excerpt": "A brief description of the post.",
  "author": "Stone C. Lasley",
  "category": "Development",
  "tags": ["typescript", "nextjs"],
  "featured": false,
  "readTime": 5,
  "content": "<p>Your HTML content here...</p>",
  "publishedAt": "2025-10-23T12:00:00Z",
  "lastUpdated": "2025-10-23T12:00:00Z"
}
```

### Adding Recipes

Create a new JSON file in `/content/recipes/`:

```json
{
  "url": "https://notion.so/recipe-url",
  "Name": "Recipe Name",
  "slug": "recipe-name",
  "Description": "Recipe description",
  "Category": "Meal Prep",
  "PrepTime": 15,
  "CookTime": 30,
  "Servings": 4,
  "Difficulty": "Easy",
  "Status": "Published",
  "heroImg": "/images/recipe.jpg",
  "publishedAt": "2025-10-23T12:00:00Z",
  "content": "<h2>Ingredients</h2><ul>...</ul><h2>Instructions</h2><ol>...</ol>"
}
```

## Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run start` - Start production server
- `npm run lint` - Run ESLint
- `npm run lint:fix` - Run ESLint and fix issues
- `npm run format` - Format code with Prettier
- `npm run format:check` - Check code formatting
- `npm run type-check` - Run TypeScript type checking

## Configuration

### Site Configuration

Edit `site.config.ts` to customize:

- Site name and description
- Navigation links
- Social media links
- Pagination settings
- Analytics (GTM container ID)

### Styling

The design system uses HSL color variables defined in `app/globals.css`. To customize:

1. Edit the CSS variables in `:root` and `.dark` selectors
2. Colors automatically adapt to light/dark themes
3. All components use these semantic color tokens

## Deployment

### Vercel (Recommended)

1. Push your code to GitHub
2. Import the project in Vercel
3. Configure the domain `stonelasley.com`
4. Set environment variables:
   - `NEXT_PUBLIC_SITE_URL`: Your production URL
   - `NEXT_PUBLIC_GTM_ID`: Google Tag Manager ID (optional)

5. Deploy!

### Other Platforms

The site is a standard Next.js application and can be deployed to any platform that supports Next.js:

- AWS Amplify
- Netlify
- Cloudflare Pages
- Digital Ocean App Platform

## Performance

The site is optimized for performance:

- ✅ All routes are statically generated at build time
- ✅ No client-side data fetching for core content
- ✅ Images optimized with Next.js Image component
- ✅ Code syntax highlighting pre-rendered
- ✅ Minimal JavaScript bundle size

**Target Metrics:**
- LCP < 2.0s
- CLS < 0.05
- TBT < 200ms
- Lighthouse: 90+ across all categories

## Accessibility

The site meets WCAG 2.2 Level AA standards:

- ✅ Keyboard navigable
- ✅ Visible focus indicators
- ✅ Semantic HTML landmarks
- ✅ Alt text for all images
- ✅ Sufficient color contrast
- ✅ `aria-label` for icon-only links
- ✅ `aria-current` for active navigation

## License

This project is private and proprietary. All rights reserved.

## Contact

Stone C. Lasley
- Website: [https://stonelasley.com](https://stonelasley.com)
- GitHub: [@stonelasley](https://github.com/stonelasley)
- Email: website@lasley.io
