# Localization boundary — W3

## Decision

Catalog UoM uses `Tooba.Localization.Contracts.ILanguageLookup` only.

## Implementation

- `UnitOfMeasureDirectory` (Catalog.Infrastructure) injects `ILanguageLookup`.
- List language resolution (Code / UrlPrefix / default / first / `Guid.Empty`) preserved in Directory.
- Translation LanguageId validation uses `ILanguageLookup.ListAsync` → `unit.language.unknown` via `Result`.
- Catalog.Infrastructure.csproj references Localization.Contracts.
- No Catalog project references Localization.Application.
- Host `HostUnitOfMeasureLanguageGate` and Program DI registration removed.
- Application `IUnitOfMeasureLanguageGate` eliminated (not required; Contracts lookup is sufficient).

## Ownership

- Localization owns language registry semantics (via existing `LanguageLookupBridge`).
- Catalog owns UoM business outcomes and HTTP.
- Host owns zero UoM language adapter after W3.
