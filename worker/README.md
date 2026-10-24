# worker

Cloudflare Worker data proxy and D1 migrations. The .NET API is the only caller (ADR 0001), and the Worker holds no domain logic: it checks the bearer secret and passes queries through to D1.

## Endpoints

- `POST /d1/query` with `{ "sql": string, "params"?: unknown[] }` returns `{ "results": object[] }`.
- `POST /d1/batch` with `{ "statements": [{ "sql": string, "params"?: unknown[] }] }` runs them as one transaction and returns `{ "results": object[][] }`.

- `HEAD /r2/objects/{key}` answers 200 (with `x-uploaded`) or 404; `PUT /r2/objects/{key}` stores the body with its `content-type`, cached as immutable.
- `GET /public/images/{key}` (no secret, only when `SERVE_IMAGES=true`, so local dev and E2E only) serves an image, standing in for the R2 custom domain.

Every other request must carry `Authorization: Bearer <WORKER_SECRET>`, or it gets `401`.

## Local

```bash
cp .dev.vars.example .dev.vars
npm run migrate:local   # apply migrations/ to the local D1 database
npm run dev             # migrate, then wrangler dev on :8787
```
