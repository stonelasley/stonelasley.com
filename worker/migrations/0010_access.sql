-- Clients and their Access Codes. Clients come from the content repo's clients file through ingest; codes are
-- issued by the admin CLI and stored only as hashes. A Client holds at most one active (unrevoked) grant.
CREATE TABLE clients (
  id TEXT PRIMARY KEY,   -- ULID
  name TEXT NOT NULL     -- display name, from the private content repo
);

CREATE TABLE access_grants (
  id TEXT PRIMARY KEY,
  client_id TEXT NOT NULL,
  code_hash TEXT NOT NULL UNIQUE,  -- SHA-256 of the normalised code
  created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
  revoked_at TEXT                  -- set when revoked or replaced; no expiry otherwise
);

CREATE INDEX access_grants_client ON access_grants (client_id);

ALTER TABLE ingest_runs ADD COLUMN clients TEXT; -- JSON [{ id, name }], or null to leave Clients alone
