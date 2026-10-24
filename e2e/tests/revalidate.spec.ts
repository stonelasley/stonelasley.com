import { execFileSync } from "node:child_process";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { test, expect } from "@playwright/test";
import { apiUrl, fixtures, ingestToken, repoRoot, revalidateSecret } from "../stack";

// Ingest through the real CLI, the way the content repo's CI will.
function ingest(directory: string) {
  execFileSync("dotnet", ["run", "--project", "api/src/Cli", "--", "ingest", directory], {
    cwd: repoRoot,
    env: { ...process.env, SL_API_URL: apiUrl, SL_INGEST_TOKEN: ingestToken },
    stdio: "pipe",
  });
}

const newPost = `---
id: 01JBX5Q7Z9K3M8N2P4R6T0V1W9
title: Freshly ingested
slug: freshly-ingested
area: training
date: 2026-10-05
audience: public
status: published
---

Published after the site was built.
`;

test.describe("Revalidation", () => {
  test("the webhook rejects a missing or wrong secret", async ({ request }) => {
    expect((await request.post("/api/revalidate")).status()).toBe(401);
    expect(
      (
        await request.post("/api/revalidate", { headers: { authorization: "Bearer wrong" } })
      ).status()
    ).toBe(401);
    expect(
      (
        await request.post("/api/revalidate", {
          headers: { authorization: `Bearer ${revalidateSecret}` },
        })
      ).status()
    ).toBe(200);
  });

  test.describe.configure({ mode: "serial" });

  test("an ingest after the build updates the site", async ({ page }) => {
    test.setTimeout(120_000);
    const content = fs.mkdtempSync(path.join(os.tmpdir(), "content-"));
    fs.cpSync(fixtures, content, { recursive: true });
    fs.mkdirSync(path.join(content, "training"), { recursive: true });
    fs.writeFileSync(path.join(content, "training", "freshly-ingested.md"), newPost);

    try {
      ingest(content);

      // The API revalidates; the next request may still be served stale while the page regenerates.
      await expect(async () => {
        await page.goto("/training");
        await expect(page.getByRole("link", { name: "Freshly ingested" })).toBeVisible({
          timeout: 1000,
        });
      }).toPass({ timeout: 30_000 });
    } finally {
      ingest(fixtures);
      fs.rmSync(content, { recursive: true, force: true });
    }

    await expect(async () => {
      await page.goto("/training");
      await expect(page.getByRole("link", { name: "Freshly ingested" })).toHaveCount(0, {
        timeout: 1000,
      });
    }).toPass({ timeout: 30_000 });
  });
});

test("preview pages are served with noindex", async ({ request }) => {
  const response = await request.get("/");

  expect(response.headers()["x-robots-tag"]).toBe("noindex");
});
