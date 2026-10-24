import { test, expect } from "@playwright/test";

const site = "https://stonelasley.com";
const post = "/ai-development/testing-at-the-seams";

test.describe("SEO", () => {
  test("a Post has a canonical URL and OpenGraph tags", async ({ page }) => {
    await page.goto(post);

    await expect(page.locator('link[rel="canonical"]')).toHaveAttribute("href", `${site}${post}`);
    await expect(page.locator('meta[property="og:url"]')).toHaveAttribute(
      "content",
      `${site}${post}`
    );
    await expect(page.locator('meta[property="og:type"]')).toHaveAttribute("content", "article");
    await expect(page.locator('meta[property="og:title"]')).toHaveAttribute(
      "content",
      "Testing at the seams"
    );
    await expect(page.locator('meta[property="og:description"]')).toHaveAttribute(
      "content",
      "A fixture Post for the end-to-end suite."
    );
  });

  test("a non-Recipe Post emits JSON-LD BlogPosting", async ({ page }) => {
    await page.goto(post);

    const jsonLd = JSON.parse(
      (await page.locator('script[type="application/ld+json"]').textContent()) ?? "{}"
    );
    expect(jsonLd["@type"]).toBe("BlogPosting");
    expect(jsonLd.headline).toBe("Testing at the seams");
    expect(jsonLd.datePublished).toBe("2026-10-01");
    expect(jsonLd.url).toBe(`${site}${post}`);
    expect(jsonLd.description).toBe("A fixture Post for the end-to-end suite.");
  });

  test("an area index has a canonical URL", async ({ page }) => {
    await page.goto("/watches");

    await expect(page.locator('link[rel="canonical"]')).toHaveAttribute("href", `${site}/watches`);
  });

  test("the sitemap lists public, published Posts only", async ({ request }) => {
    const sitemap = await (await request.get("/sitemap.xml")).text();

    expect(sitemap).toContain(`<loc>${site}${post}</loc>`);
    expect(sitemap).toContain(`<loc>${site}/recipes/fixture-protein-oats</loc>`);
    expect(sitemap).not.toContain("an-unpublished-idea");
  });

  test("robots points at the sitemap", async ({ request }) => {
    const robots = await (await request.get("/robots.txt")).text();

    expect(robots).toContain(`Sitemap: ${site}/sitemap.xml`);
  });
});

test.describe("Feeds", () => {
  test("/feed.xml lists public, published Posts", async ({ request }) => {
    const response = await request.get("/feed.xml");
    const rss = await response.text();

    expect(response.headers()["content-type"]).toContain("application/xml");
    expect(rss).toContain(`<link>${site}${post}</link>`);
    expect(rss).toContain("<title><![CDATA[Fixture protein oats]]></title>");
    expect(rss).not.toContain("an-unpublished-idea");
  });

  test("/feed.json lists public, published Posts", async ({ request }) => {
    const feed = await (await request.get("/feed.json")).json();
    const urls = feed.items.map((item: { url: string }) => item.url);

    expect(urls).toContain(`${site}${post}`);
    expect(urls).not.toContain(`${site}/ai-development/an-unpublished-idea`);
    expect(feed.items.find((item: { url: string }) => item.url === `${site}${post}`).summary).toBe(
      "A fixture Post for the end-to-end suite."
    );
  });
});

test.describe("Redirects", () => {
  for (const [from, to] of [
    ["/blog", "/ai-development"],
    ["/blog/learning-a-new-topic-with-keyword-mind-mapping", "/ai-development"],
    ["/meal-prep", "/recipes"],
  ]) {
    test(`${from} 301s to ${to}`, async ({ request }) => {
      const response = await request.get(from, { maxRedirects: 0 });

      expect(response.status()).toBe(301);
      expect(new URL(response.headers()["location"], "http://x").pathname).toBe(to);
    });
  }
});
