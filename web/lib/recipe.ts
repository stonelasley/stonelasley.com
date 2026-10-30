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
const GRAMS_PER_OUNCE = 28.349523125;

/** Metric shows weights as recipes are written, in grams; imperial converts them to ounces and pounds. */
export type Units = "metric" | "imperial";

/**
 * An ingredient's quantity at a batch scale, the way a cook reads it off the scale or the cup: grams as "g", whole
 * from 10 g up and to one decimal below; other units in cook's fractions; "each" as a bare count. Empty when the
 * ingredient has no quantity ("to taste"). In imperial, grams are scaled first and then converted, so they round once.
 */
export function scaledQuantity(
  ingredient: Ingredient,
  factor: number,
  units: Units = "metric"
): string {
  if (ingredient.quantity == null) return "";
  const value = ingredient.quantity * factor;
  if (ingredient.unit && GRAMS.has(ingredient.unit)) {
    if (units === "imperial") return ounces(value / GRAMS_PER_OUNCE);
    const grams = value >= 10 ? Math.round(value) : Math.round(value * 10) / 10;
    return `${grams} g`;
  }
  const amount = formatQuantity(Math.round(value * 100) / 100);
  return ingredient.unit && ingredient.unit !== "each" ? `${amount} ${ingredient.unit}` : amount;
}

// To the nearest ¼ oz, in pounds and ounces from 1 lb up. Under ¼ oz, one decimal, so a pinch of salt isn't 0.
function ounces(value: number): string {
  if (value < 0.25) return `${Math.round(value * 10) / 10} oz`;
  const quarters = Math.round(value * 4) / 4;
  const pounds = Math.floor(quarters / 16);
  const rest = quarters - pounds * 16;
  if (pounds === 0) return `${formatQuantity(rest)} oz`;
  return rest === 0 ? `${pounds} lb` : `${pounds} lb ${formatQuantity(rest)} oz`;
}
