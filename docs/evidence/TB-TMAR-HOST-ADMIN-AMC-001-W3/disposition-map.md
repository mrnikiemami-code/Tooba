# Disposition map — W3 UnitOfMeasure

Member-level map for `Admin/UnitOfMeasureEndpoints.cs` (+ contained gate).

| Member / concern | Classification | Destination |
|---|---|---|
| `MapUnitOfMeasureEndpoints` + 5 routes | MOVE | `Catalog.Endpoints/Admin/Units/UnitOfMeasureEndpoints.cs` |
| `ListAsync` HTTP + auth | MOVE | Catalog Endpoints via `ICatalogAdminAuthorizer` + `ISender` |
| List DbContext reads + assembly | MOVE | `IUnitOfMeasureDirectory.ListAsync` + `ListUnitOfMeasuresQuery/Handler` |
| `ResolveLanguageIdAsync` / language fallback | MOVE | Catalog Infrastructure over `ILanguageLookup` (list path) |
| `GetAsync` HTTP + auth | MOVE | Catalog Endpoints + `GetUnitOfMeasureQuery/Handler` |
| Get missing → `unit.missing` | MOVE | Directory `Result.Failure` + `CatalogErrorCodes.UnitMissing` |
| `CreateAsync` / `UpdateAsync` / `DeactivateAsync` HTTP | MOVE | Catalog Endpoints Commands via MediatR `Result` |
| `UnitOfMeasureWriteRequest` / `UnitTranslationWrite` | MOVE | Catalog.Endpoints transport records |
| `UnitOfMeasureListItem` / `UnitOfMeasureDetail` | MOVE | Catalog.Application Models |
| `ToModel` mapping | MOVE | Endpoints → Application write model |
| `HostUnitOfMeasureLanguageGate` | REMOVE | Replaced by Infrastructure `ILanguageLookup` usage inside Directory |
| Program `AddScoped<IUnitOfMeasureLanguageGate, HostUnitOfMeasureLanguageGate>` | REMOVE | No Host gate DI |
| Program `MapUnitOfMeasureEndpoints()` | REMOVE | Covered by `MapCatalogModuleEndpoints` |
| Direct `CatalogDbContext` in Host | ELIMINATE | ZERO after move |
| Direct `Localization.Application` in Host UoM | ELIMINATE | Contracts-only via Infrastructure |
| Raw `Results.Json` expected-failure catch | ELIMINATE | `ApiResponseFactory.From(Result)` |
| `UnitOfMeasureWriteContracts.cs` | ELIMINATE | Split into Units/{Commands,Models,Ports} |
| `UnitOfMeasureWriteHandlers.cs` | ELIMINATE | Per-command handlers returning `Result` |
| `IUnitOfMeasureLanguageGate` Application interface | ELIMINATE | Not required; Directory uses `ILanguageLookup` |
| StoreAppearance* | NOT_MOVED | Deferred (W2) |

## Host/Admin inventory

| | Count |
|---|---|
| Before | 58 |
| After (delete `UnitOfMeasureEndpoints.cs`) | 57 |

## Validator classification

| Request | Classification |
|---|---|
| `ListUnitOfMeasuresQuery` | NO_VALIDATOR_REQUIRED |
| `GetUnitOfMeasureQuery` | NO_VALIDATOR_REQUIRED |
| `CreateUnitOfMeasureCommand` | VALIDATOR_REQUIRED |
| `UpdateUnitOfMeasureCommand` | VALIDATOR_REQUIRED |
| `DeactivateUnitOfMeasureCommand` | NO_VALIDATOR_REQUIRED |
