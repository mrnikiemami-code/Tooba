# CQRS read/write — W3

## Queries

| Query | Handler | Port |
|---|---|---|
| `ListUnitOfMeasuresQuery(Language?)` | `ListUnitOfMeasuresHandler` | `IUnitOfMeasureDirectory.ListAsync` |
| `GetUnitOfMeasureQuery(UnitId)` | `GetUnitOfMeasureHandler` | `IUnitOfMeasureDirectory.GetAsync` → `Result<UnitOfMeasureDetail>` |

## Commands

| Command | Handler | Port |
|---|---|---|
| `CreateUnitOfMeasureCommand` | `CreateUnitOfMeasureHandler` | `CreateAsync` → `Result<Guid>` → `UnitOfMeasureIdResult` |
| `UpdateUnitOfMeasureCommand` | `UpdateUnitOfMeasureHandler` | `UpdateAsync` → `Result<Guid>` → `UnitOfMeasureIdResult` |
| `DeactivateUnitOfMeasureCommand` | `DeactivateUnitOfMeasureHandler` | `DeactivateAsync` → `Result<UnitOfMeasureDeactivateResult>` |

## Endpoints

All five routes: `ISender` + `ApiResponseFactory.From(Result)` + `ICatalogAdminAuthorizer`. No DbContext / Infrastructure / Localization.Application in Endpoints.
