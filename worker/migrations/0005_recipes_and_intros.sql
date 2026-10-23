-- A Recipe's typed core as JSON (null outside the Recipes area).
ALTER TABLE posts ADD COLUMN recipe TEXT;
ALTER TABLE ingest_staged_posts ADD COLUMN recipe TEXT;

-- Optional Markdown shown above a Content Area's index, kept in the content repo.
ALTER TABLE ingest_runs ADD COLUMN intros TEXT NOT NULL DEFAULT '{}'; -- JSON object of area slug → Markdown
CREATE TABLE area_intros (
  area TEXT PRIMARY KEY,
  intro TEXT NOT NULL
);
