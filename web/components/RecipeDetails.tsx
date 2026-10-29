import type { Recipe } from "@/lib/api";
import { ingredientText } from "@/lib/recipe";
import { PrintButton } from "./PrintButton";

interface RecipeDetailsProps {
  recipe: Recipe;
}

/** The at-a-glance facts a cook decides on: times, servings, difficulty, oven. */
export function RecipeFacts({ recipe }: RecipeDetailsProps) {
  const facts: [string, string][] = [
    ["Prep", `${recipe.prepTimeMin} min`],
    ["Cook", `${recipe.cookTimeMin} min`],
    ["Total", `${recipe.totalTimeMin} min`],
    ["Servings", String(recipe.servings)],
    ["Difficulty", recipe.difficulty],
  ];
  if (recipe.ovenTempC != null && recipe.ovenTempF != null) {
    facts.push(["Oven", `${recipe.ovenTempC} °C (${recipe.ovenTempF} °F)`]);
  }

  return (
    <section
      aria-label="Recipe details"
      className="border-border bg-card space-y-6 rounded-lg border p-6 print:border-0 print:p-0"
    >
      <dl className="text-small grid grid-cols-2 gap-x-4 gap-y-3 print:grid-cols-6">
        {facts.map(([term, value]) => (
          <div key={term}>
            <dt className="text-muted-foreground">{term}</dt>
            <dd className="font-medium">{value}</dd>
          </div>
        ))}
      </dl>
      <PrintButton />
    </section>
  );
}

export function IngredientList({ recipe }: RecipeDetailsProps) {
  return (
    <section className="border-border bg-card mb-8 rounded-lg border p-6 print:border-0 print:p-0">
      <h2 id="ingredients" className="text-heading mb-4 font-semibold">
        Ingredients
      </h2>
      <ul aria-labelledby="ingredients" className="space-y-2">
        {recipe.ingredients.map((ingredient, index) => (
          <li key={index} className="flex items-baseline gap-2">
            <span>{ingredientText(ingredient)}</span>{" "}
            {ingredient.optional && (
              <span className="text-muted-foreground text-small italic">optional</span>
            )}
          </li>
        ))}
      </ul>
    </section>
  );
}
