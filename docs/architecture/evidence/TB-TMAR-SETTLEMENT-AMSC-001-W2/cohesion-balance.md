# File cohesion / god-file balance (skill §12)

`File-Cohesion-State = COHESIVE` (`OVERSIZED_ONLY` watch on one file, single responsibility)

## Largest production files (LOC, non-generated)

```text
695  Infrastructure/Directories/SettlementDirectory.cs
451  Tests/Architecture/SettlementArchitectureGuardTests.cs        (test, not production)
251  Domain/Aggregates/SettlementEntry.cs
217  Tests/Architecture/SettlementValidatorCoverageGuardTests.cs    (test, not production)
171  Application/Payouts/Ports/SettlementDirectoryPorts.cs
166  Infrastructure/Persistence/SettlementDbContext.cs
129  Infrastructure/Queries/AdminPayoutGridQueryEngine.cs
111  Contracts/Errors/SettlementErrorCodes.cs
108  Domain/Aggregates/PayoutRequest.cs
102  Tests/Validation/SettlementValidatorTests.cs                   (test, not production)
100  Application/Payouts/Queries/AdminPayoutGridQueryPolicy.cs
 94  Tests/Behavior/SettlementErrorAndGridTests.cs                  (test, not production)
```

## Assessment

| File | LOC | Responsibilities | State |
|---|---|---|---|
| `Infrastructure/Directories/SettlementDirectory.cs` | 695 | orchestration + persistence for the single payout/settlement use-case family; the use-case guard was **extracted** to `OpenSettlementUseCaseGuard.cs` in W1 | `OVERSIZED_ONLY` — single responsibility, **WATCH**, no structure blocker |
| `Domain/Aggregates/SettlementEntry.cs` | 251 | one aggregate + its invariants | `COHESIVE` |
| `Application/Payouts/Ports/SettlementDirectoryPorts.cs` | 171 | the directory port surface (one boundary) | `COHESIVE` |
| `Infrastructure/Persistence/SettlementDbContext.cs` | 166 | one DbContext + entity configuration | `COHESIVE` |
| `Infrastructure/Queries/AdminPayoutGridQueryEngine.cs` | 129 | one DB-native grid engine | `COHESIVE` |
| `Contracts/Errors/SettlementErrorCodes.cs` | 111 | one code catalogue (constants + reachability sets) | `COHESIVE` |

## Repairs performed upstream (W1, verified here)

| Defect | Before | After |
|---|---|---|
| Mixed Application bundle + boundary events | `Application/Ports/SettlementContracts.cs` (398 LOC, `MULTI_RESPONSIBILITY_COHESION_VIOLATION`) | split by responsibility into `Application/Payouts/Ports/{SettlementDirectoryPorts,SettlementReaderPorts,SettlementGatewayPorts,SettlementRestorePolicy,IAdminPayoutGridQuery}.cs` + `Contracts/Events/*` |
| Message-text fault classifier | `Application/Errors/SettlementExceptionMapper.cs` (97 LOC, non-canonical mechanism) | retired → `Application/Composition/SettlementOperation.cs` (typed seam) |
| Guard co-located in a 701 LOC directory | `Infrastructure/Directories/SettlementDirectory.cs` | `OpenSettlementUseCaseGuard.cs` extracted; directory 701 → 695 LOC |
| Mixed admin models + display labels | `Application/Ports/SettlementAdminModels.cs` | `Application/Payouts/Models/AdminPayoutModels.cs` + `Application/Payouts/Models/SettlementDisplayLabels.cs` |

## No `OVER_SPLIT`

No cosmetic split was introduced to game a size guard: the 5 files under `Payouts/Ports` each expose a
different port family (directory / reader / gateway / restore policy / grid query), the 3 command files
each declare one command + its handler, and the 8 query files each declare one query + its handler. No
file was split into meaningless fragments.

## No new god-file

The largest file added by the AMSC work is `Application/Payouts/Ports/SettlementDirectoryPorts.cs`
(171 LOC) — below every baseline ceiling and single-responsibility. No file mixes transport, request
models, persistence and business rules.

## Watch item (recorded honestly, non-blocking)

`Infrastructure/Directories/SettlementDirectory.cs` remains the module's largest production file at
695 LOC. It is a single-responsibility orchestrator (accrual / credit / debit / neutralize / restore /
payout lifecycle persistence for the `settlement` schema) with no mixed concern left after the W1
extraction. Skill §12 explicitly permits `OVERSIZED_ONLY` as a WATCH state without forcing a structure
FAIL. A future semantic split (e.g. separating the accrual orchestration from the payout lifecycle
persistence) is a candidate for a separate Architect-authorized task, **not** a W2 structure blocker and
**not** invented here (skill §12: do not invent parallel architecture).
