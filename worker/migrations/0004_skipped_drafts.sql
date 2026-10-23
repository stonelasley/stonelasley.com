-- Drafts a run left out (drafts excluded), with the hash they were sent under. The manifest treats them as
-- unchanged, so an untouched Draft isn't uploaded (or, later, re-embedded) on every run.
CREATE TABLE skipped_drafts (
  id TEXT PRIMARY KEY,
  content_hash TEXT NOT NULL
);
