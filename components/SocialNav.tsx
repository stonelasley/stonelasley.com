import Link from "next/link";
import {
  siGithub,
  siInstagram,
  siWhatsapp,
} from "simple-icons";
import { siteConfig } from "@/site.config";

interface SocialLink {
  name: string;
  href: string;
  icon: {
    path: string;
    hex: string;
  };
  ariaLabel: string;
}

const socialLinks: SocialLink[] = [
  {
    name: "GitHub",
    href: siteConfig.social.github,
    icon: siGithub,
    ariaLabel: "Visit Stone's GitHub profile",
  },
  {
    name: "Instagram",
    href: siteConfig.social.instagram,
    icon: siInstagram,
    ariaLabel: "Visit Stone's Instagram profile",
  },
  {
    name: "WhatsApp",
    href: siteConfig.social.whatsapp,
    icon: siWhatsapp,
    ariaLabel: "Contact Stone via WhatsApp",
  },
  {
    name: "Hevy",
    href: siteConfig.social.hevy,
    icon: {
      path: "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z",
      hex: "000000",
    },
    ariaLabel: "Visit Stone's Hevy profile",
  },
  {
    name: "Email",
    href: siteConfig.social.email,
    icon: {
      path: "M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z",
      hex: "000000",
    },
    ariaLabel: "Send Stone an email",
  },
];

export function SocialNav() {
  return (
    <nav aria-label="Social media links">
      <ul className="flex items-center gap-4">
        {socialLinks.map((link) => (
          <li key={link.name}>
            <Link
              href={link.href}
              target={link.href.startsWith("http") ? "_blank" : undefined}
              rel={link.href.startsWith("http") ? "me noopener noreferrer" : undefined}
              aria-label={link.ariaLabel}
              className="text-muted-foreground hover:text-foreground transition-colors"
            >
              <svg
                role="img"
                viewBox="0 0 24 24"
                className="h-5 w-5 fill-current"
                xmlns="http://www.w3.org/2000/svg"
              >
                <title>{link.name}</title>
                <path d={link.icon.path} />
              </svg>
            </Link>
          </li>
        ))}
      </ul>
    </nav>
  );
}
