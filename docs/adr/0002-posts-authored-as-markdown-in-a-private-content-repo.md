---
status: accepted
---

# Posts are authored as Markdown in a private content repo

Every Post, public and private, is a Markdown file in a separate private `content` repo, and D1 and Vectorize are rebuildable copies of it. This repo is public, so it can't hold private Client content, and one repo for all Posts keeps a single ingest pipeline. Git as the source means a change to chunking or embedding is a re-ingest, not a data migration, and every edit gets history and review for free.

## Considered Options

- **Public Posts in this repo, private Posts elsewhere.** Rejected: two sources and two pipelines for about 26 Posts.
- **An in-app admin editor with D1 as the source of record.** Rejected for v2: more to build and secure, and D1 would stop being something you can rebuild.
- **Keep Notion as the editor and sync one way.** Rejected: it keeps the dependency v2 exists to remove.

## Consequences

- Each Post carries a ULID `id` in frontmatter. Ingest matches on it, so a slug can change, and deleting the file deletes the Post.
- Every merge sends the full set of published Posts to the API, which reconciles by content hash. Drafts (`status: draft`) are never ingested to prod.
- Images live next to their Post and are uploaded to R2 by ingest under content-hash keys.
- There's no scheduled publishing, and no way to write from a phone without a git client.
