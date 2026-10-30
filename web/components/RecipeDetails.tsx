"use client";

import { useRef, useState, useSyncExternalStore, type KeyboardEvent } from "react";
import type { Recipe } from "@/lib/api";
import { formatQuantity, scaledQuantity, type Units } from "@/lib/recipe";
import { chooseUnits, readUnits, subscribeToUnits } from "@/lib/units";
import { cn } from "@/lib/utils";
import { PrintButton } from "./PrintButton";

interface RecipeDetailsProps {
  recipe: Recipe;
}

const MIN_BATCH = 0.5;
const MAX_BATCH = 4;
const STEP = 0.5;

const unitOptions: { value: Units; label: string }[] = [
  { value: "metric", label: "Metric" },
  { value: "imperial", label: "Imperial" },
];

/**
 * What a cook decides on and shops from: times, servings, difficulty and oven, a batch scale and units, and the
 * ingredients at that scale in those units. The server renders one batch in metric, so the page reads the same
 * without JavaScript; printing prints the current scale and units.
 */
export function RecipeDetails({ recipe }: RecipeDetailsProps) {
  const [batch, setBatch] = useState(1);
  // The server can't see localStorage, so it renders metric and this catches up on hydration.
  const units = useSyncExternalStore(subscribeToUnits, readUnits, () => "metric" as const);
  const unitButtons = useRef<(HTMLButtonElement | null)[]>([]);

  const facts: [string, string][] = [
    ["Prep", `${recipe.prepTimeMin} min`],
    ["Cook", `${recipe.cookTimeMin} min`],
    ["Total", `${recipe.totalTimeMin} min`],
    ["Servings", formatQuantity(recipe.servings * batch)],
    ["Difficulty", recipe.difficulty],
  ];
  if (recipe.ovenTempC != null && recipe.ovenTempF != null) {
    facts.push(["Oven", `${recipe.ovenTempC} °C (${recipe.ovenTempF} °F)`]);
  }

  const step = (direction: 1 | -1) =>
    setBatch((current) => Math.min(MAX_BATCH, Math.max(MIN_BATCH, current + direction * STEP)));

  // Radio-group keyboard pattern, as in ThemeToggle: one tab stop; arrow keys move and select.
  const onUnitsKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const move = { ArrowRight: 1, ArrowDown: 1, ArrowLeft: -1, ArrowUp: -1 }[event.key];
    if (!move) return;
    event.preventDefault();
    const current = unitOptions.findIndex((option) => option.value === units);
    const next = (current + move + unitOptions.length) % unitOptions.length;
    chooseUnits(unitOptions[next].value);
    unitButtons.current[next]?.focus();
  };

  return (
    <div className="mb-12 max-w-[68ch] space-y-8">
      <section aria-label="Recipe details" className="space-y-6">
        <dl className="flex flex-wrap gap-x-8 gap-y-3">
          {facts.map(([term, value]) => (
            <div key={term}>
              <dt className="text-muted-foreground text-small">{term}</dt>
              <dd className="font-medium tabular-nums">{value}</dd>
            </div>
          ))}
        </dl>
        <div className="flex flex-wrap items-center gap-4 print:hidden">
          <div role="group" aria-labelledby="batch-label" className="flex items-center gap-3">
            <span id="batch-label" className="font-medium">
              Batch
            </span>
            <button
              type="button"
              onClick={() => step(-1)}
              disabled={batch <= MIN_BATCH}
              aria-label="Smaller batch"
              className="bg-muted hover:bg-accent text-heading size-11 rounded-md leading-none transition-colors active:scale-95 disabled:opacity-40"
            >
              −
            </button>
            <output
              aria-live="polite"
              className="min-w-[3ch] text-center font-semibold tabular-nums"
            >
              {formatQuantity(batch)}×
            </output>
            <button
              type="button"
              onClick={() => step(1)}
              disabled={batch >= MAX_BATCH}
              aria-label="Larger batch"
              className="bg-muted hover:bg-accent text-heading size-11 rounded-md leading-none transition-colors active:scale-95 disabled:opacity-40"
            >
              +
            </button>
          </div>
          <div className="flex items-center gap-3">
            <span id="units-label" className="font-medium">
              Units
            </span>
            <div
              role="radiogroup"
              aria-labelledby="units-label"
              onKeyDown={onUnitsKeyDown}
              className="bg-muted flex rounded-md p-0.5"
            >
              {unitOptions.map((option, index) => (
                <button
                  key={option.value}
                  ref={(button) => {
                    unitButtons.current[index] = button;
                  }}
                  type="button"
                  role="radio"
                  aria-checked={units === option.value}
                  tabIndex={units === option.value ? 0 : -1}
                  onClick={() => chooseUnits(option.value)}
                  className={cn(
                    "h-11 rounded px-3 font-medium transition-colors",
                    units === option.value
                      ? "bg-accent text-accent-foreground"
                      : "text-muted-foreground hover:text-foreground"
                  )}
                >
                  {option.label}
                </button>
              ))}
            </div>
          </div>
          <PrintButton />
        </div>
      </section>

      <section>
        <h2 id="ingredients" className="text-heading mb-4 font-semibold">
          Ingredients
        </h2>
        <table aria-labelledby="ingredients" className="w-full border-collapse">
          <tbody>
            {recipe.ingredients.map((ingredient, index) => (
              <tr key={index} className="odd:bg-muted/60 print:odd:bg-transparent">
                <td className="w-28 py-2.5 pr-4 pl-3 text-right align-top font-semibold whitespace-nowrap tabular-nums">
                  {scaledQuantity(ingredient, batch, units)}
                </td>
                <td className="py-2.5 pr-3 align-top">
                  {ingredient.name}
                  {ingredient.optional && (
                    <span className="text-muted-foreground text-small"> optional</span>
                  )}
                  {ingredient.note && (
                    <span className="text-muted-foreground text-small block">
                      {ingredient.note}
                    </span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </div>
  );
}
