import { test, expect, type Page } from "@playwright/test";

const html = (page: Page) => page.locator("html");
const theme = (page: Page) => page.getByRole("radiogroup", { name: "Theme" });

test.describe("Theme", () => {
  test("offers light, dark and system", async ({ page }) => {
    await page.goto("/");

    for (const name of ["Light", "Dark", "System"]) {
      await expect(theme(page).getByRole("radio", { name })).toBeVisible();
    }
    await expect(theme(page).getByRole("radio", { name: "System" })).toBeChecked();
  });

  test("follows the system preference by default", async ({ page }) => {
    await page.emulateMedia({ colorScheme: "dark" });
    await page.goto("/");
    await expect(html(page)).toHaveClass(/\bdark\b/);

    await page.emulateMedia({ colorScheme: "light" });
    await expect(html(page)).not.toHaveClass(/\bdark\b/);
  });

  test("a chosen theme overrides the system and persists across visits", async ({ page }) => {
    await page.emulateMedia({ colorScheme: "dark" });
    await page.goto("/");

    await theme(page).getByRole("radio", { name: "Light" }).click();
    await expect(html(page)).not.toHaveClass(/\bdark\b/);

    await page.reload();
    await expect(html(page)).not.toHaveClass(/\bdark\b/);
    await expect(theme(page).getByRole("radio", { name: "Light" })).toBeChecked();
  });

  test("applies the theme before the app's JavaScript runs, so there is no flash", async ({
    page,
  }) => {
    await page.goto("/");
    await theme(page).getByRole("radio", { name: "Dark" }).click();

    // With the app bundle blocked, only the inline head script can have set the class.
    await page.route("**/_next/static/**/*.js", (route) => route.abort());
    await page.reload();

    await expect(html(page)).toHaveClass(/\bdark\b/);
    await expect(page.locator("body")).toHaveCSS("background-color", "rgb(23, 23, 23)");
  });

  test("is one tab stop, moved with the arrow keys", async ({ page }) => {
    await page.goto("/");
    const system = theme(page).getByRole("radio", { name: "System" });
    await system.focus();

    await page.keyboard.press("ArrowRight");
    const light = theme(page).getByRole("radio", { name: "Light" });
    await expect(light).toBeFocused();
    await expect(light).toBeChecked();
    await expect(html(page)).not.toHaveClass(/\bdark\b/);

    await page.keyboard.press("ArrowLeft");
    await expect(system).toBeChecked();
    await expect(theme(page).locator('[tabindex="0"]')).toHaveCount(1);
  });

  test("follows a choice made in another tab", async ({ page, context }) => {
    await page.emulateMedia({ colorScheme: "light" });
    await page.goto("/");
    const other = await context.newPage();
    await other.goto("/");

    await theme(other).getByRole("radio", { name: "Dark" }).click();

    await expect(html(page)).toHaveClass(/\bdark\b/);
    await expect(theme(page).getByRole("radio", { name: "Dark" })).toBeChecked();
  });
});
