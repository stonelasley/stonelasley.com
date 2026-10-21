# Can Vercel run the .NET API, or does it go to Azure?

Ticket: stonelasley/stonelasley.com#11 (child of map #10). Researched 2026-10-20 from primary sources only. Each claim is tagged **[verified]** (with the page it came from) or **[unverified]**.

## Answer

Yes, Vercel can now run an ASP.NET Core API, but only as a **Container Runtime (Beta)** deployment, and .NET is not an official Vercel runtime. The recollection that container support is new is right, and it is available on the Hobby plan. It is Beta, so I would not make it the home for an API that has to stay up.

- **Vercel route:** works on Hobby and costs $0 inside Hobby allotments, but it is Beta, Hobby is non-commercial only, and no .NET-specific docs or cold-start numbers exist. [verified for support and limits, unverified for .NET behaviour]
- **Azure route:** the best free fit for a low-traffic API is **Azure Container Apps (Consumption)**. It scales to zero with no charge and has a monthly free grant. App Service F1 cannot take a custom domain. Functions Flex Consumption is also viable if the API is written as Functions.

## 1. Vercel

### 1.1 Is .NET officially supported? No.
- Official runtimes are Node.js, Bun, Python, Rust, Go, Ruby, Wasm, Edge, and Container. .NET is not listed. [verified, docs page last_updated 2026-08-12: https://vercel.com/docs/functions/runtimes]
- Community runtimes Vercel recommends are Bash, Deno, and PHP only. No .NET community runtime is recommended. [verified, same page]

### 1.2 Container Runtime is the route
- "Container Runtime is available in Beta on all plans." Functions run any code from an OCI image built from a `Dockerfile.vercel` (or `Containerfile.vercel`) at the project root. [verified, docs last_updated 2026-07-07: https://vercel.com/docs/functions/runtimes/container]
- The server must listen on `$PORT` (default 80). Containers are stateless. [verified: container docs, and https://vercel.com/blog/dockerfile-on-vercel dated 2026-06-30]
- The announcement post names ASP.NET among frameworks that "all deploy the same way" via a Dockerfile. This is the only .NET mention found from Vercel, and it is a list in a blog post, not a tested guide. [verified: https://vercel.com/blog/dockerfile-on-vercel]
- Local testing via `vercel dev` needs Docker installed. [verified: container docs]
- Not supported with containers yet: Secure Compute and Static IPs. [verified: container docs]
- No Vercel .NET guide surfaced in the pages read. [verified that none surfaced; absence is not proof none exists]

### 1.3 Limits that apply on Hobby
Container Runtime uses "the same limits and Active CPU pricing model of Vercel Functions". [verified: container docs]
- Max duration: 300 s (default and max) on Hobby. [verified, last_updated 2026-08-24: https://vercel.com/docs/functions/limitations]
- Memory: 2 GB / 1 vCPU on Hobby. [verified: same page]
- Request and response body: 4.5 MB max. [verified: same page]
- Scale down: no traffic for 5 minutes in production (30 s in preview). SIGTERM with a 30 s grace period. [verified: container docs]
- Default region `iad1`; multi-region is Pro and up. [verified: https://vercel.com/docs/functions/runtimes]
- The 250 MB bundle limit and the 5 GB large-functions beta are described for Node, Bun, and Python. Whether either applies to container images is not stated. [unverified]

### 1.4 Cost on Hobby
- Hobby monthly included usage guideline: 1,000,000 function invocations, 4 hours Active CPU, 360 GB-hrs provisioned memory, 100 GB Fast Data Transfer. [verified, last_updated 2026-09-14: https://vercel.com/docs/limits/fair-use-guidelines]
- Billing is Active CPU (CPU time while code runs, not I/O waits) plus provisioned memory. [verified: container docs and limitations page]
- Whether overage on Hobby is blocked or paused (rather than billed) is not stated on the pages read. [unverified]
- **Hobby is "restricted to non-commercial personal use only."** Donations are not commercial. A personal site with no payments, ads, or paid work is fine. A paid employee or consultant writing the code counts as commercial. [verified: fair-use page above]

### 1.5 Cold start
- Vercel says images are stored as a compressed "optimized boot image" streamed on demand, and Fluid compute keeps instances warm and reuses them. No numbers are given. [verified: dockerfile-on-vercel post]
- No .NET cold-start figure was published in anything read. [unverified]

### 1.6 Reaching Cloudflare services
- Container functions are ordinary outbound HTTPS clients, so calling Cloudflare's public HTTPS APIs (R2 S3 API, D1/KV REST API) from Vercel should work. No Vercel or Cloudflare doc was read on this. [unverified]
- Static egress IPs are not available for containers, so IP allowlisting on the Cloudflare side is not possible. [verified: container docs, Static IPs unsupported]

## 2. Azure free options for a low-traffic .NET API

### 2.1 Container Apps, Consumption plan (best fit)
- Free each calendar month, per subscription: first 180,000 vCPU-seconds, 360,000 GiB-seconds, 2 million HTTP requests. Free usage does not appear on the bill. [verified, page updated 2026-03-25: https://learn.microsoft.com/en-us/azure/container-apps/billing]
- Scaled to zero replicas: "no resource consumption charges are incurred." [verified: same page]
- Idle charges apply only if min replicas is above zero. [verified: same page]
- Runs container images, so the same Dockerfile as Vercel works. .NET 10 specifics were not read. [unverified]
- Cold start from zero, and custom domain with free managed certificate: not read in a primary source. [unverified]
- Cost at ~zero traffic: $0 inside the grant with scale-to-zero. Container registry cost is not covered. [unverified]
- Paid rates could not be read; the pricing page rendered "$-" placeholders. [unverified: https://azure.microsoft.com/en-us/pricing/details/container-apps/]

### 2.2 App Service Free (F1)
- F1 = 60 CPU minutes/day, 1 GB RAM, 1 GB storage; no SLA, not for production. [verified: https://azure.microsoft.com/en-us/pricing/details/app-service/windows/]
- Custom domain tutorial prerequisite: plan "must be a paid tier, not the Free (F1) tier." So F1 cannot serve your own domain. [verified, ms.date 2026-04-07: https://learn.microsoft.com/en-us/azure/app-service/app-service-web-tutorial-custom-domain]
- Free tier apps get CPU minute quotas and cannot scale out. [verified, ms.date 2026-08-24: https://learn.microsoft.com/en-us/azure/app-service/overview-hosting-plans]
- Verdict: ruled out by the custom domain requirement. Basic and up are paid; prices were not read. [unverified]

### 2.3 Azure Functions
- Flex Consumption (the recommended serverless plan) supports C# isolated on .NET 8, 9, and 10. Scales to zero. On-demand free grant: 250,000 executions and 100,000 GB-s per month per subscription. Linux only. Cold start is reduced only with always-ready instances, which have no free grant. [verified: https://learn.microsoft.com/en-us/azure/azure-functions/flex-consumption-plan (ms.date 2026-09-15) and https://azure.microsoft.com/en-us/pricing/details/functions/]
- Flex Consumption supports site-scoped certificates for custom domains. [verified: flex-consumption-plan page]
- Classic Consumption: free grant 1 million executions and 400,000 GB-s. It is labeled legacy; Linux Consumption retires 2028-09-30; Windows Consumption is not currently affected. [verified: https://learn.microsoft.com/en-us/azure/azure-functions/consumption-plan (ms.date 2026-04-09)]
- Code must run on the Functions isolated worker host. Whether a plain ASP.NET Core API can be hosted there with minimal change was not checked. [unverified]
- A storage account is created with each Functions app and is billed outside the free grant. [verified: functions pricing page]

### 2.4 Reaching Cloudflare services from Azure
- Same as Vercel: outbound public HTTPS from the app to Cloudflare APIs. Not checked against Cloudflare docs. [unverified]
- If Cloudflare proxies the custom domain in front of the API, that is DNS and proxy config on the Cloudflare side; not researched. [unverified]

## 3. Side by side

| | Vercel Container (Hobby) | Azure Container Apps (Consumption) | Azure Functions Flex | App Service F1 |
|---|---|---|---|---|
| .NET support | Any OCI image; ASP.NET named in blog only | Any OCI image | .NET 8/9/10 isolated | Yes, but see domain |
| Status | Beta | No beta label on billing page | GA (rolling updates in preview) | GA, no SLA |
| Free at ~zero traffic | Yes, within Hobby allotment | Yes, within monthly grant | Yes, within grant | Yes |
| Scale to zero | Yes (5 min idle) | Yes | Yes | n/a, CPU quota |
| Custom domain | Not researched | Not researched | Yes (certs doc) | **No** |
| Commercial use | **Not allowed on Hobby** | Allowed | Allowed | Not for production |
| Static egress IP | No | Not researched | Not researched | Not researched |

## 4. Recommendation

For a personal, non-commercial site where you want everything in one dashboard: Vercel Container Runtime works today and costs nothing, with Beta risk and no published .NET guidance. Expect to measure the cold start yourself.

For anything that must run reliably, or could ever touch money: **Azure Container Apps Consumption**. It runs the same Dockerfile, scales to zero, and the free grant should cover a low-traffic API. Azure is already daily work, so the operating overhead is low.

Open items worth a 30-minute spike before deciding: (1) measure .NET 10 cold start on both, (2) confirm Container Apps custom domain plus Cloudflare proxy, (3) check what happens on Vercel Hobby when allotments run out.

## Sources
- https://vercel.com/docs/functions/runtimes (2026-08-12)
- https://vercel.com/docs/functions/runtimes/container (2026-07-07)
- https://vercel.com/blog/dockerfile-on-vercel (2026-06-30)
- https://vercel.com/docs/functions/limitations (2026-08-24)
- https://vercel.com/docs/limits/fair-use-guidelines (2026-09-14)
- https://learn.microsoft.com/en-us/azure/container-apps/billing
- https://learn.microsoft.com/en-us/azure/app-service/overview-hosting-plans
- https://learn.microsoft.com/en-us/azure/app-service/app-service-web-tutorial-custom-domain
- https://azure.microsoft.com/en-us/pricing/details/app-service/windows/
- https://learn.microsoft.com/en-us/azure/azure-functions/flex-consumption-plan
- https://learn.microsoft.com/en-us/azure/azure-functions/consumption-plan
- https://azure.microsoft.com/en-us/pricing/details/functions/
- https://azure.microsoft.com/en-us/pricing/details/container-apps/
