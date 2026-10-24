# Deploy

`.github/workflows/deploy.yml` ships every layer in order, and each stage runs only if the one before it succeeded. The stages live once, in `deploy-environment.yml`, which both environments call. A same-repo PR that targets `master` deploys to **preview**. A PR stacked on another branch skips, so preview shows the next PR to merge, and fork PRs get no secrets and skip too. A merge to `master` deploys to **production** once the `PRODUCTION_ENABLED` variable is `true` (see [Production cutover](#production-cutover)). Run the workflow by hand (workflow_dispatch) to preview any branch, or to redeploy production from `master`.

| #   | Stage                                                                            | Preview                                                                               | Production                                                              |
| --- | -------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| 1   | D1 migrations (`wrangler d1 migrations apply --remote`)                          | `stonelasley-preview`                                                                 | `stonelasley-prod`                                                      |
| 2   | Worker secret + `wrangler deploy`                                                | `stonelasley-data-proxy-preview` on `data-preview.stonelasley.com`, `workers.dev` off | `stonelasley-data-proxy` on `data.stonelasley.com`                      |
| 3   | API image to GHCR                                                                | `ghcr.io/stonelasley/stonelasley-api:<sha>` (public, inherited from the repo)         | same                                                                    |
| 4   | Container Apps revision                                                          | `ca-stonelasley-api-preview` in `cae-stonelasley-preview`, scale 0 to 1               | `ca-stonelasley-api` in `cae-stonelasley`, on `PRODUCTION_API_URL`      |
| 5   | Wait on `/health`                                                                | `web/scripts/warm-api.mjs`                                                            | same; the build fails if the API never answers                          |
| 6   | `vercel build` + `vercel deploy --prebuilt`                                      | built against the preview API; Deployment Protection on                               | `--prod`, built against the production API                              |
| 7   | Point the API's revalidate webhook at that deployment, then check it answers 200 | through Vercel's automation bypass secret                                             | at `PRODUCTION_SITE_URL`                                                |
| 8   | Smoke tests (`npm run test:smoke -w e2e`)                                        | against the deployment URL                                                            | against `PRODUCTION_SITE_URL`, and also checks the site isn't `noindex` |

All Vercel previews share one preview API, so runs queue (`concurrency: deploy-preview`) and the last push wins. Production runs queue the same way (`deploy-production`).

The smoke suite (`e2e/smoke/`) reads whatever content the environment serves, so it checks shape rather than fixture text: the home page and every Content Area render, the v1 Recipe URLs resolve, the v1 redirects answer 301, search answers, and the feeds, sitemap and `robots.txt` are served. Run it by hand with `SMOKE_URL=<site> npm run test:smoke -w e2e`.

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
| `PREVIEW_ADMIN_TOKEN`, `PREVIEW_ACCESS_SESSION_KEY`           | admin endpoints; signing Client sessions                                                                                                                                                            |
| `PRODUCTION_*`                                                | the same six for production                                                                                                                                                                         |

Rotate a secret by setting a new value (`gh secret set NAME`) and re-running the deploy.

## Indexing

Everything preview serves sends `X-Robots-Tag: noindex`: the API through `Site__NoIndex=true`, and the site through `VERCEL_ENV=preview`.

## Vercel Git integration

Git auto-deploy is off (`web/vercel.json`). Only this workflow deploys, after the API it builds against is healthy.

## Images

Post images live in R2 under `images/{sha256}{ext}`. Readers load them straight from the bucket's custom domain, not through the API or the Worker (ADR 0001's one exception).

| Environment | Bucket                       | Custom domain                                     |
| ----------- | ---------------------------- | ------------------------------------------------- |
| preview     | `stonelasley-images-preview` | `img-preview.stonelasley.com`                     |
| production  | `stonelasley-images-prod`    | `img.stonelasley.com` (connected at cutover, #55) |

The spec flagged one assumption as unverified: that R2 custom domains need the zone on Cloudflare (#23). It's now **verified**. On 2026-10-24 `wrangler r2 bucket domain add stonelasley-images-preview --domain img-preview.stonelasley.com --zone-id <stonelasley.com>` connected the domain (ownership active, SSL pending). An object put with `wrangler r2 object put --remote` was then served at `https://img-preview.stonelasley.com/checks/custom-domain.txt` with `200`, `content-type: text/plain`, `server: cloudflare`, about 30 s after SSL finished provisioning. Before that it answered `403` with Cloudflare error 1014. The check object was deleted afterwards.

Images in Private Posts go to the same public bucket. Their keys are content hashes, so the URLs can't be guessed, but nothing checks access on them. This is an accepted risk: a Private Post's text is protected, but an image link someone shares will still load.

Locally, and in E2E, the Worker serves `/public/images/*` when `SERVE_IMAGES=true`, standing in for the custom domain.

## Vector search

Each environment's Worker binds its own Vectorize index (`posts-v1-preview`, `posts-v1-prod`; `posts-v1-ci` for local dev and tests) and Workers AI (`@cf/baai/bge-base-en-v1.5`, 768 dimensions). Every index already has a metadata index on `area`. Vector ids are `{postUlid}:{chunkIndex}`, and D1 keeps each Post's chunk count (`posts.vector_chunks`) so updates and deletes remove old vectors by id. Vectorize writes took 90 to 110 s to become queryable when measured on 2026-10-24.

## Instagram

The API keeps the Creator account's 60-day long-lived token and a media cache in D1's `meta` table. `.github/workflows/instagram-refresh.yml` runs daily and calls `POST /admin/instagram/refresh` (admin token). That renews the token, re-fetches the media and revalidates the site. Seed the first token once per environment:

```bash
curl -X PUT "$API/admin/instagram/token" -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" -d '{"token":"<long-lived token>"}'
```

Until a token is seeded, the home page shows no feed, and the cron reports 409.

## Re-indexing

The index version (embedding model + chunker, currently `v1`) names each Vectorize index: `posts-{version}-{env}`. To change the model or the chunker without taking search down:

1. Run **Re-index** (`reindex.yml`) with the new version, e.g. `v2`. It creates `posts-v2-preview` with its `area` metadata index and deploys the Worker with the build index bound alongside the active one. It then re-embeds every public Post into it (`POST /admin/vectors/rebuild?version=v2`) and waits until Vectorize holds every vector. Search keeps reading `v1` throughout, and a failure changes nothing live.
2. Open a PR that sets `ACTIVE_INDEX_VERSION` and the `VECTORS` `index_name` to `v2` in `worker/wrangler.jsonc`. Its deploy rebinds the Worker and runs the API on `v2`, then calls `POST /admin/vectors/promote?version=v2`. Promotion adopts the build's chunk counts. A Post that changed during the build gets re-indexed by the next ingest.
3. Delete the old index: `npx wrangler vectorize delete posts-v1-preview`.

## Cold start

Measured 2026-10-24 on preview (0.25 vCPU / 0.5 GiB, West US 3). The app had scaled to zero, with no replicas running. Cold `GET /health` took 26.5 s, which includes the API's `SELECT 1` through the Worker to D1. Warm requests that followed took 0.28 to 0.31 s. The build's warm-up allows 6 attempts of up to 30 s each, so a cold API doesn't fail a deploy.

## Production cutover

Production stays off until each of these is done, in order. Until then, merges to `master` deploy nothing, and the v1 site keeps serving `stonelasley.com`.

1. **Cloudflare.** Check D1 `stonelasley-prod` and Vectorize `posts-v1-prod` exist (both are named in `worker/wrangler.jsonc`). Create the R2 bucket `stonelasley-images-prod` and connect `img.stonelasley.com` to it.
2. **Azure.** Create the Container Apps environment `cae-stonelasley` in `rg-stonelasley`. The first production deploy creates `ca-stonelasley-api` in it.
3. **Secrets.** Set the six `PRODUCTION_*` secrets with `gh secret set`.
4. **API and Worker.** Set the variable `PRODUCTION_ENABLED=true`, then run the workflow by hand for `production` on `master`. Without `PRODUCTION_SITE_ENABLED` it stops after the API is healthy, so `stonelasley.com` doesn't change.
5. **API domain.** Bind `api.stonelasley.com` to `ca-stonelasley-api` (`az containerapp hostname add` and `bind`), then set `PRODUCTION_API_URL=https://api.stonelasley.com`.
6. **Content.** Set `PRODUCTION_API_URL` and the production ingest token in the content repo, and merge to its `main` to ingest production. Check `$PRODUCTION_API_URL/areas/recipes/posts` lists the Recipes.
7. **Site.** This is the cutover. Set `PRODUCTION_SITE_URL` to the canonical origin and `PRODUCTION_SITE_ENABLED=true`, then run the workflow again. Its Vercel `--prod` deploy takes over `stonelasley.com`, and the smoke tests check it. To roll back, promote the previous production deployment in Vercel.
