-- Chunk counts per Post in an index being built for a new index version (#44). Promoting the version copies
-- them into posts.vector_chunks; a Post missing from the build gets NULL, so the next ingest re-indexes it.
CREATE TABLE post_vector_builds (
  version TEXT NOT NULL,
  post_id TEXT NOT NULL,
  chunks INTEGER NOT NULL,
  PRIMARY KEY (version, post_id)
);
