# W17 — Target architecture decision

## Selected strategy: **E — split ownership** (with new composition module as D)

One recommended target:

```text
Catalog                = Catalog write authority + Catalog-only Admin product routes (existing + brand-options + delete)
ProductWorkspace (new) = Admin ProductWorkspace aggregate composition + remaining aggregate HTTP
Host                   = DI/composition root only after evacuation (no ProductWorkspace business files)
```

Do **not** create the module in W17.

## Who owns what

| Surface | Owner |
|---|---|
| list (`GET /`) | ProductWorkspace composition module |
| grid (`POST /query` + `AdminProductGridQueryPolicy/Engine`) | ProductWorkspace composition module |
| get aggregate (`GET /{id}`) | ProductWorkspace composition module |
| brand-options | **Catalog** (Catalog-only read; Endpoints under Catalog) |
| Catalog-only writes (create/core/title/quantity/category/brand) | **Catalog** Commands; HTTP orchestration in ProductWorkspace Endpoints that call Catalog then re-Get |
| lifecycle writes | **Catalog** Commands; same post-command composition |
| delete + Offer reference check | **Catalog** Command (`Offer.Contracts` for `AnyOffers…`); HTTP **204** can be Catalog Endpoints or thin composition pass-through |
| variant create/patch | **Catalog** Commands; post-command composition in ProductWorkspace |
| Already migrated SEO/media/history/publish-readiness | **Catalog** (unchanged) |

## Who owns final HTTP route registration?

- **ProductWorkspace.Endpoints**: remaining aggregate routes that return `ProductWorkspaceView` / list / grid (preserve exact paths under `/v1/admin/products`)
- **Catalog.Endpoints**: brand-options (move), delete (recommended), plus already-owned SEO/media/history/readiness
- **Host**: zero ProductWorkspace route maps after final wave

Path coexistence is already established (Catalog and Host both map under `/v1/admin/products/{id}/…`).

## Post-write full ProductWorkspaceView preservation

**Strategy B — post-command composition in lawful dedicated composition owner:**

1. ProductWorkspace Endpoint receives request
2. Dispatches Catalog Command via Contracts/MediatR boundary (no Catalog.Application reference from ProductWorkspace.Application — use Catalog.Contracts request types or module-local orchestration through declared ports)
3. On success, composition Query rebuilds `ProductWorkspaceView` via Contracts gateways
4. Return same status codes (200/201) and shape — **no HTTP contract break**

## Allowed project references (new module — proposed, not created)

Proposed project set:

```text
Tooba.ProductWorkspace.Domain          (minimal / possibly empty — composition-first; avoid fake domain)
Tooba.ProductWorkspace.Contracts       (stable DTOs/ports/error codes if cross-module consumers need them)
Tooba.ProductWorkspace.Application     (Queries/Commands orchestration, grid policy models)
Tooba.ProductWorkspace.Infrastructure  (adapters implementing composition ports; NO foreign DbContexts)
Tooba.ProductWorkspace.Endpoints       (Admin ProductWorkspace HTTP)
Tooba.ProductWorkspace.Tests
```

Allowed Application/Infrastructure references:

| May reference | May NOT reference |
|---|---|
| Catalog.Contracts | Catalog.Application / Infrastructure / Domain |
| Offer.Contracts | Offer.Application / Infrastructure |
| Pricing.Contracts | Pricing.Application / Infrastructure |
| Inventory.Contracts | Inventory.Application / Infrastructure |
| Tax.Contracts | Tax.Application / Infrastructure |
| Party.Contracts (`IPartyLookup`) | Party.Application (`IPartyLookupGateway` debt) |
| BuildingBlocks / ModuleContracts | Any foreign DbContext |

Satisfies lock: **no A.Application → B.Application**.

## Direct CatalogDbContext evacuation

Replace Host/`ProductWorkspaceComposer` direct uses with Catalog Contracts read ports covering at least:

- Product/Variant/Attribute/MediaReference/Category link/Brand/LocalizedText/UnitOfMeasure reads for workspace + list
- Category path / assignability probes (or reuse existing Catalog query capabilities)
- Write paths already partially on `ICatalogDirectory` → migrate to Catalog MediatR Commands (Directory becomes Infrastructure adapter)

`directCatalogDbContextEvacuationRequired = true` for composition owner and for any Host residue removal.

## PageComposition suitability

**NO** — see `ownership-options.md` (home page section layout module; Domain `PageKeys.Home` / `SectionCatalog`; Application has no product/commerce Contracts).

## New module required?

**YES** — `Tooba.ProductWorkspace.*` as above. Not created in W17.

## Forbidden cross-module dependency state

Proposed target introduces **zero** forbidden Catalog→Offer/Pricing/Inventory/Tax/Party composition and **zero** Application→Application edges. Party must move from Application gateway to Contracts `IPartyLookup` at migration time.
