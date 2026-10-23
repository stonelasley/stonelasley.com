import { describe, it, expect } from "vitest";
import { cn, formatDate } from "./utils";

describe("utils", () => {
  describe("cn (className utility)", () => {
    it("should merge class names", () => {
      expect(cn("foo", "bar")).toBe("foo bar");
    });

    it("should handle conditional classes", () => {
      expect(cn("foo", false && "bar", "baz")).toBe("foo baz");
    });

    it("should handle undefined and null", () => {
      expect(cn("foo", undefined, null, "bar")).toBe("foo bar");
    });
  });

  describe("formatDate", () => {
    it("should format dates with default format (MMMM d, yyyy)", () => {
      const date = "2024-01-15T12:00:00Z";
      const formatted = formatDate(date);
      expect(formatted).toBe("January 15, 2024");
    });

    it("should handle different months", () => {
      expect(formatDate("2024-12-25T12:00:00Z")).toBe("December 25, 2024");
      expect(formatDate("2024-06-01T12:00:00Z")).toBe("June 1, 2024");
    });

    it("should accept custom format strings", () => {
      expect(formatDate("2024-01-15T12:00:00Z", "MMM d, yyyy")).toBe("Jan 15, 2024");
      expect(formatDate("2024-01-15T12:00:00Z", "yyyy-MM-dd")).toBe("2024-01-15");
    });
  });
});
