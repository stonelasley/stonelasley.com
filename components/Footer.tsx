import Link from "next/link";
import { Container } from "./Container";
import { SocialNav } from "./SocialNav";
import { siteConfig } from "@/site.config";

export function Footer() {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="mt-auto border-t border-border bg-background">
      <Container>
        <div className="py-12 md:py-16">
          <div className="grid grid-cols-1 gap-8 md:grid-cols-3">
            <div>
              <h3 className="text-lg font-semibold mb-4">{siteConfig.name}</h3>
              <p className="text-sm text-muted-foreground max-w-xs">
                {siteConfig.description}
              </p>
            </div>
            <div>
              <h3 className="text-lg font-semibold mb-4">Navigation</h3>
              <nav aria-label="Footer navigation">
                <ul className="space-y-2">
                  {siteConfig.nav.main.map((item) => (
                    <li key={item.href}>
                      <Link
                        href={item.href}
                        className="text-sm text-muted-foreground hover:text-foreground transition-colors"
                      >
                        {item.name}
                      </Link>
                    </li>
                  ))}
                </ul>
              </nav>
            </div>
            <div>
              <h3 className="text-lg font-semibold mb-4">Connect</h3>
              <SocialNav />
            </div>
          </div>
          <div className="mt-8 pt-8 border-t border-border">
            <p className="text-sm text-muted-foreground text-center md:text-left">
              &copy; {currentYear} {siteConfig.name}. All rights reserved.
            </p>
          </div>
        </div>
      </Container>
    </footer>
  );
}
