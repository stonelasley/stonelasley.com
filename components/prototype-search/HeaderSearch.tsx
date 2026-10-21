"use client";

// PROTOTYPE — throwaway. Picks the header search affordance for the current variant.
import { PrototypeSwitcher, useVariant } from "./shared";
import { VariantAHeader } from "./VariantA";
import { VariantBHeader } from "./VariantB";
import { VariantCHeader } from "./VariantC";

export function HeaderSearch() {
  const v = useVariant();
  if (process.env.NODE_ENV === "production") return null;
  return (
    <>
      {v === "A" && <VariantAHeader />}
      {v === "B" && <VariantBHeader />}
      {v === "C" && <VariantCHeader />}
      <PrototypeSwitcher />
    </>
  );
}
