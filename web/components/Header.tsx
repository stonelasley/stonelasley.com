"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Container } from "./Container";
import { SearchPalette } from "./SearchPalette";
import { siteConfig } from "@/site.config";
import { cn } from "@/lib/utils";

export function Header() {
  const pathname = usePathname();

  const isActive = (href: string) => {
    if (href === "/") {
      return pathname === "/";
    }
    return pathname?.startsWith(href);
  };

  // The wordmark is the way home, so the bar lists only the Content Areas.
  const areas = siteConfig.nav.main.filter((item) => item.href !== "/");

  return (
    <header className="bg-background sticky top-0 z-50 w-full print:hidden">
      <Container>
        {/* Phone: wordmark and search, then the areas on a row of their own. From md: one row. */}
        <div className="flex flex-wrap items-center gap-x-8 gap-y-1 py-3 md:h-16 md:flex-nowrap md:py-0">
          <Link
            href="/"
            className="hover:text-muted-foreground text-body font-semibold tracking-tight transition-colors"
          >
            {siteConfig.name}
          </Link>
          <nav
            aria-label="Main navigation"
            className="order-last -mx-2 w-full overflow-x-auto md:order-none md:mx-0 md:w-auto"
          >
            <ul className="flex items-center gap-1 md:gap-4">
              {areas.map((item) => (
                <li key={item.href} className="shrink-0">
                  <Link
                    href={item.href}
                    className={cn(
                      "hover:text-foreground text-small block px-2 py-2 font-medium whitespace-nowrap transition-colors md:px-0",
                      isActive(item.href) ? "text-foreground" : "text-muted-foreground"
                    )}
                    aria-current={isActive(item.href) ? "page" : undefined}
                  >
                    {item.name}
                  </Link>
                </li>
              ))}
            </ul>
          </nav>
          <div className="ml-auto">
            <SearchPalette />
          </div>
        </div>
      </Container>
    </header>
  );
}
