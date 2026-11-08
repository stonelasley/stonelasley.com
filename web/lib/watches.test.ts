import { describe, expect, it } from "vitest";
import type { PostSummary } from "./api";
import {
  makePostFor,
  matchesWatchFilters,
  modelNamesFor,
  moreFromMake,
  watchLabel,
} from "./watches";

function post(
  slug: string,
  date: string,
  make: string,
  reference: string | null,
  modelName: string | null,
  decades: number[] = []
): PostSummary {
  return {
    id: slug,
    title: slug,
    slug,
    area: "watches",
    date,
    tags: [],
    excerpt: "",
    hero: null,
    watch: { make, reference, modelName, decades },
    draft: false,
  };
}

const rolex = post("rolex", "2026-07-01", "Rolex", null, null);
const sub16610 = post(
  "rolex-16610",
  "2026-08-01",
  "Rolex",
  "16610",
  "Submariner Date",
  [1980, 1990, 2000, 2010]
);
const sub126610 = post("rolex-126610ln", "2026-08-02", "Rolex", "126610LN", "Submariner Date", [
  2020,
]);
const nautilus = post(
  "patek-5711",
  "2026-08-03",
  "Patek Philippe",
  "5711",
  "Nautilus",
  [2000, 2010, 2020]
);
const posts = [nautilus, sub126610, sub16610, rolex];

const none = { make: "", model: "", decade: "" };

describe("watchLabel", () => {
  it("shows a Make Post by its make", () => {
    expect(watchLabel({ make: "Seiko", reference: null })).toBe("Seiko");
  });

  it("shows a Reference Post by make and reference", () => {
    expect(watchLabel({ make: "Seiko", reference: "6105" })).toBe("Seiko · 6105");
  });
});

describe("matchesWatchFilters", () => {
  it("keeps a Make Post under its make", () => {
    expect(matchesWatchFilters(rolex.watch, { ...none, make: "Rolex" })).toBe(true);
  });

  it("drops a Make Post under a model", () => {
    expect(matchesWatchFilters(rolex.watch, { ...none, model: "Submariner Date" })).toBe(false);
  });

  it("drops a Make Post under a decade", () => {
    expect(matchesWatchFilters(rolex.watch, { ...none, decade: "1980" })).toBe(false);
  });

  it("drops a Post with no watch", () => {
    expect(matchesWatchFilters(null, none)).toBe(false);
  });

  it("keeps a Reference under its make, model and decade", () => {
    expect(
      matchesWatchFilters(sub16610.watch, {
        make: "Rolex",
        model: "Submariner Date",
        decade: "1980",
      })
    ).toBe(true);
  });
});

describe("modelNamesFor", () => {
  it("lists a make's model names without its Make Post", () => {
    expect(modelNamesFor(posts, "Rolex")).toEqual(["Submariner Date"]);
  });
});

describe("moreFromMake", () => {
  it("lists the make's other References, newest first", () => {
    expect(moreFromMake(posts, { slug: "rolex-16610", make: "Rolex" })).toEqual([sub126610]);
  });

  it("lists every Reference from the Make Post", () => {
    expect(moreFromMake(posts, { slug: "rolex", make: "Rolex" })).toEqual([sub126610, sub16610]);
  });

  it("is empty for a make's only Reference", () => {
    expect(moreFromMake(posts, { slug: "patek-5711", make: "Patek Philippe" })).toEqual([]);
  });
});

describe("makePostFor", () => {
  it("finds the make's Make Post", () => {
    expect(makePostFor(posts, "Rolex")).toBe(rolex);
  });

  it("is undefined for a make without one", () => {
    expect(makePostFor(posts, "Patek Philippe")).toBeUndefined();
  });
});
