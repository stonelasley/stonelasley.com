import { notFound } from "next/navigation";
import Link from "next/link";
import Image from "next/image";
import { Container } from "@/components/Container";
import { Prose } from "@/components/Prose";
import { PrintButton } from "@/components/PrintButton";
import {
  getAllRecipes,
  getRecipeBySlug,
} from "@/lib/content";
import { constructMetadata, generateRecipeJsonLd } from "@/lib/metadata";

export const revalidate = false; // Static generation

interface RecipePageProps {
  params: Promise<{ slug: string }>;
}

export async function generateStaticParams() {
  const recipes = getAllRecipes();
  return recipes.map((recipe) => ({
    slug: recipe.slug,
  }));
}

export async function generateMetadata({ params }: RecipePageProps) {
  const { slug } = await params;
  const recipe = getRecipeBySlug(slug);

  if (!recipe) {
    return {};
  }

  return constructMetadata({
    title: recipe.Name,
    description: recipe.Description || `${recipe.Name} recipe`,
  });
}

export default async function RecipePage({ params }: RecipePageProps) {
  const { slug } = await params;
  const recipe = getRecipeBySlug(slug);

  if (!recipe) {
    notFound();
  }

  // Generate JSON-LD
  const jsonLd = generateRecipeJsonLd(recipe);

  const totalTime = (recipe.PrepTime || 0) + (recipe.CookTime || 0);

  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
      />
      <article className="print:p-0">
        <Container className="py-16 md:py-24">
          {/* Back link */}
          <Link
            href="/recipes"
            className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors mb-8 no-print"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 20 20"
              fill="currentColor"
              className="h-4 w-4"
            >
              <path
                fillRule="evenodd"
                d="M17 10a.75.75 0 01-.75.75H5.612l4.158 3.96a.75.75 0 11-1.04 1.08l-5.5-5.25a.75.75 0 010-1.08l5.5-5.25a.75.75 0 111.04 1.08L5.612 9.25H16.25A.75.75 0 0117 10z"
                clipRule="evenodd"
              />
            </svg>
            Back to Recipes
          </Link>

          <div className="grid gap-8 md:grid-cols-3">
            {/* Recipe Header */}
            <div className="md:col-span-2">
              {recipe.heroImg && (
                <div className="relative aspect-video w-full overflow-hidden rounded-lg mb-8">
                  <Image
                    src={recipe.heroImg}
                    alt={recipe.Name}
                    fill
                    className="object-cover"
                    sizes="(max-width: 768px) 100vw, 66vw"
                    priority
                  />
                </div>
              )}

              <header className="mb-8">
                {recipe.Category && (
                  <div className="mb-4">
                    <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                      {recipe.Category}
                    </span>
                  </div>
                )}
                <h1 className="text-4xl font-bold tracking-tight mb-4">
                  {recipe.Name}
                </h1>
                {recipe.Description && (
                  <p className="text-xl text-muted-foreground">
                    {recipe.Description}
                  </p>
                )}
              </header>

              {/* Recipe Content */}
              {recipe.content && (
                <Prose>
                  <div dangerouslySetInnerHTML={{ __html: recipe.content }} />
                </Prose>
              )}
            </div>

            {/* Recipe Meta Sidebar */}
            <div className="md:col-span-1">
              <div className="sticky top-24 space-y-6 rounded-lg border border-border bg-card p-6">
                <h2 className="text-lg font-semibold">Recipe Info</h2>

                <div className="space-y-4">
                  {recipe.PrepTime && (
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-muted-foreground">Prep Time:</span>
                      <span className="text-sm font-medium">{recipe.PrepTime} min</span>
                    </div>
                  )}

                  {recipe.CookTime && (
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-muted-foreground">Cook Time:</span>
                      <span className="text-sm font-medium">{recipe.CookTime} min</span>
                    </div>
                  )}

                  {totalTime > 0 && (
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-muted-foreground">Total Time:</span>
                      <span className="text-sm font-medium">{totalTime} min</span>
                    </div>
                  )}

                  {recipe.Servings && (
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-muted-foreground">Servings:</span>
                      <span className="text-sm font-medium">{recipe.Servings}</span>
                    </div>
                  )}

                  {recipe["OvenTemp (F)"] && (
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-muted-foreground">Oven Temp:</span>
                      <span className="text-sm font-medium">{recipe["OvenTemp (F)"]}°F</span>
                    </div>
                  )}

                  {recipe.Difficulty && (
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-muted-foreground">Difficulty:</span>
                      <span className="text-sm font-medium">{recipe.Difficulty}</span>
                    </div>
                  )}
                </div>

                <PrintButton />
              </div>
            </div>
          </div>
        </Container>
      </article>
    </>
  );
}
