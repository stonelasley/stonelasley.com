import Link from "next/link";
import Image from "next/image";
import type { Recipe } from "@/lib/schemas";

interface RecipeCardProps {
  recipe: Recipe;
}

export function RecipeCard({ recipe }: RecipeCardProps) {
  const totalTime = (recipe.PrepTime || 0) + (recipe.CookTime || 0);

  return (
    <article className="group relative flex flex-col overflow-hidden rounded-lg border border-border bg-card transition-colors hover:bg-accent">
      <Link href={`/recipes/${recipe.slug}`} className="absolute inset-0 z-10">
        <span className="sr-only">View {recipe.Name}</span>
      </Link>
      {recipe.heroImg && (
        <div className="relative aspect-video w-full overflow-hidden">
          <Image
            src={recipe.heroImg}
            alt={recipe.Name}
            fill
            className="object-cover transition-transform group-hover:scale-105"
            sizes="(max-width: 768px) 100vw, (max-width: 1200px) 50vw, 33vw"
          />
        </div>
      )}
      <div className="flex flex-1 flex-col justify-between p-6">
        <div className="flex-1">
          {recipe.Category && (
            <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
              {recipe.Category}
            </span>
          )}
          <h3 className="mt-2 text-xl font-semibold tracking-tight group-hover:underline">
            {recipe.Name}
          </h3>
          {recipe.Description && (
            <p className="mt-3 text-sm text-muted-foreground line-clamp-2">
              {recipe.Description}
            </p>
          )}
        </div>
        <div className="mt-4 flex items-center gap-4 text-sm text-muted-foreground">
          {totalTime > 0 && (
            <div className="flex items-center gap-1">
              <svg
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 20 20"
                fill="currentColor"
                className="h-4 w-4"
              >
                <path
                  fillRule="evenodd"
                  d="M10 18a8 8 0 100-16 8 8 0 000 16zm.75-13a.75.75 0 00-1.5 0v5c0 .414.336.75.75.75h4a.75.75 0 000-1.5h-3.25V5z"
                  clipRule="evenodd"
                />
              </svg>
              <span>{totalTime} min</span>
            </div>
          )}
          {recipe.Servings && (
            <>
              <span>•</span>
              <div className="flex items-center gap-1">
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  viewBox="0 0 20 20"
                  fill="currentColor"
                  className="h-4 w-4"
                >
                  <path d="M10 8a3 3 0 100-6 3 3 0 000 6zM3.465 14.493a1.23 1.23 0 00.41 1.412A9.957 9.957 0 0010 18c2.31 0 4.438-.784 6.131-2.1.43-.333.604-.903.408-1.41a7.002 7.002 0 00-13.074.003z" />
                </svg>
                <span>{recipe.Servings} servings</span>
              </div>
            </>
          )}
          {recipe.Difficulty && (
            <>
              <span>•</span>
              <span>{recipe.Difficulty}</span>
            </>
          )}
        </div>
      </div>
    </article>
  );
}
