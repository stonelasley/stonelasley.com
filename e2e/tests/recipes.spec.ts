import { test, expect } from "@playwright/test";

const recipe = "/recipes/fixture-protein-oats";

test.describe("Recipes", () => {
  test("the Recipes index shows its intro from the content repo", async ({ page }) => {
    await page.goto("/recipes");

    await expect(page.getByText("food that keeps.")).toBeVisible();
    await expect(page.getByRole("strong").filter({ hasText: "macro-friendly" })).toBeVisible();
  });

  test("a Recipe lists its ingredients apart from the body", async ({ page }) => {
    await page.goto(recipe);

    const ingredients = page.getByRole("list", { name: "Ingredients" }).getByRole("listitem");
    await expect(ingredients).toHaveText([
      "80 g rolled oats",
      "½ cup water",
      "1 scoop whey protein (vanilla)",
      "cinnamon optional",
    ]);
  });

  test("a Recipe shows its times, servings, difficulty and oven temperature", async ({ page }) => {
    await page.goto(recipe);

    const details = page.getByRole("region", { name: "Recipe details" });
    for (const [term, value] of [
      ["Prep", "5 min"],
      ["Cook", "10 min"],
      ["Total", "15 min"],
      ["Servings", "2"],
      ["Difficulty", "Easy"],
      ["Oven", "200 °C (390 °F)"],
    ]) {
      await expect(
        details.getByRole("term").filter({ hasText: new RegExp(`^${term}$`) })
      ).toBeVisible();
      await expect(
        details
          .getByRole("definition")
          .filter({ hasText: new RegExp(`^${value.replace(/[()]/g, "\\$&")}$`) })
      ).toBeVisible();
    }
  });

  test("nutrition in the body renders as a table", async ({ page }) => {
    await page.goto(recipe);

    const table = page.getByRole("table");
    await expect(table.getByRole("row", { name: "Per serving Amount" })).toBeVisible();
    await expect(table.getByRole("row", { name: "Protein 30 g" })).toBeVisible();
  });

  test("a Recipe emits JSON-LD Recipe with recipeIngredient", async ({ page }) => {
    await page.goto(recipe);

    const jsonLd = JSON.parse(
      (await page.locator('script[type="application/ld+json"]').textContent()) ?? "{}"
    );
    expect(jsonLd["@type"]).toBe("Recipe");
    expect(jsonLd.name).toBe("Fixture protein oats");
    expect(jsonLd.prepTime).toBe("PT5M");
    expect(jsonLd.cookTime).toBe("PT10M");
    expect(jsonLd.totalTime).toBe("PT15M");
    expect(jsonLd.recipeYield).toBe("2 servings");
    expect(jsonLd.recipeIngredient).toEqual([
      "80 g rolled oats",
      "½ cup water",
      "1 scoop whey protein (vanilla)",
      "cinnamon (optional)",
    ]);
  });

  test("printing keeps the Recipe and drops the site chrome", async ({ page }) => {
    await page.goto(recipe);
    await expect(page.getByRole("button", { name: "Print recipe" })).toBeVisible();

    await page.emulateMedia({ media: "print" });

    await expect(
      page.getByRole("heading", { level: 1, name: "Fixture protein oats" })
    ).toBeVisible();
    await expect(page.getByRole("list", { name: "Ingredients" })).toBeVisible();
    await expect(page.getByRole("banner")).toBeHidden();
    await expect(page.getByRole("contentinfo")).toBeHidden();
    await expect(page.getByRole("button", { name: "Print recipe" })).toBeHidden();
  });
});
