-- A Watch's typed core as JSON (null outside the Watches area).
ALTER TABLE posts ADD COLUMN watch TEXT;
ALTER TABLE ingest_staged_posts ADD COLUMN watch TEXT;
