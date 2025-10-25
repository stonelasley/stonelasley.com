import { notFound } from "next/navigation";
import Link from "next/link";
import Image from "next/image";
import { Container } from "@/components/Container";
import { Prose } from "@/components/Prose";
import { PrintButton } from "@/components/PrintButton";
import { getAllRecipes, getRecipeBySlug } from "@/lib/content";
import { constructMetadata, generateRecipeJsonLd } from "@/lib/metadata";
import { markdownToHtml } from "@/lib/markdown";

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
    title: recipe.name,
    description: recipe.description || `${recipe.name} recipe`,
  });
}

export default async function RecipePage({ params }: RecipePageProps) {
  const { slug } = await params;
  const recipe = getRecipeBySlug(slug);

  if (!recipe) {
    notFound();
  }

  // Convert Markdown to HTML
  const htmlContent = await markdownToHtml(recipe.content);

  // Generate JSON-LD
  const jsonLd = generateRecipeJsonLd(recipe);

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
            className="text-muted-foreground hover:text-foreground no-print mb-8 inline-flex items-center gap-2 text-sm transition-colors"
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
                <div className="relative mb-8 aspect-video w-full overflow-hidden rounded-lg">
                  <Image
                    src={recipe.heroImg}
                    alt={recipe.name}
                    fill
                    className="object-cover"
                    sizes="(max-width: 768px) 100vw, 66vw"
                    priority
                  />
                </div>
              )}

              <header className="mb-8">
                {recipe.category && (
                  <div className="mb-4">
                    <span className="text-muted-foreground text-xs font-medium tracking-wider uppercase">
                      {recipe.category}
                    </span>
                  </div>
                )}
                <h1 className="mb-4 text-4xl font-bold tracking-tight">{recipe.name}</h1>
                {recipe.description && (
                  <p className="text-muted-foreground text-xl">{recipe.description}</p>
                )}
              </header>

              {/* Recipe Content */}
              {htmlContent && (
                <Prose>
                  <div dangerouslySetInnerHTML={{ __html: htmlContent }} />
                </Prose>
              )}
            </div>

            {/* Recipe Meta Sidebar */}
            <div className="md:col-span-1">
              <div className="border-border bg-card sticky top-24 space-y-6 rounded-lg border p-6">
                <h2 className="text-lg font-semibold">Recipe Info</h2>

                <div className="space-y-4">
                  {recipe.prepTime && (
                    <div className="flex items-center justify-between">
                      <span className="text-muted-foreground text-sm">Prep Time:</span>
                      <span className="text-sm font-medium">{recipe.prepTime} min</span>
                    </div>
                  )}

                  {recipe.cookTime && (
                    <div className="flex items-center justify-between">
                      <span className="text-muted-foreground text-sm">Cook Time:</span>
                      <span className="text-sm font-medium">{recipe.cookTime} min</span>
                    </div>
                  )}

                  {recipe.totalTime > 0 && (
                    <div className="flex items-center justify-between">
                      <span className="text-muted-foreground text-sm">Total Time:</span>
                      <span className="text-sm font-medium">{recipe.totalTime} min</span>
                    </div>
                  )}

                  {recipe.servings && (
                    <div className="flex items-center justify-between">
                      <span className="text-muted-foreground text-sm">Servings:</span>
                      <span className="text-sm font-medium">{recipe.servings}</span>
                    </div>
                  )}

                  {recipe.ovenTemp && (
                    <div className="flex items-center justify-between">
                      <span className="text-muted-foreground text-sm">Oven Temp:</span>
                      <span className="text-sm font-medium">{recipe.ovenTemp}°F</span>
                    </div>
                  )}

                  {recipe.difficulty && (
                    <div className="flex items-center justify-between">
                      <span className="text-muted-foreground text-sm">Difficulty:</span>
                      <span className="text-sm font-medium">{recipe.difficulty}</span>
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
