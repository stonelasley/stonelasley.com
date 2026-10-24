import { execFileSync } from "node:child_process";
import { test, expect, type Page } from "@playwright/test";
import { adminToken, apiUrl, clientAlex, repoRoot } from "../stack";

function issueCode(clientId: string): string {
  return execFileSync("dotnet", ["run", "--project", "api/src/Cli", "--", "issue", clientId], {
    cwd: repoRoot,
    env: { ...process.env, SL_API_URL: apiUrl, SL_ADMIN_TOKEN: adminToken },
    encoding: "utf8",
  }).trim();
}

async function unlock(page: Page, code: string) {
  await page.goto("/access");
  await page.getByLabel("Access Code").fill(code);
  await page.getByRole("button", { name: "Unlock" }).click();
}

test.describe("Access Codes", () => {
  test.describe.configure({ mode: "serial" });

  test("a Private Post is not found without a code", async ({ page }) => {
    const response = await page.goto("/training/fixture-alex-plan");

    expect(response?.status()).toBe(404);
    await page.goto("/training");
    await expect(page.getByRole("link", { name: "Alex's fixture plan" })).toHaveCount(0);
  });

  test("a wrong code fails clearly without saying why", async ({ page }) => {
    await unlock(page, "AAAA-AAAA-AAAA-AAAA-AAAA-AAAA");

    await expect(page.getByRole("main").getByRole("alert")).toHaveText("That code didn't work.");
  });

  test("a code unlocks the Client's Posts and the shared library, stays unlocked, and never another Client's", async ({
    page,
  }) => {
    test.setTimeout(90_000);
    await unlock(page, issueCode(clientAlex));
    await expect(page.getByText("Unlocked for Alex Example.")).toBeVisible();

    const cookie = (await page.context().cookies()).find((c) => c.name === "sl_access");
    expect(cookie).toMatchObject({ httpOnly: true, sameSite: "Lax" });

    await page.goto("/training/fixture-alex-plan");
    await expect(
      page.getByRole("heading", { level: 1, name: "Alex's fixture plan" })
    ).toBeVisible();
    await page.goto("/training/fixture-clients-library");
    await expect(
      page.getByRole("heading", { level: 1, name: "Fixture Clients library" })
    ).toBeVisible();

    expect((await page.goto("/training/fixture-sam-plan"))?.status()).toBe(404);

    // Public pages still work while unlocked.
    await page.goto("/ai-development/testing-at-the-seams");
    await expect(
      page.getByRole("heading", { level: 1, name: "Testing at the seams" })
    ).toBeVisible();
  });

  test("Private Posts are served uncached", async ({ page }) => {
    await unlock(page, issueCode(clientAlex));

    const response = await page.goto("/training/fixture-alex-plan");

    expect(response?.headers()["cache-control"]).toContain("no-store");
  });

  test("“Your content” lists the Client's Posts and the shared library, with no public filler", async ({
    page,
  }) => {
    test.setTimeout(90_000);
    await unlock(page, issueCode(clientAlex));
    await page.getByRole("link", { name: "See your content" }).click();

    await expect(page).toHaveURL(/\/your-content$/);
    await expect(page.getByRole("heading", { level: 1, name: "Your content" })).toBeVisible();
    const titles = page.getByRole("main").getByRole("article").getByRole("link");
    await expect(titles).toHaveText(["Fixture Clients library", "Alex's fixture plan"]);
  });

  test("“Your content” asks for a code without a valid cookie", async ({ browser }) => {
    const context = await browser.newContext();
    await context.addCookies([
      { name: "sl_access", value: "forged.value", url: "http://localhost:3000" },
    ]);
    const page = await context.newPage();

    await page.goto("/your-content");

    await expect(page.getByLabel("Access Code")).toBeVisible();
    await expect(page.getByRole("main").getByRole("article")).toHaveCount(0);
    await context.close();
  });
});
