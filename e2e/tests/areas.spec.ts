import { test, expect } from "@playwright/test";

// Seeded from fixtures/content through the API's ingest protocol (see playwright.config.ts).

test.describe("Content Areas", () => {
  test("an area index lists its public Posts and opens one", async ({ page }) => {
    await page.goto("/ai-development");

    await expect(page.getByRole("heading", { level: 1, name: "AI & Development" })).toBeVisible();

    await page.getByRole("link", { name: "Testing at the seams" }).click();

    await expect(page).toHaveURL(/\/ai-development\/testing-at-the-seams$/);
    await expect(
      page.getByRole("heading", { level: 1, name: "Testing at the seams" })
    ).toBeVisible();
    await expect(page.getByRole("heading", { level: 2, name: "Why seams" })).toBeVisible();
    await expect(
      page.getByText("Tests that drive the public interface survive refactors.")
    ).toBeVisible();
  });

  test("an area index shows each Post's hero above it, loaded", async ({ page }) => {
    await page.goto("/ai-development");

    const card = page.getByRole("article").filter({ hasText: "Testing at the seams" });
    const hero = card.getByTestId("post-thumbnail");
    await expect(hero).toHaveAttribute("alt", "");
    await expect(hero).toHaveAttribute("src", /\/public\/images\/[0-9a-f]{64}\.png$/);
    await expect
      .poll(() => hero.evaluate((img: HTMLImageElement) => img.complete && img.naturalWidth > 0))
      .toBe(true);
    // One link per card, so the whole card opens the Post.
    await expect(card.getByRole("link")).toHaveCount(1);
  });

  test("a Post without a hero gets a stand-in: a Watch's Reference, or the Content Area", async ({
    page,
  }) => {
    await page.goto("/watches");
    const watch = page
      .getByRole("article")
      .filter({ hasText: "Rolex Submariner Date 16610" })
      .getByTestId("post-thumbnail-standin");
    await expect(watch).toContainText("16610");
    await expect(watch).toContainText("Rolex");

    await page.goto("/recipes");
    await expect(
      page
        .getByRole("article")
        .filter({ hasText: "Fixture protein oats" })
        .getByTestId("post-thumbnail-standin")
    ).toHaveText("Recipes");
  });

  test("each Content Area has an index", async ({ page }) => {
    for (const [path, name] of [
      ["/recipes", "Recipes"],
      ["/watches", "Watches"],
      ["/ai-development", "AI & Development"],
      ["/training", "Training"],
    ]) {
      await page.goto(path);
      await expect(page.getByRole("heading", { level: 1, name })).toBeVisible();
    }
  });

  test("a Recipe keeps its URL under /recipes", async ({ page }) => {
    await page.goto("/recipes/fixture-protein-oats");

    await expect(
      page.getByRole("heading", { level: 1, name: "Fixture protein oats" })
    ).toBeVisible();
  });

  test("a Post outside its own area is not found", async ({ page }) => {
    const response = await page.goto("/recipes/testing-at-the-seams");

    expect(response?.status()).toBe(404);
  });

  test("an unknown Content Area is not found", async ({ page }) => {
    const response = await page.goto("/not-an-area");

    expect(response?.status()).toBe(404);
  });

  test("on preview a Draft is listed and shown with a Draft badge", async ({ page }) => {
    await page.goto("/ai-development");
    const card = page.getByRole("article").filter({ hasText: "An unpublished idea" });
    await expect(card.getByText("Draft", { exact: true })).toBeVisible();

    await card.getByRole("link", { name: "An unpublished idea" }).click();

    await expect(
      page.getByRole("heading", { level: 1, name: "An unpublished idea" })
    ).toBeVisible();
    await expect(page.getByRole("main").getByText("Draft", { exact: true })).toBeVisible();
  });

  test("a published Post has no Draft badge", async ({ page }) => {
    await page.goto("/ai-development/testing-at-the-seams");

    await expect(page.getByRole("main").getByText("Draft", { exact: true })).toHaveCount(0);
  });
});
