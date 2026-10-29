import { test, expect } from "@playwright/test";

// The header is widest just above the md breakpoint, where the nav links appear.
for (const width of [390, 768, 820, 1024, 1440]) {
  for (const path of ["/", "/ai-development/testing-at-the-seams"]) {
    test(`${path} doesn't scroll sideways at ${width}px`, async ({ page }) => {
      await page.setViewportSize({ width, height: 900 });
      await page.goto(path);

      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - window.innerWidth
      );
      expect(overflow).toBe(0);
    });
  }
}
