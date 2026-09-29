# Host/Seller — Seller-R2 — Analyze

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R2
**Parent:** TB-TMAR-HOST-SELLER-AMC-001-R1B
**Skill:** `.cursor/skills/tooba-architecture-analyze/SKILL.md`
**Mode:** BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
**Track:** SELLER_R2_CATALOG

## 1. Scope fixed by the task

Exactly three Host-owned seller **Catalog** routes and exactly the Catalog-owned portion of
`Host/Seller/SellerPanelComposer.cs`:

| # | Route | Verb | Host handler before R2 |
| --- | --- | --- | --- |
| 1 | `/v1/seller/catalog-variants` | GET | `ListCatalogVariantsAsync` -> `SellerPanelComposer.ListCatalogVariantsAsync` |
| 2 | `/v1/seller/products/{productId:guid}/attributes/{definitionId:guid}` | PUT | `SetProductAttributeAsync` -> `ICatalogDirectory.SetProductAttributeAsync` |
| 3 | `/v1/seller/products/{productId:guid}/variant-axes` | PUT | `SetProductVariantAxesAsync` -> `ICatalogDirectory.SetProductVariantAxesAsync` |

Catalog representation inside `Host/Seller`:

- `SellerPanelComposer.cs` primary constructor dependency `CatalogDbContext catalog`
  (with `using Microsoft.EntityFrameworkCore;` and `Tooba.Catalog.Infrastructure.Persistence;`)
- `using Tooba.Catalog.Domain;` for `CatalogPublicationStatus` / `CatalogLocalizedOwnerKind`
- `ListCatalogVariantsAsync` (the whole published-variant projection body, ~25 LOC)
- `SellerPanelModels.cs` `SellerCatalogVariantOption` record (Host-owned DTO)
- `SellerPanelEndpoints.cs` injected `ICatalogDirectory catalog` on the two write routes

## 2. Ownership determination

| Capability | True owner | Evidence |
| --- | --- | --- |
| Published Catalog variant projection (`Products`/`Variants`/`LocalizedTexts`) | **Catalog** | all three tables are `Tooba.Catalog.Domain` aggregates persisted by `CatalogDbContext` |
| Product attribute value write (RawValue + EnumOptionId) | **Catalog** | the existing canonical handler is `Tooba.Catalog.Application.Attributes.ProductValues.Commands.SetProductAttributeCommand` (already reachable from `Catalog.Endpoints` Admin) |
| Product variant-axes write | **Catalog** | the existing canonical handler is `Tooba.Catalog.Application.Variants.Commands.SetProductVariantAxesCommand` (already reachable from `Catalog.Endpoints` Admin) |
| `SellerCatalogVariantOption` DTO | **Catalog** | it is the read model of capability #1; no Host business consumer remains after the route evacuates |
| Seller **platform auth** (actor binding, SpiceDB/authorization engine, dev-actor header) | **Host** | `Host/Security/Seller` R1A canonical boundary, not a Catalog concern |

Conclusion: everything in scope is Catalog business/persistence authority inside Host — a
category-A Host residue. Seller platform security must stay Host-owned and must **not** be
copied into Catalog.

## 3. Defect inventory found before migration

| Defect | Severity | R2 action |
| --- | --- | --- |
| `Host/Seller/SellerPanelComposer.cs` holds a direct `CatalogDbContext` | TMAR boundary violation (foreign persistence composition in Host) | evacuated |
| `Host/Seller/SellerPanelComposer.cs` imports `Tooba.Catalog.Domain` | foreign layer access from Host | removed |
| `Host/Seller/SellerPanelEndpoints.cs` injects `ICatalogDirectory` (Catalog `Application`) on two write routes | Host -> foreign Application dependency | replaced by Catalog-owned `ISender` + `SetProductAttributeCommand` / `SetProductVariantAxesCommand` |
| `SellerPanelModels.cs` owns `SellerCatalogVariantOption` | Host ownership of a Catalog read model | moved to `Tooba.Catalog.Application.Seller.Models` |
| Write routes classify failures via `catch (InvalidOperationException ex)` + `ex.Message` as HTTP title | second, message-parsing error system; `catalog.variant_axes.invalid` (de-facto code) diverges from the Catalog descriptor `catalog.variant.axes.duplicate` | removed; canonical `Result`/`SemanticError` + `ApiResponseFactory` used |
| `SellerPanelComposer.cs` is a 934-LOC `OVERSIZED_LEGACY` frozen baseline entry | structural debt frozen in the size baseline | shrunk to 27 LOC; baseline entry removed |
| Seller Catalog routes had no dedicated durable architecture guard | guard gap | `HostSellerAmcR2GuardTests` added |

## 4. Auth seam analysis

`Host/Seller` used `SellerPanelAccess.RequireAuthorizedAsync(request, session, guard, environment, ct)`
directly and returned `(ActorUserId, SellerPartyId)`. Catalog must not consume Host types or
the platform authorization engine. The minimal neutral seam is a Catalog-owned port:

```
Tooba.Catalog.Endpoints.Seller.ICatalogSellerAuthorizer
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(HttpContext, CancellationToken)
```

implemented by the thin Host adapter `Tooba.Host.Security.Seller.HostCatalogSellerAuthorizer`
delegating to the existing R1A seam `ISellerPanelAccess`. This mirrors the already accepted
`ICatalogAdminAuthorizer` / `HostCatalogAdminAuthorizer` pattern in the same module.

## 5. Cross-module analysis

- Catalog must not query Party persistence from Catalog handlers. The seller-existence
  precondition is satisfied through the existing `Tooba.Party.Contracts.IPartyLookup` port
  (Contracts-only), not a cross-module DbContext join.
- The `seller.missing` machine code is a **shared** code whose canonical descriptor and
  localization are owned by Order (`SellerOrderErrors.SellerMissing`, `OrderErrors.resx`). Catalog
  must **consume the same string** and must not register a duplicate descriptor
  (`ErrorDefinitionCatalog` fails fast on duplicate registration).
- Catalog keeps its own stable write codes `catalog.attribute.invalid` and
  `catalog.variant.axes.duplicate` from `Tooba.Catalog.Contracts.Errors.CatalogErrorCodes`.

## 6. Out of scope (explicitly not touched)

`GET /v1/seller/dashboard`, `GET /v1/seller/dev-contexts`, `SellerSettingsEndpoints.cs`
(settings GET/PUT), `SellerDevActorBootstrap.cs`, Order dashboard query, Party/settings work,
frontend, Seller-R3..R6.
