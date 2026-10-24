import { test, expect } from "@playwright/test";

const areas = ["/recipes", "/watches", "/ai-development", "/training"];

// Recipe URLs from the v1 site, which have to keep resolving after the cutover.
const legacyRecipes = [
  "keto-ketchup",
  "peanut-butter-blueberry-anabolic-icecream",
  "hard-bbq-sauce",
  "mexican-muscle-cornbread",
  "high-protein-coconut-cream-balls",
  "lemon-blueberry-high-protein-banana-brea",
  "buffed-up-protein-banana-bread",
];

const legacyRedirects = [
  { from: "/blog", to: "/ai-development" },
  { from: "/blog/learning-a-new-topic-with-keyword-mind-mapping", to: "/ai-development" },
  { from: "/meal-prep", to: "/recipes" },
];

test("the home page renders", async ({ page }) => {
  const response = await page.goto("/");
  expect(response?.status()).toBe(200);
  await expect(page.getByRole("banner")).toBeVisible();
});

for (const area of areas) {
  test(`${area} renders`, async ({ request }) => {
    expect((await request.get(area)).status()).toBe(200);
  });
}

for (const slug of legacyRecipes) {
  test(`the v1 Recipe URL /recipes/${slug} resolves`, async ({ page }) => {
    const response = await page.goto(`/recipes/${slug}`);
    expect(response?.status()).toBe(200);
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
  });
}

for (const { from, to } of legacyRedirects) {
  test(`${from} redirects to ${to}`, async ({ request }) => {
    const response = await request.get(from, { maxRedirects: 0 });
    expect(response.status()).toBe(301);
    expect(new URL(response.headers()["location"], "http://x").pathname).toBe(to);
  });
}

test("search answers through the API", async ({ page }) => {
  await page.goto("/search?q=ketchup");
  await expect(page.getByRole("link", { name: /ketchup/i }).first()).toBeVisible();
});

for (const path of ["/sitemap.xml", "/feed.xml", "/feed.json", "/robots.txt"]) {
  test(`${path} is served`, async ({ request }) => {
    expect((await request.get(path)).status()).toBe(200);
  });
}

test("production is indexable", async ({ request }) => {
  test.skip(process.env.SMOKE_EXPECT_INDEXED !== "true", "preview is noindex by design");
  const response = await request.get("/");
  expect(response.headers()["x-robots-tag"] ?? "").not.toContain("noindex");
});
