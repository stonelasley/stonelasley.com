-- Foods Recipe ingredients may name, for macros computed on read. The content repo's foods file replaces them on
-- each ingest that sends it (ADR 0007). No seed: until the file arrives, Recipes have no macros.
CREATE TABLE foods (
  name TEXT PRIMARY KEY,       -- how the food is written, such as 'rolled oats'
  aliases TEXT NOT NULL,       -- JSON array of other names that mean it
  protein REAL NOT NULL,       -- grams per 100 g
  carbs REAL NOT NULL,
  fat REAL NOT NULL,
  units TEXT NOT NULL          -- JSON object: unit → grams per one of it, such as {"scoop": 30}
);

ALTER TABLE ingest_runs ADD COLUMN foods TEXT; -- JSON [{ Name, Aliases, Protein, Carbs, Fat, Units }], or null to leave the foods alone
