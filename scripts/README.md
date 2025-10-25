# Notion Content Fetching Scripts

This directory contains scripts for fetching content from Notion databases and converting it to static JSON files.

## fetch-notion-content.js

Fetches blog posts and recipes from Notion at build time.

### Usage

```bash
# Manual fetch
npm run fetch-content

# Automatic fetch (runs before build)
npm run build
```

### What it does

1. **Connects to Notion API** using `NOTION_API_KEY` from `.env.local`
2. **Queries databases** for published content (Status = "Published")
3. **Converts Notion blocks to Markdown** using `notion-to-md`
4. **Downloads images locally** to `public/images/notion/` (Notion URLs expire after 1hr)
5. **Generates JSON files**:
   - Individual files: `content/blog/{slug}.json`, `content/recipes/{slug}.json`
   - Metadata: `content/metadata.json`

### Environment Variables Required

```
NOTION_API_KEY=secret_xxxxx...
BLOG_DATABASE_ID=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
RECIPE_DATABASE_ID=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

### Optional Environment Variables

```
INGREDIENT_DATABASE_ID=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
RECIPE_INGREDIENT_DATABASE_ID=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
MEALPREP_PAGE_ID=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

### Output Files

```
content/
├── blog/
│   ├── hello-world.json
│   ├── my-post.json
│   └── ...
├── recipes/
│   ├── overnight-oats.json
│   ├── chicken-meal-prep.json
│   └── ...
├── metadata.json
└── meal-prep.json (optional)

public/images/notion/
├── hello-world-0.png
├── my-post-0.jpg
└── ...
```

### Rate Limiting

- Notion API allows **3 requests per second**
- Script includes 350ms delays between API calls
- Image downloads include 100ms delays
- For large databases, you may need to increase delays

### Error Handling

The script will:
- ✓ Exit with error code 1 if required env vars are missing
- ✓ Log errors for individual content items but continue processing
- ✓ Log warnings for failed image downloads
- ✓ Create output directories if they don't exist

### Troubleshooting

**"NOTION_API_KEY not set"**
- Create `.env.local` with your Notion integration secret
- Get secret from https://www.notion.so/my-integrations

**"Failed to download image"**
- Notion image URLs expire after 1 hour
- Re-run the script to get fresh URLs
- Check network connectivity

**Zod validation errors**
- Content structure doesn't match schemas in `lib/schemas.ts`
- Check required fields are present in Notion
- Verify property names match expected schema

**Rate limit errors (429)**
- Increase delay values in script (currently 350ms)
- Reduce concurrent operations
- Wait and retry

### Development

To modify the script:

1. **Add new content types**: Create new database query and processing functions
2. **Adjust rate limiting**: Modify `delay()` values
3. **Change output format**: Update JSON structure and schemas
4. **Add image optimization**: Process images before saving

### Performance

- **Typical fetch time**: 2-5 seconds per content item
- **Bottleneck**: API rate limits (3 req/sec)
- **Optimization**: Batch fetches for ingredients/relations done upfront

### Integration with Build

The `prebuild` script in `package.json` ensures content is fetched before every production build:

```json
{
  "scripts": {
    "prebuild": "npm run fetch-content",
    "build": "next build"
  }
}
```

This guarantees fresh content on every deployment without requiring manual intervention.
