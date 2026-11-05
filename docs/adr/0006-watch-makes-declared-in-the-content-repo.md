---
status: accepted
---

# Watch makes are declared in the content repo

A Watch Post's `make` is checked against `_makes.yaml` in the content repo, a list of makes with each one's name and any aliases. Ingest matches the Post's make to a name or alias, ignoring case, and stores the name. It replaces a closed `WatchMake` enum, which needed an API change, a PR and a deploy for every new make. Nothing in the code behaves differently by make, so the list is content, and content belongs in the content repo (ADR 0002).

## Considered Options

- **Keep the enum.** Rejected: a code change per make, and removing one would make stored Posts that name it unreadable.
- **Accept any make, and reject only near-matches of known ones.** Rejected: a typo that isn't close to an existing make becomes a permanent new make on the Watches filter.

## Consequences

- A new make goes into `_makes.yaml` in the same content PR as the first Post that names it. An unknown make fails the PR's dry-run ingest.
- Like `_clients.yaml`, the file replaces the stored makes on each ingest that sends it, and an ingest without it uses the stored ones. Migration 0014 seeds the stored makes with the enum's names, so the order of deploying the API and adding the file doesn't matter.
- Aliases absorb spellings such as "Favre-Leuba" for Favre Leuba; case differences need no alias.
- Renaming a make doesn't rewrite stored Posts. They change only when re-ingested.
