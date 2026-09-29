# Host/Seller — Seller-R2 — Migration

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R2
**Parent:** TB-TMAR-HOST-SELLER-AMC-001-R1B
**Skill:** `.cursor/skills/tooba-architecture-migrate/SKILL.md`

## 1. Target architecture

```
/v1/seller/catalog-variants                            -> CatalogSellerEndpoints
/v1/seller/products/{id}/attributes/{definitionId}     -> CatalogSellerEndpoints
/v1/seller/products/{id}/variant-axes                  -> CatalogSellerEndpoints
        |  ISender (MediatR 12.5) + ApiResponseFactory + ICatalogSellerAuthorizer
        v
Tooba.Catalog.Application
   Seller/Queries/ListSellerCatalogVariantsQuery + Handler   (capability: seller variant list)
   Seller/Models/SellerCatalogVariantOption                  (read model)
   Seller/Ports/ISellerCatalogVariantDirectory               (persistence seam)
   Seller/SellerCatalogErrorCodes                            (shared code constant)
   Attributes/ProductValues/Commands/SetProductAttributeCommand   (REUSED, W10 canonical)
   Variants/Commands/SetProductVariantAxesCommand                 (REUSED, W11 canonical)
        v
Tooba.Catalog.Infrastructure
   Seller/SellerCatalogVariantDirectory : ISellerCatalogVariantDirectory  (CatalogDbContext)
   CatalogModule registers the directory
```

Authorization (Host-owned platform security, unchanged boundary):

```
Catalog Endpoints -> ICatalogSellerAuthorizer  (port, Catalog-owned)
                          ^
Host/Security/Seller/HostCatalogSellerAuthorizer  (thin adapter, R1A namespace)
                          -> ISellerPanelAccess.RequireAuthorizedAsync
```

## 2. Created files

| File | Purpose |
| --- | --- |
| `Modules/Catalog/Tooba.Catalog.Application/Seller/Models/SellerCatalogVariantOption.cs` | Catalog-owned read model (moved from `Host/Seller/SellerPanelModels.cs`) |
| `Modules/Catalog/Tooba.Catalog.Application/Seller/Ports/ISellerCatalogVariantDirectory.cs` | Catalog persistence seam for the published-variant list |
| `Modules/Catalog/Tooba.Catalog.Application/Seller/Queries/ListSellerCatalogVariantsQuery.cs` | MediatR query `IRequest<Result<IReadOnlyList<SellerCatalogVariantOption>>>` |
| `Modules/Catalog/Tooba.Catalog.Application/Seller/Queries/ListSellerCatalogVariantsHandler.cs` | Handler: Party.Contracts existence guard + directory projection |
| `Modules/Catalog/Tooba.Catalog.Application/Seller/SellerCatalogErrorCodes.cs` | `SellerMissing = "seller.missing"` (consumed, not re-registered) |
| `Modules/Catalog/Tooba.Catalog.Infrastructure/Seller/SellerCatalogVariantDirectory.cs` | `CatalogDbContext` projection preserving exact query semantics |
| `Modules/Catalog/Tooba.Catalog.Endpoints/Seller/ICatalogSellerAuthorizer.cs` | Neutral Catalog seller auth port |
| `Modules/Catalog/Tooba.Catalog.Endpoints/Seller/CatalogSellerEndpoints.cs` | Three HTTP routes + transport request records |
| `Host/Tooba.Host/Security/Seller/HostCatalogSellerAuthorizer.cs` | Thin Host adapter inside the R1A boundary |

## 3. Edited files

| File | Change |
| --- | --- |
| `Host/Tooba.Host/Seller/SellerPanelEndpoints.cs` | removed the three Catalog route mappings, the two Catalog write handlers, `ICatalogDirectory` usage and both transport records. Retains only `GET /dashboard` and `GET /dev-contexts`. |
| `Host/Tooba.Host/Seller/SellerPanelComposer.cs` | removed `CatalogDbContext`, `Microsoft.EntityFrameworkCore`, `Tooba.Catalog.Domain`, `Tooba.Catalog.Infrastructure.Persistence` and `ListCatalogVariantsAsync`. 934 -> 27 LOC. Now `GetSellerDisplayAsync` only (`IPartyLookupGateway`). |
| `Host/Tooba.Host/Seller/SellerPanelModels.cs` | removed `SellerCatalogVariantOption` (dead after evacuation). |
| `Host/Tooba.Host/Program.cs` | registered the thin adapter: `ICatalogSellerAuthorizer -> HostCatalogSellerAuthorizer`. |
| `Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs` | added `app.MapCatalogSellerEndpoints();`. |
| `Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogModule.cs` | registered `ISellerCatalogVariantDirectory -> Seller.SellerCatalogVariantDirectory`. |
| `Modules/Catalog/Tooba.Catalog.Application/Tooba.Catalog.Application.csproj` | added `ProjectReference` to `Tooba.Party.Contracts` for the `IPartyLookup` existence guard. |
| `Host/Tooba.Host.Tests/Baselines/tmar-source-size-baseline.json` | removed the now-stale `SellerPanelComposer.cs` `OVERSIZED_LEGACY` entry (no loc raised, no threshold changed, no other entry touched). |

## 4. Behavior preservation technique

- The published-variant projection body was moved **verbatim** into the Infrastructure directory:
  same `Status == Published` filter, same `OrderByDescending(UpdatedAt).Take(100)`, same
  `LocalizedTexts` fa-first `FieldKey == "name"` map, same `OrderBy(CatalogCodeSeam)` variant order,
  same `ProductStatus.ToString()` value, same `string.Empty` fallback for a missing localized name.
- The two write routes now dispatch the **already canonical W10/W11 Catalog commands** instead of
  `ICatalogDirectory` methods, so the write semantics move to the module that already owned them.
- The `null` -> empty-list meaning of `OrderedDefinitionIds` is preserved at the endpoint boundary
  (`body.OrderedDefinitionIds ?? []`).
- `seller.missing` keeps its 404 + Order-owned title/localization; the Catalog write codes keep
  their descriptor-backed statuses. `ex.Message` title classification is gone.

## 5. Route ownership after R2

Host-owned seller routes = 4: `GET /v1/seller/dashboard`, `GET /v1/seller/dev-contexts`,
`GET /v1/seller/settings`, `PUT /v1/seller/settings`.
Catalog-owned seller routes = 3 (the migrated set). Duplicate ownership = 0.

## 6. Not migrated (explicitly retained)

`GET /v1/seller/dashboard` still composes the Order dashboard summary via Order CQRS from
Host/Seller (Order/Party work is out of R2 scope), and `SellerPanelComposer.GetSellerDisplayAsync`
keeps the Party display lookup used by that dashboard shell. Seller-R3..R6 remain NOT_STARTED.
