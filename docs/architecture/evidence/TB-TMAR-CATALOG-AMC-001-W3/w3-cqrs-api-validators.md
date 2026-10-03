# TB-TMAR-CATALOG-AMC-001-W3 — CQRS / API / validators

## API result

- Storefront Browse / Settings / TemplateCatalog converted to `ApiResponseFactory.From`
- ProductMedia attach/placeholder uses `ApiResponseFactory.Created`
- CatalogDemo remains DEVELOPMENT_ONLY with `Results.Problem` (excluded from production endpoint guard)

## Error ownership

- `CatalogErrorCatalogContributor` + `CatalogErrorResourceSet` + resx moved to `Tooba.Catalog.Contracts`
- Registered in `CatalogModule` (Infrastructure)
- Endpoints `Errors/` + `Resources/` removed

## Validators

- Endpoint-reachable MediatR requests: **132**
- `VALIDATOR_REQUIRED_PRESENT`: **53**
- `NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT`: **79**
- Added 18 thin Admin/update validators
- Stripped hard-coded Persian `WithMessage` from HoldPolicy validator

## Durable guard

`CatalogModuleAmcW3CqrsGuardTests` — 4/4 PASS
