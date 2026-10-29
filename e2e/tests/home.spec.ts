import { test, expect } from "@playwright/test";

test.describe("Home Page", () => {
  test("should load the home page", async ({ page }) => {
    await page.goto("/");

    await expect(page).toHaveTitle(/Stone|Home/);
    await expect(page.locator("h1")).toBeVisible();
  });

  test("should list the most recent Posts", async ({ page }) => {
    await page.goto("/");

    await expect(
      page.getByRole("heading", { level: 3 }).getByRole("link", { name: "Testing at the seams" })
    ).toBeVisible();
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

  test("opens on a recent Post's photograph, with the name and one action", async ({ page }) => {
    await page.goto("/");

    const hero = page.getByTestId("home-hero");
    await expect
      .poll(() => hero.evaluate((img: HTMLImageElement) => img.complete && img.naturalWidth > 0))
      .toBe(true);
    await expect(page.getByRole("heading", { level: 1 })).toHaveText("Stone C. Lasley");
    await expect(page.getByRole("link", { name: "Read the watch notes" })).toHaveAttribute(
      "href",
      "/watches"
    );
  });

  test("swapping in the visit's photograph doesn't shift the layout", async ({ page }) => {
    await page.goto("/");
    await page.waitForLoadState("networkidle");

    const shift = await page.evaluate(
      () =>
        new Promise<number>((resolve) => {
          let total = 0;
          new PerformanceObserver((list) => {
            for (const entry of list.getEntries() as (PerformanceEntry & { value: number })[]) {
              total += entry.value;
            }
          }).observe({ type: "layout-shift", buffered: true });
          setTimeout(() => resolve(total), 500);
        })
    );
    expect(shift).toBeLessThan(0.05);
  });

  test("every image on the page loads", async ({ page }) => {
    await page.goto("/");

    for (const image of await page.locator("main img").all()) {
      await image.scrollIntoViewIfNeeded();
      await expect
        .poll(() => image.evaluate((img: HTMLImageElement) => img.complete && img.naturalWidth > 0))
        .toBe(true);
    }
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
