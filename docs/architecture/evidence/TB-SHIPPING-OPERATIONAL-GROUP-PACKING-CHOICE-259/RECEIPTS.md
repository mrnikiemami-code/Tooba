# TB-259 — RECEIPTS

    Task          : TB-SHIPPING-OPERATIONAL-GROUP-PACKING-CHOICE-259
    Base-SHA      : fbf24b6aec52a62960227d57a40f3bbe45879e04 (issued expected base)
    Code-SHA      : 4c71a668e
    Evidence-SHA  : this evidence commit (named by the ledger commit that follows it; that commit is HEAD)
    Runtime       : FE http://127.0.0.1:3000 -> Host http://127.0.0.1:5088
    Database      : ONE canonical tooba_alpha @ 127.0.0.1:5432 (SingleStore / store-alpha)

## Preflight

1. `git log -1` → HEAD `436c067a0`, `git rev-parse origin/main` → `436c067a0` (HEAD == origin/main,
   0 divergent commits); the issue/review/doc commits are legitimate descendants of the expected base
   `fbf24b6aec`. No fetch/force/reset/rebase/stash/amend was used.

## Focused relational proof — real `tooba_alpha`

2. `TB259_CONNECTION="Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha"
   dotnet test src/backend/Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj -c Release
   --filter "FullyQualifiedName~MarketplaceOperationalPackingChoice259Tests"` → **Passed 7 / Failed 0**
   (A common, B separate, C conflict, C2 partial-recovery ×2 shapes, C3 two contexts, D authority).
   Recorded fixture ids in `tb259-relational.txt`. Everything except the platform access reader is real
   (production domain/application paths, real Fulfillment directory/bridge, real occurrence store).

## Retained regressions

3. `TB258_CONNECTION=<tooba_alpha> dotnet test ...Tooba.Fulfillment.Tests
   --filter "FullyQualifiedName~PostDispatchException|FullyQualifiedName~AutomaticTracking"`
   → **Passed 40, Failed 0** (258 non-delivery/return/resend + 257 automatic tracking retained).
4. `TB258_CONNECTION=<tooba_alpha> dotnet test ...Tooba.Order.Tests
   --filter "FullyQualifiedName~AdminOrderPostDispatchReplayTests"` → **Passed 9, Failed 0**.
5. `dotnet test ...Tooba.Order.Tests --filter
   "FullyQualifiedName~OrderAdminOperationsArchitectureGuardTests.Application_ops_has_no_central_ExecuteAsync_dispatcher"`
   → **Passed 1, Failed 0** (the extracted plan runner is `RunReviewedPlanAsync`, not a central dispatcher).
6. Whole Order module vs the expected base worktree (`D:\temp\tb259-base` @ `fbf24b6aec`), trx identity diff:
   base `Failed 96 / Passed 613 / Total 709`, current `Failed 96 / Passed 620 / Total 716`,
   only-in-current **0**, only-in-base **0** → identical pre-existing failure identities
   (`tb259-order-suite-parity.txt`). Identical-failure claim, NOT a green claim; no baseline was edited.

## Frontend

7. `npx tsc --noEmit` → **0**.
8. `npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts`
   → **tests 45 / pass 45 / fail 0** (41 retained + 5 TB-259; `tb259-mount.txt`).
9. `npm run build` → success, BUILD_ID `UZ3uUgVwd1cDuJ1wXVeJA`.
10. FE restarted (PID **43948**): `/fa` **200**; current
    `/_next/static/UZ3uUgVwd1cDuJ1wXVeJA/_buildManifest.js` **200**; previous build
    `rEs1YLE7thpJEsRLwtJ3H` now **404**. Served chunk `/_next/static/chunks/9127-3e442ffa2d287eb7.js`
    **200**, 258 632 bytes, contains «مرسوله‌های جدا», `packingMode`, the
    `i.getItem(s)===…` read-back and «درخواست ارسال نشد».

## Backend deploy

11. `dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o artifacts/publish-primary`
    → success (0 errors). Published `Tooba.Order.Application.dll` carries `packingMode`/`separate`.
12. Host restarted (PID **24396**), `GET /health` → **200** `{"status":"ok"}`; same SingleStore /
    `store-alpha` / `tooba_alpha` installation; no edition/connection/port/appsettings change.
13. Read-only canonical review route `/fa/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf` → **200**;
    nothing about that order was written.
14. Schema: **no migration file added and none applied**; the packing choice lives in the existing
    occurrence envelope JSON (`consolidated_package_id` was already nullable). Nothing re-applied.

## Small follow-ups inside this task

15. Removed one unused Contracts constant (`WorkflowPackingModeUnavailable`) that had no consumer, and
    re-ran steps 2, 4, 5 and 6 on the final code (same results; the parity diff above is the final run).
16. Added the missing mounted case for the new durability boundary
    («TB-259 mounted: an envelope that cannot be retained refuses the NEW send and sends nothing»), then
    re-ran steps 7–9 on the final tree.
17. `AdminOrderPostDispatchReplayTests`' occurrence-store test double gained the new
    `RecordCompletedSeparateAsync` member; no assertion or scenario changed.

## Zero unintended mutations

No raw business SQL, manual DDL, reset, reseed, backfill or deletion was issued. Every relational fixture is
task-labelled (`TB259-*` / `TB259-WH / Tehran` recipient and origin metadata) inside the SAME canonical
database. Real user orders, carts, parcels, payments and services were not written; the one canonical review
order was read over HTTP only. DataProtection keys and the CapturedPreview state were not touched
(local ON / checked-in OFF preserved). 255 stays parked; Tipax stays deferred — no provider call, no
credential use, no fabricated waybill.
