import type { Recipe } from "@/lib/schemas";
import { RecipeCard } from "./RecipeCard";

interface RecipeListProps {
  recipes: Recipe[];
}

export function RecipeList({ recipes }: RecipeListProps) {
  if (recipes.length === 0) {
    return (
      <div className="text-center py-12">
        <p className="text-muted-foreground">No recipes found.</p>
      </div>
    );
  }

  return (
    <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
      {recipes.map((recipe) => (
        <RecipeCard key={recipe.url} recipe={recipe} />
      ))}
    </div>
  );
}
