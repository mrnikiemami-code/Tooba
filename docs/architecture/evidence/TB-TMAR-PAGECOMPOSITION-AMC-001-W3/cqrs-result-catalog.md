# TB-TMAR-PAGECOMPOSITION-AMC-001-W3 — CQRS / Result / catalog

## Outcome

Storefront home GET + Admin home/catalog/mutations dispatch through MediatR `ISender` → Application handlers → `PageCompositionOperation` (`SemanticException` → `Result`) → `ApiResponseFactory.From` / `Created`. Zero `Results.Json` in endpoints. Zero endpoint `catch (SemanticException)`. Zero `message.Contains` failure mapping.

## Inventory

| Request | Validator | Classification |
|---|---|---|
| `GetHomeCompositionQuery` | `GetHomeCompositionQueryValidator` | VALIDATOR_REQUIRED_PRESENT |
| `GetSectionCatalogQuery` | `GetSectionCatalogQueryValidator` | VALIDATOR_REQUIRED_PRESENT (marker / no transport fields) |
| `AdminGetHomeCompositionQuery` | `AdminGetHomeCompositionQueryValidator` | VALIDATOR_REQUIRED_PRESENT |
| `AdminReorderHomeSectionsCommand` | `AdminReorderHomeSectionsCommandValidator` | VALIDATOR_REQUIRED_PRESENT |
| `AdminUpdateHomeSectionCommand` | `AdminUpdateHomeSectionCommandValidator` | VALIDATOR_REQUIRED_PRESENT |
| `AdminAddHomeSectionCommand` | `AdminAddHomeSectionCommandValidator` | VALIDATOR_REQUIRED_PRESENT |
| `AdminRemoveHomeSectionCommand` | `AdminRemoveHomeSectionCommandValidator` | VALIDATOR_REQUIRED_PRESENT |
| `AdminRestoreDefaultHomeCompositionCommand` | `AdminRestoreDefaultHomeCompositionCommandValidator` | VALIDATOR_REQUIRED_PRESENT |

## Error ownership

- Catalog/resource set/resx: `Tooba.PageComposition.Contracts`
- Registration: `PageCompositionModule` (Infrastructure)
- Endpoints Errors/Resources: ABSENT
- Domain throws typed `SemanticException` (no Persian message parse)

## Coupling

Zero foreign Application/Infrastructure/Domain. Module remains self-contained for microservice extract.
