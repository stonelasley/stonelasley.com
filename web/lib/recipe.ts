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
