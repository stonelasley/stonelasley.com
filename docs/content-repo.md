# Content repo contract

Posts live in the private `stonelasley/content` repo (ADR 0002). This public repo holds only the contract. No content, Client names or tokens belong here.

## Layout

| Path                                           | Holds                                                                       |
| ---------------------------------------------- | --------------------------------------------------------------------------- |
| `<area>/<slug>.md` or `<area>/<slug>/index.md` | a Post: Markdown with YAML frontmatter (see `CLAUDE.md` § Content)          |
| next to a Post                                 | its images, referenced relatively (`![Hero](./hero.jpg)`)                   |
| `_areas/<area>.md`                             | a Content Area's intro                                                      |
| `_clients.yaml`                                | Clients: `[{ id: <ULID>, name: <display name> }]`                           |
| `_makes.yaml`                                  | Makes: `[{ name: <as written>, aliases: [<other spellings>] }]`             |
| `_foods.json`                                  | Foods: `{ name: { per100g: { kcal, protein, carbs, fat }, source, each } }` |

Nothing under a `_` folder is a Post.

A Watch Post's `make` must match a make's name or one of its aliases, ignoring case, and is stored under the name (ADR 0006). To add a make, add it to `_makes.yaml` in the same PR as the first Post that names it. Renaming a make doesn't rewrite Posts already stored: keep the old name as an alias, and change the `make` in those Posts so ingest picks them up again.

A Watch Post with a `make` and no `reference` is a Make Post: an article about the make as a whole (ADR 0008). It carries no specs, so `modelName`, `variants`, the production years, `caseDiameterMm`, `calibre`, `caseMaterial` and `waterResistanceM` each fail with `'<field>' needs a 'reference'.` Each make has at most one Make Post, Private Posts included, and Drafts on ingests that include Drafts; a second one fails the run, naming both slugs. Every Watch page lists the make's other public References, and each Reference links to its Make Post, so adding a Reference needs no edits to the others.

A Recipe's macros per serving come from `_foods.json` (ADR 0007), the recipe skill's sourced nutrition table: the API and the skill's `macros.ts` read the same file. Each food gives calories, and protein, carbs and fat in grams, per 100 g, and the `source` they come from (a USDA FoodData Central entry or a label). Other fields, such as `fiber` and `polyols`, are the skill's. Ingredients are measured in grams (`g`, `gram`, `grams`) or kilograms (`kg`, `kilogram`, `kilograms`), or counted by `each` or no unit, which uses the food's `each` weight. An ingredient matches a food's name, ignoring case. Optional ingredients and ones without a quantity count for nothing. If any other ingredient isn't declared, the Recipe shows no macros rather than a partial total. Macros are computed when a page is built, so correcting a food fixes every Recipe that uses it without touching the Recipes.

## Publishing

The content repo's `.github/workflows/ingest.yml` checks out this repo at `master` and runs `sl ingest` (`api/src/Cli`):

| Event           | Production                                                                                                          | Preview            |
| --------------- | ------------------------------------------------------------------------------------------------------------------- | ------------------ |
| PR push         | `--dry-run`: fails on bad frontmatter, an unknown Client ULID or make, a broken image reference or a duplicate slug | `--include-drafts` |
| merge to `main` | ingest without Drafts                                                                                               | `--include-drafts` |

- Each ingest is a full set: Posts missing from it are deleted. `sl ingest` refuses an empty directory for that reason.
- The API revalidates the site after a commit that changed anything, so revalidation follows only a successful ingest.
- The last push wins on the shared preview.

## Secrets and variables (content repo)

| Name                                    | Kind      | Value                                                                        |
| --------------------------------------- | --------- | ---------------------------------------------------------------------------- |
| `PREVIEW_INGEST_TOKEN`                  | secret    | same value as this repo's `PREVIEW_INGEST_TOKEN` (set together; rotate both) |
| `PRODUCTION_INGEST_TOKEN`               | secret    | set at production cutover (#55)                                              |
| `PREVIEW_API_URL`, `PRODUCTION_API_URL` | variables | the API base URLs; an unset URL skips that environment                       |
