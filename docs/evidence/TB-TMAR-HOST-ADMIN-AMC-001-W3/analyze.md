# Analyze — TB-TMAR-HOST-ADMIN-AMC-001-W3

## Target

`src/backend/Host/Tooba.Host/Admin/UnitOfMeasureEndpoints.cs` (includes `HostUnitOfMeasureLanguageGate`).

## Mixed responsibilities in Host file

| Responsibility | Current location | Violation |
|---|---|---|
| HTTP routes (5) | Host Admin endpoints | Host owns Catalog Admin HTTP |
| GET list read | Direct `CatalogDbContext` + `ILanguageDirectory` | Host→Infra persistence; Host→Localization.Application |
| GET detail read | Direct `CatalogDbContext` + `PlatformHttpException` | Host→Infra; exception-as-transport |
| Create/Update/Deactivate | MediatR → Catalog Application write commands | Writes already CQRS; failures mapped via IOE/PlatformHttpException catch |
| Language gate | `HostUnitOfMeasureLanguageGate` over `ILanguageDirectory` | Host UoM business adapter bridging Catalog→Localization.Application |
| Transport DTOs | Host records | Should move with endpoints or stay Endpoints-local |
| Error mapping | `catch` + `Results.Json({title,errorCode})` | Non-canonical; message-as-code |

## Catalog legacy surface

| File | Problem |
|---|---|
| `UnitOfMeasureWriteContracts.cs` | Root mixed *Contracts bundle (port + models + commands) |
| `UnitOfMeasureWriteHandlers.cs` | Bundled handlers; returns raw Guid / tuple; no Result |
| `UnitOfMeasureDirectory.cs` | Throws `PlatformHttpException` / `InvalidOperationException` for expected outcomes; depends on `IUnitOfMeasureLanguageGate` |

## Localization boundary

- Available: `Tooba.Localization.Contracts.ILanguageLookup` + `LanguageLookupSnapshot` (bridged by `LanguageLookupBridge` in Localization.Infrastructure).
- Do NOT depend on `Tooba.Localization.Application`.
- Prefer Catalog Infrastructure consuming `ILanguageLookup` for list language resolution and translation LanguageId validation.
- Remove Host `IUnitOfMeasureLanguageGate` registration after replacement.

## Ownership target

- **Catalog**: UoM use cases, models, persistence, validation/business codes, Admin HTTP endpoints.
- **Localization**: language registry/lookup via Contracts.
- **Host**: no UoM business/language adapter after W3.

## Preserve

- Routes under `/v1/admin/catalog/units` (GET/, GET/{id}, POST/, PUT/{id}, POST/{id}/deactivate).
- Client-visible codes: `unit.missing`, `unit.dimension.invalid`, `unit.code.duplicate`, `unit.language.unknown`.
- List order SortOrder→Code; language Code/UrlPrefix match; default/first/Empty fallback; translation fallback; IsReferenced.
- W1/W2/W2-R1 Quantity + StoreAppearance deferred; no W4; no other Host folder.
