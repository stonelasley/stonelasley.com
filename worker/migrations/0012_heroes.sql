-- The URL of the first image stored with a Post, shown beside it in search results. NULL until the Post is
-- ingested again (the next ingest asks for it, so stored Posts fill in), '' when it has no image of its own.
ALTER TABLE posts ADD COLUMN hero TEXT;
ALTER TABLE ingest_staged_posts ADD COLUMN hero TEXT NOT NULL DEFAULT '';
