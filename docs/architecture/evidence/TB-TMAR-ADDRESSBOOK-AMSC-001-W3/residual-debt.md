# TB-TMAR-ADDRESSBOOK-AMSC-001-W3 — residual-debt

`BlockingResidualDebt = ZERO`.

Every item below is a recorded, non-blocking observation. None is a certification prerequisite violation,
and none is converted from a violation into "debt" to justify PASS.

## R1 — unreachable outbox guard throws `InvalidOperationException`

`Infrastructure/Outbox/AddressBookOutboxRegistration.cs:25`:

```csharp
public string GetEventTypeName(Type integrationEventType) =>
    throw new InvalidOperationException("AddressBook integration event is not registered.");
```

- The module registers **no** integration event, so this guard is unreachable.
- It carries **no user-facing text** and never crosses the HTTP boundary.
- It is not one of the W0 F2 sites (which were the Domain/Infrastructure user-facing failures, all closed
  in W1).
- W3's guard therefore scans for typed-fault residue only in `CustomerAddress.cs` and
  `AddressBookDirectory.cs` (the two files that produce user-facing faults) and asserts both use
  `SemanticException` with zero `InvalidOperationException`.

Disposition: non-blocking watch.

## R2 — `Application/Validators/` root kept instead of `Validation/`

`AddressBook` uses `Application/Validators/AddressBookFluentRules.cs` (which declares both
`AddressBookValidationCodes` and `AddressBookFluentRules`); `Offer` and `AccessControl` use
`Application/Validation/`. Both names are accepted by the physical-structure guard.

Disposition: cosmetic consistency watch (W0 F5). W2 deliberately did **not** rename it — a rename with no
cohesion benefit would churn the guard and the namespace for nothing.

## R3 — Persian demo-data literals in the development seed

`Infrastructure/Adapters/AddressBookDevelopmentSeed.cs` contains Persian recipient names, cities, streets
and labels (e.g. `"گیرندهٔ نمایشی توبا"`, `"تهران"`). These are **developer seed values**, not user-facing
error text, and are not rendered through the error/localization pipeline.

Disposition: non-blocking. The module has **zero** hard-coded user-facing error strings.

## R4 — shared size-guard noise from a stray untracked worktree

`TmarSourceSizeAndInfraAppTests` scans an untracked sibling `.tmp-baseline/` working copy, producing
`NEW_OVERSIZED_FILE` entries for files that belong to that copy, not to this repository, and inflating the
scan count. This is pre-existing and repo-wide.

Disposition: non-blocking, out of scope (recorded in the SoT as a pre-existing drift item).

## R5 — pre-existing repo-wide gate drifts

| Drift | Guard |
| --- | --- |
| `Catalog` present in the manifest but missing from SoT `structureLock.certifiedModules` (and vice versa in the uncertified assertion) | `TmarCompleteReferenceStructureGateTests` ×3, `TmarDurableGuardTests` ×1 |
| `Tooba.Catalog.Contracts.Cart` vs `Tooba.Catalog.Contracts` namespace expectation | `TmarCompleteReferenceStructureGateTests` |
| `Tooba.Promotion.Infrastructure -> Tooba.Inventory/Party/Pricing.Application` foreign edges | `TmarSourceSizeAndInfraAppTests` |
| stale `.tmp-baseline` scan | `TmarSourceSizeAndInfraAppTests` ×2 |

Reproduced identically at the W0 baseline `3256fc7a` and after every wave. **AddressBook appears in none of
them.** W3 did not "fix" the `Catalog`/SoT divergence, because doing so would be an unrelated change outside
this task's scope (rule 14: do not modify unrelated files).

Disposition: non-blocking, out of scope.

## R6 — no `Tooba.AddressBook.Tests` project

Module behaviour and architecture guards live in `Tooba.Host.Tests`. Creating a module test project is a
new-project scope expansion (same disposition as the AccessControl AMSC run, W0 F5).

Disposition: non-blocking, recorded.

## R7 — untracked foreign artifact in the working tree

`docs/architecture/evidence/TB-TMAR-ORDER-AMC-001-W5-R1/worker-result.txt` is untracked and unrelated to
AddressBook (a leftover from the Order run). It was **not** committed by any AMSC wave.

Disposition: non-blocking, recorded.

## Explicitly NOT residual debt

The following were **blockers** in the W0 Analyze and are now fully closed — they were not downgraded:

| W0 blocker | Closed in | Evidence |
| --- | --- | --- |
| F1 `API-Result-Pattern-State = AD_HOC` (6 handlers returning raw DTO/`Unit`) | W1 | `TB-TMAR-ADDRESSBOOK-AMSC-001-W1/verification.md`, this run's `api-result-error-mapping.md` |
| F2 no stable codes for real business failures; 14 raw `InvalidOperationException` sites with hard-coded Persian text; ownership failures surfacing as HTTP 500 | W1 | `TB-TMAR-ADDRESSBOOK-AMSC-001-W1/verification.md`, this run's `localization-catalog.md` |
| F3/F4 `TECHNICAL_AXIS_FIRST` Application tree with 11 single-file use-case leaf folders | W2 | `TB-TMAR-ADDRESSBOOK-AMSC-001-W2/folder-granularity.md`, this run's `physical-tree.md` |
| F8 stale `AddressBookValidatorCoverageGuardTests` `preCertModules` assertion (RED at HEAD) | W1 | this run's `validator-coverage.md`, `durable-guards.md` |
