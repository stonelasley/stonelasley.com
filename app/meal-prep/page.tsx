import { notFound } from "next/navigation";
import { Container } from "@/components/Container";
import { Prose } from "@/components/Prose";
import { getMealPrepPage } from "@/lib/content";
import { constructMetadata } from "@/lib/metadata";
import { formatDate } from "@/lib/utils";
import { markdownToHtml } from "@/lib/markdown";

export const revalidate = false; // Static generation

export async function generateMetadata() {
  const mealPrep = getMealPrepPage();

  if (!mealPrep) {
    return {};
  }

  return constructMetadata({
    title: mealPrep.title,
    description: "Weekly meal prep plan for training and rest days",
  });
}

export default async function MealPrepPage() {
  const mealPrep = getMealPrepPage();

  if (!mealPrep) {
    notFound();
  }

  // Convert Markdown to HTML
  const htmlContent = await markdownToHtml(mealPrep.content);

  return (
    <article>
      <Container className="py-16 md:py-24">
        {/* Page Header */}
        <header className="mb-12 max-w-3xl">
          <h1 className="text-4xl font-bold tracking-tight mb-4 md:text-5xl">
            {mealPrep.title}
          </h1>
          <p className="text-xl text-muted-foreground">
            Weekly meal prep plan for training and rest days
          </p>
        </header>

        {/* Meal Prep Content */}
        <Prose className="max-w-3xl">
          <div dangerouslySetInnerHTML={{ __html: htmlContent }} />
        </Prose>

        {/* Page Footer */}
        <footer className="mt-16 pt-8 border-t border-border max-w-3xl">
          <div className="text-sm text-muted-foreground">
            Last updated: {formatDate(mealPrep.lastUpdated)}
          </div>
        </footer>
      </Container>
    </article>
  );
}
