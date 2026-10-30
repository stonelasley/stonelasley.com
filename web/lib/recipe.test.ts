import { describe, expect, it } from "vitest";
import { formatQuantity, ingredientText, scaledQuantity } from "./recipe";

describe("formatQuantity", () => {
  it.each([
    [2, "2"],
    [0.5, "½"],
    [1.5, "1½"],
    [0.25, "¼"],
    [0.75, "¾"],
    [0.33, "⅓"],
    [0.333, "⅓"],
    [0.67, "⅔"],
    [2.25, "2¼"],
    [1.2, "1.2"],
    [80, "80"],
  ])("shows %s as %s", (quantity, shown) => {
    expect(formatQuantity(quantity)).toBe(shown);
  });
});

describe("ingredientText", () => {
  it("joins quantity, unit and name", () => {
    expect(
      ingredientText({ quantity: 80, unit: "g", name: "rolled oats", note: null, optional: false })
    ).toBe("80 g rolled oats");
  });

  it("adds the note in brackets", () => {
    expect(
      ingredientText({ quantity: 1, unit: "scoop", name: "whey", note: "vanilla", optional: false })
    ).toBe("1 scoop whey (vanilla)");
  });

  it("leaves out a missing quantity and unit", () => {
    expect(
      ingredientText({ quantity: null, unit: null, name: "salt", note: null, optional: false })
    ).toBe("salt");
  });

  it("can mark an ingredient optional", () => {
    expect(
      ingredientText(
        { quantity: null, unit: null, name: "cinnamon", note: null, optional: true },
        { markOptional: true }
      )
    ).toBe("cinnamon (optional)");
  });
});

describe("scaledQuantity", () => {
  const ingredient = (quantity: number | null, unit: string | null) => ({
    quantity,
    unit,
    name: "x",
    note: null,
    optional: false,
  });

  it.each([
    [ingredient(80, "g"), 1, "80 g"],
    [ingredient(80, "g"), 2, "160 g"],
    [ingredient(60, "gram"), 1.5, "90 g"],
    [ingredient(13, "gram"), 0.5, "6.5 g"],
    [ingredient(3, "grams"), 0.5, "1.5 g"],
    [ingredient(14, "g"), 0.5, "7 g"],
    [ingredient(25, "g"), 0.5, "13 g"],
    [ingredient(0.5, "cup"), 1, "½ cup"],
    [ingredient(0.5, "cup"), 3, "1½ cup"],
    [ingredient(1, "scoop"), 2.5, "2½ scoop"],
    [ingredient(1, "each"), 1.5, "1½"],
    [ingredient(1, "each"), 2, "2"],
    [ingredient(0.33, "cup"), 1.5, "½ cup"],
    [ingredient(0.6, "cup"), 1.5, "0.9 cup"],
    [ingredient(null, null), 2, ""],
  ])("shows %o at ×%s as %s", (item, factor, shown) => {
    expect(scaledQuantity(item, factor)).toBe(shown);
  });
});

describe("scaledQuantity in imperial", () => {
  const ingredient = (quantity: number | null, unit: string | null) => ({
    quantity,
    unit,
    name: "x",
    note: null,
    optional: false,
  });

  it.each([
    [ingredient(28, "g"), 1, "1 oz"],
    [ingredient(100, "g"), 1, "3½ oz"],
    [ingredient(80, "gram"), 1, "2¾ oz"],
    [ingredient(454, "g"), 1, "1 lb"],
    [ingredient(453, "g"), 1, "1 lb"],
    [ingredient(567, "g"), 1, "1 lb 4 oz"],
    [ingredient(500, "grams"), 1, "1 lb 1¾ oz"],
    [ingredient(3, "g"), 1, "0.1 oz"],
    [ingredient(7, "g"), 1, "0.2 oz"],
    [ingredient(50, "g"), 1.5, "2¾ oz"],
    [ingredient(80, "g"), 2, "5¾ oz"],
    [ingredient(1, "each"), 2, "2"],
    [ingredient(0.5, "cup"), 1, "½ cup"],
    [ingredient(1, "pinch"), 1, "1 pinch"],
    [ingredient(null, null), 2, ""],
  ])("shows %o at ×%s as %s", (item, factor, shown) => {
    expect(scaledQuantity(item, factor, "imperial")).toBe(shown);
  });

  it("leaves grams alone in metric", () => {
    expect(scaledQuantity(ingredient(100, "g"), 1, "metric")).toBe("100 g");
  });
});
