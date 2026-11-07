# Make Posts: articles about a watch Make, and links between a Make's References

## Intent

The Watches area holds one kind of Post today: a Watch about one Reference (Rolex 16610). Stone wants to write about a Make itself (Seiko's history and character) and wants every Watches page to lead readers on to the other References from the same Make. Reading about Seiko, or about one Seiko Reference, should end with a list of the other Seiko References on the site.

The list must build itself from the data. Publishing a new Seiko Reference adds it to every Seiko page without editing them. This already holds for static pages: after every ingest that commits a change, the API calls the site's `/api/revalidate`, which revalidates the whole site (`web/app/api/revalidate/route.ts`), so every Seiko page re-renders with the new sibling.

## Vocabulary

`GLOSSARY.md` names the company a **Make** and lists "manufacturer" as a term to avoid, so the new kind of Post is a **Make Post**. A Watch Post about one Reference is a **Reference Post** when the two need telling apart. Update `GLOSSARY.md`:

- **Watch**: a Post in the Watches area, either a Make Post or a Reference Post.
- **Make Post** (new): a Watch about a Make as a whole. It names a make and no Reference, and carries no specs.
- **Reference Post** (new): a Watch about one Reference, carrying structured specs for it (today's definition of Watch).

The user-facing word on the site follows the same rule: "More from Seiko", "About Seiko". The word "manufacturer" does not appear in code or UI. (Builder's call: the user said "Manufacturers"; the glossary already settled the term.)

## What gets built

### 1. Authoring and ingest

A Make Post is a Watches Post whose frontmatter has `make` and no `reference`:

```yaml
---
id: 01J...
title: Seiko
slug: seiko
area: watches
date: 2026-11-07
tags: []
audience: public
status: published
make: Seiko
---
```

Rules, enforced in `api/src/Domain/Posts/MarkdownPost.Watch.cs`:

- `make` is required on every Watch, as today, and is still checked against `_makes.yaml` at ingest.
- No `reference` means a Make Post. A Make Post must not carry `modelName`, `variants`, `productionStart`, `productionEnd`, `caseDiameterMm`, `calibre`, `caseMaterial` or `waterResistanceM`; any of them fails with an error naming the field and saying it needs a `reference`. This catches a Reference Post whose `reference` was forgotten, rather than silently turning it into a Make Post.
- A `reference` keeps today's rules: `modelName` required, the rest optional.
- At most one Make Post per make. Ingest fails when a second Watch with no Reference names the same make (after alias resolution), whether the other one is in the same run or already stored. Drafts and Private Posts count; the error names both slugs, which is fine because ingest output is Stone's own. The stored-side check excludes the Post's own `id`, so re-ingesting a Make Post (even with a new slug) passes. (Builder's calls.)
- A stored Reference Post may become a Make Post by dropping its `reference` and specs; same `id`, no extra guard. (Builder's call.)

### 2. Storage and the domain type

The Watch is stored as JSON in the `posts.watch` column (migration 0006), so no migration is needed. `Watch.Reference` and `Watch.ModelName` become nullable. Add an `IsMake` (or similar) property: true when `Reference` is null. Decades for a Make Post are empty because it has no production years.

Re-ingesting existing Reference Posts must produce the same stored JSON as today (no `content_hash` churn). Rows already stored must still deserialize.

### 3. API

- `WatchSummary` (index and tiles) and the full `Watch` on a Post allow a null `reference` and `modelName`.
- `WatchFacetsAsync` skips Make Posts for model names and decades; makes include them (a make with only a Make Post still appears in the make filter).
- `SearchText` indexes a Make Post by its make. Every consumer of `Watch.Reference` must handle null without printing "Seiko " or "Seiko null": at least `SearchWatch` (`KeywordSearch.cs`), `WatchSummary` (`PostReader.cs`), the search palette's result label, and whatever the Ask context prints for a Watch. Sweep with `rg -n "Reference\b|\.reference"` across `api/src` and `web`. No consumer outside this repo reads the API.
- No new endpoint. The page builds the related lists from `getPostsByArea("watches")`, which the build already fetches. (Builder's call; a dedicated endpoint is fine if it turns out cleaner.)

### 4. Web: the Watches index

Make Posts appear in the grid like any other Post, ordered by date with everything else.

- Tile (`PostTile.tsx`) and hero stand-in (`PostThumbnail.tsx`): a Make Post shows "Seiko" where a Reference Post shows "Seiko · 6105".
- Filters (`WatchIndex.tsx`): choosing a make keeps that make's Make Post. Choosing a model or a decade drops Make Posts, since they have neither. "Seiko, 1970s" shows only Seiko References from the 1970s.

### 5. Web: the Post page

On every Watch page, after the body:

- **"More from {make}"**: every other public Reference Post with the same make, newest first by Post `date` (Stone confirmed), as tiles or a compact list (builder's call on look; reuse `PostTile` if it fits). It never includes the Post being read and never includes the Make Post. When there are none, the section is not rendered.
- **"About {make}"**: on a Reference Post, a link to its make's Make Post when one exists and is public. Not shown on the Make Post itself. Placement (near the specs or above "More from") is the builder's call.
- Makes are compared by exact string equality on the stored `make`. Ingest always stores the canonical name; a Post stored before a make was renamed keeps the old name until re-ingested, as ADR 0006 already accepts, and simply won't match until then.
- No cap on the list for now. Revisit when a make reaches dozens of References.
- A Make Post shows no `WatchSpecs` block.
- JSON-LD stays `BlogPosting` for both kinds.

Only public Posts appear in either link. The unlocked (Private Post) route renders the same public lists; a Private Reference never shows up as a sibling. Drafts follow whatever `getPostsByArea` returns in the environment.

### 6. Content repo

Out of this repo's scope, but needed for the feature to be used: Stone's content skill in the private content repo learns to write a Make Post (frontmatter above, no specs). Update `docs/content-repo.md` here to describe Make Posts and the one-per-make rule.

## Goals

- Write about a Make as a Post, published through the existing ingest.
- Every Watch page links readers to the other References from its make, with no hand-maintained lists.
- Reference pages link to their make's article.

## Non-goals

- A separate Makes area, a `/watches/makes/seiko` route, or a Makes index page. Make Posts live at `/watches/{slug}` like any Post.
- Make metadata beyond the name (logo, founding year, country). `_makes.yaml` stays name plus aliases.
- Related links across makes ("you might also like").
- Changes to the Ask or search ranking.

## Anti-goals

- A Reference Post that forgot its `reference` silently publishing as a Make Post.
- Any list that has to be edited by hand when a Reference is added.
- Re-ingest churn or unreadable stored Posts from the type change.
- A Private Post's title or slug appearing on a public page through the related lists.

## Tests

Follow the existing pattern:

- Domain (`MarkdownPost` tests): a Make Post parses; each spec field without a `reference` fails with the right message; Reference Posts unchanged.
- Ingest: one-per-make across a run and against stored Posts, including alias resolution; a second Make Post in a dry run fails.
- API: facets exclude Make Posts from models and decades; summaries carry a null reference.
- Web unit: tile label, index filters with a Make Post present.
- E2E (`e2e/tests/watches.spec.ts`): add a fixture Make Post `e2e/fixtures/content/watches/rolex.md` (made-up content). Check it in the grid and under the Rolex filter, absent under a model filter, "More from Rolex" on a Rolex Reference lists the other Rolex Reference and not itself, "About Rolex" links to the Make Post, the Patek Philippe Reference shows no "More from" section (it's the only one).

## Left to the builder

- Look and placement of "More from" and "About" on the page.
- The exact name of the Make-Post flag on `Watch` and in the web types.
- Whether a dedicated API query for siblings is cleaner than filtering the area list in the page.
- Whether to record this as an ADR (likely worth one short ADR: "A Watch without a Reference is a Make Post").
