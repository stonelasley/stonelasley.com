export const siteConfig = {
  name: "Stone C. Lasley",
  description: "Software leader and builder.",
  url: "https://stonelasley.com",
  author: {
    name: "Stone C. Lasley",
    email: "website@lasley.io",
    avatar: "/images/avatar.jpg", // You'll need to add this image
  },
  home: {
    recentPosts: 3,
  },
  social: {
    github: "https://github.com/stonelasley",
    instagram: "https://instagram.com/stoneclasley",
    whatsapp: "https://wa.link/8f2cuh",
    hevy: "https://hevy.com/user/stonelasley",
    email: "mailto:website@lasley.io",
  },
  // Navigation links
  nav: {
    main: [
      { name: "Home", href: "/" },
      { name: "Recipes", href: "/recipes" },
      { name: "Watches", href: "/watches" },
      { name: "AI & Development", href: "/ai-development" },
      { name: "Training", href: "/training" },
    ],
  },
} as const;

export type SiteConfig = typeof siteConfig;
