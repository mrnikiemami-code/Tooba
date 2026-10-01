# Migration plan — TB-TMAR-HOST-ADMIN-PANEL-AMC-001

```text
ANALYSIS_ONLY_NOT_MIGRATED_NOT_CERTIFIED
Decisive plan — fewest safe waves — each ≤20 minutes
Do NOT invent Admin BFF module
```

## Target end-state (after all waves + certify)

| Item | State |
|------|-------|
| Host/Admin/Panel | Thin KEEP: Composer dashboard + Models + Endpoints dashboard only (or empty if Architect later consolidates) |
| Sellers HTTP | Party.Endpoints |
| Dev-context HTTP | Outside Panel (Architect-chosen Development seam) |
| Api mapping on retained Host routes | ApiResponseFactory + catalog codes |
| Certification | Separate CERTIFY task after migrate waves |

## Wave 1 — Sellers evacuate to Party (~20 min)

**Source:** `ListSellersAsync` / `QuerySellersGridAsync` + Host route maps `/sellers`, `/sellers/query`  
**Destination:** `Party.Endpoints` Admin sellers endpoints + thin Application queries if required  
**Contracts reused:** `IAdminSellersGridPort`, `AdminSellerListItem`, Offer/Order Contracts as today (composition may move into Party Application/Infrastructure adapter — already partly in `AdminSellersGridAdapter`)  
**Endpoint/CQRS:** Party `ISender` + validators as needed; remove Host maps same commit  
**Errors/localization:** Party canonical ApiResponseFactory; no hard-coded FA/EN  
**Guards:** update Host Panel / Party architecture guards; HostOrder reverse audits  
**Validation:** focused Party + Host Panel compile/tests only  
**Host/Admin/Panel file count after:** 3 (Composer thinned) or still 3 with sellers methods deleted  
**Precondition for later CERTIFY:** sellers dual-ownership ZERO

## Wave 2 — Dev-context split (~15 min) — gated

**Source:** `GetDevContext` in `AdminPanelEndpoints`  
**Destination options (Architect picks one before execute):**

1. New thin file under `Host/Admin/Development/` next to `AdminDevActorBootstrap` — requires opening Admin/Development recovery unit  
2. Approved addition under locked `Host/Development/` allowlist — requires explicit Architect allowlist amendment  

**If neither authorized:** leave route temporarily but flag **BLOCKED_FOR_MIGRATE** on this slice only; do not grow closed allowlists silently.

**Contracts:** none (dev snapshot)  
**Errors:** replace `"Not Found"` with cataloged `admin.dev.unavailable` presentation  
**Host/Admin/Panel after:** Endpoints without GetDevContext  
**Sink-folder risk:** HIGH if wrong destination chosen — forbid HOST_ZERO resurrection

## Wave 3 — Host Panel hygiene for KEEP dashboard (~15–20 min)

**Source:** retained dashboard endpoint + Composer GetDashboard + AdminDashboardSummary  
**Destination:** same Host/Admin/Panel  
**Changes:**

- Replace `Results.Json` / `ToError` / raw PlatformHttpException catch with `ApiResponseFactory` / Result path
- Prefer `IAdminPanelAccess` at edge without migrating Access folder
- Remove Admin/Grid dependency (already gone after Wave 1)
- Optionally wrap dashboard in Host-local MediatR **only if** Architect requires CQRS on KEEP Host composition; otherwise document HOST_COMPOSITION_CQRS_EXCEPTION with quality (ApiResponseFactory + codes) non-negotiable

**Guards:** HostAdmin Canon / Panel composition tests  
**Host/Admin/Panel file count after:** 3 (or 2 if Models merged)  
**Certification precondition:** ZERO hard-coded EN/FA runtime titles on Panel path; ZERO Results.Json business success path

## Wave 4 — CERTIFY (separate task, after Architect ACCEPT of migrate)

Apply `tooba-architecture-certify` on remaining Host/Admin/Panel KEEP surface + Party sellers ownership. Not part of this Analyze task.

## Behavior preservation map

See `route-map.md` public-contract table — mandatory for all waves.

## Explicit non-goals

- No Host/Admin/Access start  
- No Host/Admin/Grid start  
- No new Admin module  
- No schema / frontend / production behavior change in Analyze  
- No lastAcceptedImplementation advance in Analyze

## Recommended next task

`TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W1` — migrate sellers list/query to Party.Endpoints (≤20m)  
Parallel Architect decision ticket for Wave 2 destination before W2 execute.
