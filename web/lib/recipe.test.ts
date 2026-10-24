import { describe, expect, it } from "vitest";
import { formatQuantity, ingredientText } from "./recipe";

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
