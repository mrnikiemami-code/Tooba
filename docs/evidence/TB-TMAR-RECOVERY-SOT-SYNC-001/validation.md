# Validation — TB-TMAR-RECOVERY-SOT-SYNC-001

## 1. Scope of validation

Recovery/documentation guards only. No solution-wide build, no module build beyond the existing
recovery-guard test project, no production behavior validation.

## 2. Focused test command

```text
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj \
  --filter "FullyQualifiedName~TmarDurableGuardTests|FullyQualifiedName~TmarCompleteReferenceStructureGateTests" \
  -v minimal
```

Result:

```text
Passed!  - Failed: 0, Passed: 9, Skipped: 0, Total: 9, Duration: 368 ms - Tooba.Host.Tests.dll (net8.0)
```

Covered facts (5 pre-existing durable-recovery guards + `TmarCompleteReferenceStructureGateTests` +
the repaired `Recovery_current_state_is_fresh_and_machine_readable` + the new
`Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative` … 9 tests in total).

## 3. JSON parse / schema validation

```text
Get-Content docs/architecture/tmar-current-state.json -Raw | ConvertFrom-Json
→ JSON_PARSE_OK
```

`tmar-current-state.json` parses as valid JSON and every asserted property resolves.

## 4. Uniqueness / unambiguity checks

```text
count '"workflowStop": "USER_REVIEW_AFTER_RECOVERY_SOT_SYNC_001"' in tmar-current-state.json  → 3
  (top level, currentHostEvacuation, recoverySotSync001 — no historical block)
count '"nextTask": "USER_REVIEW_AFTER_RECOVERY_SOT_SYNC_001"'     in tmar-current-state.json  → 2
  (top level, recoverySotSync001)
count '^Next TMAR task (CURRENT'                                  in TOOBA-TMAR-MASTER-RECOVERY.md → 1
count 'ADDRESSBOOK-ARCH-COMPLETE-002-STRUCTURE-001'               in tmar-current-state.json  → 4
  (addressBookLineage + historicalHostFolderLineage contexts only; never a current pointer)
```

## 5. SHA existence / ancestry

```text
git rev-list --max-count=1 498c46bd36c1d72934e97b137625cb07de84272a  → 498c46bd…  (ok)
git rev-list --max-count=1 736f23d34acb4f3989144f27675d1768fc7a65a9  → 736f23d3…  (ok)
git rev-list --max-count=1 c63f6ebb818e7e35a548c5b3e20eb18a25a244c8  → c63f6ebb…  (ok)
git rev-list --max-count=1 7d8ea21155109def56866eee2acdab2067fb457b  → 7d8ea211…  (ok)
git merge-base --is-ancestor <each sha> HEAD                          → exit 0 for all four
```

Asserted inside the new durable guard as well, so it stays provable on future runs.

## 6. Pre-existing baseline note (honest)

Before this task, `Recovery_current_state_is_fresh_and_machine_readable` failed at accepted `HEAD`
`736f23d3` because it still pinned the AddressBook-era `lastAcceptedTask` and a 9-module
`structureLock.certifiedModules` set (the certified set has since grown to 10 modules with `Content`).
This is a pre-existing stale-guard debt, not a regression introduced by this task. It is repaired here
because this task owns recovery consistency.

## 7. Production-code change check

```text
git status --short
 M docs/ai/TOOBA-RECOVERY-CONTEXT.md
 M docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
 M docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
 M docs/architecture/tmar-current-state.json
 M src/backend/Host/Tooba.Host.Tests/TmarDurableGuardTests.cs
?? docs/ai/tasks/TB-TMAR-RECOVERY-SOT-SYNC-001.task.md
?? docs/evidence/TB-TMAR-RECOVERY-SOT-SYNC-001/
```

- `src/backend/**` production: **unchanged**.
- `src/frontend/**`: **untouched**.
- Project files, package references, routes, schemas, migrations: **unchanged**.
- Module manifest `docs/architecture/tmar-module-structure-manifests.json`: **unchanged** (it was already
  correct and consistent with `structureLock.certifiedModules`; the only stale artefact was the guard's
  own assertion, repaired in the test file).
- The only `src/**` change is the durable recovery guard test file (recovery-consistency test, explicitly
  permitted).

## 8. Result

```text
Focused-Validation-State = PASS
Production-Code-Change-State = ZERO
```
