export const siteConfig = {
  name: "Stone C. Lasley",
  description: "Software leader and builder.",
  url: "https://stonelasley.com",
  author: {
    name: "Stone C. Lasley",
    email: "website@lasley.io",
    avatar: "/images/avatar.jpg", // You'll need to add this image
  },
  pagination: {
    postsPerPage: 10,
    recipesPerPage: 10,
    homeRecentPosts: 3,
  },
  social: {
    github: "https://github.com/stonelasley",
    instagram: "https://instagram.com/sclarklasley",
    whatsapp: "https://wa.link/8f2cuh",
    hevy: "https://hevy.com/users/stonelasley",
    email: "mailto:website@lasley.io",
  },
  analytics: {
    gtmContainerId: process.env.NEXT_PUBLIC_GTM_ID || "",
  },
  // Navigation links
  nav: {
    main: [
      { name: "Home", href: "/" },
      { name: "Blog", href: "/blog" },
      { name: "Recipes", href: "/recipes" },
    ],
  },
} as const;

export type SiteConfig = typeof siteConfig;
