# api

The .NET API: the only reader and writer of Cloudflare data (ADR 0001). It reaches D1 through the Worker data proxy in `../worker`, authenticating with a bearer secret.

| Project              | Role                                                                                    |
| -------------------- | --------------------------------------------------------------------------------------- |
| `src/Domain`         | Domain model: Post, Content Area, and Markdown frontmatter parsing                      |
| `src/Application`    | Use cases and the ports they need, such as `ID1Database`                                |
| `src/Infrastructure` | Adapters for those ports, such as the Worker client                                     |
| `src/Api`            | ASP.NET Core host. `/health` is healthy only when a query reaches D1 through the Worker |
| `src/Cli`            | Admin CLI (`sl`): `health`, `ingest <dir>`. Calls the API, never the Worker             |
| `src/AppHost`        | Aspire: runs the Worker, the API and `next dev` together                                |

## Run everything locally

```bash
npm install                                  # from the repo root, once
dotnet run --project api/src/AppHost         # Worker on :8787, API on :5180, web on an Aspire-assigned port
```

The AppHost hands the same `worker-secret` parameter to the API and the Worker, and the `ingest-token` parameter to the API. They default to `local-dev-secret` and `local-ingest-token` in `appsettings.json`.

## Endpoints

| Route                                 | Auth                            | Does                                                                                                                                                                                                                                        |
| ------------------------------------- | ------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `GET /health`                         | none                            | Healthy only when `SELECT 1` reaches D1 through the Worker                                                                                                                                                                                  |
| `GET /areas/{area}`                   | none                            | `{ slug, intro }`: the area's intro Markdown, or null                                                                                                                                                                                       |
| `GET /areas/{area}/posts`             | none                            | Public, published Posts in a Content Area, newest first                                                                                                                                                                                     |
| `GET /areas/{area}/posts/{slug}`      | none                            | One published Post with its Markdown body (and `recipe` or `watch` typed core). Private Posts need `X-Access-Session`, checked against the grant on every request                                                                           |
| `GET /areas/watches/facets`           | none                            | `{ makes, modelNames, decades }` across public Watches, for the index filters                                                                                                                                                               |
| `GET /posts/latest?area=&limit=`      | none                            | The newest public, published Posts (limit 1 to 50, default 5), across areas or within one                                                                                                                                                   |
| `GET /search?q=&area=`                | none                            | Keyword search (FTS5, weighted bm25 from `Search:Bm25`) over public, published Posts: `{ query, area, results: [{ id, title, slug, area, date, excerpt: [{ text, match }] }], counts: { area: n } }`. Query words are escaped and OR-joined |
| `POST /access/exchange`               | none (Turnstile, 10/min per IP) | `{ code, turnstileToken }` → `{ session, clientName }`; any failure is the same 401                                                                                                                                                         |
| `POST /admin/clients/{ulid}/codes`    | admin token                     | Issue a Client's Access Code (printed once, only its hash stored), revoking any earlier one                                                                                                                                                 |
| `DELETE /admin/clients/{ulid}/codes`  | admin token                     | Revoke a Client's code                                                                                                                                                                                                                      |
| `POST /ingest/manifest`               | ingest token                    | `{ posts: { ulid: hash }, intros?: { area: markdown }, clients?: yaml }` → `{ runId, requested }`, new or changed IDs. `?dryRun=true`, `?includeDrafts=true`                                                                                |
| `PUT /ingest/runs/{run}/posts/{ulid}` | ingest token                    | `{ markdown, images?: [{ path, content (base64) }] }`: parses, validates, stores referenced images in R2 under content-hash keys, rewrites their links, and stages one requested Post. 400 lists every error                                |
| `POST /ingest/runs/{run}/commit`      | ingest token                    | Validates the set (slugs unique per area), then in one transaction upserts staged Posts and deletes Posts missing from the manifest. Returns `{ dryRun, upserted, deleted }`; a dry run writes no Posts                                     |

The ingest token (`Ingest__Token`) and the admin token (`Admin__Token`) are separate bearer tokens, and neither is the Worker secret. `Access__SessionKey` signs Client sessions.

## Access Codes

```bash
SL_API_URL=… SL_ADMIN_TOKEN=… dotnet run --project api/src/Cli -- issue <client-ulid>   # prints the code once
SL_API_URL=… SL_ADMIN_TOKEN=… dotnet run --project api/src/Cli -- revoke <client-ulid>
```

Codes are per environment: each has its own D1, so a production code doesn't work on preview.

## Ingest a directory of Posts

```bash
SL_API_URL=http://localhost:5180 SL_INGEST_TOKEN=local-ingest-token \
  dotnet run --project api/src/Cli -- ingest e2e/fixtures/content [--dry-run] [--include-drafts]
```

The manifest is the whole content set: any stored Post missing from it is deleted. `_areas/{area}.md` files are sent as Content Area intros, and nothing under a `_` folder is read as a Post. `sl ingest` refuses an empty directory for that reason. Without `--include-drafts`, `status: draft` Posts are not stored, and a published Post turned Draft is removed.

On macOS the first run asks for Keychain access to the ASP.NET Core dev certificate. Choose "Always Allow", or the AppHost exits with `User canceled the operation`.

## Run the API alone

```bash
Worker__BaseUrl=http://127.0.0.1:8787 Worker__Secret=local-dev-secret Ingest__Token=local-ingest-token \
  dotnet run --project api/src/Api
```

## Tests

```bash
dotnet test api/tests/Domain.UnitTests       # pure domain logic
dotnet test api/tests/Api.FunctionalTests    # API in WebApplicationFactory against a real `wrangler dev`
```

The functional tests start their own `wrangler dev` on a free port, with migrations applied to a throwaway local D1. Run `npm install` at the root first so `wrangler` is available.

Vectorize and Workers AI have no local simulation. Most tests use the API's in-process vector fake; `VectorSearchTests` and `VectorIndexMaintenanceTests` bind them remotely to the `posts-v1-ci` index, in a namespace per run that's deleted by id afterwards. That needs a Cloudflare login (`wrangler login` locally, `CLOUDFLARE_API_TOKEN` in CI). Without one, set `SL_FAKE_VECTORS=1`: the Worker runs `--local` and those tests are skipped, as on fork PRs.

### Search eval

`SearchEvalTests` measures search quality: it ingests the frozen corpus in `tests/Api.FunctionalTests/SearchEval/corpus/` into the ci index, runs the graded queries in `queries.json` through `/search` in every mode, and gates on nDCG@5 against `scores.json` (ADR 0004). It is `[Explicit]`, so the ordinary run skips it. The Search Eval workflow runs it on PRs that touch the API or Worker, and nightly. It needs the real index, so it fails rather than skipping under `SL_FAKE_VECTORS=1`.

```bash
dotnet test tests/Api.FunctionalTests --filter TestCategory=SearchEval
SL_EVAL_WRITE_SCORES=1 dotnet test tests/Api.FunctionalTests --filter TestCategory=SearchEval  # accept a deliberate change as the new baseline
```

`SL_EVAL_REPORT_DIR` sets where the report, scores and status file land (default: the test run's work directory). The corpus is public: `CorpusTests` fails if a snapshot Post isn't public and published.
