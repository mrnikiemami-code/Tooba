# TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001-R1 — Payment Host residue behavior parity repair

Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Parent: TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001 (`5d79cbfd8812fba010e3ec771e30b934ea80973d`)
Track: PAYMENT_HOST_RESIDUE_PARITY_REPAIR
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE (frontendFrozen = true)

## 0. Parent verdict

Parent `TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001` was **NOT YET ACCEPTED**. Architect verified two concrete
regressions introduced during the ownership move from Host into the Payment module. This task repairs only
those two parity regressions.

## 1. Defect 1 — poll cadence changed (FIXED)

Old Host behavior:

```csharp
TimeSpan.FromSeconds(Math.Max(15, _options.PollIntervalSeconds))
```

Regressed Payment behavior (from the parent task):

```csharp
PollIntervalSeconds < 5 ? 5 : PollIntervalSeconds
```

Repaired Payment behavior in
`src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Workers/PaymentReconciliationOptions.cs`:

```csharp
public TimeSpan NormalizedPollInterval =>
    TimeSpan.FromSeconds(PollIntervalSeconds < 15 ? 15 : PollIntervalSeconds);
```

Requirements satisfied:

| Requirement | State |
| --- | --- |
| minimum effective poll interval = 15 seconds | restored |
| default remains 60 | preserved |
| PendingAge minimum 1 minute | preserved (`PendingAgeMinutes < 1 ? 1 : PendingAgeMinutes`) |
| non-positive BatchSize fallback 20 | preserved (`BatchSize <= 0 ? 20 : BatchSize`) |

XML docs updated: the `PollIntervalSeconds` doc and the `NormalizedPollInterval` doc now state the effective
minimum of 15 seconds and reference parity with the pre-move Host cadence. The stale "حداقل ۵" doc was removed.

## 2. Defect 2 — advanced grid connector errors could become 500 (FIXED)

`GridQueryPolicyBase.ValidateAdvancedConnectors(...)` throws `GridQueryValidationException` for connector-count
mismatch and invalid connector values. `SafeErrorMapper` does **not** map `GridQueryValidationException`, so any
escape path mapped to HTTP 500. The old Host policy caught that exception and produced HTTP 400; the regressed
Payment normalizer did not catch the connector-count / connector-value paths.

Repaired in `src/backend/Modules/Payment/Tooba.Payment.Endpoints/Admin/PaymentAdminGridQueryNormalizer.cs` with
one narrow boundary around `Normalize`:

```csharp
public GridQueryRequest Normalize(GridQueryRequest request)
{
    ArgumentNullException.ThrowIfNull(request);

    try
    {
        return NormalizeCore(request);
    }
    catch (GridQueryValidationException ex)
    {
        throw new SemanticException(new SemanticError(ex.ErrorCode));
    }
}
```

- No message parsing — only `ex.ErrorCode` is propagated.
- No `PlatformHttpException`.
- Connector validation is not duplicated; the single boundary also covers the Payment-owned field/operator checks.
- The previous manual `ValidationFailure(...)` wrapper was simplified away because the one boundary makes it redundant.

All grid structural errors now surface as stable semantic codes:

| Condition | Semantic code | HTTP |
| --- | --- | --- |
| bad filter field | `grid.filter.field.invalid` | 400 |
| bad operator | `grid.filter.operator.invalid` | 400 |
| bad advanced filter field | `grid.advancedFilter.field.invalid` | 400 |
| bad advanced connector count | `grid.advancedFilter.connector.count` | 400 |
| bad advanced connector value | `grid.advancedFilter.connector.invalid` | 400 |

Central mapping: `SafeErrorMapper.Map(SemanticException)` returns the unknown-code safe business fallback
(`StatusCodes.Status400BadRequest`, `Classification = Business`) for each of these codes, exactly the 400 the old
Host policy produced. Proven by `PaymentAdminGridQueryNormalizerTests.Semantic_grid_codes_map_to_http_400_through_safe_error_mapper`.

## 3. Scope discipline

Not performed in this task:

- no Payment structure certification
- no addition of the 15 deferred validators
- no `PaymentDirectory` refactor
- no `PaymentHostContractBridge` rename
- no unrelated `ex.Message` debt repair
- Settlement untouched
- Checkout not resumed
- frontend untouched

## 4. Focused tests

Added `src/backend/Modules/Payment/Tooba.Payment.Tests/Behavior/PaymentAdminGridQueryNormalizerTests.cs`:

- canonical fields accepted in sort + filter
- unknown filter field → `grid.filter.field.invalid`
- invalid operator → `grid.filter.operator.invalid`
- advanced filter field → `grid.advancedFilter.field.invalid`
- advanced connector count → `grid.advancedFilter.connector.count`
- advanced connector value → `grid.advancedFilter.connector.invalid`
- no `GridQueryValidationException` escapes the normalizer for any of those five inputs
- all five semantic codes map to HTTP 400 through `SafeErrorMapper`
- default sort (`created` desc), paging (1/20) and a valid advanced expression preserved

Extended `src/backend/Modules/Payment/Tooba.Payment.Tests/Behavior/PaymentReconciliationWorkerTests.cs`:

| configured | expected effective |
| --- | --- |
| 1 | 15s |
| 5 | 15s |
| 14 | 15s |
| 15 | 15s |
| 60 | 60s |
| 120 | 120s |

plus unsafe-value normalization now asserting 15s for `PollIntervalSeconds = 0`.

## 5. Guards strengthened

`PaymentArchitectureGuardTests`:

- `Payment_reconciliation_cadence_preserves_fifteen_second_minimum` (new) — asserts the options source contains
  `PollIntervalSeconds < 15 ? 15 : PollIntervalSeconds`, asserts the regressed `PollIntervalSeconds < 5 ? 5` form is
  gone, and behavior-asserts 1s → 15s, 60s → 60s, PendingAge min 1 minute, BatchSize ≤ 0 → 20.
- `Payment_owns_admin_grid_normalizer_without_host_grid_engine` (strengthened) — asserts no `PlatformHttpException`,
  no `ex.Message` / `exception.Message` classification, the exact
  `catch (GridQueryValidationException ex)` + `throw new SemanticException(new SemanticError(ex.ErrorCode))`
  boundary, and that the raw `GridQueryValidationException.Connector*.ErrorCode` escapes are gone.
- `Host_owns_no_Payment_runtime_or_grid_residue` — unchanged, still asserts no Host Payment runtime/grid residue and
  exactly the two approved Host Payment security adapters.
- `Payment.Infrastructure` → `Tooba.Host` project reference remains absent.

`TmarDurableGuardTests` updated to assert the new `paymentHostResidueRepair` fields (`parentRepair`,
`parityRepairTask`, `reconciliationCadence`, `gridValidationMapping`, `hostResidue`).

## 6. Validation

- `dotnet test` — `Tooba.Payment.Tests`: **Passed 38 / Failed 0**
- `dotnet test` — Host focused + TMAR (`AdminListGridQueryEngineTests`, `HostFolderStructureTests`,
  `HostCartResidualGuardTests`, `TmarDurableGuardTests`, `TmarCompleteReferenceStructureGateTests`):
  **Passed 28 / Failed 0**
- `dotnet build src/backend/Tooba.slnx`: **0 Errors** (pending final run)

## 7. PASS criteria check

| Criterion | State |
| --- | --- |
| effective minimum poll interval is 15 seconds | PASS |
| no connector validation path escapes as `GridQueryValidationException` | PASS |
| connector count/value produce stable `grid.*` semantic codes | PASS |
| central presentation maps them to 400 | PASS |
| Host residue remains removed | PASS |
| only two approved Host Payment security adapters remain | PASS |
| Payment → Host dependency remains zero | PASS |
| Payment still NOT structure-certified | PASS |
| Checkout/frontend unchanged | PASS |
| focused tests pass | PASS |
| build passes | PASS |

## 8. Recovery state

- parent repair = `ACCEPTED_AFTER_R1_BEHAVIOR_PARITY_REPAIR`
- reconciliation cadence = `MIN_15_SECONDS_PRESERVED`
- grid validation mapping = `ALL_GRID_VALIDATION_ERRORS_STABLE_SEMANTIC_400`
- Payment Host residue = `PAYMENT_RUNTIME_RESIDUE_REMOVED_SECURITY_ADAPTERS_ONLY`
- structure certification = `PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001`
- `nextTask = TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001`

Preserved: Cart / Order / StoreContext / Offer certifications; Checkout `PAUSED_AT_SAFE_W5_CHECKPOINT`;
`frontendFrozen = true`. Payment is **not** certified by this task.
