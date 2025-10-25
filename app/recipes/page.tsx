import { Container } from "@/components/Container";
import { RecipeList } from "@/components/RecipeList";
import { Pagination } from "@/components/Pagination";
import { Tag } from "@/components/Tag";
import {
  getAllRecipes,
  getRecipesByCategory,
  getAllRecipeCategories,
  paginateItems,
} from "@/lib/content";
import { siteConfig } from "@/site.config";
import { constructMetadata } from "@/lib/metadata";

export const revalidate = false; // Static generation

export const metadata = constructMetadata({
  title: "Recipes",
  description: "Macro-friendly recipes for meal prep, fitness, and healthy living.",
});

interface RecipesPageProps {
  searchParams: Promise<{ page?: string; category?: string }>;
}

export default async function RecipesPage({ searchParams }: RecipesPageProps) {
  const params = await searchParams;
  const currentPage = Number(params.page) || 1;
  const selectedCategory = params.category;

  // Get recipes, filtered by category if provided
  const allRecipes = selectedCategory
    ? getRecipesByCategory(selectedCategory)
    : getAllRecipes();

  // Paginate
  const paginatedRecipes = paginateItems(
    allRecipes,
    currentPage,
    siteConfig.pagination.recipesPerPage
  );

  // Get all categories for filter
  const allCategories = getAllRecipeCategories();

  return (
    <Container className="py-16 md:py-24">
      <div className="mb-12">
        <h1 className="text-4xl font-bold tracking-tight mb-4">Recipes</h1>
        <p className="text-lg text-muted-foreground">
          Macro-friendly recipes for meal prep, fitness, and healthy living.
        </p>
      </div>

      {/* Category Filter */}
      {allCategories.length > 0 && (
        <div className="mb-12">
          <h2 className="text-sm font-medium mb-4">Filter by category:</h2>
          <div className="flex flex-wrap gap-2">
            <Tag
              tag="All"
              active={!selectedCategory}
              href="/recipes"
            />
            {allCategories.map((category) => (
              <Tag
                key={category}
                tag={category}
                active={selectedCategory === category}
                href={`/recipes?category=${encodeURIComponent(category)}`}
              />
            ))}
          </div>
        </div>
      )}

      {/* Results Info */}
      {selectedCategory && (
        <div className="mb-8">
          <p className="text-sm text-muted-foreground">
            Showing {paginatedRecipes.totalItems}{" "}
            {paginatedRecipes.totalItems === 1 ? "recipe" : "recipes"} in{" "}
            <strong>{selectedCategory}</strong>
          </p>
        </div>
      )}

      {/* Recipe List */}
      <RecipeList recipes={paginatedRecipes.items} />

      {/* Pagination */}
      <Pagination
        currentPage={paginatedRecipes.currentPage}
        totalPages={paginatedRecipes.totalPages}
        basePath="/recipes"
        searchParams={selectedCategory ? { category: selectedCategory } : {}}
      />
    </Container>
  );
}
