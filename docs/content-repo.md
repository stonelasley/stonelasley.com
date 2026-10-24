# Content repo contract

Posts live in the private `stonelasley/content` repo (ADR 0002). This public repo holds only the contract. No content, Client names or tokens belong here.

## Layout

| Path                                           | Holds                                                              |
| ---------------------------------------------- | ------------------------------------------------------------------ |
| `<area>/<slug>.md` or `<area>/<slug>/index.md` | a Post: Markdown with YAML frontmatter (see `CLAUDE.md` § Content) |
| next to a Post                                 | its images, referenced relatively (`![Hero](./hero.jpg)`)          |
| `_areas/<area>.md`                             | a Content Area's intro                                             |
| `_clients.yaml`                                | Clients: `[{ id: <ULID>, name: <display name> }]`                  |

Nothing under a `_` folder is a Post.

## Publishing

The content repo's `.github/workflows/ingest.yml` checks out this repo at `master` and runs `sl ingest` (`api/src/Cli`):

| Event           | Production                                                                                                  | Preview            |
| --------------- | ----------------------------------------------------------------------------------------------------------- | ------------------ |
| PR push         | `--dry-run`: fails on bad frontmatter, an unknown Client ULID, a broken image reference or a duplicate slug | `--include-drafts` |
| merge to `main` | ingest without Drafts                                                                                       | `--include-drafts` |

- Each ingest is a full set: Posts missing from it are deleted. `sl ingest` refuses an empty directory for that reason.
- The API revalidates the site after a commit that changed anything, so revalidation follows only a successful ingest.
- The last push wins on the shared preview.

## Secrets and variables (content repo)

| Name                                    | Kind      | Value                                                                        |
| --------------------------------------- | --------- | ---------------------------------------------------------------------------- |
| `PREVIEW_INGEST_TOKEN`                  | secret    | same value as this repo's `PREVIEW_INGEST_TOKEN` (set together; rotate both) |
| `PRODUCTION_INGEST_TOKEN`               | secret    | set at production cutover (#55)                                              |
| `PREVIEW_API_URL`, `PRODUCTION_API_URL` | variables | the API base URLs; an unset URL skips that environment                       |
