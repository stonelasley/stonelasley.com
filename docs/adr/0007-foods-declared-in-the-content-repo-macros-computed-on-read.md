---
status: accepted
---

# Foods are declared in the content repo, and a Recipe's macros are computed on read

A Recipe shows protein, carbs, fat and calories per serving. Each ingredient's macros come from `_foods.yaml` in the content repo: a list of foods with each one's name, aliases, protein, carbs and fat in grams per 100 g, and the gram weight of any unit other than `g` and `kg`. Like makes (ADR 0006), the list is content: adding a food shouldn't need an API change. The API parses and stores the file on ingest, and computes a Recipe's macros when the Post is read, never at ingest.

Unlike a make, which is a label copied onto the Post, a food's numbers feed a total. If ingest stored the totals, correcting a food would leave every Recipe that uses it wrong until that Recipe was re-ingested. Computing on read means a corrected food fixes every Recipe on the next build. The cost is one extra query per Recipe page, and pages are pre-rendered.

## Considered Options

- **Compute and store macros at ingest.** Rejected: stale totals after a food is corrected, with nothing to show they're stale.
- **Look foods up in USDA FoodData Central.** Rejected: matching ingredient names to its entries is fuzzy, and the build would depend on a third-party API. A hand-declared list of the foods actually cooked with is short and exact.
- **Fail ingest on an ingredient no food declares.** Rejected for now: every existing Recipe would fail until the file covered all of them. A Recipe with an undeclared ingredient shows no macros instead.

## Consequences

- An ingredient matches a food's name or alias, ignoring case. Its quantity converts to grams by its unit: `g` and `kg` always work, any other unit needs an entry in the food's `units`, and a quantity with no unit (such as 2 eggs) uses the food's `each`.
- Optional ingredients and ingredients without a quantity count for nothing. If any other ingredient doesn't match a food or unit, the Recipe has no macros: a partial total would read as a whole one.
- Calories are derived as 4 per gram of protein and carbs and 9 per gram of fat, so a food doesn't carry a fourth number that could contradict the other three.
- Like `_makes.yaml`, the file replaces the stored foods on each ingest that sends it, and an ingest without it keeps the stored ones. An ingest that changes the foods revalidates the site, since every Recipe's macros may have changed.
