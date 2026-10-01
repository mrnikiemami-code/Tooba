# Ownership map — TB-TMAR-HOST-ADMIN-PANEL-AMC-001

```text
ANALYSIS_ONLY_NOT_MIGRATED_NOT_CERTIFIED
```

## File dispositions

| File | Responsibilities | Categories | Disposition |
|------|------------------|------------|-------------|
| AdminPanelComposer.cs | Dashboard compose; sellers list compose; sellers grid delegate | PRESENTATION_COMPOSITION; CROSS_MODULE_ORCHESTRATION | **MUST_SPLIT**: KEEP dashboard methods; MOVE sellers methods → Party |
| AdminPanelEndpoints.cs | Map 4 routes; auth; Results.Json; PlatformHttpException; ToError; GetDevContext | HTTP_ENDPOINT; AUTHORIZATION_ADAPTER (call); DEVELOPMENT_ONLY_RUNTIME | **MUST_SPLIT**: KEEP dashboard(+interim sellers); MOVE sellers routes; MOVE/SPLIT dev-context |
| AdminPanelModels.cs | `AdminDashboardSummary` only | CONTRACT (response DTO) | **KEEP_WITH_HOST_PANEL** |

## Responsibility → owner

| ID | Responsibility | Canonical owner now | Target owner | Reason | Microservice blocker |
|----|----------------|---------------------|--------------|--------|----------------------|
| R1 | GET dashboard counts | Host Panel | Host Panel (KEEP) | Cross-module aggregation; no natural single business module | Sync Contracts OK; later projections optional |
| R2 | AdminDashboardSummary shape | Host Panel | Host Panel | Consumer = Host composition response | None |
| R3 | GET sellers list | Host Panel | Party | Seller identity/status Party-owned; Offer/Order via Contracts already | None if Contracts preserved |
| R4 | POST sellers/query | Host Panel | Party.Endpoints | `IAdminSellersGridPort` already Party | None |
| R5 | GET dev-context | Host Panel | Admin/Development **or** approved Host Development seam | Development-only snapshot of `AdminDevActorBootstrap` | Destination locked → **NEEDS_ARCHITECT_DECISION** |
| R6 | Admin auth gate call | Host Admin/Access | Host Admin/Access (unchanged) | Platform admin security | Do not migrate Access in Panel AMC |
| R7 | AdminGridQueryEndpoint | Host Admin/Grid | Retire from Panel when sellers move | Shared Host grid helper | Do not open Grid unit |

## Boundary inventory (Composer)

| Forbidden dependency | Present? |
|----------------------|----------|
| Foreign Application | NO |
| Foreign Infrastructure | NO |
| Foreign Domain | NO |
| Foreign DbContext | NO |
| Cross-module join | NO |
| Host business authority beyond composition | NO (counts only; no policy mutation) |

## Admin access seam

`AdminPanelAccess.RequireAuthorizedAsync` is a legitimate Host-level security adapter living under **Admin/Access** (not Panel). Panel endpoints use the static helper directly. Prefer future `IAdminPanelAccess` injection at endpoint edge without moving Access folder in this AMC.

## Admin/Grid dependency

Panel sellers-query depends on `Tooba.Host.Admin.Grid.AdminGridQueryEndpoint` for auth+Json wrapper. After Party migration of sellers-query, Panel no longer needs Grid helper. Dashboard/ListSellers do not use Grid helper today.
