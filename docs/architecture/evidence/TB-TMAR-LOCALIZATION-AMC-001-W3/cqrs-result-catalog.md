# TB-TMAR-LOCALIZATION-AMC-001-W3 — CQRS / Result / catalog

## Outcome

Localization Admin language registry now dispatches through MediatR `ISender` → Application handlers → `LocalizationOperation` (`SemanticException` → `Result`) → `ApiResponseFactory.From`. Endpoints no longer call `ILanguageDirectory` or `Results.Json`.

## Inventory

| Request | Validator | Classification |
|---|---|---|
| `ListLanguagesAdminQuery` | `ListLanguagesAdminQueryValidator` | VALIDATOR_REQUIRED_PRESENT |
| `CreateLanguageCommand` | `CreateLanguageCommandValidator` | VALIDATOR_REQUIRED_PRESENT |
| `UpdateLanguageCommand` | `UpdateLanguageCommandValidator` | VALIDATOR_REQUIRED_PRESENT |
| `PatchLanguageCommand` | `PatchLanguageCommandValidator` | VALIDATOR_REQUIRED_PRESENT |

## Error ownership

- Catalog: `Tooba.Localization.Contracts.Errors.LocalizationErrorCatalogContributor`
- Resource set: `LocalizationErrorResourceSet` + `LocalizationErrors.resx` / `.fa.resx`
- Registration: `LocalizationModule` (Infrastructure)

## Host seam

`Program.AddToobaCqrsFoundation` includes `Tooba.Localization.Application` via `CreateLanguageCommand` assembly marker.

## Coupling

Zero foreign Application/Infrastructure/Domain references. Cross-module consumers remain on `Tooba.Localization.Contracts.Ports` only.
