# Host/Seller — Seller-R2 — Certification

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R2
**Parent:** TB-TMAR-HOST-SELLER-AMC-001-R1B
**Skill:** `.cursor/skills/tooba-architecture-certify/SKILL.md`
**Verdict: PASS** — the three Seller Catalog routes and the Catalog persistence composition are
Catalog-owned; Host/Seller keeps only its 4 Host-owned seller routes and has ZERO Catalog layer
leakage; behavior parity and the R1A security boundary are preserved; Recovery/SoT is synchronized.

## 1. Success criteria

| Criterion | Result |
| --- | --- |
| All 3 Catalog routes are Catalog-owned | PASS (`CatalogSellerEndpoints`) |
| Host duplicate routes = ZERO | PASS |
| Catalog Endpoints use `ISender` | PASS (`ISENDER_ONLY`, no `Results.Json`) |
| `Host/Seller` `CatalogDbContext` = ZERO | PASS (full-folder line scan) |
| `Host/Seller` Catalog Domain/Infrastructure/Persistence/Application = ZERO | PASS (full-folder line scan) |
| Exact behavior parity preserved | PASS (see `behavior-parity.md`; write-route ok-envelope/code normalizations documented and Architect-visible) |
| Validator classification correct | PASS (AUTH_SCOPED query unvalidated; reused W10/W11 commands unchanged) |
| R1A Host security boundary preserved | PASS (`Host/SellerAmcR1GuardTests` green; adapter inside `Host/Security/Seller`) |
| Host-owned seller routes = 4 | PASS |
| Seller R3 not started | PASS (`NOT_STARTED`) |
| Recovery/SoT fully synchronized | PASS |
| `automaticNextImplementationTask = NONE` | PASS |
| Frontend unchanged | PASS |
| User work preserved | PASS |

## 2. Catalog layer ownership certification

| Layer | Owner | State |
| --- | --- | --- |
| HTTP endpoints (3 routes) | `Tooba.Catalog.Endpoints.Seller` | OWNED |
| CQRS query (variant list) | `Tooba.Catalog.Application.Seller.Queries` | OWNED (`ISender`) |
| CQRS commands (attribute / variant-axes) | `Tooba.Catalog.Application.Attributes.ProductValues.Commands` + `.Variants.Commands` | REUSED (already canonical) |
| Read model | `Tooba.Catalog.Application.Seller.Models` | OWNED |
| Persistence seam | `Tooba.Catalog.Application.Seller.Ports` | OWNED |
| Persistence implementation | `Tooba.Catalog.Infrastructure.Seller` (`CatalogDbContext`) | OWNED |
| Seller transport records | `Tooba.Catalog.Endpoints.Seller` | OWNED |
| Seller platform authorization | `Tooba.Host.Security.Seller` (R1A boundary) | HOST-OWNED (thin adapter only) |

## 3. Boundary certification

| Boundary | State |
| --- | --- |
| Host `Seller/` Catalog persistence composition | ZERO |
| Host `Seller/` foreign Catalog Application/Domain/Infrastructure reference | ZERO |
| Host `Security/Seller` foreign Application/Domain/Infrastructure/Persistence | ZERO (R1A unchanged) |
| Catalog -> Host dependency | ZERO |
| Catalog -> Party | Contracts-only (`Tooba.Party.Contracts.IPartyLookup`) |
| Cross-module DbContext / join | NONE |
| Service locator in the new adapter | NONE |

## 4. Route count certification

```
Host-owned seller routes:  7 -> 4
Catalog-owned seller routes: 0 -> 3
Duplicate ownership: 0
```

## 5. Structural debt closure

`Host/Seller/SellerPanelComposer.cs` was an `OVERSIZED_LEGACY` (934 LOC) frozen baseline entry.
After R2 it is 27 LOC (dashboard display composition only); the stale baseline entry was removed
without raising any loc or changing any threshold. The durable `HostSellerAmcR2GuardTests` locks
the new shape, and `No_route_sink_regression_and_no_new_host_seller_business_file_was_added`
proves `Host/Seller` still holds exactly 5 business files with no new Host seller business file.

## 6. SoT synchronization

`tmar-current-state.json` (top level + `currentHostEvacuation` + new `hostSellerAmcR2` block),
`TOOBA-TMAR-MASTER-RECOVERY.md`, `TOOBA-ARCHITECT-BOOTSTRAP.md` and `docs/ai/TOOBA-RECOVERY-CONTEXT.md`
all agree on: latest accepted = `TB-TMAR-HOST-SELLER-AMC-001-R2`, checkpoint = `Seller`,
`workflowStop`/`nextTask` = `USER_REVIEW_HOST_SELLER_AMC_001_R2`, `staleCurrentPointerState = ZERO`,
`automaticNextImplementationTask = NONE`, `Seller-R3 = NOT_STARTED`, full Host/Seller certification
= NOT_YET. Seller R1A/R1B and the Development closure are preserved as accepted/HISTORICAL lineage.

## 7. Verdict block

```
SLICE: Seller-R2 — evacuate Seller Catalog routes + Catalog persistence composition from Host/Seller
STATUS: PASS
CATALOG SELLER ROUTE OWNERSHIP: Tooba.Catalog.Endpoints
CATALOG SELLER ROUTE COUNT MIGRATED: 3
HOST SELLER ROUTE COUNT: 7 -> 4
DUPLICATE ROUTE STATE: ZERO
CATALOG ENDPOINTS ISENDER: ISENDER_ONLY
CATALOG ENDPOINT DBCONTEXT: ZERO
HOST SELLER CATALOG PERSISTENCE: ZERO
HOST SELLER CATALOG LAYER LEAKAGE: ZERO
SELLER PANEL COMPOSER: CATALOG_COMPOSITION_EVACUATED (934 -> 27 LOC)
SELLER CATALOG MODEL: CATALOG_OWNED
BEHAVIOR PARITY: PRESERVED (documented ok-envelope/code normalizations)
ERROR CODES: seller.missing + catalog.attribute.invalid + catalog.variant.axes.duplicate PRESERVED
HOST SECURITY SELLER R1A BOUNDARY: CANONICAL_UNCHANGED
SCHEMA CHANGE: NONE
FRONTEND: UNCHANGED
FOCUSED BUILD: PASS
FOCUSED TEST: PASS (40/40 focused host guards)
FULL SELLER FOLDER CERTIFICATION: NOT_YET
SELLER-R3: NOT_STARTED
AUTOMATIC NEXT IMPLEMENTATION TASK: NONE
NEXT: USER_REVIEW_HOST_SELLER_AMC_001_R2 — no automatic next implementation task
```
