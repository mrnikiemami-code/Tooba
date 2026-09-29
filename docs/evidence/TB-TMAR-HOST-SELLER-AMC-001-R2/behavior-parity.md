# Host/Seller — Seller-R2 — Behavior Parity

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R2

## 1. Route / verb / parameter parity

| Aspect | Before R2 (Host) | After R2 (Catalog) | Verdict |
| --- | --- | --- | --- |
| Path | `/v1/seller/catalog-variants` | identical | PRESERVED |
| Path | `/v1/seller/products/{productId:guid}/attributes/{definitionId:guid}` | identical | PRESERVED |
| Path | `/v1/seller/products/{productId:guid}/variant-axes` | identical | PRESERVED |
| Verb | GET / PUT / PUT | identical | PRESERVED |
| Route params | `productId:guid`, `definitionId:guid` | identical constraints | PRESERVED |
| Group prefix | `/v1/seller` | identical | PRESERVED |

## 2. Authorization parity

| Aspect | Before R2 | After R2 | Verdict |
| --- | --- | --- | --- |
| Seller panel gate | `SellerPanelAccess.RequireAuthorizedAsync` called on every route | thin `HostCatalogSellerAuthorizer` delegates to the same `ISellerPanelAccess.RequireAuthorizedAsync` | PRESERVED |
| Actor binding inputs | request + session + guard + environment | same underlying seam, same inputs | PRESERVED |
| Return shape | `(ActorUserId, SellerPartyId)` | identical | PRESERVED |
| Unauthorized response | platform gate response | same platform gate response | PRESERVED |

## 3. Response DTO / status semantics

### GET `/v1/seller/catalog-variants`

| Aspect | Before R2 | After R2 | Verdict |
| --- | --- | --- | --- |
| Success | `200` raw JSON array of `SellerCatalogVariantOption` | identical raw JSON array (`ApiResponseFactory.From(Result<T>)` returns the raw value) | PRESERVED |
| DTO fields | `CatalogVariantId, ProductId, ProductTitle, CatalogCode, ProductStatus` | identical, identical JSON names | PRESERVED |
| Seller missing | `404` `{ title = "Seller was not found.", errorCode = "seller.missing" }` | `404` semantic error `seller.missing` with the Order-owned canonical title/localization | PRESERVED (same code + status; title comes from the canonical catalog instead of a hard-coded literal) |
| Projection | Published only; newest 100 by `UpdatedAt`; fa-first localized name; variants by `CatalogCodeSeam`; `ProductStatus.ToString()` | byte-for-byte identical logic in `SellerCatalogVariantDirectory` | PRESERVED |

### PUT attribute / PUT variant-axes

| Aspect | Before R2 | After R2 | Verdict |
| --- | --- | --- | --- |
| Success body | `{ ok = true }` | catalog command success payload (`ProductAttributeMutationOk` / `ProductVariantAxesMutationOk`) via `ApiResponseFactory` | PARITY-CLASS CHANGE — see note |
| Attribute invalid | `400` `catalog.attribute.invalid` (`InvalidOperationException` message as title) | `400` descriptor-backed stable `catalog.attribute.invalid` | PRESERVED code + status, canonical title (no message parsing) |
| Variant axes invalid | `400` `catalog.variant_axes.invalid` (Host-local string) | `400` descriptor-backed stable `catalog.variant.axes.duplicate` (canonical Catalog code) | NORMALIZED — see note |
| Null `OrderedDefinitionIds` | treated as empty list (clear axes) | `body.OrderedDefinitionIds ?? []` at the endpoint boundary => identical | PRESERVED |

**Note (ok-envelope):** the previous Host slice emitted `{ ok = true }` for the two write routes.
Those routes now dispatch the module's own canonical Catalog commands, whose success payload is the
Catalog-owned mutation acknowledgement rather than a Host-local boolean envelope. This is the
canonical Catalog contract the Admin surfaces already emit, and the task mandates
"canonical Result/SemanticError + ApiResponseFactory" for the evacuated write paths. No error code
or failure status regressed.

**Note (variant-axes code):** the Host-local string `catalog.variant_axes.invalid` was a
non-canonical duplicate of the Catalog descriptor code `catalog.variant.axes.duplicate`
(both map to HTTP 400). R2 keeps the **canonical Catalog code** and removes the Host-local
variant, as the task explicitly allows/requires when the canonical Catalog error pipeline already
supplies stable semantic errors. `catalog.attribute.invalid`, `catalog.variant.axes.duplicate` and
`seller.missing` remain descriptor-backed with unchanged statuses.

## 4. Error-code ownership parity

| Code | Owner | Duplicate registered by Catalog? |
| --- | --- | --- |
| `seller.missing` | Order descriptor catalog + `OrderErrors.resx` | NO (Catalog consumes the string via `SellerCatalogErrorCodes.SellerMissing`) |
| `catalog.attribute.invalid` | `Tooba.Catalog.Contracts.Errors.CatalogErrorCodes` | NO (already existed) |
| `catalog.variant.axes.duplicate` | `Tooba.Catalog.Contracts.Errors.CatalogErrorCodes` | NO (already existed) |

## 5. Parity risks explicitly checked

- No route added, renamed, or duplicated.
- No schema, migration, or DbContext change (`Schema-Change-State = NONE`).
- No frontend change.
- `ListCatalogVariantsQuery` is an AUTH_SCOPED_QUERY (its `SellerPartyId` is the
  authorization-derived seller), so per the accepted Offer/Settlement precedent it is
  `NO_VALIDATOR_REQUIRED` — no ceremonial validator was added.
- The reused write commands keep their W10/W11 validator classification unchanged
  (`SetProductVariantAxesCommandValidator` exists; no `SetProductAttributeCommandValidator`).
