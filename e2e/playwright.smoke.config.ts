import { defineConfig, devices } from "@playwright/test";

/**
 * Smoke tests against a deployed site (SMOKE_URL), run after every deploy. No local stack: these read whatever
 * content that environment serves, so they check shape, not fixture text.
 */
const baseURL = process.env.SMOKE_URL;
if (!baseURL) throw new Error("Set SMOKE_URL to the deployed site to smoke-test");

// Previews sit behind Vercel Deployment Protection.
const bypass = process.env.VERCEL_BYPASS;

export default defineConfig({
  testDir: "./smoke",
  forbidOnly: !!process.env.CI,
  retries: 1,
  reporter: process.env.CI ? "github" : "list",
  use: {
    baseURL,
    extraHTTPHeaders: bypass ? { "x-vercel-protection-bypass": bypass } : undefined,
    trace: "on-first-retry",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
