"use client";

export function PrintButton() {
  return (
    <button
      onClick={() => window.print()}
      className="border-border bg-background hover:bg-accent no-print w-full rounded-md border px-4 py-2 text-sm font-medium transition-colors"
    >
      Print Recipe
    </button>
  );
}
