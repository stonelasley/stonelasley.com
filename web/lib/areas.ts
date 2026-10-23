// The four Content Areas. A closed set: the API rejects any other area.
export const contentAreas = [
  {
    slug: "recipes",
    name: "Recipes",
    description: "Macro-friendly recipes for meal prep, fitness, and healthy living.",
  },
  {
    slug: "watches",
    name: "Watches",
    description: "Notes on mechanical watches, one Reference at a time.",
  },
  {
    slug: "ai-development",
    name: "AI & Development",
    description: "Software engineering, architecture, and building with AI.",
  },
  {
    slug: "training",
    name: "Training",
    description: "Training, nutrition, and contest prep.",
  },
] as const;

export type ContentArea = (typeof contentAreas)[number];
export type AreaSlug = ContentArea["slug"];

export function findArea(slug: string): ContentArea | undefined {
  return contentAreas.find((area) => area.slug === slug);
}
