# Deploy

`.github/workflows/deploy.yml` ships every layer in order, and each stage runs only if the one before it succeeded. Every same-repo PR deploys to **preview**; fork PRs get no secrets and skip. Production (#55) reuses the same stages.

| #   | Stage                                                                            | Preview                                                                               |
| --- | -------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| 1   | D1 migrations (`wrangler d1 migrations apply --remote`)                          | `stonelasley-preview`                                                                 |
| 2   | Worker secret + `wrangler deploy`                                                | `stonelasley-data-proxy-preview` on `data-preview.stonelasley.com`, `workers.dev` off |
| 3   | API image to GHCR                                                                | `ghcr.io/stonelasley/stonelasley-api:<sha>` (public, inherited from the repo)         |
| 4   | Container Apps revision                                                          | `ca-stonelasley-api-preview` in `cae-stonelasley-preview`, scale 0 to 1               |
| 5   | Wait on `/health`                                                                | `web/scripts/warm-api.mjs`                                                            |
| 6   | `vercel build` + `vercel deploy --prebuilt`                                      | built against the preview API; Deployment Protection on                               |
| 7   | Point the API's revalidate webhook at that deployment, then check it answers 200 | through Vercel's automation bypass secret                                             |

All Vercel previews share one preview API, so runs queue (`concurrency: deploy-preview`) and the last push wins.

## Secrets

GitHub Actions secrets are the only source. The workflow pushes them into Container Apps secrets and `wrangler secret`; there's no Key Vault.

| Secret                                                        | Used by                                                                                                                                                                                             |
| ------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `CLOUDFLARE_API_TOKEN` (+ variable `CLOUDFLARE_ACCOUNT_ID`)   | D1 migrations, Worker deploy                                                                                                                                                                        |
| `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` | OIDC login as `gh-stonelasley-com` (Contributor on `rg-stonelasley`). Federated subjects: `pull_request` and `ref:refs/heads/master`. A job `environment:` would change the subject and break login |
| `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID`          | `vercel pull/build/deploy`, reading the automation bypass secret                                                                                                                                    |
| `PREVIEW_WORKER_SECRET`                                       | API → Worker bearer secret                                                                                                                                                                          |
| `PREVIEW_INGEST_TOKEN`                                        | content CI → API ingest                                                                                                                                                                             |
| `PREVIEW_REVALIDATE_SECRET`                                   | API → site `/api/revalidate`                                                                                                                                                                        |

Rotate a secret by setting a new value (`gh secret set NAME`) and re-running the deploy.

## Indexing

Everything preview serves sends `X-Robots-Tag: noindex`: the API through `Site__NoIndex=true`, and the site through `VERCEL_ENV=preview`.

## Vercel Git integration

Git auto-deploy is off (`web/vercel.json`). Only this workflow deploys, after the API it builds against is healthy.

## Cold start

Measured 2026-10-24 on preview (0.25 vCPU / 0.5 GiB, West US 3). The app had scaled to zero, with no replicas running. Cold `GET /health` took 26.5 s, which includes the API's `SELECT 1` through the Worker to D1. Warm requests that followed took 0.28 to 0.31 s. The build's warm-up allows 6 attempts of up to 30 s each, so a cold API doesn't fail a deploy.
