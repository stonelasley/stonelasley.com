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

    const ingredients = page.getByRole("table", { name: "Ingredients" }).getByRole("row");
    await expect(ingredients).toHaveText([
      "80 grolled oats",
      "½ cupwater",
      "1 scoopwhey proteinvanilla",
      "cinnamon optional",
    ]);
  });

  test("scaling the batch scales every quantity and the servings", async ({ page }) => {
    await page.goto(recipe);
    const ingredients = page.getByRole("table", { name: "Ingredients" });
    const servings = page
      .getByRole("region", { name: "Recipe details" })
      .getByRole("definition")
      .nth(3);
    await expect(servings).toHaveText("2");

    await page.getByRole("button", { name: "Larger batch" }).click();
    await page.getByRole("button", { name: "Larger batch" }).click();

    await expect(page.getByRole("group", { name: "Batch" }).getByRole("status")).toHaveText("2×");
    await expect(ingredients.getByRole("row", { name: /rolled oats/ })).toContainText("160 g");
    await expect(ingredients.getByRole("row", { name: /water/ })).toContainText("1 cup");
    await expect(
      ingredients
        .getByRole("row", { name: /cinnamon/ })
        .getByRole("cell")
        .first()
    ).toHaveText("");
    await expect(servings).toHaveText("4");

    await page.getByRole("button", { name: "Smaller batch" }).click();
    await page.getByRole("button", { name: "Smaller batch" }).click();
    await page.getByRole("button", { name: "Smaller batch" }).click();
    await expect(ingredients.getByRole("row", { name: /rolled oats/ })).toContainText("40 g");
    await expect(page.getByRole("button", { name: "Smaller batch" })).toBeDisabled();
  });

  test("imperial shows weights in ounces, and the choice survives a reload", async ({ page }) => {
    await page.goto(recipe);
    const ingredients = page.getByRole("table", { name: "Ingredients" });
    const units = page.getByRole("radiogroup", { name: "Units" });
    await expect(units.getByRole("radio", { name: "Metric" })).toHaveAttribute(
      "aria-checked",
      "true"
    );
    await expect(ingredients.getByRole("row", { name: /rolled oats/ })).toContainText("80 g");

    await units.getByRole("radio", { name: "Imperial" }).click();
    await expect(ingredients.getByRole("row", { name: /rolled oats/ })).toContainText("2¾ oz");
    await expect(ingredients.getByRole("row", { name: /water/ })).toContainText("½ cup");

    await page.getByRole("button", { name: "Larger batch" }).click();
    await page.getByRole("button", { name: "Larger batch" }).click();
    await expect(ingredients.getByRole("row", { name: /rolled oats/ })).toContainText("5¾ oz");

    await page.reload();
    await expect(units.getByRole("radio", { name: "Imperial" })).toHaveAttribute(
      "aria-checked",
      "true"
    );
    await expect(ingredients.getByRole("row", { name: /rolled oats/ })).toContainText("2¾ oz");

    await units.getByRole("radio", { name: "Imperial" }).focus();
    await page.keyboard.press("ArrowLeft");
    await expect(units.getByRole("radio", { name: "Metric" })).toBeFocused();
    await expect(ingredients.getByRole("row", { name: /rolled oats/ })).toContainText("80 g");
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

    const table = page.getByRole("table").filter({ hasText: "Per serving" });
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
    await expect(page.getByRole("table", { name: "Ingredients" })).toBeVisible();
    await expect(page.getByRole("banner")).toBeHidden();
    await expect(page.getByRole("contentinfo")).toBeHidden();
    await expect(page.getByRole("button", { name: "Print recipe" })).toBeHidden();
  });

  test("printing puts the details before the method and always prints light", async ({ page }) => {
    await page.emulateMedia({ colorScheme: "dark" });
    await page.goto(recipe);
    await expect(page.locator("html")).toHaveClass(/\bdark\b/);

    await page.emulateMedia({ colorScheme: "dark", media: "print" });

    const details = await page.getByRole("region", { name: "Recipe details" }).boundingBox();
    const method = await page.getByText("Stir the oats and water").boundingBox();
    expect(details!.y).toBeLessThan(method!.y);
    await expect(page.locator("body")).toHaveCSS("background-color", "rgb(255, 255, 255)");
    await expect(page.locator("body")).toHaveCSS("color", "rgb(23, 23, 23)");
  });
});
