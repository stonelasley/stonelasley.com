"use client";

export function PrintButton() {
  return (
    <button
      onClick={() => window.print()}
      className="w-full rounded-md border border-border bg-background px-4 py-2 text-sm font-medium hover:bg-accent transition-colors no-print"
    >
      Print Recipe
    </button>
  );
}
