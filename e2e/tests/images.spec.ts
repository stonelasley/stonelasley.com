import { test, expect } from "@playwright/test";
import { workerPort } from "../stack";

test("a Post's image loads from the image host under a content-hash key", async ({ page }) => {
  await page.goto("/ai-development/testing-at-the-seams");

  const image = page.getByRole("img", { name: "A single dot" });
  await expect(image).toBeVisible();
  await expect(image).toHaveAttribute(
    "src",
    new RegExp(`^http://127\\.0\\.0\\.1:${workerPort}/public/images/[0-9a-f]{64}\\.png$`)
  );
  await expect.poll(() => image.evaluate((img: HTMLImageElement) => img.naturalWidth)).toBe(1);
});
