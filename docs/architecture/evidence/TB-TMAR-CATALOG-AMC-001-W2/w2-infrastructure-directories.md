# TB-TMAR-CATALOG-AMC-001-W2 — Infrastructure Directories

## Changes

- Moved 33 Infrastructure root directory/gateway/reader/workspace files into `Directories/` with namespace `Tooba.Catalog.Infrastructure.Directories`
- Moved `CatalogOutboxRegistration.cs` into `Outbox/`
- Infrastructure root allowlist: `CatalogModule.cs` + GlobalUsings + `.csproj`
- Updated 27 Host.Tests architecture guard path strings to `Directories/`
- GlobalUsings added for Directories/Outbox

## Remaining

- `CatalogDirectory.cs` remains a large single-responsibility facade (~1697 LOC) under `Directories/` — cohesion classification `OVERSIZED_ONLY` for W3/W4
- Canonical API/Result/validator work deferred to W3

## Build

- Catalog.Infrastructure + Host build PASS
