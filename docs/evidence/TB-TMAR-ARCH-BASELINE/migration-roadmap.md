# Migration Roadmap — NO BIG BANG

| Wave | Objective | Prereq | Risk | Effort | Exit |
|---|---|---|---|---|---|
| 1 Architecture Foundation | MediatR 12.5.0, FV, IClock, IIdGenerator, pipelines, Host freeze tests, error-code foundation | Baseline ACCEPT | M | M | guards green; no mass Directory rewrite |
| 2 Host Write Removal | Move dangerous Host writes to modules | Wave 1 | H | L | Host write allowlist shrinks |
| 3 CQRS Adoption | Stage A/B strangler on touched paths | Wave 1 | M | L | new use-cases via Handlers |
| 4 Contracts Extraction | Pricing/Inventory/Offer/Catalog Contracts | Wave 1 | M | L | App→App edges decline |
| 5 Domain Ownership Corrections | Move Catalog storefront/settings/template types | Waves 2–4 | H | L | ownership registry updated |
| 6 Read Gateway Migration | Replace multi-DbContext composers | Wave 4 | M | L | composers use gateways |
| 7 Error/Locale Standardization | ErrorCode + ProblemDetails + locale policy | Wave 1 | M | M | Domain FA strings gone on touched paths |
| 8 Cache Adoption Cleanup | IMemoryCache → ICache | Wave 1 | S | S | no Host IMemoryCache bypass |
| 9 Physical Folder Reorg | Modules/*/Contracts layout | ownership+deps repaired | M | M | no behavior change |
| 10 Microservice Readiness Verify | boundary checklist | Waves 4–6 | M | M | extraction candidates documented |

NEW code follows target after Wave 1 ACCEPT. OLD migrates when touched / high-risk / extraction-critical.
