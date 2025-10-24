"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Container } from "./Container";
import { ThemeToggle } from "./ThemeToggle";
import { SocialNav } from "./SocialNav";
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

  return (
    <header className="sticky top-0 z-50 w-full border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <Container>
        <div className="flex h-16 items-center justify-between">
          <div className="flex items-center gap-8">
            <Link
              href="/"
              className="text-xl font-bold tracking-tight hover:text-muted-foreground transition-colors"
            >
              {siteConfig.name}
            </Link>
            <nav aria-label="Main navigation" className="hidden md:block">
              <ul className="flex items-center gap-6">
                {siteConfig.nav.main.map((item) => (
                  <li key={item.href}>
                    <Link
                      href={item.href}
                      className={cn(
                        "text-sm font-medium transition-colors hover:text-foreground/80",
                        isActive(item.href)
                          ? "text-foreground"
                          : "text-muted-foreground"
                      )}
                      aria-current={isActive(item.href) ? "page" : undefined}
                    >
                      {item.name}
                    </Link>
                  </li>
                ))}
              </ul>
            </nav>
          </div>
          <div className="flex items-center gap-4">
            <div className="hidden md:block">
              <SocialNav />
            </div>
            <ThemeToggle />
          </div>
        </div>
      </Container>
    </header>
  );
}
