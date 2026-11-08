---
status: accepted
---

# A Watch without a Reference is a Make Post

An article about a make as a whole is a Watch Post with a `make` and no `reference`. It is ingested, stored and served like any Post, appears in the Watches grid, and lists the make's References. Every Reference links to its make's Make Post and to the make's other References. Those lists are built from the Watches list when a page renders, so a new Reference shows up on its siblings without touching them.

## Considered Options

- **A separate Makes area or `/watches/makes/{make}` route.** Rejected: a new route, list and filter for what is just another article in Watches.
- **Make bodies in `_makes.yaml`.** Rejected: an article belongs in a Post, with its images, drafts and audience; the makes file stays a name list (ADR 0006).

## Consequences

- A spec field without a `reference` fails ingest, so a Reference Post whose reference was forgotten can't publish as a Make Post.
- One Make Post per make, checked when a run commits across what it keeps and what stays stored, like duplicate slugs. Drafts count on every ingest that includes them, which every preview ingest does.
- No migration: the Watch is stored as JSON, and `reference` and `modelName` are null for a Make Post.
