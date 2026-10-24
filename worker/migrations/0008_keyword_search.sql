-- The keyword leg of search: one row per public, published Post, rebuilt by ingest.
-- Columns are weighted at query time (bm25), in this order: post_id (unindexed), title, structured, tags, body.
CREATE VIRTUAL TABLE posts_fts USING fts5(
  post_id UNINDEXED,
  title,
  structured, -- the typed core as words: Watch make/Reference/model/calibre, Recipe ingredients
  tags,
  body,       -- the Markdown body as plain text
  tokenize = 'porter unicode61'
);

ALTER TABLE ingest_staged_posts ADD COLUMN search_structured TEXT NOT NULL DEFAULT '';
ALTER TABLE ingest_staged_posts ADD COLUMN search_body TEXT NOT NULL DEFAULT '';
