# Stale / duplicate physical copy — AccessControl (W0)

`Physical-Copy-State = CLEAN`

| Check | Result |
| --- | --- |
| Leftover path after a previous move | none inside the module |
| Two live paths for one responsibility | none |
| Solution entry pointing at a deleted path | none — all five `.csproj` paths exist on disk |
| Duplicate type name inside the module | two intentional same-name pairs across boundaries: `AccessOwnerScope` (Contracts boundary DTO vs Application-internal value) and `AccessControlErrorCodes` (Contracts) vs the `access.*` literals inside `AccessControlDirectory.cs` |
| `.tmp-baseline` snapshot | **outside** the module; a stale sibling git worktree (see below) |

## Duplicate-name pair 1 — `AccessOwnerScope`

- `Tooba.AccessControl.Contracts.Access.AccessOwnerScope` — stable module-boundary DTO consumed by
  other modules through `IAccessControlEffectiveAccessReader`.
- `Tooba.AccessControl.Application.Models.AccessOwnerScope` — Application-internal value used by the
  directory port.

This is a legitimate boundary/internal split, explicitly mapped by
`Infrastructure/Adapters/AccessControlEffectiveAccessReader.cs`. Not a duplicate copy.

## Duplicate-name pair 2 — stable error codes

`Contracts/Errors/AccessControlErrorCodes.cs` is the single owned vocabulary. The 20 raw string
literals in `Infrastructure/Directories/AccessControlDirectory.cs` are a second *source* of the same
codes without being a second *type*. This is cohesion defect F2, repaired in W1.

## Sibling `.tmp-baseline` worktree (environment observation, not a module copy)

`git worktree list` at this HEAD:

```text
D:/Users/User/source/repos/SarvNewVer               eea29fb8 [main]
C:/Users/User/AppData/Local/Temp/order-base-wt      04c22c37 (detached HEAD)
D:/Users/User/source/repos/_order_head_baseline     04c22c37 (detached HEAD)
D:/Users/User/source/repos/SarvBaselineR3           a98aca6e (detached HEAD)
D:/Users/User/source/repos/SarvNewVer/.tmp-baseline 87a22d7c (detached HEAD)
D:/Users/User/source/repos/SarvNewVer-Codex         144d1828 [codex/p09]
```

- `.tmp-baseline` HEAD `87a22d7c` is an **ancestor of `main`**; `git log <tmp> --not HEAD` is empty,
  so the worktree contains no unique commit and destroys no work if pruned.
- `.gitignore` line 37 (`.tmp-*`) hides it from `git status` but
  `TmarSourceSizeGuard.ScanHandWrittenSources` walks the filesystem and its excluded-directory set
  contains `tmp` and `.tmp`, **not** `.tmp-baseline`.
- Consequence: the guard reports the snapshot's older files as new oversized files and reports the
  snapshot as a second source of `Infrastructure → foreign Application` edges. This is exactly the
  condition already recorded in `docs/architecture/tmar-current-state.json` (~line 2492) and in
  `docs/evidence/TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001/validation.md`.

This is **not** an AccessControl defect and not a repository commit defect. It is a local
environment artefact. It is recorded so that the AccessControl waves do not misattribute the failure
to their own changes, and so the resolution is explicit:

- either prune the stale worktree registration (`git worktree remove .tmp-baseline` / `git worktree prune`),
- or extend `TmarSourceSizeGuard`'s excluded-directory set to ignore `.tmp-*` worktrees.

Both are out of scope for this module AMSC run and are reported to the Architect as a separate
environment/recovery decision.
