# TB-TMAR-HOST-STOREFRONT-AMC-001-R1 — Migrate

## Scope

Evacuate Host residual **template-catalog preview** (Fashion + Industry) into Catalog CQRS / Infrastructure / Endpoints.

## Disposition executed

| Item | Before | After |
| --- | --- | --- |
| `FashionTemplatePreviewQuery.cs` | Host/Storefront | Catalog.Infrastructure/Development/TemplateCatalog + Application ports/models |
| `IndustryTemplatePreviewQuery.cs` | Host/Storefront | Catalog.Infrastructure/Development/TemplateCatalog |
| `IndustryPersistedTemplateCatalog.cs` | Host/Storefront | Catalog.Infrastructure/Development/TemplateCatalog |
| DTOs | Host nested records | Catalog.Application.TemplateCatalog.Models |
| Routes | Host `StorefrontEndpoints` | Catalog `CatalogTemplateCatalogStorefrontEndpoints` via `ISender` |
| DI | Host `Program.cs` scoped queries | CatalogModule `IFashionTemplatePreviewReader` / `IIndustryTemplatePreviewReader` |

## HTTP parity

- `GET /v1/storefront/template-catalog/fashion/preview`
- `GET /v1/storefront/template-catalog/{templateKey}/preview`
- 404 body shape preserved: `{ title: "Not Found", errorCode }`
- Codes: `template_catalog.fashion.missing`, `template_catalog.use_fashion_route`, `template_catalog.{key}.missing`

## Explicit non-goals (later waves)

- StorefrontComposer / browse BFF
- Appearance / checkout-identity / media route moves (R2)
- DemoCatalog bootstrap (R4)
- Catalog ARCH-COMPLETE-002 full certify (FOUNDATION_PARTIAL authorized)

## Guards

- `HostStorefrontAmcR1GuardTests`
- W34 template-seed guard retargeted to Catalog TemplateCatalog paths

## Host/Development

No sink into Host/Development.
