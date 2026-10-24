import path from "node:path";

// The full stack Playwright starts (see playwright.config.ts). Tests that ingest import these.
export const workerPort = 8788;
export const apiPort = 5181;
export const webPort = 3000;
export const workerSecret = "e2e-worker-secret";
export const ingestToken = "e2e-ingest-token";
export const revalidateSecret = "e2e-revalidate-secret";
export const apiUrl = `http://127.0.0.1:${apiPort}`;
export const repoRoot = path.join(__dirname, "..");
export const fixtures = path.join(__dirname, "fixtures", "content");
