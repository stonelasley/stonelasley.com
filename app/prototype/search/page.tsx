"use client";

// PROTOTYPE — throwaway. Three variants of "what a reader sees when they search"
// (issue #29), switchable via ?variant=A|B|C. Stands in for a future /search route.
import { Suspense } from "react";
import { useVariant } from "@/components/prototype-search/shared";
import { VariantBPage } from "@/components/prototype-search/VariantB";

function Body() {
  const v = useVariant();
  if (v === "B") return <VariantBPage />;
  return (
    <div className="text-muted-foreground mx-auto max-w-xl px-4 py-24 text-center text-sm">
      Variant {v} has no search page: results live only in the{" "}
      {v === "A" ? "header dropdown" : "⌘K palette"}. A link to this URL lands here.
    </div>
  );
}

export default function PrototypeSearchPage() {
  return (
    <Suspense>
      <Body />
    </Suspense>
  );
}
