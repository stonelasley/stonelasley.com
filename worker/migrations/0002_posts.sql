-- Posts as ingested from the content repo. D1 is a rebuildable copy of it (ADR 0002).
CREATE TABLE posts (
  id TEXT PRIMARY KEY,                  -- ULID from frontmatter
  area TEXT NOT NULL,                   -- Content Area slug, such as 'ai-development'
  slug TEXT NOT NULL,
  title TEXT NOT NULL,
  date TEXT NOT NULL,                   -- YYYY-MM-DD, display only
  tags TEXT NOT NULL DEFAULT '[]',      -- JSON array of strings
  audience TEXT NOT NULL,
  status TEXT NOT NULL,                 -- 'published' or 'draft'
  body TEXT NOT NULL,                   -- Markdown
  content_hash TEXT NOT NULL,           -- as sent in the ingest manifest
  updated_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
  UNIQUE (area, slug)
);

CREATE INDEX posts_area_date ON posts (area, date DESC);

-- One manifest → upload → commit run.
CREATE TABLE ingest_runs (
  id TEXT PRIMARY KEY,
  hashes TEXT NOT NULL,                 -- JSON object of ULID → content hash for every Post in the manifest
  requested TEXT NOT NULL,              -- JSON array of the ULIDs the manifest asked to be uploaded
  created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
  committed_at TEXT
);

-- Uploaded Posts held until their run commits.
CREATE TABLE ingest_staged_posts (
  run_id TEXT NOT NULL REFERENCES ingest_runs (id),
  id TEXT NOT NULL,
  area TEXT NOT NULL,
  slug TEXT NOT NULL,
  title TEXT NOT NULL,
  date TEXT NOT NULL,
  tags TEXT NOT NULL,
  audience TEXT NOT NULL,
  status TEXT NOT NULL,
  body TEXT NOT NULL,
  content_hash TEXT NOT NULL,
  PRIMARY KEY (run_id, id)
);
