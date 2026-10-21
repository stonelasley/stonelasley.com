# Per-client Access Codes: the simplest sound pattern

Issue: #14 (child of map #10). Date: 2026-10-20.
Context: coaching clients unlock private, health-adjacent Training content with a code the site owner issues. No full accounts. Next.js on Vercel, .NET API, Cloudflare available.

Legend: **[V]** verified against the cited primary source. **[U]** unverified, my inference or design judgment.

## Answer

Treat each code as a high-entropy bearer secret, store only a hash, and trade it once for a short-lived signed session cookie. Revocation lives in one database row per code.

1. **Format:** 24 characters of Crockford base32 from a CSPRNG (120 bits), shown in groups of four (`ABCD-EFGH-...`). Strip hyphens and case before hashing.
2. **At rest:** store `SHA-256(code)` only, plus `grant_id`, `label` (a non-identifying alias), `expires_at`, `revoked_at`, `created_at`, `last_used_at`. Lookup is by hash. The plaintext is shown once at issue time.
3. **Redeem:** a Next.js Server Action posts the code and a Turnstile token to the .NET API. The API verifies Turnstile server-side, rate-limits, looks up the hash, and returns `grant_id` on success.
4. **Session:** Next.js sets an `HttpOnly; Secure; SameSite=Lax` cookie holding a `jose` HS256 JWT `{grantId, exp}`. Every protected render re-checks the grant row (not revoked, not expired) through a data access layer, not just the cookie signature.
5. **Revoke:** set `revoked_at`. Takes effect on the next check (cache for at most about 60 s if you cache).
6. **Brute force:** at 120 bits, online guessing is not feasible. The limiter and Turnstile exist to stop noise and cost abuse, not to protect the keyspace.

## 1. Code format and entropy

- NIST SP 800-63B-4 look-up secrets: must come from an approved random bit generator, minimum six decimal digits, single-use, stored hashed. **[V]** https://pages.nist.gov/800-63-4/sp800-63b.html (sec. 3.1.2)
- The same section says secrets under 112 bits must be salted and hashed with a password hashing scheme (salt at least 32 bits) and have verifier rate limiting. **[V]** same URL (sec. 3.1.2.2). I chose 120 bits so the code clears that 112-bit line, which is what lets a plain SHA-256 be defensible instead of Argon2id. That inference is **[U]**: the NIST text I read states the under-112 requirement, and I did not find wording that explicitly blesses a fast hash at or above it.
- OWASP says session identifiers need at least 64 bits of entropy from a CSPRNG. **[V]** https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html. That applies to the session secret. The code is a longer-lived credential, hence the higher target.
- Note: NIST look-up secrets are single-use. A reusable access code is deliberately not that. It behaves like a long-lived API key, so treat it as one (see section 6).
- Typing 24 characters is tedious. Mitigation: issue it as a link (`/unlock#code=...`, fragment not query so it is not sent to servers or logged) and offer paste. **[U]**

## 2. Hashing at rest

- OWASP Password Storage: fast hashes like SHA-256 are unsuitable for passwords; use Argon2id (min 19 MiB, t=2, p=1), scrypt, bcrypt (legacy), or PBKDF2-HMAC-SHA256 (600,000 iterations, FIPS). **[V]** https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html
- The page is silent on high-entropy random secrets. Why a fast hash is acceptable here: slow hashes defend against guessing low-entropy human choices; 120 random bits cannot be guessed offline. **[U]** (standard reasoning, not stated by the cheat sheet).
- Optional: key the hash (HMAC-SHA256 with a pepper held in Key Vault or an env secret) so a database leak alone yields nothing. OWASP describes peppers as defense in depth stored outside the database. **[V]** same Password Storage URL. Whether to bother at 120 bits: **[U]** optional.
- Lookup by hash needs no per-row salt. Use constant-time comparison if you ever compare in app code. **[U]**
- If you prefer shorter codes (for example 80 bits): switch to Argon2id with a short public `grant_id` prefix (`ID.SECRET`) so you look up the row by ID and verify the secret with the slow hash. **[U]** design.

## 3. Exchanging a code for a session

Next.js documents two session types: stateless (data or token in the cookie, simpler, "can be less secure if not implemented correctly") and database sessions (only an encrypted ID in the cookie, more secure, more complex). It recommends `jose` or `iron-session`. **[V]** https://nextjs.org/docs/app/guides/authentication

- Cookie options Next.js recommends: `httpOnly`, `secure`, `sameSite: 'lax'`, `expires`, `path`; set cookies on the server. **[V]** same URL. OWASP also requires `SameSite=Strict` or `Lax`, `Secure`, `HttpOnly`, and server-side enforced idle and absolute timeouts. **[V]** OWASP Session Management URL above.
- Recommended hybrid: stateless JWT in the cookie for the cheap "optimistic" check, plus a grant-row lookup for the authoritative check. Next.js describes this split: optimistic checks read the cookie, secure checks hit the database. **[V]** same Next.js URL.
- Next.js warns that Proxy (middleware) checks should not be the only defense; do the real check close to the data (a data access layer), and check in Server Actions and Route Handlers too. **[V]** same URL.
- Keep the JWT payload minimal: `grantId`, `exp`. No client name, no health data. **[V]** Next.js says keep the payload to minimal data and no PII.
- Lifetime: the Next.js example uses 7 days. For health-adjacent content I would use an absolute 7 days with a sliding refresh and a hard cap at the code's own `expires_at`. **[U]** NIST 800-63B-4 sets AAL1 reauthentication at SHOULD no more than 30 days. **[V]** NIST URL above.
- Who sets the cookie: Next.js (same origin as the site), after a server-to-server call to the .NET API authenticated with a shared API key. This avoids cross-site cookie and CORS problems between the Vercel domain and the API domain. **[U]**
- Where the content lives matters. If Training content is static or pre-rendered, a data access layer will not protect it; Next.js says use Proxy for static routes shared between users. **[V]** same URL. Simplest: render protected pages dynamically, or serve the content from the API behind the grant check.

## 4. Revocation and expiry

- OWASP: invalidation must happen server-side; deleting the cookie does not invalidate a stolen copy. **[V]** OWASP Session Management URL.
- So a pure stateless JWT cannot be revoked until it expires. That is why the grant-row check exists. Revoke = set `revoked_at`; expire = `expires_at`. Both checked on every protected request. **[U]** design, following the OWASP point.
- Per-client codes (one grant each) are what make revocation precise. Issuing a shared code would force rotating everyone.
- Rotation: issue a new grant, set the old `revoked_at`. Log `last_used_at` and a coarse use count so you can spot a leaked code (many IPs on one grant). **[U]**

## 5. Brute-force protection

- NIST: verifiers must limit consecutive failed attempts on an authenticator (cap of 100) and rate limiting is required for secrets under 64 bits in some authenticator types. **[V]** NIST URL (sec. 3.2.2, 3.1.3.3). These rules assume you know which account is being attacked. With anonymous code entry you do not, so a per-account counter does not apply. Use per-IP plus a global failure budget instead. **[U]**
- ASP.NET Core ships rate-limiting middleware (`AddRateLimiter`) with fixed window, sliding window, token bucket, and concurrency limiters, partitionable by IP, user or key, rejecting with 429. **[V]** https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0
- The same page warns that partitioning by client IP is exposed to source-address spoofing DoS, and that rate limiting is not a DDoS defense. **[V]** same URL.
- Behind a proxy, `RemoteIpAddress` is the proxy unless forwarded headers are configured; trust only known proxies (`KnownProxies` / `KnownNetworks`). **[V]** https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0. Because Next.js calls the API server-to-server from Vercel, the API will see Vercel's address, not the visitor's. Forward the visitor IP explicitly from the Server Action and trust it only on the authenticated server-to-server channel. **[U]**
- Limiter state in `AddRateLimiter` is in-process. On multiple API instances each keeps its own counters. **[U]** (I did not find this stated on the page I read; it follows from the in-memory design).
- Suggested limits: 5 redeem attempts per IP per minute, a global budget of failed attempts per hour that alerts you, and a uniform response and timing for unknown vs revoked vs expired codes. **[U]**
- Cloudflare Turnstile: widget token is verified by POSTing `secret` and `response` (optional `remoteip`, `idempotency_key`) to `https://challenges.cloudflare.com/turnstile/v0/siteverify`. Tokens are valid 300 seconds and single-use (replay returns `timeout-or-duplicate`). The widget alone protects nothing; you must call siteverify from your backend. **[V]** https://developers.cloudflare.com/turnstile/get-started/server-side-validation/. Also check the returned `hostname` matches your site. **[U]** (field is documented, the check is my recommendation).
- Turnstile is cheap insurance on the redeem form (stops scripted spam and cost abuse). It does not add security at 120 bits.

## 6. Where codes are weaker than accounts

- **Bearer secret:** whoever holds the code has access. It can be forwarded, screenshotted, posted, or sit in a chat history or browser history. Accounts at least bind to an email and a password the user chose. **[U]**
- **No identity binding or second factor:** no MFA, no device binding, no "is this the same person" signal. NIST ranks look-up secrets as a single-factor "something you have" authenticator. **[U]** (framing); look-up secret definition **[V]** NIST URL.
- **Delivery channel is the trust root:** if the channel you send the code through is compromised, so is the access. **[U]**
- **No self-service recovery:** lost code means you reissue by hand. That is also a feature: no password reset to attack. **[U]**
- **Link delivery leaks:** query strings land in server logs, history and referrers. Use a URL fragment and exchange immediately. **[U]**
- **Granularity:** one code per client means revocation is clean, but sharing within a household is undetectable beyond usage anomalies. **[U]**
- **Compliance:** this note makes no HIPAA or privacy-law claim. If content is regulated health data, get that checked separately. **[U]** (out of scope).

## 7. Is a magic-link upgrade cheap later?

Yes, mostly. **[U]** (design judgment, no primary source says so).

- Add an optional `email` to the grant. A magic link is the same primitive: a random single-use token, hashed at rest, redeemed once into the same session cookie. The grant table, the session cookie, and the data access layer do not change.
- NIST out-of-band guidance: secrets under 64 bits need rate limiting, and generating a new secret must not reset the failure count. **[V]** NIST URL (sec. 3.1.3.3). Use 120-bit tokens and this stops mattering.
- What the upgrade actually costs: an email provider, deliverability work, and a "send me a link" endpoint that must not reveal whether an email exists. Those are the real cost, not the session plumbing.
- Benefit: lost codes self-heal, and you can bind sessions to a verified email, closing the biggest gap in section 6.

## Open items for the implementation ticket

- Choose 120-bit codes vs shorter-plus-Argon2id (decision depends on how clients receive and type them).
- Decide whether the protected content is dynamic pages or API-served (affects whether Proxy is needed).
- Decide session lifetime (7 days sliding is my default).
- Decide on a pepper.

## Sources

- NIST SP 800-63B-4: https://pages.nist.gov/800-63-4/sp800-63b.html (I read the first ~100k of ~289k characters; the session-secret section (sec. 5) was not read, so the 64-bit session secret claim relies on OWASP.)
- OWASP Session Management: https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html
- OWASP Password Storage: https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html
- Cloudflare Turnstile server-side validation: https://developers.cloudflare.com/turnstile/get-started/server-side-validation/
- ASP.NET Core rate limiting: https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0
- ASP.NET Core proxy/forwarded headers: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0
- Next.js authentication guide: https://nextjs.org/docs/app/guides/authentication
