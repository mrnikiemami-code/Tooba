# Domain Purity Audit

## Structural

- Domain → Infrastructure/Host/Application project refs: **0** (ArchitectureBoundaryTests + csproj scan)
- Direct system clock / Guid / UuidV7 hits under Modules (non-Migration): see `.tmp-tmar-clock-id.txt` (**428** raw hits across Application/Infrastructure/Domain — filter Domain carefully in Foundation task)

## Known Domain impurities to clean in later waves

- Localized FA strings in Domain exceptions (e.g. Catalog slug messages) — anti-pattern per ARCH-DOMAIN-001 proposal
- Registries/settings types in Catalog Domain that are storefront-owned (see ownership map)
- Prefer explicit `now` / id parameters over entity-injected clocks

## Valid pattern

Pure Domain methods receive `now`, IDs, and resolved policies as inputs. Do **not** inject `IClock` into every entity.
