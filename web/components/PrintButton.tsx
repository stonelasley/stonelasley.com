"use client";

export function PrintButton() {
  return (
    <button
      type="button"
      onClick={() => window.print()}
      className="bg-muted hover:bg-accent no-print text-small h-11 rounded-md px-4 font-medium transition-colors active:scale-95"
    >
      Print recipe
    </button>
  );
}
