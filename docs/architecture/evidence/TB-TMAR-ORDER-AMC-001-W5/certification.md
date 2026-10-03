# TB-TMAR-ORDER-AMC-001-W5 — Certification verdict

## Verdict

```text
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
```

The single W4 blocker (expected-failure classification by parsing `InvalidOperationException.Message`) is
**removed at the source**, not exempted. See `typed-fault-migration.md` for the exact diff surface.

## Structure gate (inherited from W4, re-verified on the current disk)

| Gate | State |
|---|---|
| `Structure-State` | `READY_FOR_CERTIFY` |
| `Folder-Granularity-State` | `PROFESSIONAL_SHALLOW` |
| `Solution-Explorer-State` | `CANONICAL` |
| `Path-Namespace-State` | `EXACT` |
| `Physical-Copy-State` | `CLEAN` |
| `Root-Allowlist-State` | `ENFORCED` |
| `File-Cohesion-State` | `COHESIVE` |
| Host final closure | `PRESERVED` |

W5 introduced **zero** new production files, **zero** deletions, **zero** renames, and **zero** `.slnx`
changes in `Modules/Order`. Measured production `.cs` count is unchanged at **324**, so the W4 structure
evidence remains current for this exact surface (structure regression cannot be hidden by a same-shape edit).

## W4 blocker closure

| Blocker | State | Evidence |
|---|---|---|
| B1 storefront message classification | `CLOSED` | `StorefrontOrderResult` catches `ContractOperationException`, maps `ex.Code`; message parser deleted |
| B2 admin fulfillment message classification | `CLOSED` | `AdminOrderFulfillmentOperations` catches `ContractOperationException`, maps `ex.Code`; message-text classification removed, typed `shipping_service.*` code classification preserved |
| B3 untyped expected-fault throws | `CLOSED` | 8 Order production files migrated to `ContractOperationException(code)` |

## Certified-PASS surface (verified this wave)

| Check | State | Evidence |
|---|---|---|
| Expected-fault carrier | `TYPED_CONTRACT_OPERATION_FAULT` | `ContractOperationException.Code` |
| Message classification | `ZERO` | production scan of `Modules/Order` → 0 hits |
| Observable stable codes | `PRESERVED` | every migrated flow keeps its exact machine code |
| Cross-module boundary | `LEGAL_CONTRACTS_ONLY` | 0 foreign csproj refs to Order Application/Infrastructure/Domain |
| Cross-module joins | `NONE` | no foreign DbSet / SQL join |
| Endpoint ownership | `MODULE_ENDPOINTS` | 43 route registrations in `Tooba.Order.Endpoints`; Host owns none |
| CQRS / MediatR | `COMPLIANT` | `ISender` + `IRequestHandler<,>`, MediatR 12.5.0 |
| Validator coverage | `EXHAUSTIVE` | manifest classifies every endpoint-reachable request |
| API result mapping | `CANONICAL` | `ApiResponseFactory` only; `Results.Json` = 0 |
| Localization / catalog | `CANONICAL` | `OrderErrorResourceSet` + `OrderErrors(.fa).resx`; no duplicate descriptor owner |
| Logging | `CANONICAL` | `ILogger<T>` only; 0 `Console`/`Debug` writes |
| Correlation / tracing | `CANONICAL` | no parallel correlation, no `StartActivity` in Application/Endpoints |
| Persistence ownership | `CORRECT` | one `OrderDbContext`; 21 migrations untouched |
| Schema / migration safety | `UNCHANGED` | no migration added/regenerated |
| Host authority | `ALLOWED_COMPOSITION_ROOT` only | W5 touched zero Host production files |
| Host final-closure regression | `NONE` | `HOST_ROOT_FINAL_CERTIFIED` preserved |
| Closed-folder / sink regression | `NONE` | no file moved into a closed folder |
| Error descriptors | `UNREGISTERED_CODES` = 0 | all Order-owned codes resolve |
| Microservice extractability | `SEAMLESS` | no foreign Application/Infrastructure/Domain coupling; typed contract faults at the boundary |

## Focused validation

```text
dotnet build src/backend/Tooba.slnx
Build succeeded. 0 Error(s)

dotnet test src/backend/Modules/Order/Tooba.Order.Tests
Passed! - Failed: 0, Passed: 139, Skipped: 0, Total: 139

dotnet test src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests
Passed! - Failed: 0, Passed: 65, Skipped: 0, Total: 65

dotnet test src/backend/Modules/Payment/Tooba.Payment.Tests
Passed! - Failed: 0, Passed: 95, Skipped: 0, Total: 95
```

### Order-scoped Host guards (rebuilt, then run)

```text
dotnet test src/backend/Host/Tooba.Host.Tests --filter "<Order checkout/fulfillment/supply/reservation guards>"
Passed! - Failed: 0, Passed: 112, Skipped: 1, Total: 113
```

### No-regression proof (whole Host suite, baseline vs W5)

The W5 changes were stashed and the whole `Tooba.Host.Tests` suite was run at the clean `bea74d71` baseline,
then re-run with W5 applied and the failing-test sets diffed.

| Metric | Baseline (`bea74d71`, clean tree) | With W5 |
|---|---|---|
| Failed | 116 | 93 |
| Passed | 1704 | 1727 |
| Total | 1950 | 1950 |

```text
NEW failures introduced by W5 = 0
Failures fixed by W5        = 23
```

No assertion was weakened, no baseline widened, and no guard suppressed to reach this state.

### Pre-existing failures (out of scope, not caused by this task)

The remaining 93 Host failures are identical to the clean baseline set. Representative causes, all
independent of Order AMC-001:

- `HostOrderReverseAuditGuardTests` — R7 Host inventory JSON never reconciled after `86bfb80a` / `c08c4afa`
  relocated Host files to `Host/Composition/*`; `Host Order inventory drift` (Host-only, no Order file).
- ~14 `HostAdminAmc*` guards — `Host_Admin_count_15_...` counts drift from unrelated Admin waves.
- `ContentCategoryTreeRulesTests` / `ConsolidatedPackageTests` / `FulfillmentLineQuantity*` — stale
  `InvalidOperationException` assertions against the already-shipped `51a3eed7` typed-fault migration.
- `CorrelationRuntimeTests` — DI composition cannot resolve `ICartInventoryHoldPort` (unrelated to Order).
- `TmarSourceSizeAndInfraAppTests` / `TmarFoundationTests` — source-size and foreign-edge baselines from
  other modules (Promotion→Identity, Party) outside the Order surface.

## Manifest and SoT

- `tmar-module-structure-manifests.json`: Order is a single certified entry with `structureCertified: true`,
  `lockVersion: ARCH-COMPLETE-002`, and exactly the five certified production projects
  (`Tooba.Order.Domain`, `Tooba.Order.Contracts`, `Tooba.Order.Application`, `Tooba.Order.Endpoints`,
  `Tooba.Order.Infrastructure`) with root allowlists equal to the real on-disk top-level `.cs` files.
  This exactness was reconciled and made durable in `TB-TMAR-ORDER-AMC-001-W5-R1`; see
  `docs/architecture/evidence/TB-TMAR-ORDER-AMC-001-W5-R1/manifest-reconciliation.md`.
- `tmar-current-state.json` → `orderAmc001`: promoted to
  `W5_CERTIFY_COMPLETE_REFERENCE_PATTERN`, `completeReferencePattern: true`,
  `certificationVerdict: COMPLETE_REFERENCE_PATTERN_STRUCTURE_CERTIFIED`, `certificationBlockers: []`,
  `messageClassificationState: ZERO`, `manifestPromoted: true`, `evidenceW5` recorded.

## Residual non-blocking debt

None attributable to Order. The pre-existing Host/other-module failures listed above are outside the bounded
Order AMC-001 scope and were not repaired (and were not made worse).
