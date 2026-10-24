// One-time, throwaway migration (#54): turns the Notion Recipes (as fetched into web/content by
// `npm run fetch-content -w web`) and the Watch Specs database (read live, with the NOTION_API_KEY in
// web/.env.local) into Markdown Posts for the private content repo, for hand curation before the first merge.
// Nothing syncs back. Delete with the Notion fetch at cutover (#55).
//
//   node web/scripts/migrate-notion.mjs <content-repo-checkout>
import fs from "node:fs";
import path from "node:path";
import { randomBytes } from "node:crypto";
import { Client } from "@notionhq/client";
import { NotionToMarkdown } from "notion-to-md";
import dotenv from "dotenv";

const [outDir] = process.argv.slice(2);
if (!outDir) {
  console.error("usage: node web/scripts/migrate-notion.mjs <content-repo-checkout>");
  process.exit(2);
}
const webDir = path.join(import.meta.dirname, "..");
dotenv.config({ path: path.join(webDir, ".env.local"), quiet: true });

const CROCKFORD = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
function ulid(date) {
  let time = date.getTime();
  let head = "";
  for (let i = 0; i < 10; i++) {
    head = CROCKFORD[time % 32] + head;
    time = Math.floor(time / 32);
  }
  return head + [...randomBytes(16)].map((b) => CROCKFORD[b % 32]).join("");
}

// Notion titles carry decorative emoji; Post titles don't.
const cleanTitle = (title) => title.replace(/^[^\p{L}\p{N}]+/u, "").trim();

// Citations shouldn't carry tracking parameters.
const stripUtm = (markdown) =>
  markdown.replace(/https?:\/\/[^\s)>\]]+/g, (url) => {
    try {
      const parsed = new URL(url);
      [...parsed.searchParams.keys()]
        .filter((k) => k.startsWith("utm_"))
        .forEach((k) => parsed.searchParams.delete(k));
      return parsed.toString();
    } catch {
      return url;
    }
  });

const yaml = (value) => JSON.stringify(value); // JSON strings are valid YAML scalars

const recipesDir = path.join(webDir, "content", "recipes");
const report = [];
for (const file of fs
  .readdirSync(recipesDir)
  .filter((f) => f.endsWith(".json"))
  .sort()) {
  const recipe = JSON.parse(fs.readFileSync(path.join(recipesDir, file), "utf8"));
  const postDir = path.join(outDir, "recipes", recipe.slug);
  fs.mkdirSync(postDir, { recursive: true });

  let hero = "";
  if (recipe.heroImg) {
    const source = path.join(webDir, "public", recipe.heroImg);
    const name = `hero${path.extname(source)}`;
    fs.copyFileSync(source, path.join(postDir, name));
    hero = `![${cleanTitle(recipe.name)}](./${name})\n\n`;
  }

  const date = recipe.lastUpdated.slice(0, 10);
  const tags = [
    ...new Set([recipe.category, ...recipe.tags].filter(Boolean).map((t) => t.toLowerCase())),
  ];
  const ingredients = (recipe.ingredients ?? []).map((i) => {
    const note = [i.brand, i.purpose].filter(Boolean).join("; ");
    return `  - { ${[
      i.quantity != null ? `quantity: ${i.quantity}` : null,
      i.unit ? `unit: ${yaml(i.unit)}` : null,
      `name: ${yaml(i.name)}`,
      note ? `note: ${yaml(note)}` : null,
      i.optional ? "optional: true" : null,
    ]
      .filter(Boolean)
      .join(", ")} }`;
  });

  const frontmatter = [
    "---",
    `id: ${ulid(new Date(recipe.lastUpdated))}`,
    `title: ${yaml(cleanTitle(recipe.name))}`,
    `slug: ${recipe.slug}`,
    "area: recipes",
    `date: ${date}`,
    `tags: [${tags.map(yaml).join(", ")}]`,
    "audience: public",
    "status: published",
    `prepTimeMin: ${recipe.prepTime}`,
    `cookTimeMin: ${recipe.cookTime}`,
    // Servings are whole numbers on the site; a fractional Notion value is rounded and flagged for curation.
    `servings: ${Math.max(1, Math.round(recipe.servings))}`,
    `difficulty: ${recipe.difficulty}`,
    ...(recipe.ovenTemp ? [`ovenTempC: ${Math.round(((recipe.ovenTemp - 32) * 5) / 9)}`] : []),
    "ingredients:",
    ...ingredients,
    "---",
  ].join("\n");

  const intro = recipe.description ? `${recipe.description.trim()}\n\n` : "";
  fs.writeFileSync(
    path.join(postDir, "index.md"),
    `${frontmatter}\n\n${hero}${intro}${stripUtm(recipe.content).trim()}\n`
  );
  const flags = [
    Number.isInteger(recipe.servings) ? null : `servings ${recipe.servings} rounded`,
    /\w$/.test(recipe.slug) && recipe.slug.length >= 40 ? "slug looks truncated" : null,
  ].filter(Boolean);
  report.push(
    `${recipe.slug}: ${ingredients.length} ingredients${recipe.ovenTemp ? `, ${recipe.ovenTemp}°F` : ""}${hero ? ", hero" : ""}` +
      (flags.length ? `  ⚠ ${flags.join("; ")}` : "")
  );
}
console.log(`Wrote ${report.length} Recipes:\n  ${report.join("\n  ")}`);

// Watches come from the Watch Specs database. They land as Drafts: their Notion notes are research, not finished
// Posts. The Image property and any images in the body are third-party photos, so they are neither copied nor
// hotlinked.
const WATCH_DATABASE_ID = process.env.WATCH_DATABASE_ID ?? "29048366-4402-801d-b059-dd3a88ff6a92";
const notion = new Client({ auth: process.env.NOTION_API_KEY });
const n2m = new NotionToMarkdown({ notionClient: notion });
const plain = (richText) =>
  (richText ?? [])
    .map((t) => t.plain_text)
    .join("")
    .trim();
const slugify = (text) =>
  text
    .normalize("NFKD")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-|-$/g, "");
const dropImages = (markdown) =>
  markdown.replace(/!\[[^\]]*\]\([^)]*\)/g, "").replace(/\n{3,}/g, "\n\n");

const pages = [];
for (let cursor; ; ) {
  const page = await notion.databases.query({
    database_id: WATCH_DATABASE_ID,
    start_cursor: cursor,
  });
  pages.push(...page.results);
  if (!page.has_more) break;
  cursor = page.next_cursor;
}

const watchReport = [];
for (const page of pages) {
  const reference = plain(page.properties.reference?.title);
  const make = page.properties.Make?.select?.name ?? "";
  const notionName = plain(page.properties.Name?.rich_text);
  // A quoted nickname such as “Triple Zero” is not part of the model name; it is kept as a tag for curation.
  const nickname = notionName.match(/[“"]([^”"]+)[”"]/)?.[1];
  const modelName = notionName.replace(/[“"][^”"]+[”"]/, "").trim();
  const title = [make, modelName, reference].filter(Boolean).join(" ");
  const slug = slugify(title);

  // Notion pages often open with a divider, which would read as a stray rule under the title.
  // Notion pages often open with a divider, which would read as a stray rule under the title.
  const body = dropImages(
    stripUtm(n2m.toMarkdownString(await n2m.pageToMarkdown(page.id)).parent ?? "")
  )
    .trim()
    .replace(/^(?:---\s*)+/, "")
    .trim();
  const frontmatter = [
    "---",
    `id: ${ulid(new Date(page.created_time))}`,
    `title: ${yaml(title)}`,
    `slug: ${slug}`,
    "area: watches",
    `date: ${page.created_time.slice(0, 10)}`,
    `tags: [${(nickname ? [slugify(nickname)] : []).map(yaml).join(", ")}]`,
    "audience: public",
    "status: draft",
    `make: ${yaml(make)}`,
    `reference: ${yaml(reference)}`,
    `modelName: ${yaml(modelName)}`,
    "---",
  ].join("\n");
  fs.mkdirSync(path.join(outDir, "watches"), { recursive: true });
  fs.writeFileSync(path.join(outDir, "watches", `${slug}.md`), `${frontmatter}\n\n${body}\n`);

  const flags = [
    !make || !reference || !modelName ? "missing make, reference or modelName" : null,
    nickname ? `nickname “${nickname}” moved to tags` : null,
    body ? null : "empty body",
  ].filter(Boolean);
  watchReport.push(
    `${slug}: ${body.length} chars` + (flags.length ? `  ⚠ ${flags.join("; ")}` : "")
  );
  await new Promise((resolve) => setTimeout(resolve, 350)); // Notion allows 3 requests a second
}
console.log(`Wrote ${watchReport.length} Watches (drafts):\n  ${watchReport.sort().join("\n  ")}`);
