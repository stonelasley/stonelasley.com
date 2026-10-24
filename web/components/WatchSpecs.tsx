import type { Watch } from "@/lib/api";

interface WatchSpecsProps {
  watch: Watch;
}

function production(watch: Watch): string | null {
  if (watch.productionStart == null) return null;
  if (watch.inProduction) return `${watch.productionStart}–present (in production)`;
  return `${watch.productionStart}–${watch.productionEnd}`;
}

/** The key specs of one Reference, at a glance. Rows without a value are left out. */
export function WatchSpecs({ watch }: WatchSpecsProps) {
  const rows: [string, string | null][] = [
    ["Make", watch.make],
    ["Reference", watch.reference],
    ["Model", watch.modelName],
    ["Variants", watch.variants.length > 0 ? watch.variants.join(", ") : null],
    ["Production", production(watch)],
    ["Case diameter", watch.caseDiameterMm != null ? `${watch.caseDiameterMm} mm` : null],
    ["Calibre", watch.calibre],
    ["Case material", watch.caseMaterial],
    [
      "Water resistance",
      watch.waterResistanceM != null
        ? `${watch.waterResistanceM} m (${watch.waterResistanceFt} ft)`
        : null,
    ],
  ];

  return (
    <section
      aria-label="Specifications"
      className="border-border bg-card mb-12 max-w-3xl rounded-lg border p-6"
    >
      <dl className="grid gap-x-6 gap-y-3 text-sm sm:grid-cols-2">
        {rows
          .filter((row): row is [string, string] => row[1] !== null)
          .map(([term, value]) => (
            <div key={term} role="group" aria-label={term} className="flex justify-between gap-4">
              <dt className="text-muted-foreground">{term}</dt>
              <dd className="text-right font-medium">{value}</dd>
            </div>
          ))}
      </dl>
    </section>
  );
}
