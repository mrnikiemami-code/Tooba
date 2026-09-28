# TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R1 — Closure

## Status

`CERTIFIED` — landed on `main`.

## What R1 did

1. Reconciled `chatgpt/host-root-boundaries-001` (`70d13759`) onto the latest
   `main` (`a2e01cf5`, containing the completed Authorization AMC) through a normal
   merge with zero conflicts, creating integration branch
   `cursor/host-root-global-boundaries-001-r1`.
2. Resolved the certification blocker by isolating the Order registration out of the
   pre-existing dirty surface:
   - `OrderModule.cs` → **byte-identical to `main`**
   - `Tooba.Order.Infrastructure.csproj` → **byte-identical to `main`**
   - new `Order/ReservationCycle/ReservationCycleRegistration.cs` owns the DI
     composition, consumed once from Host composition.
3. Repaired the build break surfaced by the Authorization AMC's removal of the
   Settlement global-using shim (`ToobaModuleComposition.cs` now names
   `Tooba.Settlement.Infrastructure.DependencyInjection` explicitly).
4. Repointed stale path assertions to the new module owners and added 8 behavioral
   characterization facts for the Payment-owned hold policy.

## Blocker state

```text
ISOLATED_FROM_TOUCHED_SURFACE_AND_CERTIFIABLE
```

## Evidence

- `reconcile.md`
- `blocker-analysis.md`
- `migration-repair.md`
- `validation.md`
- `certification.md`

## Stop

`workflowStop = USER_REVIEW_HOST_ROOT_GLOBAL_BOUNDARIES_001_R1`

Awaiting Architect `ACCEPT` / `REPAIR` / `BLOCK`.
