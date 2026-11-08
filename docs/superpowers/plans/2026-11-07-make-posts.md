# Make Posts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A Watch Post with a `make` and no `reference` publishes as a Make Post. Every Watch page ends with that make's other Reference Posts, and each Reference Post links to its Make Post.

**Architecture:** `Watch.Reference` and `Watch.ModelName` become nullable in the domain. The stored JSON column needs no migration. Ingest enforces one Make Post per make at commit, the same way `SlugConflicts` works. The web derives sibling lists from `getPostsByArea("watches")` in one shared helper module, and the index, tiles and Post page all use it.

**Tech Stack:** .NET 10 (NUnit + Shouldly, `TreatWarningsAsErrors`, nullable enabled), Next.js 16 / React 19, Zod, Vitest, Playwright.

**Spec:** `docs/superpowers/specs/2026-11-07-make-posts-design.md` (issue #122). Branch `feat/make-posts`.

## Global Constraints

- The term is **Make Post**. "Manufacturer" never appears in code, copy or tests.
- UI copy, exact: `More from {make}` and `About {make}`.
- A Make Post's tile and stand-in show `{make}`, and a Reference Post's show `{make} · {reference}` (unchanged).
- Spec field without `reference`: the error is `'{field}' needs a 'reference'.` (the style of the existing `'productionEnd' needs a 'productionStart'.`).
- Fixture content is made up (public repo).
- Commit messages: conventional, suffixed `(#122)`, and ending with `Claude-Session: https://claude.ai/code/session_01VMovAWzwS5jemLdKFD3KiZ`.
- Run API tests from `api/` with `dotnet test`, web unit tests with `npm run test -- --run`, and E2E with `npm run test:e2e`.

## Review Focus

1. **A Reference Post with a forgotten `reference`** must fail ingest, not publish as a Make Post. Task 1 pins it per field.
2. **Re-ingesting the same Make Post with a changed slug** must pass the one-per-make check. Task 3 pins it.
3. **A make's only Post is its Make Post.** The make filter must still offer it, and its page must show no empty "More from" heading. Tasks 2 and 5 pin it.
4. **A Private Reference Post** must never be listed under "More from" on a public page. Task 4 pins it: the helper takes only what `getPostsByArea` returns, and that list is public-only.
5. **Make + model filter while a Make Post exists.** The Model select must not offer an empty or `null` option for the Make Post. Task 4 pins it.

---

### Task 1: Domain — parse Make Posts

**Files:**

- Modify: `api/src/Domain/Posts/Watch.cs`, `api/src/Domain/Posts/MarkdownPost.Watch.cs`, `api/src/Domain/Posts/SearchText.cs`
- Modify (compile only, null-safe): `api/src/Application/Search/KeywordSearch.cs:19-21`, `api/src/Application/Posts/PostReader.cs:21-24`
- Test: `api/tests/Domain.UnitTests/Posts/WatchTests.cs`, `api/tests/Domain.UnitTests/Posts/SearchTextTests.cs`

**Interfaces:**

- Produces: `Watch(string Make, string? Reference, string? ModelName, ...)` with the rest unchanged, and `bool IsMakePost => Reference is null`. `SearchWatch(string Make, string? Reference)`. `WatchSummary(string Make, string? Reference, string? ModelName, IReadOnlyList<int> Decades)`.

- [ ] **Step 1: Write the failing tests** in `WatchTests.cs`:

```csharp
private const string MakePost = """
    ---
    id: 01JBX5Q7Z9K3M8N2P4R6T0V1X1
    title: Rolex
    slug: rolex
    area: watches
    date: 2026-08-01
    audience: public
    status: published
    make: Rolex
    ---
    About Rolex.
    """;

[Test]
public void A_watch_with_a_make_and_no_reference_is_a_make_post()
{
    var watch = Parse(MakePost).Watch.ShouldNotBeNull();
    watch.Make.ShouldBe("Rolex");
    watch.Reference.ShouldBeNull();
    watch.ModelName.ShouldBeNull();
    watch.IsMakePost.ShouldBeTrue();
    watch.Decades(2026).ShouldBeEmpty();
}

[TestCase("modelName: Submariner")]
[TestCase("variants: [16610LV]")]
[TestCase("productionStart: 1988")]
[TestCase("productionEnd: 2010")]
[TestCase("caseDiameterMm: 40")]
[TestCase("calibre: \"3135\"")]
[TestCase("caseMaterial: Oystersteel")]
[TestCase("waterResistanceM: 300")]
public void A_spec_field_without_a_reference_is_rejected(string line)
{
    var field = line[..line.IndexOf(':')];
    var result = Parse(MakePost.Replace("make: Rolex\n", $"make: Rolex\n{line}\n"));
    result.Errors.ShouldContain($"'{field}' needs a 'reference'.");
}
```

Also assert that `Parse(ValidWatch).Watch!.IsMakePost` is false. In `SearchTextTests.cs`, add a test that a Make Post's structured text is `"Rolex"`.

- [ ] **Step 2: Run them and check they fail.** `cd api && dotnet test --filter "FullyQualifiedName~Domain.UnitTests.Posts"`. Expected: a compile error or failures on `IsMakePost`/null reference.

- [ ] **Step 3: Implement.** In `ReadWatch`, when `reference` is blank, require only `make`, and add one `needs a 'reference'` error for each present spec field. Treat `variants` as present when the list is non-null. With a reference, keep today's rules. In `SearchText.Structured`, skip null parts and run `SplitParts` only over non-null references. In `SearchWatch.Of` and `WatchSummary.Of`, pass the nullables through.

- [ ] **Step 4: Run all API tests and check they pass.** `cd api && dotnet test`. Expected: all green, no warnings. Existing Reference tests are unchanged.

- [ ] **Step 5: Commit.** `feat: parse a Watch with no reference as a Make Post (#122)`

### Task 2: API read side — summaries and facets

**Files:**

- Modify: `api/src/Application/Posts/PostReader.cs:119-127`
- Test: `api/tests/Api.FunctionalTests/WatchTests.cs`, `api/tests/Api.FunctionalTests/TestPosts.cs`

**Interfaces:**

- Consumes: Task 1 types.
- Produces: `TestPosts.MakeCore(string make = "Rolex") => $"make: {make}"`. The API returns `reference: null, modelName: null, decades: []` for a Make Post.

- [ ] **Step 1: Write the failing tests.** Make the response records' `Reference`/`ModelName` nullable, then add:
  - `A_make_post_is_read_with_no_reference`: ingest one Make Post, GET it, assert `Watch.Make == "Rolex"`, `Reference` null, `ModelName` null, `Decades` empty in the list item.
  - `Facets_count_a_make_post_for_makes_only`: ingest the Rolex Reference from `WatchCore()` and a `MakeCore("Patek Philippe")` Make Post. Assert `Makes == ["Patek Philippe", "Rolex"]`, `ModelNames == ["Submariner Date"]`, and decades unchanged from the Reference alone.

- [ ] **Step 2: Run them and check they fail.** `dotnet test --filter "FullyQualifiedName~Api.FunctionalTests.WatchTests"`. Expected: the facets test fails on a null model name.

- [ ] **Step 3: Implement.** In `WatchFacetsAsync`, take model names only from summaries whose `ModelName` is non-null. Decades already come out empty.

- [ ] **Step 4: Run them and check they pass.** Run the same command. Expected: PASS.

- [ ] **Step 5: Commit.** `feat: serve Make Posts and keep them out of model facets (#122)`

### Task 3: Ingest — one Make Post per make

**Files:**

- Create: `api/src/Domain/Posts/MakePostConflicts.cs`
- Modify: `api/src/Application/Ingest/IngestService.cs` (`CommitAsync` around line 163-183, `AddressesAsync`, `StoredAddress`)
- Test: `api/tests/Domain.UnitTests/Posts/MakePostConflictsTests.cs`, `api/tests/Api.FunctionalTests/WatchTests.cs`

**Interfaces:**

- Produces: `public sealed record MakePostAddress(PostAddress Address, string Make);` and `public static IReadOnlyList<string> MakePostConflicts.Find(IEnumerable<MakePostAddress> posts)`. It returns one error per make that has more than one Make Post: `Posts {slugs joined like SlugConflicts} both|all are Make Posts for {make}.` Group by make with ordinal comparison, since makes are stored canonical. Slugs are ordered ordinally.

- [ ] **Step 1: Write the failing domain tests.** These mirror `SlugConflictsTests`: two Rolex Make Posts give `["Posts rolex and rolex-history both are Make Posts for Rolex."]`; different makes give no errors; three give `all`.

- [ ] **Step 2: Write the failing functional tests.**
  - `A_second_make_post_for_a_make_fails_the_run`: run one has two `MakeCore()` Posts with different slugs, as a dry run. Commit returns 400 containing `are Make Posts for Rolex`.
  - `A_make_post_conflicts_with_a_stored_one_under_an_alias`: ingest a `MakeCore("TAG Heuer")` Make Post using the `Makes` constant, then run a full set with it plus a new `MakeCore("TAG-Heuer")` Post. The commit fails.
  - `Re_ingesting_a_make_post_with_a_new_slug_passes`: ingest a Make Post, then re-ingest the same id with a new slug. The commit succeeds and the GET at the new slug returns it.

- [ ] **Step 3: Run them and check they fail.** `dotnet test --filter "FullyQualifiedName~MakePostConflicts|FullyQualifiedName~Api.FunctionalTests.WatchTests"`.

- [ ] **Step 4: Implement.**
  - Add `string? MakePost` to `StoredAddress`.
  - Have both `AddressesAsync` queries in `CommitAsync` also select `CASE WHEN json_extract(watch, '$.reference') IS NULL THEN json_extract(watch, '$.make') END AS make_post`. The JSON is camelCase (`JsonSerializerDefaults.Web`).
  - After the slug check, run `MakePostConflicts.Find` over `untouched.Concat(kept)` where `MakePost` is non-null, and return `Invalid` on any conflict.
  - This set already excludes the Post's own stored row when it is re-staged, so a slug change passes. It includes drafts whenever the run includes them, which every preview ingest does (`docs/content-repo.md`). That is how "drafts count" holds.

- [ ] **Step 5: Run all API tests and check they pass.** `cd api && dotnet test`. Expected: PASS.

- [ ] **Step 6: Commit.** `feat: allow one Make Post per make at ingest (#122)`

### Task 4: Web — schemas, watch helpers, tiles, index, Post page

**Files:**

- Modify: `web/lib/api.ts` (`watchSummarySchema`, `watchSchema`, search-hit `watch` near line 243)
- Create: `web/lib/watches.ts`
- Test: `web/lib/watches.test.ts`

**Interfaces:**

- Produces, from `web/lib/watches.ts`:
  - `watchLabel(watch: { make: string; reference: string | null }): string`: `"Seiko"` or `"Seiko · 6105"`.
  - `matchesWatchFilters(watch: WatchSummary | null, f: { make: string; model: string; decade: string }): boolean`
  - `modelNamesFor(posts: PostSummary[], make: string): string[]`: sorted, distinct, and never null.
  - `moreFromMake(posts: PostSummary[], current: { slug: string; make: string }): PostSummary[]`: Reference Posts with the same make, excluding `current.slug` and Make Posts, newest first by `date`.
  - `makePostFor(posts: PostSummary[], make: string): PostSummary | undefined`
- Zod: `reference` and `modelName` become `z.string().nullable()` in both watch schemas, and the search-hit `reference` becomes nullable too.

- [ ] **Step 1: Write the failing tests** in `watches.test.ts`, with fixture summaries for a Rolex Make Post (2026-07-01), Rolex 16610 (2026-08-01), Rolex 126610LN (2026-08-02) and Patek 5711:
  - `watchLabel` gives both forms.
  - Filters: `make=Rolex` keeps the Make Post. `model=Submariner Date` drops it. `decade=1980` drops it. A null watch is dropped.
  - `modelNamesFor(posts, "Rolex")` equals `["Submariner Date"]`, with no null entry.
  - `moreFromMake` from 16610 gives `[126610LN]`. From the Make Post it gives `[126610LN, 16610]`. From 5711 it gives `[]`.
  - `makePostFor(posts, "Rolex")` is the Make Post, and `makePostFor(posts, "Patek Philippe")` is `undefined`.

- [ ] **Step 2: Run them and check they fail.** `npm run test -- --run web/lib/watches.test.ts`. Expected: the module is not found.

- [ ] **Step 3: Implement** the five functions and the schema changes.

- [ ] **Step 4: Run them and check they pass.** `npm run test -- --run web/lib/watches.test.ts`. Don't commit yet. The schema change breaks the type-check until the call sites below are fixed, and each commit must stay green.

**UI part of this task. Files:**

- Modify: `web/components/PostTile.tsx`, `web/components/PostThumbnail.tsx`, `web/app/page.tsx` (`Exhibit`), `web/components/WatchIndex.tsx`, `web/components/PostView.tsx`
- Create: `web/components/MakeLinks.tsx`

**Interfaces:**

- Consumes: everything from `web/lib/watches.ts` above.
- Produces: `MakeLinks({ slug, watch }: { slug: string; watch: Watch })`, an async server component. It calls `getPostsByArea("watches")` and renders:
  - An `About {make}` link to `makePostFor(...)` when the current Post is a Reference Post and a Make Post exists.
  - A `<section aria-labelledby>` whose heading is `More from {make}`, listing `moreFromMake(...)` with `PostTile` (`headingLevel="h3"`). The section is omitted when the list is empty.

- [ ] **Step 5: Change the call sites.**
  - The `PostTile` eyebrow uses `watchLabel`.
  - The `PostThumbnail` label is `watch.reference ?? watch.make`, and the length check reads that label.
  - The `Exhibit` big text is `post.watch.reference ?? post.watch.make`.
  - `WatchIndex` uses `matchesWatchFilters` and `modelNamesFor`, and its make-change "kept" check guards a null `modelName`.
  - `PostView` renders `WatchSpecs` only when `post.watch.reference !== null`. The web always tells Make Posts apart by `reference === null` and doesn't add `isMakePost` to the Zod schema. After the body, `PostView` renders `<MakeLinks slug={post.slug} watch={post.watch} />` for any Watch.

- [ ] **Step 6: Type-check, lint and unit-test.** `npm run type-check && npm run lint && npm run test -- --run`. Expected: all clean.

- [ ] **Step 7: Commit.** `feat: show Make Posts in the grid and link a make's References (#122)`

### Task 5: E2E — fixture Make Post and flows

**Files:**

- Create: `e2e/fixtures/content/watches/rolex.md`, with id `01JBX5Q7Z9K3M8N2P4R6T0V1X2`, title `Rolex`, slug `rolex`, date `2026-07-01`, `make: Rolex`, no image, and a one-line made-up body.
- Modify: `e2e/tests/watches.spec.ts`, plus any other spec whose Watches count changes.

- [ ] **Step 1: Write the tests.**
  - Index: the count is now 4. Make=Rolex shows 3 including `Rolex`. Model=Submariner Date shows 2. Decade=1980 shows only 16610. The Make=Rolex Model options are `["All models", "Submariner Date"]`.
  - The Rolex Make Post has no `Specifications` region. Its `More from Rolex` region lists `["Rolex Submariner Date 126610LN", "Rolex Submariner Date 16610"]`.
  - 16610: `More from Rolex` lists only 126610LN, and the `About Rolex` link goes to `/watches/rolex`.
  - 5711: no `More from` heading and no `About` link.
  - Tile eyebrow on the index for the Make Post reads `Rolex`.

- [ ] **Step 2: Run them.** `npm run test:e2e`. Expected: the new tests pass. Fix any other spec whose counts of Watches or "recent Posts" moved because of the new fixture (check `home.spec.ts`, `search.spec.ts`, `areas.spec.ts`).

- [ ] **Step 3: Commit.** `test: E2E for Make Posts (#122)`

### Task 6: Docs

**Files:**

- Modify: `GLOSSARY.md` (Watch, plus the new Make Post and Reference Post entries, worded as in the spec's Vocabulary section), `docs/content-repo.md` (Make Post frontmatter, the one-per-make rule, and the `needs a 'reference'` error), `web/lib/areas.ts:11` (the Watches description "one Reference at a time" now reads false, so change it to "Notes on mechanical watches: makes and their References.")
- Create: `docs/adr/0008-a-watch-without-a-reference-is-a-make-post.md`. Status accepted. In under 15 lines: the decision, the rejected alternatives (a separate Makes area; Make bodies in `_makes.yaml`), and the consequences (one per make enforced at commit; spec fields need a reference).

- [ ] **Step 1: Write the docs.** Then run `npm run format` and confirm with `npm run lint` that nothing else changed.
- [ ] **Step 2: Commit.** `docs: Make Posts in the glossary, content-repo guide and ADR 0008 (#122)`
