# TB-TMAR-TAX-AMSC-001-W3 — Focused validation

Environment: `D:\Users\User\source\repos\SarvNewVer`, .NET 8, `net8.0` test targets.

## 1. Solution build

```
cd src/backend
dotnet build Tooba.slnx -v q --nologo
```

Result: **`0 Error(s)`** (213 pre-existing analyzer warnings, unchanged).

## 2. Module behavior tests

```
dotnet test Modules\Tax\Tooba.Tax.Tests\Tooba.Tax.Tests.csproj --nologo -v q
```

Result: **`Passed! - Failed: 0, Passed: 11, Skipped: 0, Total: 11`**

| Suite | Facts |
| --- | --- |
| `Architecture/TaxArchitectureGuardTests` | durable boundary guards |
| `Contracts/TaxCalculationShapeTests` | contracts shape |
| `Domain/TaxRuleInvariantTests` | domain invariants + typed codes |
| `Infrastructure/TaxDbContextOwnershipTests` | schema/DbContext ownership |

## 3. Tax AMSC wave guards (W1 + W2 + W3)

```
dotnet test Host\Tooba.Host.Tests\Tooba.Host.Tests.csproj `
  --filter "FullyQualifiedName~TaxModuleAmsc001" --nologo -v q
```

Result: **`Failed: 0, Passed: 24, Skipped: 0, Total: 24`**

| Guard | Facts | Result |
| --- | --- | --- |
| `TaxModuleAmsc001W1MigrateGuardTests` | 8 | pass |
| `TaxModuleAmsc001W2StructureGuardTests` (repointed to the promoted `modules[]` entry) | 8 | pass |
| `TaxModuleAmsc001W3CertGuardTests` (new) | 8 | pass |

## 4. Focused certification-adjacent guards

```
dotnet test Host\Tooba.Host.Tests\Tooba.Host.Tests.csproj `
  --filter "FullyQualifiedName~TaxModuleAmsc001|FullyQualifiedName~TaxArchitectureGuardTests|FullyQualifiedName~TaxFoundationTests|FullyQualifiedName~PricingModuleAmsc001W3R3|FullyQualifiedName~ContractsW4CharacterizationTests" `
  --nologo -v q
```

Result: **`Failed: 0, Passed: 36, Skipped: 1, Total: 37`**

This covers the repointed W2 structure guard, the extended repository-global certified-module pin lists in
`PricingModuleAmsc001W3R3CertGuardTests`, the Host `TaxFoundationTests` consumer and the
`ContractsW4CharacterizationTests` boundary characterization.

## 5. Repository-global structure gate (pinned certified-module lists extended with `Tax`)

```
dotnet test Host\Tooba.Host.Tests\Tooba.Host.Tests.csproj `
  --filter "FullyQualifiedName~TmarCompleteReferenceStructureGateTests" --nologo -v q
```

Result: **1 failure, pre-existing and unrelated to Tax.**

```
Tooba.Host.Tests.Architecture.TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment
  Assert.Equal() Failure: Strings differ
  Expected: "Tooba.Catalog.Contracts.Cart"
  Actual:   "Tooba.Catalog.Contracts"
```

The failing project is `Tooba.Catalog.Contracts/Cart` (with `Tooba.Cart.Contracts/{Checkout,Presentation}`
on the same path) — a **pre-existing, unrelated** deviation from the exact path↔namespace rule, explicitly
documented in the gate source itself as a boundary-Contracts aggregation that predates this work and is out
of scope for a module-local certification. It is **identical at the W2 starting head** and Tax's own
project set has no such deviation. Not repaired here (it belongs to another module's surface).

The other two facts in this gate class (`Manifest_is_well_formed_and_only_declared_modules_are_certified`,
`Uncertified_modules_are_explicitly_not_claimed`) **pass** with the promoted state.

## 6. Full Host suite — set-difference against the W2 baseline

Baseline was produced in an isolated `git worktree` pinned at the W2 commit `eff3cf5b`:

```
git worktree add --detach D:\Users\User\source\repos\_w3base eff3cf5b
cd D:\Users\User\source\repos\_w3base\src\backend
dotnet test Host\Tooba.Host.Tests\Tooba.Host.Tests.csproj --nologo -v q --logger "trx;LogFileName=w2base.trx"
```

| Run | Passed | Skipped | Failed | Total |
| --- | --- | --- | --- | --- |
| W2 baseline (`eff3cf5b`) | 2296 | 130 | **71** | 2497 |
| W3 working tree | 2304 | 130 | **71** | 2505 |

(The W3 tree carries 8 more tests: the new `TaxModuleAmsc001W3CertGuardTests`.)

Set-difference of the distinct failing test ids (`failing-baseline-w2.txt` vs `failing-current.txt`):

```
=== NEW (in current, not baseline) ===
(empty)
=== FIXED (in baseline, not current) ===
(empty)
```

**NEW FAILURES = 0.** All 71 failures are byte-identical at both heads and pre-exist this task
(`HostAdminAmcW30PwTaxonomyGuardTests` Host/Admin count drift, `ProductWorkspaceModuleAmsc001W3R2CertGuardTests`,
`TmarCompleteReferenceStructureGateTests` Catalog/Cart namespace deviation, `FulfillmentFoundationTests`,
`TmarDurableGuardTests` global recovery pins, `TmarSourceSizeAndInfraAppTests` baseline inventory,
`ProductHistoryTests`, `StoryModuleAmsc001W3R1RecoveryGuardTests`, …).

### One transient failure found and removed during the wave

The first full-suite run reported **72** failures. The extra failure was
`StoryModuleAmsc001W3R1RecoveryGuardTests.Story_amsc_lineage_is_fully_reconciled_with_real_shas`, whose
assertion is `Assert.DoesNotContain("\"PENDING_W3_COMMIT\"", <tmar-current-state.json>)` — a
**repository-global** rule that the Story W3-R1 recovery wave installed to forbid self-referential
placeholder commits anywhere in the SoT. The W3 SoT block had been written with
`commit: "PENDING_W3_COMMIT"` / `commitFull: "PENDING_W3_COMMIT"`. This is a genuine rule violation
(certification cannot lock a self-referential placeholder), not a false positive, so it was **repaired**
rather than excused: the placeholder fields were removed and replaced by
`commitState: "REPORTED_VIA_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_PLACEHOLDER"` — the same convention the
Support and Story certification waves use (the cert commit SHA is reported through the Bridge Result and
reconciled by the Architect separately). After the repair the full suite is back to exactly the 71-failure
baseline with zero new failures.

## 7. Guard-integrity statement

- Guards added: `TaxModuleAmsc001W3CertGuardTests` (8 facts).
- Guards repointed (not weakened): `TaxModuleAmsc001W2StructureGuardTests` now reads the promoted
  `modules[]` entry instead of `preCertModules` — the same structure record with the same allowlists, and
  the pre-cert duplicate is additionally asserted absent.
- Guards extended (not weakened): `TmarCompleteReferenceStructureGateTests` (modules pin, uncertified pin,
  certifiedModules pin) and `PricingModuleAmsc001W3R3CertGuardTests` (both pinned arrays) gained `Tax`,
  exactly as the Returns/Promotion/Support certification precedents did.
- Guards deleted: **none**. Assertions relaxed: **none**. Baselines widened: **none**.
- No open-ended test/repair loop: one focused failure (`Assert.DoesNotContain` placeholder rule) had one
  clear deterministic local cause and was repaired once; only the affected validation was re-run.

## 8. Machine checks

- `tmar-module-structure-manifests.json` parses; `modules[]` = 31 entries with `Tax` exactly once and
  `structureCertified: true`; `preCertModules` = `[]`.
- `tmar-current-state.json` parses; `structureLock.certifiedModules` = 31 entries with `Tax` exactly once;
  no `PENDING_W3_COMMIT` / `PENDING_W3R1_COMMIT` placeholder remains.
- `Tooba.slnx` `/Modules/Tax/` folder holds exactly 5 projects.
- Tax production: 0 route mappings, 0 `ISender`/MediatR, 0 foreign module layer edges, 0 foreign
  `DbContext`, 0 `TypeForwardedTo`, 0 path↔namespace mismatches, 0 files over the 800 LOC ceiling,
  1 migration.
