import { test, expect } from "@playwright/test";

test.describe("Home Page", () => {
  test("should load the home page", async ({ page }) => {
    await page.goto("/");

    // Check that the page title contains the site name
    await expect(page).toHaveTitle(/Stone/);

    // Check for main heading
    await expect(page.locator("h1")).toBeVisible();

    // Check for navigation links
    await expect(page.getByRole("link", { name: /blog/i })).toBeVisible();
    await expect(page.getByRole("link", { name: /recipes/i })).toBeVisible();
  });

  test("should navigate to blog page", async ({ page }) => {
    await page.goto("/");

    // Click the blog link
    await page.getByRole("link", { name: /blog/i }).first().click();

    // Wait for navigation
    await page.waitForURL("**/blog**");

    // Verify we're on the blog page
    expect(page.url()).toContain("/blog");
  });

  test("should navigate to recipes page", async ({ page }) => {
    await page.goto("/");

    // Click the recipes link
    await page
      .getByRole("link", { name: /recipes/i })
      .first()
      .click();

    // Wait for navigation
    await page.waitForURL("**/recipes**");

    // Verify we're on the recipes page
    expect(page.url()).toContain("/recipes");
  });
});
