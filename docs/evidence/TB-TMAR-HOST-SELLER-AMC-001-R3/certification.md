# Host/Seller — Seller-R3 — Certification

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R3
**Parent:** TB-TMAR-HOST-SELLER-AMC-001-R2
**Skill:** `.cursor/skills/tooba-architecture-certify/SKILL.md`
**Verdict: PASS** — the two Seller settings routes and the seller settings capability checks are
Party-owned; Host/Seller keeps only its 2 Host-owned seller routes and has ZERO settings layer
leakage; behavior parity and the R1A security boundary are preserved; Recovery/SoT is synchronized.

## 1. Success criteria

| Criterion | Result |
| --- | --- |
| Both settings routes are Party-owned | PASS (`PartySellerSettingsEndpoints`) |
| Host duplicate routes = ZERO | PASS |
| Host-owned seller routes = 2 | PASS |
| `Host/Seller` business files = 4 | PASS |
| `SellerSettingsEndpoints.cs` absent | PASS |
| Host settings layer leakage removed | PASS (`AccessControl.Application`/`Domain`, `IAccessControlDirectory`, `EnsureSellerCapabilityAsync` = ZERO) |
| Party Endpoints use `ISender` only | PASS (`ISENDER_ONLY`, no `Results.Json`) |
| Party Endpoints `DbContext` = ZERO | PASS (full-folder line scan) |
| Party seller settings CQRS present | PASS (query + command + handlers) |
| Validator classification correct | PASS (write validator present; AUTH_SCOPED read query unvalidated) |
| Seller authorization stays Host-owned | PASS (thin `HostPartySellerAuthorizer` inside `Host/Security/Seller`) |
| Exact behavior parity preserved | PASS (see `validation.md`) |
| Stable error codes preserved | PASS (`seller.settings.missing`, `seller.settings.rejected`, `seller.authorization.denied`) |
| Schema/migration change | NONE |
| Frontend unchanged | PASS |
| Full Host/Seller certification | NOT_YET |
| Seller-R4 not started | PASS (`NOT_STARTED`) |
| Recovery/SoT fully synchronized | PASS |
| `automaticNextImplementationTask = NONE` | PASS |
| User work preserved | PASS |

## 2. Party layer ownership certification

| Layer | Owner | State |
| --- | --- | --- |
| HTTP endpoints (2 routes) | `Tooba.Party.Endpoints.Seller` | OWNED |
| CQRS read | `Tooba.Party.Application.Seller.Queries` | OWNED (`ISender`) |
| CQRS write | `Tooba.Party.Application.Seller.Commands` | OWNED (`ISender`) |
| Transport validator | `Tooba.Party.Application.Seller.Validators` | OWNED |
| Read/write models + codes | `Tooba.Party.Application.Seller` | OWNED |
| Persistence seam | `Tooba.Party.Contracts` (`IPartySellerSettings`) | OWNED |
| Persistence implementation | `Tooba.Party.Infrastructure.Seller` (`IPartyDirectory`) | OWNED |
| Seller transport record | `Tooba.Party.Endpoints.Seller` | OWNED |
| Seller platform authorization | `Tooba.Host.Security.Seller` (R1A boundary) | HOST-OWNED (thin adapter only) |

## 3. Boundary certification

| Boundary | State |
| --- | --- |
| Host `Seller/` settings layer coupling (AccessControl Application/Domain, `IAccessControlDirectory`) | ZERO |
| Host `Seller/` duplicated settings capability helper | ZERO |
| Host `Security/Seller` foreign Application/Domain/Infrastructure/Persistence | ZERO (R1A unchanged) |
| Party Endpoints -> `DbContext` / EF | ZERO |
| Party Endpoints -> foreign module Application/Domain/Infrastructure | ZERO |
| Party -> Host dependency | ZERO |
| Cross-module DbContext / join | NONE |
| Service locator in the new adapter | NONE |

## 4. Route/file count certification

```
Host-owned seller routes:   4 -> 2
Party-owned seller routes:  0 -> 2
Host/Seller business files: 5 -> 4
Duplicate ownership: 0
```

## 5. Structural state

`Host/Seller` now holds exactly `SellerPanelEndpoints.cs`, `SellerPanelComposer.cs`,
`SellerPanelModels.cs`, `SellerDevActorBootstrap.cs`. The durable `HostSellerAmcR3GuardTests`
locks the new shape and proves no sink-folder regression (no
`Host/Seller/PartySellerSettingsEndpoints.cs`).

## 6. SoT synchronization

`tmar-current-state.json` (top level + `currentHostEvacuation` + new `hostSellerAmcR3` block),
`TOOBA-TMAR-MASTER-RECOVERY.md`, `TOOBA-ARCHITECT-BOOTSTRAP.md` and `docs/ai/TOOBA-RECOVERY-CONTEXT.md`
all agree on: latest accepted = `TB-TMAR-HOST-SELLER-AMC-001-R3`, checkpoint = `Seller`,
`workflowStop`/`nextTask` = `USER_REVIEW_HOST_SELLER_AMC_001_R3`, `staleCurrentPointerState = ZERO`,
`automaticNextImplementationTask = NONE`, full Host/Seller certification = NOT_YET,
Seller-R4 = NOT_STARTED. The accepted Seller R2 and R1A/R1B checkpoints and the Development closure
are preserved as accepted/HISTORICAL lineage.

## 7. Verdict block

```
SLICE: Seller-R3 — evacuate Seller settings routes + capability checks from Host/Seller to Party
STATUS: PASS
PARTY SELLER SETTINGS ROUTE OWNERSHIP: Tooba.Party.Endpoints
PARTY SELLER SETTINGS ROUTE COUNT MIGRATED: 2
HOST SELLER ROUTE COUNT: 4 -> 2
HOST SELLER FILE COUNT: 5 -> 4
SELLER SETTINGS ENDPOINTS: ABSENT
DUPLICATE ROUTE STATE: ZERO
PARTY ENDPOINTS ISENDER: ISENDER_ONLY
PARTY ENDPOINT DBCONTEXT: ZERO
PARTY SELLER CQRS: COMPLETE_QUERY_PLUS_COMMAND
VALIDATOR: WRITE_REQUIRED_PRESENT; AUTH_SCOPED_READ_NO_VALIDATOR
HOST SELLER SETTINGS LAYER LEAKAGE: ZERO
SELLER AUTHORIZATION: HOST_OWNED_THIN_ADAPTER (R1A boundary unchanged)
BEHAVIOR PARITY: PRESERVED
ERROR CODES: seller.settings.missing + seller.settings.rejected + seller.authorization.denied PRESERVED
SCHEMA CHANGE: NONE
FRONTEND: UNCHANGED
SINK FOLDER REGRESSION: NONE
FOCUSED BUILD: PASS
FOCUSED TEST: PASS
FULL SELLER FOLDER CERTIFICATION: NOT_YET
SELLER-R4: NOT_STARTED
AUTOMATIC NEXT IMPLEMENTATION TASK: NONE
NEXT: USER_REVIEW_HOST_SELLER_AMC_001_R3 — no automatic next implementation task
```
