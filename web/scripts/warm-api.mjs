// Wakes the API before `next build` pre-renders pages from it. The API scales to zero, so the first
// request can take a while; a build that can't reach it fails rather than ship empty pages.
import { pathToFileURL } from "node:url";

/**
 * @param {object} options
 * @param {string} options.url API base URL
 * @param {number} [options.attempts]
 * @param {number} [options.timeoutMs] per attempt
 * @param {number} [options.delayMs] between attempts
 * @param {typeof fetch} [options.fetch]
 * @param {(ms: number) => Promise<void>} [options.sleep]
 */
export async function warmApi({
  url,
  attempts = 6,
  timeoutMs = 30_000,
  delayMs = 5_000,
  fetch = globalThis.fetch,
  sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms)),
}) {
  const health = `${url.replace(/\/$/, "")}/health`;
  for (let attempt = 1; attempt <= attempts; attempt++) {
    try {
      const response = await fetch(health, { signal: AbortSignal.timeout(timeoutMs) });
      if (response.ok) {
        return;
      }
      console.warn(
        `warm-api: ${health} answered ${response.status} (attempt ${attempt}/${attempts})`
      );
    } catch (error) {
      console.warn(`warm-api: ${health} unreachable (attempt ${attempt}/${attempts}): ${error}`);
    }
    if (attempt < attempts) {
      await sleep(delayMs);
    }
  }
  throw new Error(`${health} did not become healthy after ${attempts} attempts`);
}

if (import.meta.url === pathToFileURL(process.argv[1]).href) {
  // On Vercel there's no local API to fall back to, so a missing API_URL is a broken deploy.
  if (process.env.VERCEL && !process.env.API_URL) {
    console.error("warm-api: API_URL is not set");
    process.exit(1);
  }
  const url = process.env.API_URL ?? "http://localhost:5180";
  warmApi({ url }).then(
    () => console.log(`warm-api: ${url} is healthy`),
    (error) => {
      console.error(`warm-api: ${error.message}`);
      process.exit(1);
    }
  );
}
