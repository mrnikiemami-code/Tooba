# Host/Seller — Seller-R1A — Certification

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R1A
**Parent:** TB-TMAR-HOST-SELLER-AMC-001-R1 (`3c13e4bcbf2b0a32ef4a701b48e8782a41d6b51f`)
**Standard:** `tooba-architecture-certify` (V2) / ARCH-COMPLETE-002
**Verdict for the R1A slice: PASS — the R1 foreign-Application leakage is eliminated and the Host seller security boundary is ZERO-clean.**
**Full Host/Seller certification: NOT YET** (5 Host business files remain for Seller-R2..R6).

## 1. Blocker closure

| Architect blocker on R1 | R1A resolution | Verdict |
| --- | --- | --- |
| `HostSellerOrderViewAccessReader.cs` → `Tooba.Order.Application.Seller.Ports` | File deleted; port implemented Order-owned in `Tooba.Order.Infrastructure` | CLOSED |
| `HostOrderSellerAuthorizer.cs` → `Tooba.Order.Application.Seller.SellerOrderErrors` | Reference removed; uses Host-owned `SellerSecurityErrorCodes.ActorMissing` | CLOSED |
| `HostSupportSellerAuthorizer.cs` → `Tooba.Support.Application.Errors.SupportErrorCodes` | Reference removed; uses Host-owned `SellerSecurityErrorCodes.AuthorizationDenied` | CLOSED |

## 2. Boundary certification

| Check | Result |
| --- | --- |
| Cohesive single responsibility per file | PASS |
| Correct capability folder (`Host/Security/Seller`) | PASS |
| Exact path ↔ namespace (`Tooba.Host.Security.Seller`) | PASS |
| Boundary file count | 10 |
| Foreign Application in boundary | **ZERO** |
| Foreign Domain in boundary | **ZERO** |
| Foreign Infrastructure in boundary | **ZERO** |
| Foreign Persistence / `DbContext` / `DbSet` in boundary | **ZERO** |
| Service locator (`RequestServices`) in boundary | **ZERO** |
| Cross-module EF join | NONE |
| Neutral seam `IPlatformEffectiveAccessReader` | PRESERVED (single seam, no parallel mechanism) |
| No hard-coded user-facing text introduced | PASS (values unchanged; constants only relocate the owner) |
| No unintended behavior/schema change | PASS |

## 3. Ownership classification

| Category | Files | Verdict |
| --- | --- | --- |
| `ALLOWED_SECURITY_ADAPTER` | 8 thin module-Endpoints authorizers + `HostSellerPanelAccess` | LEGAL |
| `ALLOWED_HOST_SECURITY_CONFIG` | `SellerSecurityErrorCodes` | LEGAL |
| `ALLOWED_GLOBAL_AUTH_PLATFORM_BOUNDARY` | `SellerPanelAccess` | LEGAL |
| `ILLEGAL_BUSINESS_AUTHORITY` | — | ZERO |
| `ILLEGAL_PERSISTENCE_AUTHORITY` | — | ZERO |
| `ILLEGAL_FOREIGN_APPLICATION_PORT_IMPLEMENTATION` (Host) | — | ZERO |

## 4. Order-owned port certification

- `Tooba.Order.Infrastructure.Seller.SellerOrderViewAccessReader` implements `ISellerOrderViewAccessReader` intra-module.
- Consumes only the neutral `IPlatformEffectiveAccessReader` + `Tooba.BuildingBlocks.Security`; no `Tooba.AccessControl` reference.
- Preserves `order.view`, `DeniedByCeiling` exclusion, `GlobalWithinOwner`, category scope projection, denied-snapshot semantics.
- Registered by `OrderModule.cs`; Host `Program.cs` registration removed.

## 5. Stable codes certification

`seller.actor.missing`, `seller.identity.missing`, `seller.authorization.denied`, `seller.authorization.unavailable` — values unchanged; owner is now `Tooba.Host.Security.Seller.SellerSecurityErrorCodes`. No module-specific business error code duplicated into Host.

## 6. Regression / closed-folder audit

`Host/Development`, `Host/Admin`, and other closed Host folders unchanged; no new Host folder created; `Host/Seller` retains its 5 deferred business files. No sink-folder regression.

## 7. Durable guard

`HostSellerAmcR1GuardTests` (9 facts) PASS with the R1 foreign-Application allowlist **removed** and replaced by a ZERO-tolerance rule. Order and Support module architecture guards updated to the new physical path/owner and PASS. No guard weakened, skipped, or baseline-widened.

## 8. SoT / manifest

- `tmar-current-state.json`: new `hostSellerAmcR1A` checkpoint (all required fields present).
- `TOOBA-TMAR-MASTER-RECOVERY.md`, `TOOBA-ARCHITECT-BOOTSTRAP.md`, `TOOBA-RECOVERY-CONTEXT.md`: current checkpoint = Seller R1A security-boundary repair; full Host/Seller OPEN; `workflowStop = USER_REVIEW_HOST_SELLER_AMC_001_R1A`; automatic next implementation task = NONE. Development closure lineage preserved as HISTORICAL.
- Manifest: unchanged (Seller is not yet a structure-certified module).

## 9. Residual / deferred (explicitly out of R1A scope)

- `Host/Seller` still owns its seller business endpoints/composer/models/dev bootstrap → Seller-R2…R6.
- Hardcoded Persian text / unregistered `seller.*` codes in the remaining Host seller business endpoints → Seller-R2/R3/R6.
- Pre-existing unrelated focused-test drift (`OrderSellerPanelArchitectureGuardTests.R4_...` missing Host `Customer/CustomerPanelComposer.cs` at HEAD; Order error-code descriptor count drift) — documented in `validation.md`, not introduced by R1A.

## 10. Verdict

```
SLICE: Seller-R1A (remove foreign Application leakage from Host/Security/Seller + reconcile Recovery/SoT)
STATUS: PASS (slice-scoped)
BOUNDARY: Host/Security/Seller — 10 files, path↔namespace EXACT
FOREIGN APPLICATION / DOMAIN / INFRASTRUCTURE / PERSISTENCE: ZERO
ORDER VIEW-ACCESS PORT: ORDER-INFRASTRUCTURE-OWNED
HOST SELLER SECURITY CODES: HOST-BOUNDARY-OWNED, VALUES UNCHANGED
NEUTRAL EFFECTIVE-ACCESS SEAM: PRESERVED
SERVICE LOCATOR / FOREIGN PERSISTENCE: ZERO
ROUTE / HEADER / STATUS / DTO / SCHEMA / FRONTEND CHANGE: NONE
FOCUSED BUILD: 0 ERRORS
FOCUSED GUARDS: PASS
FULL SELLER FOLDER CERTIFICATION: NOT YET (Seller-R2..R6)
NEXT: USER_REVIEW_HOST_SELLER_AMC_001_R1A — no automatic next implementation task
```
