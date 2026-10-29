/** Marks a Draft. Drafts appear only on preview, so Stone can review them in place. */
export function DraftBadge() {
  return (
    <span className="text-small rounded border border-amber-500 px-1.5 py-0.5 align-middle font-semibold tracking-wide text-amber-700 uppercase dark:text-amber-400">
      Draft
    </span>
  );
}
