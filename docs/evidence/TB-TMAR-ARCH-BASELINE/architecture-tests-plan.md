# Architecture Tests / CI Guard Design

Extend `ArchitectureBoundaryTests` + new TMAR guards:

- Domain ↛ Infra/Host/foreign Application (exists)
- Infra ↛ foreign Infra (exists)
- no global business DbContext (exists)
- NEW: Host write allowlist freeze
- NEW: App↛foreign App after Contracts gate
- NEW: MediatR required for NEW use-cases (convention/test)
- NEW: forbid Domain localized exception literals (heuristic)
- NEW: forbid UtcNow/Guid.NewGuid in Domain after gate
- NEW: forbid IMemoryCache outside provider
- NEW: ownership registry namespace consistency

Semantic BC ownership ≠ fully automatic — registry + review.
