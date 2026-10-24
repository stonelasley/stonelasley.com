import { test, expect, type Page } from "@playwright/test";

const palette = (page: Page) => page.getByRole("dialog", { name: "Search" });
const input = (page: Page) => palette(page).getByRole("combobox", { name: "Search Posts" });
const options = (page: Page) => palette(page).getByRole("option");

// Keyboard shortcuts only work once the header has hydrated.
async function visit(page: Page, path: string) {
  await page.goto(path);
  await expect(page.getByRole("banner").getByRole("button", { name: "Search" })).toHaveAttribute(
    "data-ready",
    "true"
  );
}

test.describe("Command palette", () => {
  test("⌘K, Ctrl+K and / open it from anywhere", async ({ page }) => {
    await visit(page, "/recipes");

    for (const key of ["Meta+k", "Control+k", "/"]) {
      await page.keyboard.press(key);
      await expect(palette(page)).toBeVisible();
      await expect(input(page)).toBeFocused();
      await page.keyboard.press("Escape");
      await expect(palette(page)).toBeHidden();
    }
  });

  test("/ is ignored while typing in a field", async ({ page }) => {
    await visit(page, "/search");
    await page.getByRole("searchbox", { name: "Search" }).fill("a/b");

    await expect(palette(page)).toBeHidden();
    await expect(page.getByRole("searchbox", { name: "Search" })).toHaveValue("a/b");
  });

  test("the header search button opens it", async ({ page }) => {
    await visit(page, "/");

    await page.getByRole("banner").getByRole("button", { name: "Search" }).click();

    await expect(palette(page)).toBeVisible();
  });

  test("an empty query shows the latest Posts", async ({ page }) => {
    await visit(page, "/");
    await page.keyboard.press("Control+k");

    await expect(palette(page).getByText("Latest", { exact: true })).toBeVisible();
    await expect(options(page).first()).toContainText("Testing at the seams");
  });

  test("results appear as you type, after a debounce and a minimum length, grouped by area", async ({
    page,
  }) => {
    const searches: string[] = [];
    page.on("request", (request) => {
      if (request.url().includes("/api/search")) searches.push(request.url());
    });
    await visit(page, "/");
    await page.keyboard.press("Control+k");

    await input(page).pressSequentially("r");
    await page.waitForTimeout(400);
    expect(searches).toHaveLength(0); // one letter is below the minimum

    await input(page).pressSequentially("efactors", { delay: 30 });

    const group = palette(page).getByRole("group", { name: "AI & Development" });
    await expect(group.getByRole("option", { name: /Testing at the seams/ })).toBeVisible();
    await expect(group.locator("mark").first()).toHaveText("refactors");
    expect(searches).toHaveLength(1); // typed quickly, so one request after the debounce
  });

  test("Content Area chips filter, and Tab cycles them", async ({ page }) => {
    await visit(page, "/");
    await page.keyboard.press("Control+k");
    await input(page).fill("fixture");
    await expect(palette(page).getByRole("group", { name: "Watches" })).toBeVisible();

    await page.keyboard.press("Tab");
    await expect(
      palette(page).getByRole("button", { name: "Recipes", pressed: true })
    ).toBeVisible();
    await expect(palette(page).getByRole("group", { name: "Watches" })).toHaveCount(0);
    await expect(palette(page).getByRole("group", { name: "Recipes" })).toBeVisible();

    await page.keyboard.press("Shift+Tab");
    await expect(palette(page).getByRole("button", { name: "All", pressed: true })).toBeVisible();
    await expect(input(page)).toBeFocused();
  });

  test("↑/↓ move and Enter opens a result", async ({ page }) => {
    await visit(page, "/");
    await page.keyboard.press("Control+k");
    await input(page).fill("16610");
    const watch = palette(page).getByRole("option", { name: "Rolex Submariner Date 16610" });
    await expect(watch).toHaveAttribute("aria-selected", "true");

    await page.keyboard.press("ArrowDown");
    await expect(options(page).nth(1)).toHaveAttribute("aria-selected", "true");
    await page.keyboard.press("ArrowUp");
    await page.keyboard.press("Enter");

    await expect(page).toHaveURL(/\/watches\/rolex-submariner-date-16610$/);
    await expect(palette(page)).toBeHidden();
  });

  test("“All results” opens the search page with the query and area", async ({ page }) => {
    await visit(page, "/");
    await page.keyboard.press("Control+k");
    await input(page).fill("fixture");
    await page.keyboard.press("Tab");
    await page.keyboard.press("Tab"); // Watches

    await palette(page).getByRole("option", { name: "All results for “fixture”" }).click();

    await expect(page).toHaveURL(/\/search\?q=fixture&area=watches$/);
  });

  test("on mobile it is a full-screen sheet", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await visit(page, "/");
    await page.keyboard.press("Control+k");

    const box = await palette(page).boundingBox();
    expect(box).toMatchObject({ x: 0, y: 0, width: 390, height: 844 });
  });
});
