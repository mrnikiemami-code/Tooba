# TB-TMAR-INVENTORY-AMSC-001-W3 — Certify

## Mode

`ARCHITECT_DIRECT_AMSC` — Certify. No production behavior change: this wave records and locks the
ARCH-COMPLETE-002 certification state produced by W0 → W2 and adds the durable certification guard.

Baseline: `branch = main`, `HEAD == origin/main == 87101cb4` (W2).

---

## Verdict

| Field | Value |
|---|---|
| Verdict | `COMPLETE_REFERENCE_PATTERN` |
| Lock | `ARCH-COMPLETE-002` |
| Structure-State | `CERTIFIED` (W2 `READY_FOR_CERTIFY` preserved as historical W2 truth) |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| SolutionExplorer-State | `CANONICAL` (`/Modules/Inventory/`, 5 projects) |
| PathNamespace-State | `EXACT` |
| RootAllowlist-State | `ENFORCED` |
| PhysicalCopy-State | `CLEAN` |
| File-Cohesion-State | `COHESIVE` |
| HttpApplicability | `INTERNAL_ONLY` |
| Endpoint-Ownership-State | `NOT_APPLICABLE` (no Endpoints project by design) |
| Endpoint-reachable requests | `0` |
| Validator-Coverage-State | `NOT_APPLICABLE_INTERNAL_ONLY` |
| Foreign App/Infra/Domain coupling | `ZERO` |
| Blocking residual debt | `ZERO` |
| `microserviceExtractable` | `true` |
| `automaticNextImplementationTask` | `NONE` |

## Wave lineage (accepted)

| Wave | Skill | Commit |
|---|---|---|
| W0 Analyze | `tooba-architecture-analyze` | `c6917553` |
| W1 Migrate | `tooba-architecture-migrate` | `133d413d` |
| W2 Structure | `tooba-architecture-structure` | `87101cb4` |
| W3 Certify | `tooba-architecture-certify` | *(this commit)* |

The earlier golden-wave lineage (`TB-TMAR-INVENTORY-APPLICABILITY-REVERIFY-001`, commit `2814da32`)
remains in the repository as historical evidence only; the AMSC-001 W0→W3 lineage is authoritative for
the current Inventory module.

## ARCH-COMPLETE-002 certification checklist

| Requirement | Evidence | State |
|---|---|---|
| Application capability folders | `Checkout/`, `Composition/`, `Orders/`, `Ports/`; root `.cs` = none; no `Commands`/`Queries`/`Models`/`Validators` technical axis | PASS |
| Endpoints capability folders | `Tooba.Inventory.Endpoints` ABSENT BY DESIGN (`INTERNAL_ONLY`) | N/A |
| Infrastructure capability folders | `Adapters/`, `DependencyInjection/`, `Directories/`, `Events/`, `Messaging/`, `Persistence/Migrations/`; root `.cs` = none | PASS |
| Contracts capability folders | `Availability/`, `Cart/`, `Checkout/`, `Errors/`, `Fulfillment/`, `Orders/`, `Resources/`, `Returns/`, `Seller/`; root `.cs` = none | PASS |
| Domain capability folders | `Aggregates/`, `Events/`, `ValueObjects/`; root `.cs` = none | PASS |
| Path ↔ namespace exact equality | all five projects, path-derived equality (0 mismatches) | PASS |
| Root allowlists | empty on all five projects and equal to disk; forbidden roots absent | PASS |
| CQRS / MediatR | `INTERNAL_USE_CASE_BOUNDARIES` by design — Inventory is a directory/port provider, not an HTTP surface | N/A |
| Validator coverage | `NOT_APPLICABLE_INTERNAL_ONLY`: zero endpoint-reachable requests; the seller stock write route is Offer-owned | PASS |
| API result pattern | `Result`/`Result<T>` + `ApiResponseFactory` at the consumer seams; zero raw `inventory.*` literals | PASS |
| Stable error-code owner | `Contracts/Errors/InventoryErrorCodes.cs` (29 declared codes) → 29 registered descriptors in `InventoryErrorCatalogContributor`; zero duplicate descriptor ownership | PASS |
| Localization | `InventoryErrors.resx` + `InventoryErrors.fa.resx`; every declared `inventory.*` code carries an entry in both cultures; `InventoryErrorResourceSet` claims the `inventory.` keyspace | PASS |
| Typed faults | Domain/Application/Infrastructure raise `ContractOperationException` with module-owned codes; `Application/Composition/InventoryOperation.cs` is the single typed-fault → `Result` seam (`IsKnown` filter); zero raw `InvalidOperationException` faults | PASS |
| Logging / telemetry / correlation | canonical; no ad-hoc pipeline; no sensitive value logged | PASS |
| Contracts-only boundaries | the only foreign seams are `Tooba.Offer.Contracts` and `Tooba.Catalog.Contracts`; zero foreign Application/Domain/Infrastructure/Endpoints project edge in any Inventory project | PASS |
| No cross-module persistence/join | own `inventory` schema only; zero cross-module join | PASS |
| Schema preservation | 0 migration files touched; migration IDs, `Up`/`Down`, designer + snapshot unchanged | PASS |
| Durable guards | W3 cert guard + W2 structure guard + inherited `InventoryAmcW1MigrateGuardTests`, `InventoryArchitectureGuardTests` and the Inventory behavior suites | PASS |
| Manifest + SoT | manifest `modules` Inventory entry `structureCertified: true` with the AMSC-001 note; `structureLock.certifiedModules` contains `Inventory` exactly once; AMSC W0→W3 SoT records; Master Recovery module checkpoint | PASS |
| Host closure | no `Host/Tooba.Host/Inventory` folder, no `Tooba.Host.Inventory` namespace, no `MapInventoryEndpoints` anywhere in production | PASS |

## Internal-only rationale (recorded, not a gap)

Inventory owns stock positions, reservations and locations, but it exposes no HTTP surface by design:
the seller stock write route `/offers/{offerId:guid}/inventory` is **Offer-owned**
(`Tooba.Offer.Endpoints` → `ISender` → `SetOfferInventoryCommand` → the Inventory Contracts port
`ISellerOfferInventoryGateway`). Because Inventory has zero endpoint-reachable requests, the
ARCH-COMPLETE-002 validator matrix is `NOT_APPLICABLE_INTERNAL_ONLY` **by construction** rather than by
omission, and no ceremonial validator was added. The Offer-side validator for that route stays
Offer-owned. The other consumer seams are equally non-HTTP: Order supply/recovery, Payment unpaid
supply mapping, Returns restock, Cart holds, Checkout reservation, Fulfillment lifecycle.

## Error-code ownership (exhaustive)

| Item | Count / Value |
|---|---|
| Declared `inventory.*` codes (`InventoryErrorCodes`) | 29 |
| Registered descriptors (`InventoryErrorCatalogContributor`) | 29 |
| Unregistered declared code | NONE |
| Duplicate descriptor owner | NONE |
| Deliberately unclaimed (Order-owned) | `inventory.reservation.retry_limit_reached`, the `inventory.recovery.*` keyspace |
| `.resx` coverage (English) | 29 / 29 |
| `.resx` coverage (Persian) | 29 / 29 |

`docs/architecture/evidence/TB-TMAR-INVENTORY-AMSC-001-W3/audit-codes.js` reproduces this inventory
from the live tree.

## Microservice extractability

| Surface | State |
|---|---|
| Foreign `*.Application` project edge in any Inventory project | ZERO |
| Foreign `*.Infrastructure` project edge in any Inventory project | ZERO |
| Foreign `*.Domain` project edge in any Inventory project | ZERO |
| Foreign `*.Endpoints` project edge in any Inventory project | ZERO |
| Foreign `DbContext` reach-through / cross-module join | ZERO |
| Own-module `Domain → Contracts` edge | PRESENT (legal, for stable codes only) |
| Foreign `Contracts` seams | `Tooba.Offer.Contracts`, `Tooba.Catalog.Contracts` |
| Schema ownership | own `inventory` schema |

Inventory can be lifted into an isolated microservice by replacing the two Contracts seams with
transport adapters and the own `inventory` schema connection — no code movement is required.

## Pre-existing drift (not Inventory, not repaired here)

The full Host suite is red at the W3 starting HEAD `87101cb4` with **82 pre-existing failures**, none
of which involves Inventory: the `Host/Admin` StoreAppearance file-count guards
(`HostAdminAmcW7/W14R1/W16R1/W29PwIdentity/W35HoldPolicy/StoreLanding/StoreMenu/CheckoutAbuse`),
Grid/Catalog/Party/Reviews/Storefront module guards, `Fulfillment`/`Tax`/`Pricing`/`Promotion` domain
tests, the `TmarDurableGuardTests` Master-Recovery pins, the stale `TmarCompleteReferenceStructureGateTests`
hardcoded module lists and the source-size baseline guards.

Two guard families are red **because of inherited repository-global drift**, and were deliberately
**not** "fixed" by weakening them:

1. `TmarCompleteReferenceStructureGateTests` pinned a 21-module hardcoded certified list while the
   manifest/SoT already carried 24 certified modules (Catalog, CustomerProfile and Inventory were
   certified without updating it). Its list assertions were reconciled to the real certified set
   (a repair, no assertion weakened) and a module-scoped `Inventory_projects_satisfy_root_allowlists_and_exact_namespace_alignment`
   fact was added. The remaining red assertion is the unrelated pre-existing
   `Tooba.Catalog.Contracts/Cart` path↔namespace deviation (that Contracts project aggregates a
   capability folder under the project namespace; the same shape exists in `Tooba.Cart.Contracts/Checkout`
   and `Presentation`). It pre-dates this task, is outside a module-local certification, and would
   change a certified module's namespaces if "repaired" here — recorded, not touched.
2. `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` /
   `Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative` pin the
   repository-global Host-root recovery checkpoint (`currentHostCheckpoint`, the `Current Grid work
   checkpoint (CURRENT` wording, and a stale 16-module certified list). This module-local task must
   not displace the Host root checkpoint, so those facts stay red exactly as they were at `87101cb4`.

Neither family involves Inventory and neither was weakened.

## SoT / manifest changes in this wave

- `docs/architecture/tmar-module-structure-manifests.json` — the Inventory record moved from
  `preCertModules` into the certified `modules` array with `structureCertified: true` and the AMSC-001
  `certificationNote`. Every allowlist, forbidden-root and forbidden-folder value was carried over
  verbatim; exactly one Inventory entry remains and no unrelated record changed.
- `docs/architecture/tmar-current-state.json` — the `inventoryModuleAmsc001W3` record was added
  (state `INVENTORY_AMSC_001_CERTIFIED`, verdict `COMPLETE_REFERENCE_PATTERN`, `structureCertified: true`,
  `INTERNAL_ONLY` / `NOT_APPLICABLE` / `INTERNAL_USE_CASE_BOUNDARIES`, zero endpoint-reachable requests,
  zero foreign coupling, `microserviceExtractable: true`, `automaticNextImplementationTask: NONE`), the
  W0 `PENDING_W0_COMMIT` placeholder was reconciled to `c6917553`, and
  `structureLock.certifiedModules` now carries `Inventory` exactly once. The repository-global Host root
  checkpoint (`lastAcceptedTask`, `nextTask`, `workflowStop`, `automaticNextImplementationTask`) is
  untouched.
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` — module-local Inventory AMSC checkpoint with the
  accepted lineage and the `USER_REVIEW_INVENTORY_AMSC_001_W3` stop gate; the authoritative current
  region before the explicit historical boundary is preserved.
- `src/backend/Host/Tooba.Host.Tests/Architecture/InventoryModuleAmsc001W3CertGuardTests.cs` — new
  durable certification lock (5 facts).
- `src/backend/Host/Tooba.Host.Tests/Architecture/InventoryModuleAmsc001W2StructureGuardTests.cs` —
  the manifest assertion was reconciled from `preCertModules`/`structureCertified: false` to the
  certified array (a strengthening, no structural assertion weakened).
- `src/backend/Host/Tooba.Host.Tests/Architecture/TmarCompleteReferenceStructureGateTests.cs` —
  stale certified-module lists reconciled + the Inventory module-scoped allowlist/namespace fact added.
- `docs/architecture/evidence/TB-TMAR-INVENTORY-AMSC-001-W3/` — this certification, the SoT/manifest
  patch scripts, the code and namespace audits and the guard-run logs.

## Verification

- `dotnet build src/backend/Tooba.slnx` → **0 errors** (no new warnings introduced by the W3 guard).
- Focused Inventory suite (`Tooba.Inventory.Tests` + `InventoryModuleAmsc001W2StructureGuardTests` +
  `InventoryModuleAmsc001W3CertGuardTests` + `ErrorCatalogUniqueCodeGuardTests`) → green.
- `TmarCompleteReferenceStructureGateTests` — the two module-list facts and the new Inventory fact are
  green; the only remaining red is the pre-existing `Tooba.Catalog.Contracts/Cart` namespace deviation
  recorded above.
- Full Host suite vs the W3 starting HEAD `87101cb4`: identical failure set by test name, **zero new
  failures, zero guard weakened**.

## Behavior preservation

| Surface | State |
|---|---|
| Routes / methods / response DTO shape | `UNCHANGED` (no Inventory route exists) |
| Status codes and stable `inventory.*` code values | `UNCHANGED` (29 codes) |
| Reservation hold/commit/promote/release/consume semantics | `UNCHANGED` |
| Order supply mode matrix and `FOR UPDATE SKIP LOCKED` reclaim | `UNCHANGED` |
| Schema, table, columns, keys, migration identity | `UNCHANGED` (0 migration files touched) |
| DI lifetimes / outbox registration | `UNCHANGED` |
| Host composition seams | `UNCHANGED` |

## Handoff

`Structure-State = CERTIFIED`. No next implementation task is authorized:
`automaticNextImplementationTask = NONE`, stop gate `USER_REVIEW_INVENTORY_AMSC_001_W3`.
