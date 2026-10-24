import { test, expect } from "@playwright/test";

test.describe("Search page", () => {
  test("every page has a search button in its header", async ({ page }) => {
    for (const path of ["/", "/recipes", "/ai-development/testing-at-the-seams"]) {
      await page.goto(path);
      await expect(page.getByRole("banner").getByRole("button", { name: "Search" })).toBeVisible();
    }
  });

  test("searching on submit lists matches with highlighted excerpts", async ({ page }) => {
    await page.goto("/search");

    await page.getByRole("searchbox", { name: "Search" }).fill("refactors");
    await page.getByRole("main").getByRole("button", { name: "Search" }).click();

    await expect(page).toHaveURL(/\/search\?q=refactors$/);
    const result = page.getByRole("article").filter({ hasText: "Testing at the seams" });
    await expect(result.getByRole("link", { name: "Testing at the seams" })).toHaveAttribute(
      "href",
      "/ai-development/testing-at-the-seams"
    );
    await expect(result.locator("mark")).toHaveText("refactors");
    await expect(result.getByText("AI & Development")).toBeVisible();
    await expect(result.locator("time")).toHaveAttribute("datetime", "2026-10-01");
  });

  test("an exact Reference puts that Watch first", async ({ page }) => {
    await page.goto("/search?q=16610");

    await expect(page.getByRole("article").first().getByRole("link").first()).toHaveText(
      "Rolex Submariner Date 16610"
    );
  });

  test("Content Area tabs show counts and filter, and the link can be shared", async ({ page }) => {
    await page.goto("/search?q=fixture");

    const tabs = page.getByRole("navigation", { name: "Filter by Content Area" });
    await expect(tabs.getByRole("link", { name: /^All/ })).toHaveAttribute("aria-current", "page");
    await tabs.getByRole("link", { name: /^Watches 3$/ }).click();

    await expect(page).toHaveURL(/\/search\?q=fixture&area=watches$/);
    await expect(page.getByRole("article")).toHaveCount(3);
    await expect(tabs.getByRole("link", { name: /^Watches 3$/ })).toHaveAttribute(
      "aria-current",
      "page"
    );
  });

  test("back returns to the previous search", async ({ page }) => {
    await page.goto("/search?q=oats");
    await page.getByRole("searchbox", { name: "Search" }).fill("seams");
    await page.getByRole("main").getByRole("button", { name: "Search" }).click();
    await expect(page).toHaveURL(/q=seams/);

    await page.goBack();

    await expect(page).toHaveURL(/q=oats/);
    await expect(page.getByRole("searchbox", { name: "Search" })).toHaveValue("oats");
  });

  test("nothing in the chosen area offers matches from other areas", async ({ page }) => {
    await page.goto("/search?q=oats&area=watches");

    await expect(page.getByText("Nothing matched “oats” in Watches.")).toBeVisible();
    await expect(page.getByRole("link", { name: "Recipes (1)" })).toHaveAttribute(
      "href",
      "/search?q=oats&area=recipes"
    );
  });

  test("nothing anywhere suggests fewer words and links each area", async ({ page }) => {
    await page.goto("/search?q=xylophone+zeppelin");

    await expect(
      page.getByText("Nothing matched “xylophone zeppelin”. Try fewer words, or browse:")
    ).toBeVisible();
    for (const name of ["Recipes", "Watches", "AI & Development", "Training"]) {
      await expect(page.getByRole("main").getByRole("link", { name, exact: true })).toBeVisible();
    }
  });

  test("Drafts never appear", async ({ page }) => {
    await page.goto("/search?q=unpublished");

    await expect(page.getByRole("article")).toHaveCount(0);
  });
});
