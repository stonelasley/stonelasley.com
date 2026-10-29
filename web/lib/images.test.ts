import { describe, expect, it } from "vitest";
import { resizedImage, resizedSrcSet } from "./images";

const hero = "https://img.stonelasley.com/images/abc123.png";

describe("resizedImage", () => {
  it("asks Cloudflare for a square crop of an image in the zone", () => {
    expect(resizedImage(hero, 192)).toBe(
      "https://img.stonelasley.com/cdn-cgi/image/width=192,height=192,fit=cover,format=auto/images/abc123.png"
    );
  });

  it("resizes on the preview image host too", () => {
    expect(resizedImage("https://img-preview.stonelasley.com/images/abc123.png", 96)).toBe(
      "https://img-preview.stonelasley.com/cdn-cgi/image/width=96,height=96,fit=cover,format=auto/images/abc123.png"
    );
  });

  it("leaves images from other hosts alone", () => {
    expect(resizedImage("http://127.0.0.1:8787/public/images/abc123.png", 96)).toBe(
      "http://127.0.0.1:8787/public/images/abc123.png"
    );
    expect(resizedImage("https://notstonelasley.com/a.png", 96)).toBe(
      "https://notstonelasley.com/a.png"
    );
  });
});

describe("resizedSrcSet", () => {
  it("lists each size with its width", () => {
    expect(resizedSrcSet(hero, [96, 192])).toBe(
      "https://img.stonelasley.com/cdn-cgi/image/width=96,height=96,fit=cover,format=auto/images/abc123.png 96w, " +
        "https://img.stonelasley.com/cdn-cgi/image/width=192,height=192,fit=cover,format=auto/images/abc123.png 192w"
    );
  });

  it("is absent where nothing can be resized", () => {
    expect(
      resizedSrcSet("http://127.0.0.1:8787/public/images/abc123.png", [96, 192])
    ).toBeUndefined();
  });
});
