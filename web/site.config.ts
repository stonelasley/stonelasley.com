export const siteConfig = {
  name: "Stone C. Lasley",
  description:
    "Software engineering leader in Utah. Notes on mechanical watches, recipes weighed to the gram, and training.",
  // The homepage's line under the name, and its About paragraphs. Stone approves changes to these.
  intro:
    "I run a software engineering org in Utah. I also compete in bodybuilding, collect mechanical watches, and cook to the gram.",
  about: [
    "I'm an AI leader, software builder, and lifelong optimizer. I help teams turn AI from an emerging technology into a practical advantage—adopting it thoughtfully, applying AI-assisted development effectively, and building scalable solutions that deliver meaningful results.",
    "For me, AI isn't limited to work. I use it throughout my life to train smarter, improve my coaching, optimize my health, and make better decisions. I'm constantly experimenting with ways to combine AI, data, and disciplined habits to improve how I perform, create, and live.",
    "This website is part of that experimentation. It's a live application where I test AI-driven development concepts, workflows, and approaches—including the idea of “zero-touch coding.” The application has been 100% coded by AI agents. My role has been to provide the vision, requirements, direction, and judgment while allowing the agents to handle the implementation. This approach to development is progressing quickly, and I'm still learning how to trust it, optimize it, and come to terms with what it means for the future of building software.",
    "Most importantly, I'm a husband of 20 years and the proud father of a seven-year-old boy. When I'm not building, training, or exploring new ideas, I'm spending time with my family, experimenting with meal-prep recipes, or finding the next opportunity to improve. This site is where I share what I'm learning about AI leadership, personal optimization, fitness, health, and food.",
  ],
  url: "https://stonelasley.com",
  author: {
    name: "Stone C. Lasley",
    email: "website@lasley.io",
    avatar: "/images/about/headshot.jpg",
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
