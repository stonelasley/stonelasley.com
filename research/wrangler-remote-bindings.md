# Wrangler remote bindings for Vectorize and Workers AI in CI

Date: 2026-10-20. Ticket: stonelasley/stonelasley.com#27.
Sources are Cloudflare first-party docs (developers.cloudflare.com). Items not stated in a source are marked **inferred** or **unverified**. The docs pages were read through a summarizing fetch tool, so "verbatim" quotes below are as relayed by that tool; re-check anything load-bearing against the page.

## Answer

**Yes.** `wrangler dev` can bind Vectorize and Workers AI remotely while D1 and R2 stay local. Vectorize and Workers AI have no local simulation, so remote is the only way they work in dev. D1 and R2 support both modes. Cloudflare's docs say: "your Worker still executes locally, only the underlying resources your bindings connect to change."

**Partly** for namespace isolation. Namespaces give an isolated slice for queries and writes, but there is **no documented delete-by-namespace**. Cleanup means tracking vector IDs and calling `deleteByIds`. A query `namespace` option is shown in the CLI docs but not on the binding client-api page (see below).

**The fallback (a fake behind the Vectorize port) is not required** for functional tests. It is still reasonable to keep it for speed and cost; see Recommendation.

## Remote bindings

- Remote bindings are GA, announced 2025-09-16, "without using an experimental flag" in Wrangler, the Vite plugin and the Vitest plugin. The changelog post names **Wrangler v4.37.0** as the version to try it with. [changelog](https://developers.cloudflare.com/changelog/post/2025-09-16-remote-bindings-ga/)
- Enable per binding with `remote: true` in the binding definition. [development-testing](https://developers.cloudflare.com/workers/development-testing/)
- Support table (Local simulation / Remote binding connection): Vectorize no/yes, Workers AI no/yes, D1 yes/yes, R2 yes/yes. [bindings-per-env](https://developers.cloudflare.com/workers/development-testing/bindings-per-env/)
- Workers AI: setting `remote` to `false` is an error. Vectorize: omitting `remote` triggers a warning. [development-testing](https://developers.cloudflare.com/workers/development-testing/)
- Not supported as remote (error): Durable Objects, Workflows, vars, secrets, static assets, version metadata, Analytics Engine, Hyperdrive, rate limiting.
- `wrangler dev --local` temporarily turns off all remote bindings (from a search-result summary of the docs; **unverified** on the page itself).
- Bindings are not inherited across environments; each environment must redefine them (same search summary; **unverified** on page).
- The configuration page lists the Vectorize binding with only `binding` and `index_name`, and Workers AI with only `binding`; the `remote` field was not seen there (the page was truncated, last ~4.6k chars unread). Treat `remote: true` on those two as documented on the development-testing page, and **verify with a real `wrangler dev` run**.
- The Workers AI config page warns usage "will incur usage charges even in local development." Remote operations also add network latency, and writes "will affect your actual data."
- Minimum Wrangler version is stated only in the GA changelog (4.37.0). Any later behavior fixes are **unverified**; pin a recent 4.x.

## Namespaces and isolation

- Namespace is an optional partition key set on insert/upsert: "you can optionally provide a `namespace: string` value when performing an insert or upsert operation." A vector belongs to at most one namespace. [insert-vectors](https://developers.cloudflare.com/vectorize/best-practices/insert-vectors/), [metadata-filtering](https://developers.cloudflare.com/vectorize/reference/metadata-filtering/)
- "Vectorize supports namespace filtering by default" (no metadata index needed). "A namespace filter is applied before metadata filter(s)." [metadata-filtering](https://developers.cloudflare.com/vectorize/reference/metadata-filtering/)
- The CLI `wrangler vectorize query` has a namespace option ("Filter the query results based on this namespace"). [wrangler-commands](https://developers.cloudflare.com/vectorize/reference/wrangler-commands/)
- The binding client-api page did not list a `namespace` query option (only `topK`, `returnValues`, `returnMetadata`; `filter` is on the metadata-filtering page). The Workers binding likely accepts `namespace` in query options (**inferred**, **unverified**). Confirm against the `VectorizeQueryOptions` type in `@cloudflare/workers-types` before relying on it. [client-api](https://developers.cloudflare.com/vectorize/reference/client-api/)
- **Cleanup:** binding methods are `insert`, `upsert`, `query`, `queryById`, `getByIds`, `deleteByIds`, `describe`. CLI has `delete-vectors --ids` and `list-vectors` (no namespace option, 1-1000 per page). The REST API documents one delete endpoint, by IDs, requiring the `Vectorize Write` permission. **No delete-by-namespace or delete-by-filter is documented.** So the test harness must record the IDs it upserts and delete them in teardown. Use deterministic ID prefixes per run, e.g. `ci-<runid>-...` (**inferred** design). [client-api](https://developers.cloudflare.com/vectorize/reference/client-api/), [REST delete_by_ids](https://developers.cloudflare.com/api/resources/vectorize/subresources/indexes/methods/delete_by_ids/)
- Whether an emptied namespace still counts toward the namespace cap is **unverified**.

## Limits

| Item | Value | Source |
|---|---|---|
| Namespaces per index | 50,000 (Paid), 1,000 (Free) on the limits page. The insert-vectors page says "up to 1,000 per index". **Docs conflict**; assume 1,000 to be safe | [limits](https://developers.cloudflare.com/vectorize/platform/limits/), [insert-vectors](https://developers.cloudflare.com/vectorize/best-practices/insert-vectors/) |
| Namespace name length | 64 bytes | limits |
| Vectors per index | 20,000,000 | limits |
| Upsert batch | 1,000 via Workers, 5,000 via HTTP API; insert page: 200,000 total vectors or 1,000 updates per batch, whichever first | limits, insert-vectors |
| topK | 100 without values/metadata; 50 with `returnValues` or metadata `all` | limits, client-api |
| Vector ID length | 64 bytes | limits |
| Metadata per vector | 10 KiB; 10 metadata indexes per index | limits |
| Dimensions | up to 1,536 (float32) | limits |
| Query rate limit | none listed on the limits page | limits |
| Workers AI free allowance | 10,000 neurons/day | [pricing](https://developers.cloudflare.com/workers-ai/platform/pricing/) |
| Workers AI paid | $0.011 per 1,000 neurons | pricing |
| bge-small-en-v1.5 | 1,841 neurons per M input tokens | pricing |
| bge-base-en-v1.5 | 6,058 neurons per M input tokens | pricing |
| bge-large-en-v1.5 | 18,582 neurons per M input tokens | pricing |

Worked cost (**inferred** arithmetic): a 200-token passage on bge-base is about 1.2 neurons, so the 10,000/day free tier covers roughly 8,000 such embeddings a day. CI embedding cost is negligible; the free tier is per account per day and shared with any other Workers AI use. Embedding dimensionality per model is **unverified** here (check it fits the index).

## Consistency after upsert

- Writes are asynchronous: "an asynchronous job needs to read the current index files from R2" before changes are visible. Vectorize "writes changes immediately to a write ahead log for durability." A large batch is "visible in queries within minutes." [insert-vectors](https://developers.cloudflare.com/vectorize/best-practices/insert-vectors/)
- No fixed delay is documented, and the query-vectors page says nothing about delay. Insert, upsert and delete "return a mutation identifier"; the field name is not given on the page (field name and any way to await it are **unverified**).
- Practical consequence (**inferred**): tests must poll (query until the expected ID appears, with a timeout) rather than assume read-your-writes. Delete visibility is similarly delayed.

## GitHub Actions gotchas

- Auth: `CLOUDFLARE_API_TOKEN` is the documented env var for CI/CD; `CLOUDFLARE_ACCOUNT_ID` is the account variable. [system-environment-variables](https://developers.cloudflare.com/workers/wrangler/system-environment-variables/)
- Token scopes: only `Vectorize Write` is stated verbatim (REST delete). Scopes needed for the remote-binding proxy session, Workers AI, and Vectorize read are **not documented in what I read**. Likely a Workers Scripts edit scope plus Workers AI and Vectorize permissions (**inferred**); find the minimum by trial and use a dedicated, least-privilege token.
- Cost and data: remote bindings hit the real index. Use a dedicated test index (not prod), unique per-run namespace and ID prefix, and teardown that runs even on failure (`if: always()`).
- Fork PRs do not receive repo secrets in GitHub Actions (general GitHub behavior, not from Cloudflare docs). Since the repo is public, remote-binding tests need to be skipped or gated on fork PRs.
- Cloudflare Access: if a target Worker is behind Access, CI needs `CLOUDFLARE_ACCESS_CLIENT_ID` and `CLOUDFLARE_ACCESS_CLIENT_SECRET` or Wrangler errors. Probably not applicable to Vectorize/AI bindings directly (**inferred**).
- Miniflare is "not a hardened sandbox"; do not run untrusted code with live credentials in the job.
- Concurrency: parallel CI runs share one index; namespace plus ID prefix isolates data but they share quotas and the async-write delay.

## Recommendation (inferred)

1. Functional tests that need real search semantics: run `wrangler dev` with `remote: true` on the Vectorize and AI bindings, D1/R2 left local, per-run namespace, ID-tracked teardown, polling for visibility. Gate to non-fork runs with secrets.
2. Keep a fake behind the Vectorize port for the fast default suite and fork PRs; use the real index only for a thin integration/E2E layer. This also removes flakiness from the async-write window.

## Unverified / open

- `remote: true` accepted in the Vectorize/AI config schema on the wrangler config page (truncated read); verify by running.
- `namespace` option on the binding `query()`; mutation id field and awaiting it.
- Namespace cap conflict (1,000 vs 50,000 on Paid) and whether emptied namespaces count.
- Exact token scopes; behavior of `--local` and per-environment inheritance (from search summary only).
- No workers-sdk release notes were read; version guidance rests on the GA changelog alone.
