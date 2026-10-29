export const siteConfig = {
  name: "Stone C. Lasley",
  description:
    "Software engineering leader in Utah. Notes on mechanical watches, recipes weighed to the gram, and training.",
  // The homepage's line under the name, and its About paragraphs. Stone approves changes to these.
  intro:
    "I run a software engineering org in Utah. I also compete in bodybuilding, collect mechanical watches, and cook to the gram.",
  about: [
    "I'm a director of software engineering. Most of my week goes to people, planning and architecture, and I still design and write the systems myself.",
    "Outside work I compete in open-class bodybuilding, which is why the recipes here are high in protein and every ingredient is weighed. The watch notes started with the two 116520 Daytonas I wear, one black dial and one white, and have grown one reference at a time.",
  ],
  url: "https://stonelasley.com",
  author: {
    name: "Stone C. Lasley",
    email: "website@lasley.io",
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
