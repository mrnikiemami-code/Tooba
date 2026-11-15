# TB-270-R1 — RECEIPTS (read-only seam; no data, no deployment, no provider)

Worker: `tooba-worker-01` · Channel `tooba-main` · Task row `f2e354c7-1c7c-4ac7-86d9-07c6bf309c2d` ·
Task `TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R1`.

## 1. Task payload and base

```
GET /api/tasks?channelId=tooba-main
  -> f2e354c7-1c7c-4ac7-86d9-07c6bf309c2d | TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R1 | Claimed
     claimedAtUtc 2026-11-15T05:20:44.5503287Z
     (parent 270 row dc286be7-…bf459b already Completed)

git rev-parse HEAD                       -> 1b1f8b68f0a1fa0278293f2ca91998d477943e2b
git rev-parse origin/main                -> 1b1f8b68f0a1fa0278293f2ca91998d477943e2b
git merge-base --is-ancestor 1b1f8b68f… HEAD        -> 0 (ancestor)
git merge-base --is-ancestor 1b1f8b68f… origin/main -> 0 (ancestor)
```

Expected-Base-SHA `1b1f8b68f0a1fa0278293f2ca91998d477943e2b` == actual work base == local `origin/main`. No
RECOVERY_CONFLICT and no incoming delta to integrate.

## 2. Changed paths

```
 M src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/DependencyInjection/ShippingModule.cs   (+6 lines: using + one scoped registration)
?? src/backend/Modules/Shipping/Tooba.Shipping.Contracts/Scheduling/ShippingAttemptSchedulingContracts.cs      (288 lines)
?? src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Scheduling/ShippingAttemptSchedulingLookup.cs     (319 lines)
?? src/backend/Modules/Shipping/Tooba.Shipping.Tests/Scheduling/ShippingAttemptSchedulingLookupTests.cs         (797 lines)
```

No other tracked file changed (`git diff --stat HEAD` shows only the DI file). The eight pre-existing untracked
`scripts/tb266r1-*.mjs` scratch files are not part of this change set and were not committed.

## 3. Focused suite (run once, after the final change)

```
dotnet test Modules\Shipping\Tooba.Shipping.Tests\Tooba.Shipping.Tests.csproj -c Release \
  --filter "FullyQualifiedName~ShippingAttemptSchedulingLookupTests"
  -> Passed!  Failed: 0, Passed: 7, Skipped: 0, Total: 7, Duration: 578 ms
```

Two fixture corrections were needed before that green run, both disclosed rather than silently smoothed over:

1. first run `6 failed / 1 passed` with `SemanticException: shipping.authoring.region_group_not_found` raised inside the
   fixture builder. Root cause: the fixture claimed fixed group ids at `ShippingAuthoringDraft.Create`, but group identity
   is **aggregate-owned** (`SetInitialGroups` always mints ids); the later `ReplaceGroupRateRows` therefore targeted ids
   that never existed. Fix: create with `null` ids, read the two assigned ids back from `RegionGroups`, and carry them on
   the fixture. This is a test-fixture defect, not a product defect — the product path never accepts caller-asserted group
   ids on create.
2. then `1 failed / 6 passed`: the "two rows with one id" corruption duplicated `RateRows[0]` (ordered by id) instead of the
   requested row, so the lookup correctly answered `CapturedInputs` for the non-duplicated row. Fix: duplicate the exact
   requested row. No product code changed for either correction.

## 4. Build

```
dotnet build Tooba.slnx -c Release
  -> 0 Error(s), 379 Warning(s)   (warnings pre-existing: NU1900 offline vulnerability feed, xUnit analyzers, CS8625)
```

## 5. Whole-project observations (not re-run loops; disclosed so nobody mistakes them for this change)

Running the whole Shipping test project once (`-c Release`, no filter) gave `Passed 771 / Failed 88 / Total 859`. Every
sampled failure is an environment-gated disposable-Postgres matrix that refuses to run:

```
System.InvalidOperationException : TOOBA_TB249R6_POSTGRES must point at the disposable
127.0.0.1:15433/shipping_tb249r6 instance; the TB249R6 relational matrix refuses to run without it.
System.InvalidOperationException : TOOBA_TB214_POSTGRES must point at the disposable
127.0.0.1:15432/shipping_tb214 instance; the capture storage facts refuse to run (or to report success) without it.
```

A `FullyQualifiedName~Composition` subset gave `20 passed / 1 failed`:
`ShippingErrorCatalogAndResourcesTests.Production_composition_registers_the_catalog_and_resources_once` asserts that the
error catalog contains `shipping.carrier.shipment.intent_conflict`. That catalog file is untouched by this change (the only
tracked modification is the one DI registration line and no error code was added), so the failure is pre-existing and
unrelated; it is reported, not fixed, and not masked.

No disposable database, Docker instance, port 15432/15433 or `TOOBA_*_POSTGRES` environment variable was created or set.
ONE canonical `tooba_alpha` @5432 SingleStore remains the only database; nothing was written to it.

## 6. Environment

Read-only and untouched by this round: Host 5088, FE 3000 (still the TB-269 build `BUILD_ID UmAnnlHYHCf-Sa5-dTMkm`), no
migration, no DDL, no business SQL, no fixture, no settings edit, no stock write, no cleanup, no provider/waybill call, no
money movement, no FE rebuild, no service restart. The user's orders, the Returned-source/Delivered-resend trial history
and the shipping draft were not read for mutation and not written.

## 7. Git

Expected base == actual base == `1b1f8b68f0a1fa0278293f2ca91998d477943e2b`. CODE_SHA and EVIDENCE_SHA are pinned in
`result-contract.txt` after the commits; `HEAD == origin/main` parity is re-verified after each push. No force, amend,
reset, rebase or stash.
