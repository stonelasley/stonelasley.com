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

| Route                                 | Auth         | Does                                                                     |
| ------------------------------------- | ------------ | ------------------------------------------------------------------------ |
| `GET /health`                         | none         | Healthy only when `SELECT 1` reaches D1 through the Worker               |
| `GET /areas/{area}/posts`             | none         | Public, published Posts in a Content Area, newest first                  |
| `GET /areas/{area}/posts/{slug}`      | none         | One public, published Post with its Markdown body                        |
| `POST /ingest/manifest`               | ingest token | `{ posts: { ulid: hash } }` → `{ runId, requested }`, new or changed IDs |
| `PUT /ingest/runs/{run}/posts/{ulid}` | ingest token | `{ markdown }`: parses, validates and stages one requested Post          |
| `POST /ingest/runs/{run}/commit`      | ingest token | Applies every staged Post in one statement                               |

The ingest token is a bearer token (`Ingest__Token`), separate from the Worker secret.

## Ingest a directory of Posts

```bash
SL_API_URL=http://localhost:5180 SL_INGEST_TOKEN=local-ingest-token \
  dotnet run --project api/src/Cli -- ingest e2e/fixtures/content
```

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
