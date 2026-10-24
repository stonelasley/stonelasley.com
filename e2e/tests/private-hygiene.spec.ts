import { execFileSync } from "node:child_process";
import { test, expect } from "@playwright/test";
import { adminToken, apiUrl, clientAlex, repoRoot } from "../stack";

const privatePosts = ["fixture-alex-plan", "fixture-clients-library", "fixture-sam-plan"];

test.describe("Private Post hygiene", () => {
  test("Private Posts never appear in the sitemap or either feed", async ({ request }) => {
    const sitemap = await (await request.get("/sitemap.xml")).text();
    const rss = await (await request.get("/feed.xml")).text();
    const json = await (await request.get("/feed.json")).text();

    for (const slug of privatePosts) {
      expect(sitemap).not.toContain(slug);
      expect(rss).not.toContain(slug);
      expect(json).not.toContain(slug);
    }
  });

  test("an unlocked Private Post is noindex and carries no description", async ({ page }) => {
    const code = execFileSync(
      "dotnet",
      ["run", "--project", "api/src/Cli", "--", "issue", clientAlex],
      {
        cwd: repoRoot,
        env: { ...process.env, SL_API_URL: apiUrl, SL_ADMIN_TOKEN: adminToken },
        encoding: "utf8",
      }
    ).trim();
    await page.goto("/access");
    await page.getByLabel("Access Code").fill(code);
    await page.getByRole("button", { name: "Unlock" }).click();

    const response = await page.goto("/training/fixture-alex-plan");

    await expect(
      page.getByRole("heading", { level: 1, name: "Alex's fixture plan" })
    ).toBeVisible();
    await expect(page.locator('meta[name="robots"]')).toHaveAttribute("content", /noindex/);
    expect(response?.headers()["x-robots-tag"]).toContain("noindex");
    await expect(page.locator('meta[name="description"]')).toHaveCount(0);
    await expect(page.locator('meta[property="og:description"]')).toHaveCount(0);
    await expect(page.locator('meta[name="twitter:description"]')).toHaveCount(0);
    await expect(page.locator('script[type="application/ld+json"]')).toHaveCount(0);
  });
});
