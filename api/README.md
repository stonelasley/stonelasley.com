# api

The .NET API: the only reader and writer of Cloudflare data (ADR 0001). It reaches D1 through the Worker data proxy in `../worker`, authenticating with a bearer secret.

| Project              | Role                                                                                    |
| -------------------- | --------------------------------------------------------------------------------------- |
| `src/Domain`         | Domain model (empty until the first Post lands)                                         |
| `src/Application`    | Use cases and the ports they need, such as `ID1Database`                                |
| `src/Infrastructure` | Adapters for those ports, such as the Worker client                                     |
| `src/Api`            | ASP.NET Core host. `/health` is healthy only when a query reaches D1 through the Worker |
| `src/Cli`            | Admin CLI (`sl`). Calls the API, never the Worker                                       |
| `src/AppHost`        | Aspire: runs the Worker, the API and `next dev` together                                |

## Run everything locally

```bash
npm install                                  # from the repo root, once
dotnet run --project api/src/AppHost         # Worker on :8787, API on :5180, web on an Aspire-assigned port
```

The AppHost hands the same `worker-secret` parameter to the API and the Worker. It defaults to `local-dev-secret` in `appsettings.json`.

On macOS the first run asks for Keychain access to the ASP.NET Core dev certificate. Choose "Always Allow", or the AppHost exits with `User canceled the operation`.

## Run the API alone

```bash
Worker__BaseUrl=http://127.0.0.1:8787 Worker__Secret=local-dev-secret dotnet run --project api/src/Api
```

## Tests

```bash
dotnet test api/tests/Domain.UnitTests       # pure domain logic
dotnet test api/tests/Api.FunctionalTests    # API in WebApplicationFactory against a real `wrangler dev`
```

The functional tests start their own `wrangler dev` on a free port, with migrations applied to a throwaway local D1. Run `npm install` at the root first so `wrangler` is available.
