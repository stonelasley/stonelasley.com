import type { Ingredient } from "./api";

// Fractions cooks actually measure in, matched within a small tolerance so 0.33 reads as ⅓.
const fractions: [number, string][] = [
  [1 / 4, "¼"],
  [1 / 3, "⅓"],
  [1 / 2, "½"],
  [2 / 3, "⅔"],
  [3 / 4, "¾"],
];

export function formatQuantity(quantity: number): string {
  const whole = Math.floor(quantity);
  const rest = quantity - whole;
  if (rest < 0.005) return String(whole);
  const fraction = fractions.find(([value]) => Math.abs(rest - value) < 0.01);
  if (!fraction) return String(quantity);
  return whole === 0 ? fraction[1] : `${whole}${fraction[1]}`;
}

/** "1 scoop whey (vanilla)": the line a cook shops from. */
export function ingredientText(ingredient: Ingredient, { markOptional = false } = {}): string {
  const parts = [
    ingredient.quantity != null ? formatQuantity(ingredient.quantity) : null,
    ingredient.unit,
    ingredient.name,
  ].filter(Boolean);
  let text = parts.join(" ");
  if (ingredient.note) text += ` (${ingredient.note})`;
  if (markOptional && ingredient.optional) text += " (optional)";
  return text;
}

const GRAMS = new Set(["g", "gram", "grams"]);

/**
 * An ingredient's quantity at a batch scale, the way a cook reads it off the scale or the cup: grams as "g", whole
 * from 10 g up and to one decimal below; other units in cook's fractions; "each" as a bare count. Empty when the
 * ingredient has no quantity ("to taste").
 */
export function scaledQuantity(ingredient: Ingredient, factor: number): string {
  if (ingredient.quantity == null) return "";
  const value = ingredient.quantity * factor;
  if (ingredient.unit && GRAMS.has(ingredient.unit)) {
    const grams = value >= 10 ? Math.round(value) : Math.round(value * 10) / 10;
    return `${grams} g`;
  }
  const amount = formatQuantity(Math.round(value * 100) / 100);
  return ingredient.unit && ingredient.unit !== "each" ? `${amount} ${ingredient.unit}` : amount;
}
