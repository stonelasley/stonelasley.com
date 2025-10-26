import { test, expect } from "@playwright/test";

test.describe("Home Page", () => {
  test("should load the home page", async ({ page }) => {
    await page.goto("/", { waitUntil: "networkidle" });

    // Check that the page title contains the site name or default title
    await expect(page).toHaveTitle(/Stone|Home/);

    // Check for main heading with more flexible timeout
    await expect(page.locator("h1")).toBeVisible({ timeout: 10000 });

    // Check for navigation links
    const blogLink = page.getByRole("link", { name: /blog/i }).first();
    const recipesLink = page.getByRole("link", { name: /recipes/i }).first();

    await expect(blogLink).toBeVisible({ timeout: 10000 });
    await expect(recipesLink).toBeVisible({ timeout: 10000 });
  });

  test("should navigate to blog page", async ({ page }) => {
    await page.goto("/", { waitUntil: "networkidle" });

    // Click the blog link
    const blogLink = page.getByRole("link", { name: /blog/i }).first();
    await blogLink.click();

    // Wait for navigation with timeout
    await page.waitForURL("**/blog**", { timeout: 10000 });

    // Verify we're on the blog page
    expect(page.url()).toContain("/blog");

    // Verify blog page loaded
    await expect(page.locator("h1")).toBeVisible();
  });

  test("should navigate to recipes page", async ({ page }) => {
    await page.goto("/", { waitUntil: "networkidle" });

    // Click the recipes link
    const recipesLink = page.getByRole("link", { name: /recipes/i }).first();
    await recipesLink.click();

    // Wait for navigation with timeout
    await page.waitForURL("**/recipes**", { timeout: 10000 });

    // Verify we're on the recipes page
    expect(page.url()).toContain("/recipes");

    // Verify recipes page loaded
    await expect(page.locator("h1")).toBeVisible();
  });
});
