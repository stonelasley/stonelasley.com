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

test.describe("Header", () => {
  test("holds the wordmark, the Content Areas and search, and nothing else", async ({ page }) => {
    await page.goto("/");
    const header = page.getByRole("banner");

    const names = await header.getByRole("link").allInnerTexts();
    expect(names.map((n) => n.trim()).filter(Boolean)).toEqual([
      "Stone C. Lasley",
      "Recipes",
      "Watches",
      "AI & Development",
      "Training",
      "⌘K",
    ]);
    await expect(header.getByRole("radiogroup", { name: "Theme" })).toHaveCount(0);
  });

  test("on a phone, every Content Area is reachable from the header", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto("/");

    const nav = page.getByRole("navigation", { name: "Main navigation" });
    for (const name of ["Recipes", "Watches", "AI & Development", "Training"]) {
      await expect(nav.getByRole("link", { name })).toBeVisible();
    }
  });

  test("the footer keeps the social links and the theme choice", async ({ page }) => {
    await page.goto("/");
    const footer = page.getByRole("contentinfo");

    await expect(footer.getByRole("radiogroup", { name: "Theme" })).toBeVisible();
    await expect(footer.getByRole("link", { name: /GitHub/i })).toBeVisible();
  });
});
