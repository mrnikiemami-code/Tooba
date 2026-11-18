# TB-279-R2 — RECEIPTS

All receipts were captured on 2026-11-18 (UTC) in `D:\Users\User\source\repos\SarvNewVer`. No secret, key value,
connection string or token appears in any command or output below.

## Repository / claim

```
git merge-base --is-ancestor 6e3d1873f7f9926c2906ae4e26547a6c9ef97eeb HEAD   -> 0
git log --oneline -4
  3b1ce75d9 feat(returns): connect the reviewed operator/customer contracts and close the 279-R1 gaps (TB-279-R2)
  22a0ec148 chore(returns): untrack the accidental data-protection key ring and task scratch scripts (TB-279-R2 containment)
  ff69838cf docs(returns): issue TB-279-R2 connected closure and containment
  6e3d1873f docs(returns): record TB-279-R1 partial review and containment findings
git diff --stat 6e3d1873..3b1ce75d9 -- src  -> 43 files changed, 1391 insertions(+), 205 deletions(-)
```

A. Containment commit `22a0ec148` — index removals only, local files preserved; `git status --porcelain` immediately
after it was empty. Preservation copies were hash-compared under `D:\temp\tb279r2-containment` (see
`key-containment.md`); the key directory and the scratch scripts remain on disk and are ignored by exact scoped rules.

## Focused backend proof

```
dotnet test Modules\Returns\Tooba.Returns.Tests\Tooba.Returns.Tests.csproj -c Release --nologo
  Passed! - Failed: 0, Passed: 177, Skipped: 0, Total: 177

dotnet test Modules\Returns\Tooba.Returns.Tests\Tooba.Returns.Tests.csproj -c Release \
  --filter "FullyQualifiedName~ReturnsDecisionCorrection279DirectoryTests"
  Passed! - Failed: 0, Passed: 15, Skipped: 0, Total: 15

dotnet test Host\Tooba.Host.Tests\Tooba.Host.Tests.csproj -c Release \
  --filter "FullyQualifiedName~ReturnsModuleAmsc001|FullyQualifiedName~ReturnsClosure279RelationalTests"
  Passed! - Failed: 0, Passed: 37, Skipped: 0, Total: 37
```

The 15 directory facts drive the REAL `ReturnDirectory`/`ReturnEligibilityEvaluator` over stored rows. The three facts
added by R2 are:

- `R2_a_plan_portion_finer_than_the_owning_unit_is_refused_before_any_row` — `0.0001` on a 3-decimal line ⇒
  `return.resolution.portion_invalid`, `ResolutionCountAsync()==0`.
- `R2_a_handback_finer_than_the_owning_unit_and_an_unproven_carrier_are_refused` — `0.0001` ⇒
  `return.custody.quantity_invalid`; `carrier_delivered_back` ⇒ `return.custody.proof_kind_unavailable`;
  `CustodyCountAsync()==0`, gateway calls 0, inventory calls 0.
- `R2_a_handed_back_remainder_keeps_the_accepted_units_occupied` — delivered/requested/received 3, accepted 2,
  rejected-received 1, handback 0.5 ⇒ allocation sum **2.5**, eligibility remaining **0.5**, allocation rows 2
  (one live 2.5 + one released 0.5).

## Build

```
dotnet build Tooba.slnx -c Release --nologo -v q     -> exit 0 (52 s)
dotnet publish (Host, publish-primary)               -> exit 0, 0 errors
```

## Focused frontend proof

```
node --experimental-strip-types --test app/returns/return-api.test.ts app/returns/return-quantity.test.ts \
  app/returns/return-submission.test.ts app/returns/return-submission-279.test.ts
  tests 29 / pass 29 / fail 0

npx tsx --tsconfig tsconfig.mount-tests.json --test app/returns/return-request-modal-278.mount.test.ts
  tests 11 / pass 11 / fail 0

npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-return-workspace-279.mount.test.ts \
  app/admin/admin-return-workspace-277.mount.test.ts app/admin/admin-return-workspace-276.mount.test.ts
  tests 23 / pass 23 / fail 0

npx tsc --noEmit                                     -> TSC_EXIT=0
npm run build (next)                                 -> BUILD_ID IdLHLUerRPBB-OzXddPwk
```

Note: a bare `npx vitest run` over this repository fails at COLLECTION for every file (`Failed to parse source for
import analysis … jsx to preserve`); this repository's supported runners are the Node test runner and `tsx` with
`tsconfig.mount-tests.json`, which produced the results above. The failing sweep is reported, not hidden.

## Served state

```
GET http://127.0.0.1:5088/health        -> 200
GET http://127.0.0.1:5088/health/ready  -> 200
port 5088 owner PID                     -> 7808
artifacts\publish-primary\Tooba.Host.dll SHA256
  -> 7B8AAB852972C1CDCED3A12AA2A057700183FA16AE1A2FA89A60D209CF499977
port 3000 owner PID                     -> 9904
src/frontend/.next/BUILD_ID             -> IdLHLUerRPBB-OzXddPwk
GET http://127.0.0.1:3000/fa/admin/returns -> 200
```

## Schema / data

- `git show --name-only 3b1ce75d9 | grep -i migration` → empty: R2 adds **no** migration, DDL, backfill or seed.
- No reset/reseed/restore/cleanup, no live receipt/decision/resolution/custody/refund/provider/stock/wallet call was
  made by this task; all money/stock facts are controlled-port facts and the relational facts are rollback-only.

## Declared deviations

1. The production `ReturnDirectory.Allocations.cs` / `ReturnDirectory.Resolutions.cs` files were temporarily
   instrumented with a file log while diagnosing the occupancy fact; every instrumentation line was removed and the
   suites above were re-run after removal. `git status` shows only the test file changed.
2. The whole-repository `vitest` sweep was attempted once and failed at collection (see above); the supported runners
   were used for the reported proof.
3. The R2 counterexample suite was re-run four times while the diagnostic assertions were corrected; the final 15/15 and
   177/177 runs are the reported ones.
