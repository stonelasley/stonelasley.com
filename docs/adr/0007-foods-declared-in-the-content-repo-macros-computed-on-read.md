---
status: accepted
---

# Foods are declared in the content repo, and a Recipe's macros are computed on read

A Recipe shows protein, carbs, fat and calories per serving. Each ingredient's macros come from `_foods.json` in the content repo: the recipe skill's nutrition table, which maps each food's name to its calories, protein, carbs and fat per 100 g, the USDA entry or label those numbers come from, and the gram weight of one when it's counted by `each`. Like makes (ADR 0006), the list is content: adding a food shouldn't need an API change. The API parses and stores the file on ingest, and computes a Recipe's macros when the Post is read, never at ingest.

Unlike a make, which is a label copied onto the Post, a food's numbers feed a total. If ingest stored the totals, correcting a food would leave every Recipe that uses it wrong until that Recipe was re-ingested. Computing on read means a corrected food fixes every Recipe on the next build. The cost is one extra query per Recipe page, and pages are pre-rendered.

## Considered Options

- **Compute and store macros at ingest.** Rejected: stale totals after a food is corrected, with nothing to show they're stale.
- **Look foods up in USDA FoodData Central.** Rejected: matching ingredient names to its entries is fuzzy, and the build would depend on a third-party API. A hand-declared list of the foods actually cooked with is short and exact.
- **A foods file of the API's own (`_foods.yaml`, the first version of this decision).** Rejected: the content repo's recipe skill already kept a sourced table and computes macros from it while a Recipe is written. Two files meant two copies of every number, and the API's copy had no source and derived calories from the macros, overstating sweeteners such as allulose.
- **Fail ingest on an ingredient no food declares.** Rejected for now: every existing Recipe would fail until the file covered all of them. A Recipe with an undeclared ingredient shows no macros instead.

## Consequences

- The API and the skill's `macros.ts` read the same file, so the site shows the numbers the Recipe was written with. The API reads only `per100g` (`kcal`, `protein`, `carbs`, `fat`), `source` and `each`; the other fields are the skill's.
- An ingredient matches a food's name, ignoring case, with no aliases: a Recipe names a food the way the table does. Its quantity converts to grams by its unit: grams (`g`, `gram`, `grams`) and kilograms (`kg`, `kilogram`, `kilograms`), or `each` or no unit (such as 2 eggs) by the food's `each`. No other unit converts.
- Every food needs a `source`, so a number from memory fails ingest.
- Optional ingredients and ingredients without a quantity count for nothing. If any other ingredient doesn't match a food or unit, the Recipe has no macros: a partial total would read as a whole one.
- Calories are the table's own, not derived from the macros: USDA's figure, or the label's, accounts for fiber and sugar alcohols where 4/4/9 doesn't.
- Like `_makes.yaml`, the file replaces the stored foods on each ingest that sends it, and an ingest without it keeps the stored ones. An ingest that changes the foods revalidates the site, since every Recipe's macros may have changed.
