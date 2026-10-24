-- The vector leg. Each public, published Post is split into chunks (see Chunker), embedded, and stored in
-- Vectorize as {postId}:{chunkIndex}. D1 keeps each chunk's text (for excerpts) and how many vectors a Post
-- has (to delete them by id). vector_chunks is NULL until the Post's vectors are stored, which makes the next
-- manifest ask for the Post again.
ALTER TABLE posts ADD COLUMN vector_chunks INTEGER;

CREATE TABLE post_chunks (
  post_id TEXT NOT NULL,
  idx INTEGER NOT NULL,
  text TEXT NOT NULL,
  PRIMARY KEY (post_id, idx)
);
