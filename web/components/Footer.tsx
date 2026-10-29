import Link from "next/link";
import { Container } from "./Container";
import { SocialNav } from "./SocialNav";
import { siteConfig } from "@/site.config";

export function Footer() {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="border-border bg-background mt-auto border-t print:hidden">
      <Container>
        <div className="py-12 md:py-16">
          <div className="grid grid-cols-1 gap-8 md:grid-cols-3">
            <div>
              <h3 className="text-body mb-4 font-semibold">{siteConfig.name}</h3>
              <p className="text-muted-foreground text-small max-w-xs">{siteConfig.description}</p>
            </div>
            <div>
              <h3 className="text-body mb-4 font-semibold">Navigation</h3>
              <nav aria-label="Footer navigation">
                <ul className="space-y-2">
                  {siteConfig.nav.main.map((item) => (
                    <li key={item.href}>
                      <Link
                        href={item.href}
                        className="text-muted-foreground hover:text-foreground text-small transition-colors"
                      >
                        {item.name}
                      </Link>
                    </li>
                  ))}
                </ul>
              </nav>
            </div>
            <div>
              <h3 className="text-body mb-4 font-semibold">Connect</h3>
              <SocialNav />
            </div>
          </div>
          <div className="border-border mt-8 border-t pt-8">
            <p className="text-muted-foreground text-small text-center md:text-left">
              &copy; {currentYear} {siteConfig.name}. All rights reserved.
            </p>
          </div>
        </div>
      </Container>
    </footer>
  );
}
