# ADR — Story module may consume foreign Contracts only

Status: accepted
Date: 2026-11-21

## Context

Story needs Catalog product paths/titles, Party shop display names, Media authorization,
Offer fulfillment assignments and Localization locale data. The AMSC W6 guard already
states `LEGAL_CONTRACTS_ONLY` for source text, but its csproj assertion still banned the
`*Contracts` assemblies themselves. That made the guard and the implementation disagree.

## Decision

Story Infrastructure may reference only the `*.Contracts` projects of Catalog, Party,
Offer, Media and Localization. It must not reference their Application, Infrastructure
or Domain projects. Cross-module reads stay behind owning Contracts ports; Story does
not read another module's EF model or domain aggregate.

## Consequences

- The W6 guard permits `Tooba.<Module>.Contracts` and continues to reject
  `Tooba.<Module>.Application|Infrastructure|Domain` in both source and csproj.
- Any new foreign dependency needs a Contracts port and an explicit architecture review.
- This ADR does not authorize a second quote, media, party or authorization engine.
