# Host/Seller — Seller-R1 (security boundary rehome) — Analyze

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R1
**Parent task:** TB-TMAR-HOST-SELLER-AMC-001 (`docs/evidence/TB-TMAR-HOST-SELLER-AMC-001/seller-folder-analyze.md`)
**Skills:** `tooba-architecture-analyze` → `tooba-architecture-migrate` → `tooba-architecture-certify`
**Scope:** Slice **Seller-R1** only. Routes, request/response shapes, headers, status codes and business behavior are unchanged.

## 1. Why R1 exists

The parent analyze classified every file in `src/backend/Host/Tooba.Host/Seller/` and produced a six-slice plan. R1 must be first because every later slice depends on a cleaned, canonical Host seller **security** boundary:

- The Host seller platform authorization core (`SellerPanelAccess`) and its nine thin module-endpoint authorizers are a legitimate **Host platform security boundary**, not seller business authority. They must live under the canonical Host security platform folder, not mixed into the Host seller business composition folder.
- Three of those files (`HostSellerOrderViewAccessReader`, `HostSupportSellerAuthorizer`, and `SellerSettingsEndpoints`) referenced `Tooba.AccessControl.Application` / `Tooba.AccessControl.Domain` directly. A neutral platform seam (`Tooba.BuildingBlocks.Security.IPlatformEffectiveAccessReader`) already exists, is implemented AccessControl-side by `PlatformEffectiveAccessReader`, and is already registered in `Program.cs`.
- `SellerPanelModels.cs` carried six `global using` aliases of `Tooba.Offer.Contracts.Dtos.*` inside Host (a namespace-alias workaround, forbidden by `ARCH-COMPLETE-002`).

## 2. Files in scope (10 of the 14)

| # | File (pre-R1 path) | Disposition |
| --- | --- | --- |
| 1 | `Seller/HostSellerPanelAccess.cs` | MOVE → `Security/Seller/` |
| 2 | `Seller/SellerPanelAccess.cs` | MOVE → `Security/Seller/` |
| 3 | `Seller/HostSellerOrderViewAccessReader.cs` | MOVE → `Security/Seller/` + neutral seam |
| 4 | `Seller/HostSupportSellerAuthorizer.cs` | MOVE → `Security/Seller/` + neutral seam |
| 5 | `Seller/HostOfferSellerAuthorizer.cs` | MOVE → `Security/Seller/` |
| 6 | `Seller/HostOrderSellerAuthorizer.cs` | MOVE → `Security/Seller/` |
| 7 | `Seller/HostReturnSellerAuthorizer.cs` | MOVE → `Security/Seller/` |
| 8 | `Seller/HostSettlementSellerAuthorizer.cs` | MOVE → `Security/Seller/` |
| 9 | `Seller/HostNotificationSellerAuthorizer.cs` | MOVE → `Security/Seller/` |
| 10 | `Seller/HostPromotionSellerAuthorizer.cs` | MOVE → `Security/Seller/` |

Remaining in `Host/Seller/` after R1 (deferred to R2–R6, unchanged by this slice): `SellerPanelEndpoints.cs`, `SellerSettingsEndpoints.cs`, `SellerPanelComposer.cs`, `SellerPanelModels.cs`, `SellerDevActorBootstrap.cs`.

## 3. Illegal dependencies eliminated by R1 (parent analyze §4)

| Parent ref | Illegal today | R1 resolution |
| --- | --- | --- |
| I4a | `HostSupportSellerAuthorizer` → `Tooba.AccessControl.Application.IAccessControlDirectory` + `Tooba.AccessControl.Domain` | `IPlatformEffectiveAccessReader` + `PlatformAccessOwnerKind.Seller` / `PlatformAccessScopeKind` |
| I4b | `HostSellerOrderViewAccessReader` → same AccessControl Application/Domain | same neutral seam |
| I8 | Six `global using` Offer DTO aliases in `SellerPanelModels.cs` | R1 removes the aliases from Host production — see §4 |

## 4. Accepted residual coupling in the boundary (documented, not hidden)

The nine thin adapters implement module **Endpoints** ports and therefore legitimately reference module-owned security port namespaces:

- `Tooba.Order.Application.Seller` / `Tooba.Order.Application.Seller.Ports` — `ISellerOrderViewAccessReader` port (module-owned seller view port).
- `Tooba.Support.Application.Errors` — `SupportErrorCodes.SellerAuthorizationDenied` (module-owned stable error code).
- Module `Endpoints/Seller` authorizer port namespaces (Offer/Order/Returns/Settlement/Notification/Promotion/Support).

These are **Contracts/port-level** references owned by the consuming module, not business persistence or business-rule coupling. They are explicitly allowlisted and asserted by the new durable guard. No `AccessControl.Application` / `AccessControl.Domain` / foreign DbContext reference remains in the boundary.

## 5. Behavior-preservation invariants (must hold after R1)

- 7 Host seller routes unchanged: `/v1/seller/dashboard`, `/v1/seller/catalog-variants`, `/v1/seller/products/{productId}/attributes/{definitionId}`, `/v1/seller/products/{productId}/variant-axes`, `/v1/seller/settings` (GET/PUT), `/v1/seller/dev-contexts`.
- Headers unchanged: `X-Tooba-Seller-Party-Id`, `X-Tooba-Dev-Actor-User-Id`.
- Status/error codes unchanged: `401 seller.actor.missing`, `400 seller.identity.missing`, `403 seller.authorization.denied`, `503 seller.authorization.unavailable`, `403 SupportErrorCodes.SellerAuthorizationDenied`.
- Capability semantics unchanged: GlobalWithinOwner scope, `DeniedByCeiling` exclusion, `order.view` global-vs-category projection.
- No schema/migration change. No route/verb/route-parameter change. No response DTO shape change.
