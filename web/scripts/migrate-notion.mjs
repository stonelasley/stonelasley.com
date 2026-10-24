// One-time, throwaway migration (#54): turns the Notion Recipes (as fetched into web/content by
// `npm run fetch-content -w web`) into Markdown Posts for the private content repo, for hand curation before the
// first merge. Nothing syncs back. Delete with the Notion fetch at cutover (#55).
//
//   node web/scripts/migrate-notion.mjs <content-repo-checkout>
import fs from "node:fs";
import path from "node:path";
import { randomBytes } from "node:crypto";

const [outDir] = process.argv.slice(2);
if (!outDir) {
  console.error("usage: node web/scripts/migrate-notion.mjs <content-repo-checkout>");
  process.exit(2);
}
const webDir = path.join(import.meta.dirname, "..");

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
