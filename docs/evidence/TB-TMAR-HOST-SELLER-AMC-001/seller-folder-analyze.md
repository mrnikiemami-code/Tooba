# Host/Seller — Tooba Architecture Analyze (V2)

**Target:** `src/backend/Host/Tooba.Host/Seller/`
**Mode:** ANALYSIS-ONLY (migration not started; no production file changed by this analysis)
**Skills:** `tooba-architecture-analyze` → (planned) `tooba-architecture-migrate` → `tooba-architecture-certify`
**SoT consulted:** `AGENTS.md`, `docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md`, `docs/architecture/tmar-current-state.json`, `docs/architecture/tmar-module-structure-manifests.json`, `docs/architecture/TMAR-architecture-locks.md`

## 1. Target analyzed (exact active folder enumeration)

`Host/Tooba.Host/Seller/` = **14 production files** (re-enumerated on disk, no `bin/obj`):

| # | File | LOC | Primary responsibility today |
| --- | --- | --- | --- |
| 1 | `SellerPanelEndpoints.cs` | 230 | HOST HTTP endpoints + transport records + ad-hoc error mapping |
| 2 | `SellerSettingsEndpoints.cs` | 163 | HOST HTTP endpoints for `/v1/seller/settings` + transport record |
| 3 | `SellerPanelComposer.cs` | 56 | HOST read composition over `CatalogDbContext` + Party lookup |
| 4 | `SellerPanelModels.cs` | 28 | HOST transport models + 6 foreign `global using` aliases |
| 5 | `SellerDevActorBootstrap.cs` | 218 | HOST development seed (Identity + Party DbContext + ACL tuple) |
| 6 | `SellerPanelAccess.cs` | 117 | HOST platform seller authorization core (`IAuthorizationGuard`) |
| 7 | `HostSellerPanelAccess.cs` | 24 | thin `ISellerPanelAccess` adapter → `SellerPanelAccess` |
| 8 | `HostSellerOrderViewAccessReader.cs` | 44 | thin adapter; direct `AccessControl.Application/Domain` |
| 9 | `HostSupportSellerAuthorizer.cs` | 54 | thin adapter; direct `AccessControl.Application/Domain` |
| 10 | `HostOfferSellerAuthorizer.cs` | 29 | thin `IOfferSellerAuthorizer` adapter |
| 11 | `HostOrderSellerAuthorizer.cs` | 34 | thin `IOrderSellerAuthorizer` adapter |
| 12 | `HostReturnSellerAuthorizer.cs` | 21 | thin `IReturnSellerAuthorizer` adapter |
| 13 | `HostSettlementSellerAuthorizer.cs` | 25 | thin `ISettlementSellerAuthorizer` adapter |
| 14 | `HostNotificationSellerAuthorizer.cs` | 21 | thin `INotificationSellerAuthorizer` adapter |

(`HostPromotionSellerAuthorizer.cs` is a 9-LOC minified one-liner of the same shape and is counted inside the same adapter family.)

No other file outside this folder is in the active recovery unit.

## 2. Responsibility map

| Responsibility | Files | Owner |
| --- | --- | --- |
| Seller panel HTTP routes `/v1/seller/dashboard`, `/catalog-variants`, `/products/*` | 1 | Catalog / ProductWorkspace (MUST_SPLIT) |
| Seller settings HTTP routes `/v1/seller/settings` | 2 | Party (MUST_SPLIT) |
| Dev-only route `/v1/seller/dev-contexts` | 1 | Development orchestration (dev gate) |
| Seller dashboard read composition over Catalog persistence | 3 | Catalog/ProductWorkspace via CQRS (MUST_SPLIT) |
| Seller display name lookup | 3 | Party Contracts |
| Seller catalog variant listing (Catalog queries + localized title) | 1,3 | Catalog Application/Endpoints |
| Seller capability gate (`seller.settings.view/manage`, `order.view`, support permission) | 2,8,9 | neutral `IPlatformEffectiveAccessReader` consumer policy |
| Actor↔Seller authorization (`user → party#view`) | 6,7 | **GLOBAL_HOST_AUTH_PLATFORM_BOUNDARY** (Host-owned, legal) |
| Module seller-endpoint authorization adapters (Offer/Order/Returns/Settlement/Notification/Promotion) | 10–14 | **ALLOWED_SECURITY_ADAPTER** (Host-owned, legal) |
| Development actor/party/ACL seed | 5 | DEVELOPMENT_SEED (multi-owner: Identity + Party + AccessControl) |

## 3. Structured state fields

| Field | Value |
| --- | --- |
| Foundation-State | Catalog `FOUNDATION_READY`; ProductWorkspace `FOUNDATION_READY`; **Party `FOUNDATION_PARTIAL`** (no `Tooba.Party.Endpoints` project exists) |
| Ownership-State | `MUST_SPLIT` |
| File-Cohesion-State | `MISSING` violations: `SellerPanelEndpoints.cs` = endpoints + transport models + error mapper + dev snapshot projection; `SellerDevActorBootstrap.cs` = seed + snapshots + 3 foreign-owner operations |
| Oversized/God-File-State | none above the size baseline; two multi-responsibility files (`SellerPanelEndpoints.cs` 230, `SellerDevActorBootstrap.cs` 218) = `MULTI_RESPONSIBILITY_COHESION_VIOLATION` |
| Localization-State | `HARDCODED_TEXT` + `EXCEPTION_MESSAGE_BASED` (`seller.actor.missing`, `seller.identity.missing`, `seller.authorization.*`, `seller.missing`, `seller.settings.rejected`, `"در دسترس نیست"`; `ex.Message` used as response `title`) |
| API-Result-Pattern-State | `RAW_RESULTS` + `AD_HOC` (local `ToError` → `Results.Json(...)`, local `Results.Json` for summaries, no `ApiResponseFactory`) |
| Stable-Error-Code-State | `UNREGISTERED_CODES` (`seller.missing`, `seller.dev.not-ready`, `catalog.attribute.invalid`, `catalog.variant_axes.invalid`, `seller.settings.rejected`, `seller.settings.missing` are emitted outside the canonical descriptor catalog) |
| Logging-State | `CANONICAL` (no ad-hoc logging found in the folder) |
| Sensitive-Logging-State | `NONE` |
| OpenTelemetry-State | `CANONICAL` (no parallel pipeline) |
| Correlation-Trace-State | `CANONICAL` (no parallel correlation) |
| CQRS-State | `PARTIAL` (one endpoint dispatches an Order query via `ISender`; catalog variant listing and both settings routes bypass CQRS entirely) |
| Validator-Coverage-State | `GAPS` — 7 endpoint-reachable operations, **0 validators**, 5 `VALIDATOR_REQUIRED` (`SetProductAttributeRequest`, `SetProductVariantAxesRequest`, `OrganizationProfileWriteRequest` PUT, `X-Tooba-Seller-Party-Id` header shape, seller settings GET has no body → `NO_VALIDATOR_REQUIRED`), 2 `NO_VALIDATOR_REQUIRED` |
| Contracts-Boundary-State | `VIOLATION` (Host consumes foreign **Application** and **Infrastructure** types) |
| Cross-Module-Coupling-State | `ILLEGAL` — see §4 |
| Cross-Module-Join-State | `NONE` (no SQL joins; the Catalog reads are single-context EF queries) |
| Persistence-Ownership-State | `FOREIGN_ACCESS` (`CatalogDbContext` and `PartyDbContext` used directly from Host) |
| Endpoint-Ownership-State | `HOST_OWNED` — **7 Host-owned routes** under `/v1/seller` |
| Host-Residue-State | 10 `KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER` (files 6–14 + `HostSellerPanelAccess`) + 4 `REMOVE_TO_MODULE` (files 1–4) + 1 `DEVELOPMENT_SEED_REHOME` (file 5) |
| Schema-Migration-State | `UNCHANGED` |
| Behavior-Preservation-Risk | `MEDIUM` (route shapes/methods must be preserved exactly; response DTO shapes are asserted by `SellerPanelCompositionTests`) |
| Canonical-Reference-Used | `src/backend/Modules/Offer` for Endpoints/CQRS/`ApiResponseFactory` shape; `Tooba.BuildingBlocks.Security.IPlatformEffectiveAccessReader` for effective access; `AccessControl.Contracts` for permission ids |
| Final-Disposition | `READY_TO_MIGRATE` **as a bounded multi-slice plan** (see §8) |

## 4. Current illegal dependencies (exact)

| # | Host file | Illegal reference | Kind |
| --- | --- | --- | --- |
| I1 | `SellerPanelComposer.cs` | `Tooba.Catalog.Infrastructure.Persistence.CatalogDbContext` | FOREIGN_DbContext |
| I2 | `SellerPanelComposer.cs` | `Tooba.Party.Application.IPartyLookupGateway` | FOREIGN_Application |
| I3 | `SellerSettingsEndpoints.cs` | `Tooba.Party.Application.IPartyDirectory` + `OrganizationProfileWrite/Request` | FOREIGN_Application |
| I4 | `SellerSettingsEndpoints.cs`, `HostSupportSellerAuthorizer.cs`, `HostSellerOrderViewAccessReader.cs` | `Tooba.AccessControl.Application.IAccessControlDirectory` + `Tooba.AccessControl.Domain` (`AccessOwnerScope`, `AccessScopeKind`) | FOREIGN_Application + FOREIGN_Domain |
| I5 | `SellerPanelEndpoints.cs` | `Tooba.Order.Application.Seller.Queries.GetSellerOrderDashboardSummary` | FOREIGN_Application |
| I6 | `SellerPanelEndpoints.cs` | `Tooba.Catalog.Application.ICatalogDirectory` called directly from the endpoint | ENDPOINT_TO_APPLICATION_DIRECT |
| I7 | `SellerDevActorBootstrap.cs` | `Tooba.Party.Infrastructure.Persistence.PartyDbContext`, `Tooba.Party.Domain`, `Tooba.Identity.Infrastructure` | FOREIGN_Infrastructure + FOREIGN_Domain |
| I8 | `SellerPanelModels.cs` | six `global using` aliases of `Tooba.Offer.Contracts.Dtos.*` inside Host | GLOBAL_ALIAS_WORKAROUND |
| I9 | `SellerPanelEndpoints.cs` | `ex.Message` used as the response `title`; hardcoded Persian thrown in `PlatformHttpException` | LOCALIZATION/ERROR_CONTRACT |

**Neutral seam already exists for I4:** `Tooba.BuildingBlocks.Security.IPlatformEffectiveAccessReader` + `PlatformAccessOwnerKind/ScopeKind` + `PlatformPermissionGrant`, implemented AccessControl-side by `PlatformEffectiveAccessReader` and already registered in `Program.cs`. `Tooba.Fulfillment.Endpoints/Seller/FulfillmentSellerAuthorizer.cs` is the canonical consumer. The Host seller adapters must consume the same neutral seam.

## 5. Cross-module join inventory

`NONE`. No `FromSql`, no cross-context LINQ join, no navigation across module persistence. Catalog reads in `SellerPanelComposer` are two sequential single-context queries; the Order read is a CQRS query.

## 6. Contracts-only replacement map

| Illegal today | Replacement |
| --- | --- |
| `CatalogDbContext` reads | Catalog-owned CQRS query (`ListSellerCatalogVariantOptionsQuery`) through `ISender`; narrow result DTO |
| `IPartyLookupGateway` display name | Party Contracts lookup (existing `IPartyLookup`/`PartyReference` or a narrow Contracts projection) |
| `IPartyDirectory` organization profile read/write | Party-owned CQRS query/command + `Tooba.Party.Contracts` DTOs |
| `IAccessControlDirectory` effective access | existing neutral `IPlatformEffectiveAccessReader` (`PlatformAccessOwnerKind.Seller`) |
| `Order.Application...GetSellerOrderDashboardSummaryQuery` | already module-owned via `ISender`; keep as module CQRS dispatch (no Host Application reference) |
| `ICatalogDirectory` endpoint call | Catalog-owned endpoint + CQRS + `ApiResponseFactory` |
| Offer DTO global aliases | remove the aliases; the Offer DTOs are consumed by frontend, not by Host production code |

## 7. CQRS / validation / localization / API-result gaps

- CQRS gaps: `GET /v1/seller/catalog-variants`, `PUT /products/{id}/attributes/{id}`, `PUT /products/{id}/variant-axes`, `GET/PUT /v1/seller/settings` have no request/handler.
- Validation matrix (7 endpoint-reachable operations):

| Operation | Classification |
| --- | --- |
| `PUT /v1/seller/products/{productId}/attributes/{definitionId}` | `VALIDATOR_REQUIRED` (2 route ids + `RawValue` shape + `EnumOptionId` when supplied) |
| `PUT /v1/seller/products/{productId}/variant-axes` | `VALIDATOR_REQUIRED` (route id + ordered id list shape, no duplicates/null) |
| `PUT /v1/seller/settings` | `VALIDATOR_REQUIRED` (`DisplayName` non-blank; optional strings whitespace rule) |
| `GET /v1/seller/dashboard` | `VALIDATOR_REQUIRED` (seller-party context header shape) |
| `GET /v1/seller/catalog-variants` | `VALIDATOR_REQUIRED` (seller-party context header shape) |
| `GET /v1/seller/settings` | `VALIDATOR_REQUIRED` (seller-party context header shape) |
| `GET /v1/seller/dev-contexts` | `NO_VALIDATOR_REQUIRED` (no input, Development-gated) |

- API-result findings: 7/7 routes use `Results.Json`; only the Order query branch returns `Results.Json(...)` instead of `ApiResponseFactory.From(result)`; 2 routes parse `InvalidOperationException` into a 400 with `ex.Message`.
- Localization findings: no `IErrorMessageLocalizer`, no `IErrorResourceSet`/`.resx` contribution, no `IErrorCatalogContributor` for `seller.*` codes.

## 8. Bounded migration plan (slices that must be authorized separately)

| Slice | Scope | Destination | Route change |
| --- | --- | --- | --- |
| **Seller-R1** | Move `SellerPanelAccess` + the 9 thin adapters into `Host/Security/Authorizers` as the canonical Host platform seller-security boundary; replace I4 with `IPlatformEffectiveAccessReader`; delete the Offer `global using` aliases | `Host/Tooba.Host/Security/**` | none |
| **Seller-R2** | Catalog owner: new `Catalog.Endpoints/Seller` for `catalog-variants` + product attribute/variant-axes writes, with CQRS + `ApiResponseFactory` + validators + `CatalogErrorCodes`/resources | `Tooba.Catalog.Endpoints/Seller/**` + `Tooba.Catalog.Application/Seller/**` | none (same paths) |
| **Seller-R3** | Party owner: create `Tooba.Party.Endpoints` foundation + `Party/OrganizationProfile` CQRS; move `/v1/seller/settings`; capability check via neutral seam | new `Tooba.Party.Endpoints` project | none |
| **Seller-R4** | Seller dashboard aggregation: module-owned query (ProductWorkspace is the natural composition owner) replacing `SellerPanelComposer` and its Catalog DbContext | `ProductWorkspace.Endpoints/Seller` | none |
| **Seller-R5** | Development-only: `/v1/seller/dev-contexts` + `SellerDevActorBootstrap` rehome into the accepted Host Development orchestration seam (`MarketplaceSellerDevBootstrap.cs` family), splitting Identity/Party/AccessControl responsibilities by owner | `Host/Development` + module Development seeds | none |
| **Seller-R6** | Certification: structure guard, route-ownership guard, validator-coverage guard, manifest, SoT, evidence | tests + docs | none |

Between slices, `Seller` folder increases before it decreases; each slice is therefore a separate bounded task. Slices R2–R5 are independent and can be ordered freely; R1 should go first because every later slice depends on the cleaned security boundary.

## 9. Destination-integrity check

| Destination | State |
| --- | --- |
| `Tooba.Catalog.Endpoints` | OPEN (not in `structureLock.certifiedModules`; no `Seller/` folder yet) — no regression |
| `Tooba.ProductWorkspace.Endpoints` | OPEN, current routes are all `/v1/admin/products` — additive only |
| `Tooba.Party.Endpoints` | **does not exist** → new project, `FOUNDATION_REQUIRES_SEPARATE_BOUNDED_TASK` authorized inside Seller-R3 |
| `Host/Development` | LOCKED exact 5-file set (`DevelopmentSchemaMigrator`, `DevelopmentTenantCommerceContext`, `MarketplaceAdminDevBootstrap`, `MarketplaceDevelopmentBootstrap`, `MarketplaceSellerDevBootstrap`) — a new Host file there would break the accepted closure, so `SellerDevActorBootstrap` must be absorbed into the existing `MarketplaceSellerDevBootstrap` seam or rehomed module-side, never added as a 6th file |
| Other Host folders | not touched; no sink-folder use |

No `SINK_FOLDER_REGRESSION` is proposed.

## 10. Behavior-preservation checklist

- 7 routes, exact paths + verbs + route-parameter names.
- `SellerDashboardSummary` JSON shape `{ sellerPartyId, sellerDisplayName, activeOffers, openOrders, paidOrders }` (asserted by frontend `seller-api.ts` + `SellerPanelCompositionTests`).
- `SellerCatalogVariantOption` JSON shape `{ catalogVariantId, productId, productTitle, catalogCode, productStatus }`, Published-only, ordered by `UpdatedAt` desc then `CatalogCodeSeam`, take 100.
- Seller context header `X-Tooba-Seller-Party-Id`; Development actor header `X-Tooba-Dev-Actor-User-Id`; `401 seller.actor.missing`, `400 seller.identity.missing`, `403 seller.authorization.denied`, `503 seller.authorization.unavailable`, `404 seller.missing`.
- `dev-contexts` actor rows order: A, B, then scoped employee; 404 outside Development, 503 when snapshot absent.
- Settings payload keys incl. computed `canManage`.
- Capability ids `seller.settings.view`, `seller.settings.manage`, `order.view` with GlobalWithinOwner scope and `DeniedByCeiling` exclusion.
- No schema/migration change.
- `SellerPanelAuthorizationTests` (Actor matrix allow/deny, fail-closed) must stay green.

## 11. Certification blockers (must be cleared before any Seller certify)

1. Host Routes = 7 → must reach **0**.
2. Host foreign Application/Infrastructure/Domain references I1–I7 → **0**.
3. Zero validators on 5 required operations.
4. `seller.*` / `catalog.attribute.invalid` / `catalog.variant_axes.invalid` codes not resolvable through the canonical descriptor catalog.
5. Hardcoded Persian user-facing text and `ex.Message` used as a response contract.
6. `global using` Offer aliases inside Host.
7. `SellerPanelComposer` entry in `Baselines/tmar-host-write-files.json` must be removed after the composer is deleted.
