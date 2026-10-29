import { test, expect } from "@playwright/test";

for (const path of ["/", "/watches", "/ai-development/testing-at-the-seams"]) {
  test(`${path} sets its text in the site's typeface, at 13px or more, in at most five sizes`, async ({
    page,
  }) => {
    await page.goto(path);
    await page.evaluate(() => document.fonts.ready);

    const text = await page.evaluate(() =>
      [...document.querySelectorAll("body *")]
        .filter((el) => {
          const own = [...el.childNodes].some((n) => n.nodeType === 3 && n.textContent!.trim());
          const box = el.getBoundingClientRect();
          return own && box.width > 0 && box.height > 0 && !el.closest("pre, code");
        })
        .map((el) => {
          const style = getComputedStyle(el);
          return { family: style.fontFamily, size: parseFloat(style.fontSize) };
        })
    );

    expect(text.length).toBeGreaterThan(0);
    for (const { family } of text) expect(family).toMatch(/Hanken/);
    expect(Math.min(...text.map((t) => t.size))).toBeGreaterThanOrEqual(13);
    expect(new Set(text.map((t) => t.size)).size).toBeLessThanOrEqual(5);
  });
}
