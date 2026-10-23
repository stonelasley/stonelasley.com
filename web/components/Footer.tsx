import Link from "next/link";
import { Container } from "./Container";
import { SocialNav } from "./SocialNav";
import { siteConfig } from "@/site.config";

export function Footer() {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="border-border bg-background mt-auto border-t">
      <Container>
        <div className="py-12 md:py-16">
          <div className="grid grid-cols-1 gap-8 md:grid-cols-3">
            <div>
              <h3 className="mb-4 text-lg font-semibold">{siteConfig.name}</h3>
              <p className="text-muted-foreground max-w-xs text-sm">{siteConfig.description}</p>
            </div>
            <div>
              <h3 className="mb-4 text-lg font-semibold">Navigation</h3>
              <nav aria-label="Footer navigation">
                <ul className="space-y-2">
                  {siteConfig.nav.main.map((item) => (
                    <li key={item.href}>
                      <Link
                        href={item.href}
                        className="text-muted-foreground hover:text-foreground text-sm transition-colors"
                      >
                        {item.name}
                      </Link>
                    </li>
                  ))}
                </ul>
              </nav>
            </div>
            <div>
              <h3 className="mb-4 text-lg font-semibold">Connect</h3>
              <SocialNav />
            </div>
          </div>
          <div className="border-border mt-8 border-t pt-8">
            <p className="text-muted-foreground text-center text-sm md:text-left">
              &copy; {currentYear} {siteConfig.name}. All rights reserved.
            </p>
          </div>
        </div>
      </Container>
    </footer>
  );
}
