import path from "node:path";
import { defineConfig, devices } from "@playwright/test";

/**
 * Runs against the full stack: the Worker under `wrangler dev` on a throwaway local D1, the .NET API,
 * and `next start` built from API data after the fixture Posts are ingested.
 * See https://playwright.dev/docs/test-configuration.
 */
const workerPort = 8788;
const apiPort = 5181;
const webPort = 3000;
const workerSecret = "e2e-worker-secret";
const ingestToken = "e2e-ingest-token";
const apiUrl = `http://127.0.0.1:${apiPort}`;
const d1State = path.join(__dirname, ".wrangler-state");

export default defineConfig({
  testDir: "./tests",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: "html",
  use: {
    baseURL: `http://localhost:${webPort}`,
    trace: "on-first-retry",
  },

  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],

  // Started in order; each waits for the one before it to answer.
  webServer: [
    {
      name: "worker",
      command: [
        `rm -rf "${d1State}"`,
        `npx wrangler d1 migrations apply DB --local --persist-to "${d1State}"`,
        `npx wrangler dev --ip 127.0.0.1 --port ${workerPort} --persist-to "${d1State}" --var WORKER_SECRET:${workerSecret} --show-interactive-dev-session=false`,
      ].join(" && "),
      cwd: "../worker",
      // The Worker answers 401 without the secret, which Playwright counts as up.
      url: `http://127.0.0.1:${workerPort}`,
      reuseExistingServer: false,
      timeout: 120 * 1000,
    },
    {
      name: "api",
      command: "dotnet run --project api/src/Api --no-launch-profile",
      cwd: "..",
      env: {
        ASPNETCORE_URLS: apiUrl,
        Worker__BaseUrl: `http://127.0.0.1:${workerPort}`,
        Worker__Secret: workerSecret,
        Ingest__Token: ingestToken,
      },
      url: `${apiUrl}/health`,
      reuseExistingServer: false,
      timeout: 180 * 1000,
    },
    {
      name: "web",
      command: [
        "dotnet run --project api/src/Cli -- ingest e2e/fixtures/content",
        "npm run build",
        "npm run start",
      ].join(" && "),
      cwd: "..",
      env: {
        SL_API_URL: apiUrl,
        SL_INGEST_TOKEN: ingestToken,
        API_URL: apiUrl,
        PORT: String(webPort),
      },
      url: `http://localhost:${webPort}`,
      reuseExistingServer: !process.env.CI,
      timeout: 300 * 1000,
    },
  ],
});
