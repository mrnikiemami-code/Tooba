# Analyze — TB-TMAR-HOST-ADMIN-PANEL-AMC-001

```text
ANALYSIS_ONLY_NOT_MIGRATED_NOT_CERTIFIED
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Skill: tooba-architecture-analyze
Active folder: src/backend/Host/Tooba.Host/Admin/Panel/
```

## Folder enumeration

### Start

| Path | Bytes (approx) |
|------|----------------|
| AdminPanelComposer.cs | present |
| AdminPanelEndpoints.cs | present |
| AdminPanelModels.cs | present |

**Initial production file count: 3**

### End (re-enumerated before Result)

Same three files. No adds/deletes during analysis.

**Final production file count: 3**

## Executive disposition (decisive)

Host/Admin/Panel is a **mixed** surface:

| Responsibility | Category | Disposition |
|----------------|----------|-------------|
| Dashboard aggregation (Catalog+Offer+Order counters) | PRESENTATION_COMPOSITION / CROSS_MODULE_ORCHESTRATION | **KEEP_AS_THIN_HOST_CROSS_MODULE_COMPOSITION** |
| `AdminDashboardSummary` DTO | CONTRACT (presentation response) | **KEEP_WITH_HOST_PANEL** (semantic consumer = Host composition response) |
| Sellers list composition (Offer+Party+Order) | CROSS_MODULE_ORCHESTRATION | **MOVE → Party** (Party already owns `IAdminSellersGridPort` / `AdminSellerListItem`) |
| Sellers grid query delegation | HTTP_ENDPOINT → Party Contracts port | **MOVE → Party.Endpoints** |
| GET `/v1/admin/dev-context` | DEVELOPMENT_ONLY_RUNTIME | **MUST_SPLIT out of Panel**; destination **NEEDS_ARCHITECT_DECISION** (Admin/Development unopened; Host/Development allowlist locked) |
| Endpoint auth via `AdminPanelAccess` | AUTHORIZATION_ADAPTER (Host/Admin/Access) | **KEEP in Access**; Panel must not reopen Access; future call via `IAdminPanelAccess` preferred |
| `AdminGridQueryEndpoint` usage | Host/Admin/Grid helper | **Classify only**; do not open Grid; sellers-query migration removes Panel dependency |
| Raw `Results.Json` / local `ToError` / `PlatformHttpException` catch | NON_CANONICAL API mapping | **HYGIENE required** on retained Host routes before CERTIFY |
| Hard-coded `"Not Found"` on dev-context 404 | NON_CANONICAL localization | **REMOVE** with canonical ErrorDescriptor path when route moves/hygiened |

Do **not** invent a new Admin/BFF module.

## Cross-module boundary proof (Composer)

| Check | State |
|-------|-------|
| Foreign `.Application` | ZERO |
| Foreign `.Infrastructure` | ZERO |
| Foreign `.Domain` | ZERO |
| Foreign DbContext / DbSet | ZERO |
| Cross-module EF/SQL join | ZERO |
| Shared mutable entities | ZERO |
| Contracts-only ports | YES — Catalog / Offer / Order / Party Contracts |

Legal Contracts used:

- `ICatalogAdminProductCountGateway` (Catalog.Contracts)
- `IOfferQueryGateway` (Offer.Contracts)
- `IPartyAdminSellerReadGateway` (Party.Contracts)
- `IAdminOrderDashboardMetricsPort` / `IAdminSellerOrderCountPort` (Order.Contracts.Admin)
- `IAdminSellersGridPort` / `AdminSellerListItem` (Party.Contracts)
- `GridQueryRequest` / `GridPageResponse<>` (BuildingBlocks.Grid)

## CQRS / MediatR readiness

All four Panel routes **bypass** `Endpoint → ISender → IRequest/Handler → Result<T> → ApiResponseFactory`.

- Dashboard / ListSellers: direct `AdminPanelComposer` + `Results.Json`
- Sellers query: `AdminGridQueryEndpoint` + composer delegate + `Results.Json`
- Dev-context: environment/snapshot branch + `Results.Json`

MediatR 12.5 not used on this surface. Future KEEP Host dashboard composition may remain non-MediatR **only if** Architect accepts Host presentation composition exception **and** ApiResponseFactory/catalog errors are applied (quality not waived).

## API result / error audit

| Pattern | Location | Canonical? |
|---------|----------|------------|
| `Results.Json(success)` | Endpoints ExecuteAsync / Grid helper | NO — should be ApiResponseFactory |
| `catch (PlatformHttpException)` + `ToError` | Endpoints | NO — should use `ApiResponseFactory.FromPlatformException` / global presentation |
| `Results.Json({ title, errorCode }, statusCode)` | ToError + Grid helper | Preserves status + code; **titles** currently Persian from Access / English `"Not Found"` |
| Global `IExceptionPresentationService` | Not used on this path | Unexpected exceptions not mapped through Panel |

## Hard-coded user-facing text (Panel runtime)

| Text | Kind | Notes |
|------|------|-------|
| `"Not Found"` | EN user-facing title | `GetDevContext` 404 — **NON_CANONICAL** |
| Persian titles in `AdminPanelAccess` | FA user-facing | Outside Panel folder; affects Panel via thrown `PlatformHttpException` — classify Access separately; do not reopen Access here |
| XML/comments Persian | docs only | OK |

Stable codes already present on Panel/Access path: `admin.dev.unavailable`, `admin.tenant.missing`, `admin.authorization.*`, `admin.actor.missing`.

## Path / namespace / cohesion

| File | Namespace | Path↔namespace |
|------|-----------|----------------|
| AdminPanel*.cs | `Tooba.Host.Admin.Panel` | EXACT |

Cohesion: **MUST_SPLIT** — Endpoints mixes cross-module ops routes with Development-only dev-context; Composer mixes dashboard KEEP with sellers MOVE candidates.

## Registration / callers

- `Program.cs`: `AddScoped<AdminPanelComposer>()`; `MapAdminPanelEndpoints()`
- Guards/tests reference Panel paths (Canon, composition tests) — update in migrate waves

## Closed-folder integrity

| Proposed target | Classification |
|-----------------|----------------|
| Host/Admin/Panel (retain dashboard) | OPEN_FOR_CURRENT_TASK |
| Party.Endpoints / Party Application | OPEN (existing module HTTP owner) |
| Host/Admin/Access | LOCKED_BY_ACCEPTED_DISPOSITION — do not start |
| Host/Admin/Grid | LOCKED — inspect-only |
| Host/Admin/Development | UNOPENED — NEEDS_ARCHITECT_DECISION to place moved dev-context |
| Host/Development allowlist | LOCKED_BY_ACCEPTED_DISPOSITION — no silent growth |
| New Admin BFF module | FORBIDDEN by task |

## Microservice readiness

Dashboard Contracts sync composition is acceptable for current monolith Host; true service separation later may need projections/events — **not required** to invent now. Sellers grid already Party-owned adapter → natural Party HTTP ownership.
