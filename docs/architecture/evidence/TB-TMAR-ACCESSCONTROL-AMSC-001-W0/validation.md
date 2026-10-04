# Validation evidence — AccessControl (W0)

Baseline captured at HEAD `eea29fb8` (== `origin/main`), before any AMSC change.

## Focused builds

| Command | Result |
| --- | --- |
| `dotnet build src/backend/Tooba.slnx` | PASS — 0 errors (solution already green at this HEAD) |

## Focused tests

| Filter | Result |
| --- | --- |
| `FullyQualifiedName~AccessControl` (Tooba.Host.Tests) | **Failed: 1, Passed: 29, Skipped: 4, Total: 34** |

The single failure is:

```text
Tooba.Host.Tests.Architecture.AccessControlModuleAmcW1SolutionGuardTests
  .AccessControl_projects_are_grouped_under_Modules_AccessControl_including_Endpoints
  System.ArgumentOutOfRangeException : Index was out of range ...
  at ... AccessControlModuleAmcW1SolutionGuardTests.cs:line 20
```

Diagnosis: **stale guard parser** (finding F4), not a solution-grouping defect. The canonical
`/Modules/AccessControl/` folder and all five project entries are present and correct. The guard
assumes a flat `<Folder Name="/Modules/">` root folder that no longer exists.

| Filter | Result |
| --- | --- |
| `FullyQualifiedName~AccessControlFoundationTests` \| `~AccessControlRuntimeScopeTests` \| `~AccessControlValidatorTests` | PASS — 12 passed, 4 skipped (skips are Docker/Testcontainers gated) |
| `FullyQualifiedName~AccessControlValidatorTests` | PASS — inventory 6 required + 14 no-validator-required, DI resolution, namespace/folder checks |

## Repository-wide guard observations (environment, not AccessControl)

| Filter | Result |
| --- | --- |
| `FullyQualifiedName~TmarSourceSizeAndInfraAppTests` | **Failed: 3, Passed: 3** |

All three failures are attributable to the stale sibling `.tmp-baseline` git worktree being scanned:

- `Hand_written_source_size_does_not_expand_beyond_baseline` — every reported `NEW_OVERSIZED_FILE`
  path is prefixed `.tmp-baseline/`, plus `BASELINE_ENTRY_MISSING_FILE` for the pre-move
  `Modules/AccessControl/.../AccessControlDirectory.cs` path (now under `Directories/`).
- `Infrastructure_to_foreign_Application_edges_do_not_expand_beyond_baseline` — extra edges are
  `Tooba.Promotion.Infrastructure -> Tooba.Inventory.Application`, `-> Tooba.Party.Application`,
  `-> Tooba.Pricing.Application`; none is an AccessControl edge.
- `Source_size_inventory_evidence_exists_and_matches_scan_count` — inventory `7244` vs scan `1845`,
  a pre-existing repository-wide evidence mismatch unrelated to AccessControl.

This condition is already documented in `docs/architecture/tmar-current-state.json` (~line 2492) as
reproduced at a clean HEAD. It is reported to the Architect as an environment/recovery decision and
is explicitly excluded from the AccessControl certification surface.

## Post-change re-scan checklist (to run in W1 and W3)

- `rg '"access\.[a-z_.]+"' Modules/AccessControl` outside `Contracts/Errors` and `.resx` → expect 0.
- `rg 'using Tooba.AccessControl.Domain' Endpoints` → expect 0.
- `rg 'using Tooba.AccessControl.Infrastructure' Endpoints` → expect 0.
- `rg 'Results\.(Json|BadRequest|Problem)' Endpoints` → expect 0.
- `rg 'using Tooba\.<Foreign>\.(Application|Infrastructure|Domain)'` → expect 0.
- `rg 'catch \(AccessControlException' Endpoints` → expect 0.
- `rg 'Console\.WriteLine|Debug\.WriteLine'` → expect 0.
- path↔namespace exactness over all production `.cs` → expect 0 mismatches.
