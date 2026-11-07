-- Foods now come from the content repo's sourced nutrition table (ADR 0007): calories are its own number, not derived
-- from the macros, and the only unit besides grams is `each`. The table is replaced on every ingest that sends the
-- file, so it is rebuilt empty rather than migrated.
DROP TABLE foods;
CREATE TABLE foods (
  name TEXT PRIMARY KEY,       -- how the food is written, such as 'Rolled Oats'
  kcal REAL NOT NULL,          -- per 100 g
  protein REAL NOT NULL,       -- grams per 100 g
  carbs REAL NOT NULL,
  fat REAL NOT NULL,
  each_grams REAL              -- grams per one, for ingredients counted by 'each'; null when it can't be
);
