<!--
Sync Impact Report:
- Version: 0.0.0 → 1.0.0 (Initial constitution creation)
- Modified Principles: N/A (initial creation)
- Added Sections: All sections (Core Principles, Architecture Constraints, Quality Standards, Governance)
- Removed Sections: None
- Templates Requiring Updates:
  ✅ .specify/templates/plan-template.md - Constitution Check section updated with all 7 principles
  ✅ .specify/templates/spec-template.md - Added Constitution Compliance section to Success Criteria
  ✅ .specify/templates/tasks-template.md - Enhanced Polish phase with constitution compliance checks
  ✅ .specify/templates/checklist-template.md - Reviewed, no changes needed (generic template)
  ✅ .specify/templates/agent-file-template.md - Reviewed, no changes needed (generic template)
- Follow-up TODOs: None
-->

# stonelasley.com Constitution

## Core Principles

### I. Static-First Architecture

Every page MUST be pre-rendered at build time using Next.js static generation. Runtime data fetching is prohibited for core content (blog posts, recipes, pages).

**Rationale**: Static generation ensures optimal performance (LCP < 2.0s), eliminates runtime dependencies on external APIs (Notion), and provides resilience against API failures or rate limits.

**Rules**:
- All routes MUST use `generateStaticParams()` for dynamic segments
- Content MUST be compiled from JSON files at build time
- Client-side data fetching is ONLY permitted for non-content features (analytics, theme preference)
- Notion API calls MUST occur only during build, never at runtime

### II. Type Safety First

All data structures MUST be validated using Zod schemas before use. TypeScript strict mode is mandatory.

**Rationale**: Type safety prevents runtime errors, ensures data integrity, and provides self-documenting code. Zod validation catches content schema violations at build time before deployment.

**Rules**:
- Every JSON content file MUST pass Zod schema validation
- TypeScript strict mode MUST be enabled
- Build MUST fail on type errors or schema validation failures
- No `any` types except when interfacing with untyped third-party libraries (must be documented)

### III. Performance by Default

All features MUST meet or exceed performance targets: LCP < 2.0s, CLS < 0.05, TBT < 200ms, Lighthouse 90+ across all categories.

**Rationale**: Performance is a feature, not an optimization. A fast site improves user experience, SEO rankings, and accessibility for users on slow connections.

**Rules**:
- Minimal client-side JavaScript (server components by default)
- Images MUST use Next.js Image component with proper sizing
- Code syntax highlighting MUST be pre-rendered at build time (Shiki)
- Bundle size increases require justification and performance impact analysis

### IV. Accessibility is Non-Negotiable

All features MUST meet WCAG 2.2 Level AA standards.

**Rationale**: Accessibility is a fundamental right, not a nice-to-have. It ensures the site is usable by everyone, including users with disabilities.

**Rules**:
- Semantic HTML with proper heading hierarchy
- Keyboard navigation fully supported
- Sufficient color contrast (4.5:1 for normal text, 3:1 for large text)
- All images MUST have meaningful alt text
- `aria-label` required for icon-only links
- `aria-current` required for active navigation states
- Focus indicators MUST be visible

### V. Content as Code

All content MUST be versioned, validated, and deployable through the same pipeline as code.

**Rationale**: Treating content as code enables version control, rollback capability, schema enforcement, and prevents content-related production failures.

**Rules**:
- Content stored as JSON files in `content/` directory
- All content validated against schemas in `lib/schemas.ts`
- Notion acts as CMS but is NOT a production dependency
- Content changes go through git workflow (commit, review, deploy)
- Build MUST fail on invalid content before deployment

### VI. Progressive Enhancement

Features MUST work without JavaScript where possible. Client-side enhancement is additive, not required.

**Rationale**: Progressive enhancement ensures core functionality works for all users, regardless of JavaScript availability or network conditions.

**Rules**:
- Server components by default
- Client components only when interactivity required (`'use client'` directive)
- Theme toggle degrades gracefully to system preference
- Navigation MUST work with disabled JavaScript
- Forms MUST work with server actions where applicable

### VII. Simplicity Over Complexity

Choose the simplest solution that meets requirements. Avoid premature optimization and over-engineering.

**Rationale**: Simple code is easier to maintain, debug, and reason about. Complexity should be justified by measurable value.

**Rules**:
- Prefer zero-dependency solutions when feasible
- No abstractions until third use case identified
- New dependencies require justification (document in CLAUDE.md if core)
- Delete unused code immediately (no commented-out code)
- YAGNI (You Aren't Gonna Need It) principle applies

## Architecture Constraints

### Technology Stack (Fixed)

**Framework**: Next.js 16 App Router + React 19
**Language**: TypeScript 5 (strict mode)
**Styling**: Tailwind CSS 4 with CSS variables for theming
**Validation**: Zod 4.x for schema validation
**Testing**: Vitest (unit), Playwright (e2e)
**CMS**: Notion (headless, build-time only)

**Rationale**: This stack balances modern best practices with stability and performance. Changes require architectural justification.

### Content Management (Notion Integration)

- Notion is a headless CMS for authoring only
- Content fetched via `npm run fetch-content` or `prebuild` hook
- Images downloaded locally to `public/images/notion/`
- Notion is NEVER a production runtime dependency
- All content validated against Zod schemas after fetch

**Rationale**: Decouples authoring (Notion) from production (static JSON), preventing API rate limits, downtime, or content schema drift from affecting live site.

### Directory Structure (Mandatory)

```
app/                    # Next.js App Router pages
components/             # React components
content/               # Static JSON (blog/, recipes/)
lib/                   # Utilities, schemas, types
public/images/notion/  # Downloaded images
scripts/               # Build scripts (fetch-notion-content.js)
site.config.ts         # Central configuration
```

**Rationale**: Clear separation of concerns: presentation (app/components), data (content), business logic (lib), and tooling (scripts).

### Routing Conventions

- Dynamic routes: `/blog/[slug]`, `/recipes/[slug]`
- All dynamic params generated statically via `generateStaticParams()`
- Query params permitted for filtering (e.g., `/blog?tag=typescript`)
- No runtime route generation

## Quality Standards

### Code Quality

- **Linting**: ESLint with Next.js config, no warnings allowed
- **Formatting**: Prettier with Tailwind plugin, enforced pre-commit
- **Type Checking**: `npm run type-check` must pass before commit
- **Testing**: Unit tests for utilities/logic, E2E tests for critical user flows

### Content Quality

- **Schema Compliance**: All JSON validated against Zod schemas
- **SEO Metadata**: Every page MUST have title, description, OpenGraph tags
- **Structured Data**: JSON-LD for articles (blog posts)
- **Feeds**: RSS and JSON feeds auto-generated from content

### Performance Quality

- **Build Validation**: Lighthouse CI (if configured) must pass 90+ scores
- **Image Optimization**: All images optimized via Next.js Image component
- **Bundle Monitoring**: Check bundle size on significant dependency additions
- **Runtime Monitoring**: Analytics (GTM) optional but non-blocking

### Accessibility Quality

- **Automated Testing**: axe DevTools or similar in E2E tests
- **Manual Testing**: Keyboard navigation verified for new interactive features
- **Color Contrast**: Design system CSS variables meet WCAG AA ratios
- **Screen Reader**: Test with screen reader for complex interactions

## Governance

### Amendment Process

1. **Proposal**: Document proposed change and rationale in GitHub issue or discussion
2. **Review**: Review impact on existing templates (plan.md, spec.md, tasks.md)
3. **Approval**: Maintainer approval required (Stone C. Lasley for this project)
4. **Migration**: Update constitution, increment version, sync dependent templates
5. **Documentation**: Update CLAUDE.md if architectural principles change

### Versioning Policy

- **MAJOR** (X.0.0): Backward-incompatible principle removals or redefinitions (e.g., removing Type Safety First)
- **MINOR** (x.Y.0): New principle added or materially expanded guidance (e.g., adding Security principle)
- **PATCH** (x.y.Z): Clarifications, wording improvements, typo fixes, non-semantic refinements

### Compliance Review

- All pull requests MUST verify compliance with constitution principles
- Plan templates (`plan.md`) MUST include Constitution Check gate
- Complexity MUST be justified in plan if violating simplicity principle
- Breaking changes to architecture constraints require constitution amendment

### Runtime Guidance

For day-to-day development guidance beyond constitutional principles, refer to:
- **CLAUDE.md**: Detailed project context, common tasks, gotchas
- **README.md**: Setup instructions, available scripts, deployment
- **site.config.ts**: Site-specific configuration values

**Version**: 1.0.0 | **Ratified**: 2025-12-04 | **Last Amended**: 2025-12-04
