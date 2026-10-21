---
status: accepted
---

# The API runs on Azure Container Apps, with DNS on Cloudflare

The frontend stays on Vercel and the data stays on Cloudflare, but the .NET API runs on Azure Container Apps Consumption. Vercel can only run .NET through its Beta Container Runtime, which has no official .NET support, and its Hobby plan is non-commercial. Container Apps is GA, scales to zero for free, and is a platform Stone already runs. The API reaches Cloudflare through a Worker data proxy (see ADR 0001), and the domain's DNS moves to Cloudflare so that R2 can serve images from a custom domain.

## Considered Options

- **Everything on Vercel, with the API in a Beta container.** One vendor fewer, but it's Beta, there's no .NET guidance and no cold-start data, and Hobby forbids commercial use.
- **Azure App Service F1 or Functions Flex.** F1 can't take a custom domain, and Functions would reshape the API around triggers.
- **Keep DNS at Namecheap and serve images from `r2.dev` or through the API.** `r2.dev` is rate-limited and not meant for production, and proxying images contradicts serving them straight from R2.

## Consequences

- Three providers: Vercel (frontend), Azure (API), Cloudflare (DNS, data, Worker proxy). The domain stays registered at Namecheap. Vercel's records stay DNS-only.
- Two environments, production and preview, each with its own Container Apps app, Worker, D1 database, Vectorize index and R2 bucket. Vercel preview deploys share the preview API. Local dev runs the API against the preview Worker, because Vectorize has no local emulator.
- The Worker trusts the API through a single bearer secret. `workers.dev` is turned off and the Worker is served from a custom route.
- Vercel builds call the API (cold start included) after a `/health` warm-up. A production build fails rather than ship empty pages.
- GitHub Actions secrets are the single source, pushed into Container Apps and `wrangler secret`. There's no Key Vault.
- Staying on Vercel Hobby assumes the site is personal. Selling coaching through the site would force Vercel Pro or a move to Cloudflare.
