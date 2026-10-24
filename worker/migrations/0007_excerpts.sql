-- A Post's opening prose as plain text, computed at ingest for index summaries, feeds and link previews.
ALTER TABLE posts ADD COLUMN excerpt TEXT NOT NULL DEFAULT '';
ALTER TABLE ingest_staged_posts ADD COLUMN excerpt TEXT NOT NULL DEFAULT '';
