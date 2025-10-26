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
    <header className="border-border bg-background/95 supports-[backdrop-filter]:bg-background/60 sticky top-0 z-50 w-full border-b backdrop-blur">
      <Container>
        <div className="flex h-16 items-center justify-between">
          <div className="flex items-center gap-8">
            <Link
              href="/"
              className="hover:text-muted-foreground text-xl font-bold tracking-tight transition-colors"
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
                        "hover:text-foreground/80 text-sm font-medium transition-colors",
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
