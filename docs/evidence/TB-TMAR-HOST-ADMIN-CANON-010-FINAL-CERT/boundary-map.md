# TB-TMAR-HOST-ADMIN-CANON-010-FINAL-CERT — Boundary Map

All **15** recursive production files under `src/backend/Host/Tooba.Host/Admin`.
Every entry is classified **PLATFORM_KEEP**.

---

## Access (2 files) — `Tooba.Host.Admin.Access`

### 1. `Access/AdminPanelAccess.cs`

* **Responsibility** — canonical platform panel-authorization policy: resolves the authenticated
  actor (Bearer session, or `X-Tooba-Dev-Actor-User-Id` in Development only) and evaluates the
  `tenant#view` gate against the resolved server-side tenant.
* **Dependencies** — `Tooba.BuildingBlocks`
  (`ICurrentTenant`, `CurrentAuthenticatedSession`, `IAuthorizationGuard`, `IHostEnvironment`,
  `PlatformHttpException`, `AuthorizationCheck`).
* **Classification** — PLATFORM_KEEP
* **Certification state** — CERTIFIED: no foreign module layer, fail-closed 503 on
  `Unavailable`, 403 on deny, 401 `admin.actor.missing` when unauthenticated.

### 2. `Access/HostAdminPanelAccess.cs`

* **Responsibility** — `IAdminPanelAccess` implementation; Single-Store delegates to the tenant
  policy, Development + Marketplace uses the synthetic `marketplace-platform` tenant.
* **Dependencies** — `Tooba.BuildingBlocks`, `Tooba.BuildingBlocks.Security`.
* **Classification** — PLATFORM_KEEP
* **Certification state** — CERTIFIED: sole owner of `MarketplacePlatformTenantId`; no duplicated
  policy in any other Host/Admin file.

---

## Access/Authorizers (8 files) — `Tooba.Host.Admin.Access.Authorizers`

### 3. `HostOrderAdminAuthorizer.cs`

* **Responsibility** — Order admin Endpoints adapter: panel gate + capability check with fail-closed
  503.
* **Dependencies** — `Microsoft.AspNetCore.Http`, `Tooba.BuildingBlocks`,
  `Tooba.BuildingBlocks.Security`, `Tooba.Order.Endpoints`, `Tooba.Order.Endpoints.Errors`.
* **Boundary** — Endpoints authorizer seam + Endpoints-owned error-code seam.
* **Classification** — PLATFORM_KEEP
* **Certification state** — CERTIFIED: neutral authorization abstraction, no AccessControl, no
  service locator, distinct 403/503 codes.

### 4. `HostOrderAdminEffectiveAccessReader.cs`

* **Responsibility** — thin adapter from the neutral platform effective-access seam to the Order
  admin effective-access contract.
* **Dependencies** — `Tooba.BuildingBlocks.Security`, `Tooba.Order.Contracts.Admin.Operations`.
* **Boundary** — neutral BuildingBlocks seam + Order Contracts.
* **Classification** — PLATFORM_KEEP
* **Certification state** — CERTIFIED: `Tooba.AccessControl` = ZERO, `Tooba.Order.Application` = ZERO.

### 5. `HostPaymentAdminAuthorizer.cs`

* **Responsibility** — thin Payment admin panel-gate adapter.
* **Dependencies** — `Tooba.BuildingBlocks.Security`, `Tooba.Payment.Endpoints.Admin`.
* **Classification** — PLATFORM_KEEP
* **Certification state** — CERTIFIED: pure delegation, no tenant/authorization policy.

### 6. `HostPromotionAdminAuthorizer.cs`

* **Responsibility** — thin Promotion admin panel-gate adapter.
* **Dependencies** — `Tooba.BuildingBlocks.Security`, `Tooba.Promotion.Endpoints.Admin`.
* **Classification** — PLATFORM_KEEP
* **Certification state** — CERTIFIED: pure delegation.

### 7. `HostReturnAdminAuthorizer.cs`

* **Responsibility** — thin Returns admin panel-gate adapter.
* **Dependencies** — `Tooba.BuildingBlocks.Security`, `Tooba.Returns.Endpoints.Admin`.
* **Classification** — PLATFORM_KEEP
* **Certification state** — CERTIFIED: pure delegation.

### 8. `HostSettlementAdminAuthorizer.cs`

* **Responsibility** — thin Settlement admin panel-gate adapter.
* **Dependencies** — `Tooba.BuildingBlocks.Security`, `Tooba.Settlement.Endpoints.Admin`.
* **Classification** — PLATFORM_KEEP
* **Certification state** — CERTIFIED: no `MarketplacePlatformTenantId` duplication, no
  `AuthorizationCheck`.

### 9. `HostSupportAdminAuthorizer.cs`

* **Responsibility** — Support admin Endpoints adapter: panel gate + capability check with
  fail-closed 503.
* **Dependencies** — `Tooba.BuildingBlocks`, `Tooba.BuildingBlocks.Presentation.Errors`,
  `Tooba.BuildingBlocks.Security`, `Tooba.Support.Endpoints.Admin`.
* **Boundary** — Support Endpoints authorizer + Endpoints-owned auth-code seam; shared 403 code from
  the Foundation catalog.
* **Classification** — PLATFORM_KEEP
* **Certification state** — CERTIFIED (CANON-009): `Tooba.Support.Application` = ZERO.

### 10. `HostWalletAdminAuthorizer.cs`

* **Responsibility** — Wallet admin Endpoints adapter: panel gate + capability check with
  fail-closed 503.
* **Dependencies** — `Tooba.BuildingBlocks`, `Tooba.BuildingBlocks.Presentation.Errors`,
  `Tooba.BuildingBlocks.Security`, `Tooba.Wallet.Endpoints.Admin`.
* **Boundary** — Wallet Endpoints authorizer + Endpoints-owned auth-code seam; shared 403 code from
  the Foundation catalog.
* **Classification** — PLATFORM_KEEP
* **Certification state** — CERTIFIED (CANON-009): `Tooba.Wallet.Application` = ZERO.

---

## Panel (3 files) — `Tooba.Host.Admin.Panel`

### 11. `AdminPanelComposer.cs`

* **Responsibility** — narrow Host composition of cross-module admin surfaces (dashboard, sellers
  list, sellers grid).
* **Dependencies** — `Tooba.BuildingBlocks.Grid`, `Tooba.Catalog.Contracts`,
  `Tooba.Host.Grid`, `Tooba.Offer.Contracts.Dtos`, `Tooba.Offer.Contracts.Ports`,
  `Tooba.Order.Contracts.Admin`, `Tooba.Party.Contracts`.
* **Boundary** — Contracts-only toward every business module.
* **Classification** — PLATFORM_KEEP
* **Certification state** — CERTIFIED: no DbContext/IQueryable/ISender, no foreign
  Application/Infrastructure/Domain.

### 12. `AdminPanelEndpoints.cs`

* **Responsibility** — read-only cross-module admin routes (`/dashboard`, `/sellers`,
  `/sellers/query`, `/dev-context`) and the shared panel-gate execution wrapper.
* **Dependencies** — `Tooba.BuildingBlocks`, `Tooba.BuildingBlocks.Grid`,
  `Tooba.Host.Admin.Access`, `Tooba.Host.Admin.Development`, `Tooba.Host.Admin.Grid`.
* **Classification** — PLATFORM_KEEP
* **Certification state** — CERTIFIED: Order/Payment/Customer routes remain evacuated; no
  service locator.

### 13. `AdminPanelModels.cs`

* **Responsibility** — Host-owned admin panel DTOs (`AdminDashboardSummary`,
  `AdminSellerListItem`) plus evacuation provenance comments.
* **Dependencies** — none.
* **Classification** — PLATFORM_KEEP
* **Certification state** — CERTIFIED: no module types; migration comments are documentation only.

---

## Grid (1 file) — `Tooba.Host.Admin.Grid`

### 14. `AdminGridQueryEndpoint.cs`

* **Responsibility** — generic `POST .../query` admin grid HTTP boundary with the panel gate applied
  uniformly.
* **Dependencies** — `Tooba.BuildingBlocks`, `Tooba.BuildingBlocks.Grid`,
  `Tooba.Host.Admin.Access`.
* **Classification** — PLATFORM_KEEP
* **Certification state** — CERTIFIED: no persistence, no module coupling, no service locator.

---

## Development (1 file) — `Tooba.Host.Admin.Development`

### 15. `AdminDevActorBootstrap.cs`

* **Responsibility** — Development-only admin actor bootstrap: register/find the admin identity,
  idempotently upsert the `user → tenant#member` tuple, expose an in-memory snapshot.
* **Dependencies** — `Tooba.BuildingBlocks`, `Tooba.Identity.Contracts`,
  `Tooba.Identity.Contracts.Problems`.
* **Boundary** — Identity Contracts only; explicit `IServiceProvider provider` parameter (canonical
  repo DI composition pattern, CANON-007).
* **Classification** — PLATFORM_KEEP
* **Certification state** — CERTIFIED: `Identity.Infrastructure` = ZERO, no blanket
  `catch (InvalidOperationException)`, no `RequestServices`.

---

## Summary

| Folder | Files | Verdict |
| --- | --- | --- |
| `Access` | 2 | PLATFORM_KEEP / CERTIFIED |
| `Access/Authorizers` | 8 | PLATFORM_KEEP / CERTIFIED |
| `Panel` | 3 | PLATFORM_KEEP / CERTIFIED |
| `Grid` | 1 | PLATFORM_KEEP / CERTIFIED |
| `Development` | 1 | PLATFORM_KEEP / CERTIFIED |
| **Total** | **15** | **CANONICAL_PLATFORM_BOUNDARY_CERTIFIED** |

No file is a candidate for module evacuation. No file owns business logic.
