import { test, expect } from "@playwright/test";

test("the home page embeds recent Instagram photos linking to their posts", async ({ page }) => {
  await page.goto("/");

  const feed = page.getByRole("region", { name: "On Instagram" });
  await expect(feed.getByRole("img")).toHaveCount(3);
  await expect(feed.getByRole("link", { name: "Fixture photo 3" })).toHaveAttribute(
    "href",
    "https://www.instagram.com/p/fixture3/"
  );
});
