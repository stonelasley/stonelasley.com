# worker

Cloudflare Worker data proxy and D1 migrations. The .NET API is the only caller (ADR 0001), and the Worker holds no domain logic: it checks the bearer secret and passes queries through to D1.

## Endpoints

- `POST /d1/query` with `{ "sql": string, "params"?: unknown[] }` returns `{ "results": object[] }`.
- `POST /d1/batch` with `{ "statements": [{ "sql": string, "params"?: unknown[] }] }` runs them as one transaction and returns `{ "results": object[][] }`.

Every request must carry `Authorization: Bearer <WORKER_SECRET>`, or it gets `401`.

## Local

```bash
cp .dev.vars.example .dev.vars
npm run migrate:local   # apply migrations/ to the local D1 database
npm run dev             # migrate, then wrangler dev on :8787
```
