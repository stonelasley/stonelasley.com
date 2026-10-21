---
status: accepted
---

# The .NET API is the single gateway to Cloudflare

Posts live on Cloudflare (D1, Vectorize, R2, Workers AI). Next.js could read D1 directly, which would be less to build. We chose to make the .NET API the only reader and writer of Cloudflare data instead. The site is meant to show off development ability, and the interesting parts of search (ingest, chunking, embedding, RRF fusion) belong in a real domain layer, not split across a frontend and a sidecar. Keeping Access Code checks and Cloudflare credentials in one service also means secrets and auth logic live in one place.

## Considered Options

- **Next.js reads D1 directly for public Posts, with .NET handling only search and Access Codes.** Faster to build, but it splits the domain model across two codebases and turns the API into a sidecar.
- **Cloudflare Workers own the data and search logic, with .NET on the side.** Rejected because it moves the learning and the portfolio work out of .NET. If D1's REST API proves too slow, a thin Worker may stand in for data access only, holding no ranking or domain logic.

## Consequences

- Next.js holds no Cloudflare credentials and calls only the API. The one exception is public images, which are served directly from an R2 custom domain.
- The API must be reachable while Vercel builds, because public Posts are pre-rendered from API data. Ingest calls a Vercel revalidate webhook to refresh changed pages.
- The API is hosted to scale to zero, so it runs no background jobs. Scheduled work, such as the Instagram token refresh, is triggered from outside by a GitHub Actions cron.
- Every query pays for a .NET → Cloudflare REST round trip. Whether that's acceptable is checked by "Is D1's REST API fast enough for per-request queries from .NET?" (#21).
