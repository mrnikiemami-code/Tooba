# Host/Seller — Seller-R1 — Certification

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R1
**Parent:** TB-TMAR-HOST-SELLER-AMC-001
**Standard:** `tooba-architecture-certify` (V2) / `ARCH-COMPLETE-002`
**Verdict for the R1 slice: SLICE PASS — Host seller security boundary canonical, neutral seam adopted.**
**Full Seller folder certification: NOT YET** (deferred to Seller-R6; 7 Host seller routes remain by design).

## 1. Touched-surface re-read (all 10 boundary files)

| Check | Result |
| --- | --- |
| Cohesive single responsibility per file | PASS |
| Correct capability folder (`Host/Security/Seller`) | PASS |
| Exact path ↔ namespace (`Tooba.Host.Security.Seller`) | PASS |
| No root dump | PASS |
| No obsolete/duplicate type left in `Host/Seller` | PASS (10 stale files deleted) |
| No hard-coded user-facing text introduced | PASS (R1 moved existing behavior verbatim; localization remediation is Seller-R2/R3 scope) |
| No foreign Application/Infrastructure/Domain leakage | PASS for AccessControl; module Endpoints-port/error-code references explicitly allowlisted and documented |
| No parallel canonical mechanism | PASS (`IPlatformEffectiveAccessReader` is the existing neutral seam; no second access path invented) |
| No unintended behavior/schema change | PASS (routes/headers/codes/DTOs/schema untouched) |

## 2. Host authority classification (R1 boundary)

| Category | Files | Verdict |
| --- | --- | --- |
| `ALLOWED_SECURITY_ADAPTER` | `HostSellerPanelAccess`, `HostSellerOrderViewAccessReader`, `HostOffer/Order/Return/Settlement/Notification/PromotionSellerAuthorizer`, `HostSupportSellerAuthorizer` | LEGAL |
| `ALLOWED_GLOBAL_AUTH_PLATFORM_BOUNDARY` | `SellerPanelAccess` (Actor/session projection + `user → party#view` gate) | LEGAL |
| `ILLEGAL_BUSINESS_AUTHORITY` | — | ZERO |
| `ILLEGAL_PERSISTENCE_AUTHORITY` | — | ZERO |
| `ILLEGAL_ENDPOINT_OWNERSHIP` (in moved set) | — | ZERO |

No service locator (`RequestServices`) remains in the boundary.

## 3. Cross-module boundary audit (boundary files only)

- `Tooba.AccessControl.Application` / `Tooba.AccessControl.Domain` → **ZERO** (replaced by neutral seam).
- Foreign `DbContext` / `DbSet` / persistence → **ZERO**.
- Cross-module SQL/EF join → **NONE**.
- Allowed references: module-owned Endpoints authorizer ports, `Tooba.Order.Application.Seller.Ports.ISellerOrderViewAccessReader`, `Tooba.Support.Application.Errors.SupportErrorCodes` — all consumed as module-owned Contracts/port surfaces (asserted with explicit allowlist in `HostSellerAmcR1GuardTests`).

## 4. Closed-folder regression audit

| Destination | Baseline | Post-R1 | Verdict |
| --- | --- | --- | --- |
| `Host/Development` | locked 5-file set | unchanged | NO_REGRESSION |
| `Host/Admin` | accepted `Admin/Access/Authorizers/*` + `Admin/Panel/*` | unchanged | NO_REGRESSION |
| Other closed Host folders | unchanged | unchanged | NO_REGRESSION |
| New Host folders | — | none created | NO_REGRESSION |

No `SINK_FOLDER_REGRESSION`.

## 5. Persistence / migration safety

Schema `UNCHANGED`; no migration added/modified; migration order/Up/Down/snapshot untouched.

## 6. Durable guard

`HostSellerAmcR1GuardTests` — **all facts PASS**. Existing module architecture guards updated to the new physical path and **all PASS** (Settlement 7, Support 3, Returns 3, Offer 29, Notification 7, Host order-audit 13, host seller/security focused 22). No guard was weakened, skipped, or baseline-widened.

## 7. SoT / manifest

- `tmar-current-state.json`: new `hostSellerAmcR1` checkpoint added; current recovery pointer (`USER_REVIEW_HOST_DEVELOPMENT_PRODUCTWORKSPACE_MIGRATION_SEAM_001_R1`) intentionally left unchanged (R1 is an out-of-band Seller slice; the historical Development R1 checkpoint remains authority).
- Manifest: unchanged (Seller is not a structure-certified module yet; full certification is Seller-R6).
- Evidence: `docs/evidence/TB-TMAR-HOST-SELLER-AMC-001/` (analyze, validation) + `docs/evidence/TB-TMAR-HOST-SELLER-AMC-001-R1/` (this set).

## 8. Residual non-blocking debt (explicitly out of R1 scope)

- `Host/Seller` still owns 7 routes, `SellerPanelComposer` (Catalog DbContext), `SellerPanelModels` (Offer DTO aliases), `SellerDevActorBootstrap` → Seller-R2…R5.
- Hardcoded Persian text / `ex.Message` titles / unregistered `seller.*` codes in the remaining Host seller business endpoints → Seller-R2/R3/R6.
- Pre-existing unrelated focused-test drift and DB-gated skips (documented in `validation.md`).

## 9. Verdict

```
SLICE: Seller-R1 (Host seller platform security boundary rehome + neutral seam adoption)
STATUS: PASS (slice-scoped)
BOUNDARY: Host/Security/Seller — 10 files, path↔namespace EXACT
ACCESS_CONTROL_APPLICATION_DOMAIN_IN_BOUNDARY: ZERO
SERVICE_LOCATOR_IN_BOUNDARY: ZERO
FOREIGN_PERSISTENCE_IN_BOUNDARY: ZERO
ROUTE/HEADER/STATUS/DTO/SCHEMA CHANGE: NONE
FOCUSED BUILD: 0 ERRORS
FOCUSED GUARDS: ALL PASS
FULL SELLER FOLDER CERTIFICATION: NOT YET (Seller-R2..R6)
```
