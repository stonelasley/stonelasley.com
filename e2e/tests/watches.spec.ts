import { test, expect, type Locator, type Page } from "@playwright/test";

// The <dd> that follows the <dt> naming a spec.
const spec = (specs: Locator, term: string) =>
  specs
    .locator("dt", { hasText: new RegExp(`^${term}$`) })
    .locator("xpath=following-sibling::dd[1]");

const listed = (page: Page) => page.getByRole("main").getByRole("article").getByRole("link");

test.describe("Watches", () => {
  test("a Watch shows its specs at a glance", async ({ page }) => {
    await page.goto("/watches/rolex-submariner-date-16610");

    const specs = page.getByRole("region", { name: "Specifications" });
    for (const [term, value] of [
      ["Make", "Rolex"],
      ["Reference", "16610"],
      ["Model", "Submariner Date"],
      ["Variants", "16610LV, 16610T"],
      ["Production", "1988–2010"],
      ["Case diameter", "40 mm"],
      ["Calibre", "3135"],
      ["Case material", "Oystersteel"],
      ["Water resistance", "300 m (984 ft)"],
    ]) {
      await expect(spec(specs, term)).toHaveText(value);
    }
  });

  test("a Watch with no production end shows it is still in production", async ({ page }) => {
    await page.goto("/watches/rolex-submariner-date-126610ln");

    const specs = page.getByRole("region", { name: "Specifications" });
    await expect(spec(specs, "Production")).toHaveText("2020–present (in production)");
    await expect(specs.locator("dt", { hasText: "Calibre" })).toHaveCount(0);
  });

  test("the index filters by make, model name and decade", async ({ page }) => {
    await page.goto("/watches");
    await expect(listed(page)).toHaveCount(3);

    await page.getByLabel("Make").selectOption("Patek Philippe");
    await expect(listed(page)).toHaveText(["Patek Philippe Nautilus 5711"]);
    await expect(page).toHaveURL(/\?make=Patek\+Philippe$/);

    await page.getByLabel("Make").selectOption("");
    await page.getByLabel("Model").selectOption("Submariner Date");
    await expect(listed(page)).toHaveCount(2);

    await page.getByLabel("Decade").selectOption("1980");
    await expect(listed(page)).toHaveText(["Rolex Submariner Date 16610"]);
  });

  test("choosing a make narrows the models to that make's", async ({ page }) => {
    await page.goto("/watches?model=Submariner+Date");

    await page.getByLabel("Make").selectOption("Patek Philippe");

    await expect(page.getByLabel("Model").locator("option")).toHaveText(["All models", "Nautilus"]);
    await expect(page.getByLabel("Model")).toHaveValue("");
    await expect(listed(page)).toHaveText(["Patek Philippe Nautilus 5711"]);
  });

  test("a filtered index link can be shared", async ({ page }) => {
    await page.goto("/watches?decade=2020");

    await expect(page.getByLabel("Decade")).toHaveValue("2020");
    await expect(listed(page)).toHaveText([
      "Patek Philippe Nautilus 5711",
      "Rolex Submariner Date 126610LN",
    ]);
  });

  test("the decade filter offers only decades with Watches", async ({ page }) => {
    await page.goto("/watches");

    // Options render after hydration; toHaveText retries until they're there.
    await expect(page.getByLabel("Decade").locator("option").nth(3)).toHaveText("2000s");
    await expect(page.getByLabel("Decade").locator("option").nth(0)).toHaveText("All decades");
    await expect(page.getByLabel("Decade").locator("option").nth(1)).toHaveText("1980s");
  });
});
