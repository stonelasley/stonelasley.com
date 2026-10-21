# Cloudflare free tier: content storage and search

Ticket: stonelasley/stonelasley.com#12 (map #10). Researched 2026-10-20 from Cloudflare developers docs. Every claim is marked **[V]** (verified on the cited page this session) or **[U]** (unverified).

## Answer

Cloudflare's free tier fits this site with large headroom. Recommended shape for the learning goal:

- **D1** holds Post fields and the Markdown body, with an **FTS5** index for the keyword leg.
- **R2** holds images.
- **Vectorize** (1,536 dims max) plus **Workers AI** `bge-*` embeddings give the vector leg.
- Fusion (e.g. reciprocal rank fusion) and optional reranking with `bge-reranker-base` live in our own .NET code.
- Every service above has a documented REST endpoint, so a .NET API can call them without a Worker. D1 REST is described as primarily control-plane, and REST rate limits and latency are undocumented on the pages read, so spike before locking topology.
- **AI Search** is the managed shortcut (hybrid on by default). It hides chunking, embedding and ranking, so it teaches less. Free allowance is 1,000 semantic and 1,000 full-text queries per month.

KV is not needed for this design (could cache fused results, optional).

## Free-tier limits

| Service | Free limit | Source |
|---|---|---|
| D1 | 5M rows read/day, 100K rows written/day, 5 GB total storage, 500 MB per DB, 10 DBs, 100 KB SQL statement, 2 MB max row, 100 bound params. Daily limits reset 00:00 UTC; hitting them returns errors. | [V] https://developers.cloudflare.com/d1/platform/pricing/ , https://developers.cloudflare.com/d1/platform/limits/ |
| R2 | 10 GB-month storage, 1M Class A ops/month, 10M Class B ops/month, egress free. Standard storage only. | [V] https://developers.cloudflare.com/r2/pricing/ |
| KV | 100K reads/day, 1,000 writes/day (different keys), 1 GB storage, 25 MiB value, 512 B key | [V] https://developers.cloudflare.com/kv/platform/limits/ |
| Vectorize | 30M queried vector dimensions/month, 5M stored dimensions; 100 indexes, max 1,536 dims, topK 50 with values/metadata (100 without), 10 KiB metadata/vector | [V] https://developers.cloudflare.com/vectorize/platform/pricing/ , https://developers.cloudflare.com/vectorize/platform/limits/ |
| Workers AI | 10,000 Neurons/day free. bge-small 1,841 / bge-base 6,058 / bge-large 18,582 / bge-m3 1,075 neurons per M input tokens; bge-reranker-base 283 neurons per M tokens | [V] https://developers.cloudflare.com/workers-ai/platform/pricing/ |
| AI Search | Free-plan limits: 100 instances, 100K files/instance, 500 pages crawled/day, 10 MiB text files. Included monthly: 5M ingestion tokens, 10 GB-month storage, 1,000 semantic + 1,000 full-text queries. Billing begins 2026-11-01; overage $0.75/1K semantic, $0.10/1K full-text. | [V] https://developers.cloudflare.com/ai-search/platform/limits-pricing/ |

Sizing check (our own arithmetic, not a Cloudflare claim): 500 posts x ~8 chunks x 768 dims = ~3.1M stored dimensions, under the 5M cap; 1,536-dim models would exceed it at that chunk count. bge-base is 768 dims with a 512-token input limit per text [V] https://developers.cloudflare.com/workers-ai/models/bge-base-en-v1.5/ . Embedding hundreds of posts costs a few thousand neurons, under the daily 10K (arithmetic from the neuron table above).

## Per-service answers

### D1 (structured fields, Markdown body, keyword search)
- Full-text search: the SQL statements page states D1 supports a subset of SQLite extensions including the **FTS5 module** (and fts5vocab). [V] https://developers.cloudflare.com/d1/sql-api/sql-statements/ . Whether `bm25()` ranking works on D1 was not checked [U]; confirm with a spike.
- REST: `POST /accounts/{account_id}/d1/database/{database_id}/query` with `sql`, `params` or `batch`; needs an API token with D1 Read/Write. [V] https://developers.cloudflare.com/api/resources/d1/subresources/database/methods/query/ . The best-practices page says the REST API "primarily interacts with the control plane" [V] https://developers.cloudflare.com/d1/best-practices/query-d1/ . REST rate limits and latency are not documented on the pages read [U]. Likely fine for low volume; verify under load.

### R2 (images)
- S3-compatible API at `https://<ACCOUNT_ID>.r2.cloudflarestorage.com`, usable by any S3 client such as the AWS SDK for .NET. Some operations are not implemented; check the ones used. [V] https://developers.cloudflare.com/r2/api/s3/api/

### KV
- REST: GET/PUT/DELETE `/accounts/{account_id}/storage/kv/namespaces/{namespace_id}/values/{key_name}`. [V] https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/subresources/values/ . Free writes (1,000/day) are the tight limit. Not a good primary store here.

### Vectorize
- REST v2: `POST .../vectorize/v2/indexes/{index_name}/{query|insert|upsert|get_by_ids}`. [V] https://developers.cloudflare.com/api/resources/vectorize/ . Docs examples use Worker bindings [V] https://developers.cloudflare.com/vectorize/best-practices/query-vectors/ ; REST is documented only in the API reference.

### Workers AI (embeddings and reranker)
- REST: `POST https://api.cloudflare.com/client/v4/accounts/{ACCOUNT_ID}/ai/run/{model}` with a bearer token. [V] https://developers.cloudflare.com/workers-ai/get-started/rest-api/
- `@cf/baai/bge-reranker-base` takes `query`, `contexts[].text`, optional `top_k`; returns relevance scores in [0,1]. Context window and max contexts are not stated. [V] https://developers.cloudflare.com/workers-ai/models/bge-reranker-base/

### AI Search (managed; "formerly AutoRAG")
- Managed search service; automated indexing; hybrid search on by default; metadata filtering; queryable via Workers binding, REST API or MCP. [V] https://developers.cloudflare.com/ai-search/ . The "formerly AutoRAG" rename is not stated on that page [U].
- REST: `.../ai-search/namespaces/{namespace}/instances/{id}/search` (retrieval only) and `.../chat/completions` (retrieve + generate), bearer token with AI Search Edit/Run. [V] https://developers.cloudflare.com/ai-search/usage/rest-api/ . Hybrid, reranking and chunking parameters are not on that page [U].
- Chunking: only chunk size and overlap (0 to 30%) are configurable; recursive splitting. [V] https://developers.cloudflare.com/ai-search/configuration/chunking/

## Who owns which pipeline stage

| Stage | DIY (D1 + Vectorize + Workers AI) | AI Search |
|---|---|---|
| Chunking | Our code (full control) | Managed; size and overlap knobs only |
| Embedding | We call Workers AI via REST | Managed |
| Keyword index | FTS5 in D1 (we write SQL) | Managed full-text |
| Vector index | We upsert/query Vectorize | Managed |
| Fusion (RRF etc.) | Our code | Managed (hybrid default) |
| Reranking | Optional: we call bge-reranker-base | Not verified [U] |
| RAG generation | We call a Workers AI LLM | `chat/completions` |

## Caveats and open items
- D1/Vectorize REST latency and rate limits unverified; spike before locking the API-to-Cloudflare topology.
- FTS5 usage and `bm25()` support to be confirmed against a real D1 database.
- Free-tier numbers change; re-check before build start.
- Nothing here requires a Worker, but a thin Worker in front of D1/Vectorize remains an option if REST proves slow.
