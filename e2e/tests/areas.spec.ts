import { test, expect } from "@playwright/test";

// Seeded from fixtures/content through the API's ingest protocol (see playwright.config.ts).

test.describe("Content Areas", () => {
  test("an area index lists its public Posts and opens one", async ({ page }) => {
    await page.goto("/ai-development");

    await expect(page.getByRole("heading", { level: 1, name: "AI & Development" })).toBeVisible();
    await expect(page.getByRole("link", { name: "An unpublished idea" })).toHaveCount(0);

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
});
