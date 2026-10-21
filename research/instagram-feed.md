# Embedding a personal Instagram feed after Basic Display

Issue: #13. Researched 2026-10-20 from Meta developer docs. Every claim is tagged **Verified** (with the URL it was read at) or **Unverified**.

## Answer

Basic Display is gone and has no replacement for personal accounts. Switch the Instagram account to Creator or Business (free, no Facebook Page needed), use the **Instagram API with Instagram Login** in a Standard Access app that you own, and fetch posts server-side at build time with a long-lived token that you refresh before the 60 days run out. Do not use oEmbed for a grid feed. Do not pay for a third-party widget unless you refuse to touch tokens.

## Facts

### Basic Display retirement
- Retired 2024-12-04. **Verified.** https://developers.facebook.com/blog/post/2024/09/04/update-on-instagram-basic-display-api/
- Meta says there will be "no longer be a set of Instagram APIs for consumer developer apps"; replacements target business and creator accounts. The page does not explicitly say personal accounts are unsupported, but nothing in the replacement docs allows them. **Verified (quote); inference on personal accounts.** same URL

### Option A: Instagram API with Instagram Login (recommended)
- Host `graph.instagram.com`; for professional accounts without a linked Facebook Page. **Verified.** https://developers.facebook.com/docs/instagram-platform/overview
- Account type: professional (Business or Creator). `account_type` returns `Business` or `Media_Creator`. **Verified.** https://developers.facebook.com/docs/instagram-platform/instagram-api-with-instagram-login/get-started
- App type must be a Business-type Meta app. **Verified.** same URL
- Access levels: Standard Access is default and "suits development, testing, and apps used only by people with roles on the app". Advanced Access (App Review plus Business Verification) is required for accounts "you don't own or manage". **Verified.** https://developers.facebook.com/docs/instagram-platform/overview
- Inference: showing your own account's posts needs only Standard Access, so no App Review. The fetched pages do not state this about your own account in so many words, and they say nothing on app roles/testers. **Unverified (inference).**
- Tokens: the App Dashboard "Generate token" button gives a long-lived token valid 60 days. Business Login flow tokens are short-lived (1 hour). **Verified.** get-started URL above
- Exchange short for long-lived: `GET graph.instagram.com/access_token?grant_type=ig_exchange_token`, server-side only. **Verified.** https://developers.facebook.com/docs/instagram-platform/instagram-api-with-instagram-login/business-login
- Refresh: `GET graph.instagram.com/refresh_access_token?grant_type=ig_refresh_token`; token must be at least 24 hours old, unexpired, and have `instagram_business_basic`. Refresh gives another 60 days. An unrefreshed token expires and cannot be refreshed. **Verified.** business-login URL above
- Rate limit: Business Use Case limiting; non-messaging calls capped at `4800 * Number of Impressions` per 24h per app/user pair. A static site fetching once per build is far below any limit. **Verified.** overview URL
- Cost: no fee stated in any fetched page. **Unverified.**
- Which fields and permission scope `/me/media` needs (e.g. `media_url`, `thumbnail_url`, `permalink`): the scope name is confirmed only as a refresh precondition. The media endpoint reference was not fetched successfully. **Unverified.**
- Whether `media_url` CDN links expire: not covered in fetched pages. **Unverified.** The build-time design below sidesteps it by downloading images.

### Option B: Instagram API with Facebook Login
- Host `graph.facebook.com`; account must be professional and linked to a Facebook Page; user needs Page task rights. Hashtag search, product tagging etc. only here. **Verified.** overview URL
- Same access-level and rate-limit model as above. **Verified.** overview URL
- Token lifetime for this path (Page tokens): not fetched. **Unverified.**
- Verdict: strictly more setup than A for a solo owner; only worth it if you already run a Facebook Page and want features exclusive to this path.

### Option C: Official oEmbed
- `GET /instagram_oembed?url=<post URL>` on `graph.facebook.com`; returns embed `html`. Supports image/carousel/video/Reels posts and profiles; private, inactive, age-restricted accounts and Stories are not supported. 1,000 requests per hour. **Verified.** https://developers.facebook.com/docs/instagram-platform/oembed
- Token requirement and App Review ("Meta oEmbed Read" feature): the page does not say. **Unverified.** (From memory it needs an app token and review; do not rely on this.)
- Thumbnails/media JSON: sample response shows only version, provider, type, width, html. **Verified.**
- It embeds one URL at a time; it does not list "my recent posts". You would still need to know post URLs, so it is not a feed. It works for public posts, so a personal account would work. **Verified (public-only); inference on personal.**
- The free "Embed" option on instagram.com post menus yields blockquote + `embed.js` HTML with no API or token. The Help Center article could not be fetched. **Unverified.** Manual per post, third-party script, no automation.

### Option D: Third-party widgets (Elfsight, Behold, Juicer, etc.)
- Not researched against first-party sources. They sit on top of option A or B and need the same professional account. Pricing and ToS **Unverified**. They add a third-party script and a monthly fee for something you can build in about 50 lines.

### ToS on caching media
- Meta Platform Terms contain no fixed retention period and do not explicitly address caching. Platform Data (including derived copies) must be deleted "as soon as reasonably possible" when no longer needed for a legitimate business purpose, when a user requests deletion, or when Meta requests it. **Verified.** https://developers.facebook.com/terms/dfc_platform_terms/
- Section 3.a.viii defers to the Developer Docs for permitted processing; those were not found. **Unverified** whether Developer Docs add caching rules.
- Practical reading: you are the account owner and sole user, so deletion requests are yours. Build-time download of your own images is low risk, but keep the images removable (rebuild without them) and do not republish others' content.

## Recommendation for a solo developer

1. Convert the Instagram account to Creator (or Business). Free; no Facebook Page required for Option A. Personal accounts cannot use the API.
2. Create a Business-type Meta app, add the Instagram API with Instagram Login product, add your own account, generate a long-lived token in App Dashboard. Stay on Standard Access; skip App Review.
3. In a prebuild script like the existing Notion fetch, call `/me/media` once per build, download the images into `public/images/`, write JSON validated by Zod, and refresh the token in the same run. Static output keeps zero runtime API calls, matching this site's architecture.
4. Risk: the token dies if nothing refreshes it within 60 days. Build/deploy at least monthly (a scheduled CI run is enough) and fail loudly on refresh error.
5. Before building, prove it with one curl against `/me/media` to resolve the Unverified items (fields, media URL expiry, no review needed).

Account type needed: **Instagram Creator or Business** (professional). No Facebook Page for Option A.
