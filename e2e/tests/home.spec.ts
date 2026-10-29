import { test, expect } from "@playwright/test";

test.describe("Home Page", () => {
  test("should load the home page", async ({ page }) => {
    await page.goto("/");

    await expect(page).toHaveTitle(/Stone|Home/);
    await expect(page.locator("h1")).toBeVisible();
  });

  test("should list the most recent Posts", async ({ page }) => {
    await page.goto("/");

    await expect(page.getByRole("link", { name: "Testing at the seams" })).toBeVisible();
  });

  test("shows each recent Post's hero above it", async ({ page }) => {
    await page.goto("/");

    const card = page.getByRole("article").filter({ hasText: "Testing at the seams" });
    await expect(card.getByRole("heading", { level: 3 })).toHaveText("Testing at the seams");
    const hero = card.getByTestId("post-thumbnail");
    await expect
      .poll(() => hero.evaluate((img: HTMLImageElement) => img.complete && img.naturalWidth > 0))
      .toBe(true);
  });

  for (const [name, path] of [
    ["Recipes", "/recipes"],
    ["Watches", "/watches"],
    ["AI & Development", "/ai-development"],
    ["Training", "/training"],
  ]) {
    test(`should navigate to ${name}`, async ({ page }) => {
      await page.goto("/");

      await page
        .getByRole("navigation", { name: "Main navigation" })
        .getByRole("link", { name })
        .click();

      await expect(page).toHaveURL(new RegExp(`${path}$`));
      await expect(page.getByRole("heading", { level: 1, name })).toBeVisible();
    });
  }
});
