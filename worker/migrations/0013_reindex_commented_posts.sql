-- Search used to index HTML comments in Post bodies (#85). Clearing the stored hash of each Post that has one
-- makes the next ingest ask for it again, so its keyword text and vectors are rebuilt without them. The hash, not
-- vector_chunks, so the old chunks are still counted and deleted, and the keyword row serves until then.
UPDATE posts SET content_hash = '' WHERE body LIKE '%<!--%';
