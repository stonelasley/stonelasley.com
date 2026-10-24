-- A dry run validates and reports but writes no Posts. With drafts excluded, `status: draft` Posts are
-- neither stored nor kept.
ALTER TABLE ingest_runs ADD COLUMN dry_run INTEGER NOT NULL DEFAULT 0;
ALTER TABLE ingest_runs ADD COLUMN include_drafts INTEGER NOT NULL DEFAULT 0;
